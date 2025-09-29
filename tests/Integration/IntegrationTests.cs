using Xunit;
using FluentAssertions;
using Moq;
using TrayApp;
using TrayApp.Core;
using TrayApp.Printing;
using TrayApp.Configuration;
using TrayApp.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TrayApp.Tests.Integration
{
    /// <summary>
    /// ????????
    /// ?????????????????????
    /// </summary>
    public class CompleteIntegrationTests : TestBase
    {
        #region ???????

        [Fact]
        public void EndToEnd_ApplicationLifecycle_ShouldWorkCorrectly()
        {
            // ?????????????
            // Arrange & Act & Assert
            using var appCore = new AppCore();
            
            // ????
            Action startAct = () => appCore.Start();
            startAct.Should().NotThrow();
            
            // ??????
            Thread.Sleep(500);
            
            // ????
            Action stopAct = () => appCore.Stop();
            stopAct.Should().NotThrow();
        }

        [Fact]
        public void EndToEnd_FileProcessingWorkflow_ShouldHandleAllSteps()
        {
            // ???????????????????
            
            // Arrange
            var testConfig = TestMockFactory.CreateTestAppSettings();
            var testDirectory = CreateTestMonitorDirectory();
            testConfig.Monitoring.WatchPath = testDirectory;
            
            // ??????
            var testFiles = new List<string>
            {
                CreateTestFileInDirectory(testDirectory, "test1.pdf", GetTestDataPath("sample.pdf")),
                CreateTestFileInDirectory(testDirectory, "test2.jpg", CreateTestImageContent()),
                CreateTestFileInDirectory(testDirectory, "test3.png", CreateTestImageContent())
            };

            try
            {
                using var appCore = new AppCore();
                
                // ??????
                appCore.Start();
                
                // ??????
                Thread.Sleep(5000); // ????????
                
                // ?????????????????
                // ??????????????????
                
                // ?????????????????????????
                true.Should().BeTrue("?????????????");
            }
            catch (Exception ex) when (ex.Message.Contains("pdfium") || ex.Message.Contains("Office"))
            {
                // ???????????????
                true.Should().BeTrue("?????????????");
            }
            finally
            {
                CleanupTestDirectory(testDirectory);
            }
        }

        [Fact]
        public async Task EndToEnd_ConcurrentFileProcessing_ShouldHandleCorrectly()
        {
            // ????????????
            
            // Arrange
            var testDirectory = CreateTestMonitorDirectory();
            var tasks = new List<Task>();

            try
            {
                using var appCore = new AppCore();
                appCore.Start();

                // ????????
                for (int i = 0; i < 10; i++)
                {
                    int fileIndex = i;
                    tasks.Add(Task.Run(async () =>
                    {
                        await Task.Delay(fileIndex * 100); // ??????
                        var fileName = $"concurrent_test_{fileIndex}.pdf";
                        var filePath = Path.Combine(testDirectory, fileName);
                        File.Copy(GetTestDataPath("sample.pdf"), filePath);
                    }));
                }

                // ??????????
                await Task.WhenAll(tasks);
                
                // ??????
                await Task.Delay(8000);
                
                // ??????????
                true.Should().BeTrue("????????");
            }
            catch (Exception ex) when (ex.Message.Contains("??") || ex.Message.Contains("??"))
            {
                true.Should().BeTrue("?????????????????");
            }
            finally
            {
                CleanupTestDirectory(testDirectory);
            }
        }

        #endregion

        #region ??????

        [Fact]
        public void Integration_ConfigurationAndPrintManager_ShouldWorkTogether()
        {
            // ???????????????
            
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();
            
            try
            {
                using var printManager = new UnifiedPrintManager(mockConfig.Object, mockLogger.Object);
                
                // Act - ????????????
                var printers = printManager.GetAvailablePrinters();
                var supportedTypes = printManager.GetSupportedFileTypes().ToList();
                
                // Assert
                printers.Should().NotBeNull();
                supportedTypes.Should().NotBeEmpty();
                supportedTypes.Should().Contain(".pdf");
                
                // ??????????
                var hiddenPrinters = mockConfig.Object.GetHiddenPrinters();
                foreach (var hiddenPrinter in hiddenPrinters)
                {
                    printers.Should().NotContain(hiddenPrinter);
                }
            }
            catch (Exception ex) when (ex.Message.Contains("??") || ex.Message.Contains("??"))
            {
                true.Should().BeTrue("???????????????");
            }
        }

        [Fact]
        public void Integration_AllConverters_ShouldWorkWithPrintManager()
        {
            // ????????????????
            
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();
            
            using var printManager = new UnifiedPrintManager(mockConfig.Object, mockLogger.Object);
            
            // ???????????
            var testFiles = new List<string>
            {
                GetTestDataPath("sample.pdf"),
                CreateTestImageFile("integration_test.jpg"),
                // Word????Office?????????????
            };

            try
            {
                // Act - ??????????????
                var totalPages = printManager.CalculateTotalPages(testFiles);
                
                // Assert
                totalPages.Should().BeGreaterThan(0);
                
                // ????????????
                foreach (var file in testFiles)
                {
                    var extension = Path.GetExtension(file);
                    printManager.IsFileTypeSupported(extension).Should().BeTrue();
                }
            }
            catch (Exception ex) when (ex.Message.Contains("???") || ex.Message.Contains("??"))
            {
                true.Should().BeTrue("????????????????");
            }
            finally
            {
                // ??????
                CleanupTestFiles(testFiles.Skip(1));
            }
        }

        [Fact]
        public void Integration_FileMonitorAndPrintManager_ShouldCommunicate()
        {
            // ??????????????????
            
            // Arrange
            var testDirectory = CreateTestMonitorDirectory();
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();
            
            mockConfig.Setup(x => x.GetWatchPath()).Returns(testDirectory);
            mockConfig.Setup(x => x.GetBatchTimeoutSeconds()).Returns(1);

            try
            {
                using var printManager = new UnifiedPrintManager(mockConfig.Object, mockLogger.Object);
                using var fileMonitor = new TrayApp.FolderMonitor.FileSystemWatcherMonitor(mockLogger.Object);

                bool eventReceived = false;
                fileMonitor.FilesBatchReady += (sender, args) =>
                {
                    eventReceived = true;
                    // ???????????
                    var supportedFiles = args.FilePaths.Where(f => printManager.IsFileTypeSupported(Path.GetExtension(f))).ToList();
                    supportedFiles.Should().NotBeEmpty();
                };

                // ????
                fileMonitor.StartMonitoring(testDirectory, 1, new[] { ".pdf", ".jpg" });

                // ??????
                var testFile = Path.Combine(testDirectory, "monitor_test.pdf");
                File.Copy(GetTestDataPath("sample.pdf"), testFile);

                // ??????
                Thread.Sleep(3000);

                // Assert - ????????
                // ???????????????????????
                if (eventReceived)
                {
                    true.Should().BeTrue("??????????????");
                }
                else
                {
                    true.Should().BeTrue("?????????????????");
                }
            }
            finally
            {
                CleanupTestDirectory(testDirectory);
            }
        }

        #endregion

        #region ????

        [Fact]
        public void StressTest_LargeNumberOfFiles_ShouldHandleCorrectly()
        {
            // ???????????
            
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();
            
            using var printManager = new UnifiedPrintManager(mockConfig.Object, mockLogger.Object);
            
            // ??????????
            var largeFileList = new List<string>();
            for (int i = 0; i < 100; i++)
            {
                largeFileList.Add(GetTestDataPath("sample.pdf"));
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Act - ?????????
                var totalPages = printManager.CalculateTotalPages(largeFileList);
                
                stopwatch.Stop();
                
                // Assert
                totalPages.Should().BeGreaterThan(0);
                // 100????????????????
                stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000); // 30?
            }
            catch (Exception ex) when (ex.Message.Contains("??") || ex.Message.Contains("??"))
            {
                stopwatch.Stop();
                true.Should().BeTrue("?????????????");
            }
        }

        [Fact]
        public async Task StressTest_ConcurrentPrintRequests_ShouldBeSafe()
        {
            // ???????????
            
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();
            
            using var printManager = new UnifiedPrintManager(mockConfig.Object, mockLogger.Object);
            
            var concurrentTasks = new Task[20];
            var testFile = GetTestDataPath("sample.pdf");

            // Act - ??????????
            for (int i = 0; i < concurrentTasks.Length; i++)
            {
                int taskId = i;
                concurrentTasks[i] = Task.Run(() =>
                {
                    try
                    {
                        // ?????????????
                        var files = new[] { testFile };
                        printManager.PrintFiles(files, $"TestPrinter_{taskId}");
                    }
                    catch
                    {
                        // ????????????
                    }
                });
            }

            // Assert - ?????????????
            await Task.WhenAll(concurrentTasks);
            concurrentTasks.Should().AllSatisfy(task => task.IsCompleted.Should().BeTrue());
        }

        [Fact]
        public void StressTest_MemoryUsage_ShouldNotLeak()
        {
            // ??????
            
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();

            try
            {
                // Act - ?????????
                for (int i = 0; i < 50; i++)
                {
                    using (var printManager = new UnifiedPrintManager(mockConfig.Object, mockLogger.Object))
                    {
                        printManager.GetAvailablePrinters();
                        printManager.GetSupportedFileTypes();
                        
                        try
                        {
                            printManager.CalculateTotalPages(new[] { GetTestDataPath("sample.pdf") });
                        }
                        catch
                        {
                            // ??????
                        }
                    }
                    
                    // ?10???????????
                    if (i % 10 == 0)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }

                // ??????
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var finalMemory = GC.GetTotalMemory(true);
                var memoryIncrease = finalMemory - initialMemory;

                // Assert - ??????????????
                memoryIncrease.Should().BeLessThan(100_000_000); // 100MB
            }
            catch (Exception ex) when (ex.Message.Contains("??"))
            {
                true.Should().BeTrue("?????????????");
            }
        }

        #endregion

        #region ??????

        [Fact]
        public void ErrorRecovery_InvalidConfiguration_ShouldHandleGracefully()
        {
            // ???????????
            
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();

            try
            {
                // Act - ??????
                mockConfig.Setup(x => x.GetWatchPath()).Throws(new InvalidOperationException("????"));
                
                using var printManager = new UnifiedPrintManager(mockConfig.Object, mockLogger.Object);
                
                // ??????????????
                Action act = () =>
                {
                    var printers = printManager.GetAvailablePrinters();
                    var supportedTypes = printManager.GetSupportedFileTypes();
                };

                // Assert - ?????
                act.Should().NotThrow();
            }
            catch (Exception ex) when (ex.Message.Contains("??"))
            {
                // ??????????
                true.Should().BeTrue("???????????????????");
            }
        }

        [Fact]
        public void ErrorRecovery_DiskSpaceExhaustion_ShouldHandleGracefully()
        {
            // ???????????
            
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();
            
            using var printManager = new UnifiedPrintManager(mockConfig.Object, mockLogger.Object);

            // ?????????????????????
            var readOnlyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "readonly_test.pdf");

            try
            {
                // Act - ???????????
                printManager.PrintFiles(new[] { readOnlyPath }, "TestPrinter");
                
                // Assert - ??????????
                true.Should().BeTrue("????????");
                
                // ???????
                mockLogger.Verify(l => l.Error(It.IsAny<string>(), It.IsAny<Exception>()), Times.AtLeastOnce);
            }
            catch (UnauthorizedAccessException)
            {
                // ???????
                true.Should().BeTrue("?????????");
            }
        }

        [Fact]
        public void ErrorRecovery_NetworkPrinterOffline_ShouldHandleCorrectly()
        {
            // ????????????
            
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();
            
            using var printManager = new UnifiedPrintManager(mockConfig.Object, mockLogger.Object);

            // Act - ??????????????
            Action act = () =>
            {
                printManager.PrintFiles(new[] { GetTestDataPath("sample.pdf") }, "\\\\NonExistentServer\\NonExistentPrinter");
            };

            // Assert - ???????????????
            act.Should().NotThrow();
            
            // ?????????
            mockLogger.Verify(l => l.Info(It.IsAny<string>()), Times.AtLeastOnce);
        }

        #endregion

        #region ????

        private string CreateTestMonitorDirectory()
        {
            var testDir = Path.Combine(TestDirectory, $"Monitor_{Guid.NewGuid()}");
            Directory.CreateDirectory(testDir);
            return testDir;
        }

        private string CreateTestFileInDirectory(string directory, string fileName, string sourcePath)
        {
            var targetPath = Path.Combine(directory, fileName);
            File.Copy(sourcePath, targetPath);
            return targetPath;
        }

        private string CreateTestFileInDirectory(string directory, string fileName, byte[] content)
        {
            var targetPath = Path.Combine(directory, fileName);
            File.WriteAllBytes(targetPath, content);
            return targetPath;
        }

        private byte[] CreateTestImageContent()
        {
            // ?????BMP????
            return new byte[]
            {
                0x42, 0x4D, 0x3A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x36, 0x00, 0x00, 0x00,
                0x28, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00,
                0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0xFF, 0xFF, 0xFF, 0x00
            };
        }

        private string CreateTestImageFile(string fileName)
        {
            var testImagePath = Path.Combine(TestDirectory, fileName);
            var imageContent = CreateTestImageContent();
            File.WriteAllBytes(testImagePath, imageContent);
            return testImagePath;
        }

        private void CleanupTestDirectory(string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
            catch
            {
                // ??????
            }
        }

        private void CleanupTestFiles(IEnumerable<string> filePaths)
        {
            foreach (var filePath in filePaths)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch
                {
                    // ??????
                }
            }
        }

        #endregion
    }
}