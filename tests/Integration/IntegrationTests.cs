using Xunit;
using FluentAssertions;
using TrayApp;
using TrayApp.Core;
using TrayApp.FolderMonitor;
using TrayApp.Printing;
using TrayApp.Configuration;
using TrayApp.FileOperations;
using TrayApp.TaskHistory;
using TrayApp.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TrayApp.Tests.Integration
{
    /// <summary>
    /// ??????
    /// ?????????????????
    /// </summary>
    [Trait("Category", "Integration")]
    public class IntegrationTests : TestBase
    {
        [Fact]
        public void FullWorkflow_ConfigurationAndServices_ShouldWorkTogether()
        {
            // ??????????????????????????

            // Arrange
            var tempConfigDir = CreateTestDirectory();
            var configFile = Path.Combine(tempConfigDir, "test_config.json");
            
            try
            {
                var logger = new FileLogger();
                var configService = new JsonConfigurationService(configFile, logger);
                var printManager = new UnifiedPrintManager(configService, logger);
                var fileOperator = new TimestampFileOperator(logger);
                var taskHistory = new TaskHistoryManager(configService, logger);

                // Act & Assert - ?????????????
                configService.Should().NotBeNull();
                printManager.Should().NotBeNull();
                fileOperator.Should().NotBeNull();
                taskHistory.Should().NotBeNull();

                // ??????????
                var settings = configService.GetSettings();
                settings.Should().NotBeNull();
                settings.Monitoring.Should().NotBeNull();

                // ????????????????
                var printers = printManager.GetAvailablePrinters();
                printers.Should().NotBeNull();

                // ????????
                var supportedTypes = printManager.GetSupportedFileTypes();
                supportedTypes.Should().NotBeNull();
                supportedTypes.Should().NotBeEmpty();

                // ????
                printManager.Dispose();
                taskHistory.Dispose();
            }
            finally
            {
                CleanupTestDirectory(tempConfigDir);
            }
        }

        [Fact]
        public void FileProcessingWorkflow_ShouldHandleCompleteProcess()
        {
            // ?????????????

            // Arrange
            var tempDir = CreateTestDirectory();
            var watchDir = Path.Combine(tempDir, "watch");
            var configFile = Path.Combine(tempDir, "config.json");
            Directory.CreateDirectory(watchDir);

            try
            {
                var logger = new FileLogger();
                var configService = new JsonConfigurationService(configFile, logger);
                var fileOperator = new TimestampFileOperator(logger);
                
                // ??????
                var testFiles = new List<string>
                {
                    Path.Combine(watchDir, "test1.txt"),
                    Path.Combine(watchDir, "test2.txt")
                };

                foreach (var file in testFiles)
                {
                    File.WriteAllText(file, "test content");
                }

                // Act - ????????
                var timestampDir = fileOperator.MoveFilesToTimestampDirectory(testFiles, tempDir);

                // Assert
                Directory.Exists(timestampDir).Should().BeTrue();
                foreach (var originalFile in testFiles)
                {
                    File.Exists(originalFile).Should().BeFalse(); // ????????
                    var fileName = Path.GetFileName(originalFile);
                    var movedFile = Path.Combine(timestampDir, fileName);
                    File.Exists(movedFile).Should().BeTrue(); // ??????????
                }
            }
            finally
            {
                CleanupTestDirectory(tempDir);
            }
        }

        [Fact]
        public void ConfigurationPersistence_ShouldMaintainDataIntegrity()
        {
            // ?????????????

            // Arrange
            var tempDir = CreateTestDirectory();
            var configFile = Path.Combine(tempDir, "persistence_test.json");

            try
            {
                var logger = new FileLogger();
                
                // ??????
                var originalConfig = new JsonConfigurationService(configFile, logger);
                var originalSettings = originalConfig.GetSettings();
                
                // ????
                originalSettings.Monitoring.WatchPath = @"C:\TestPath";
                originalSettings.Monitoring.BatchTimeoutSeconds = 10;
                originalSettings.PrintSettings.DefaultCopies = 3;
                originalConfig.SaveSettings(originalSettings);

                // Act - ??????
                var reloadedConfig = new JsonConfigurationService(configFile, logger);
                var reloadedSettings = reloadedConfig.GetSettings();

                // Assert - ????????
                reloadedSettings.Monitoring.WatchPath.Should().Be(@"C:\TestPath");
                reloadedSettings.Monitoring.BatchTimeoutSeconds.Should().Be(10);
                reloadedSettings.PrintSettings.DefaultCopies.Should().Be(3);
            }
            finally
            {
                CleanupTestDirectory(tempDir);
            }
        }

        [Fact]
        public void TaskHistoryWorkflow_ShouldRecordAndRetrieveTasks()
        {
            // ???????????????

            // Arrange
            var tempDir = CreateTestDirectory();
            var configFile = Path.Combine(tempDir, "task_history_test.json");

            try
            {
                var logger = new FileLogger();
                var configService = new JsonConfigurationService(configFile, logger);
                
                using var taskHistory = new TaskHistoryManager(configService, logger);

                // Act - ??????
                var task1 = new PrintTaskRecord
                {
                    Timestamp = DateTime.Now.AddMinutes(-10),
                    FileCount = 2,
                    TotalPages = 5,
                    PrinterName = "TestPrinter1",
                    FilePaths = new List<string> { "file1.pdf", "file2.pdf" }
                };

                var task2 = new PrintTaskRecord
                {
                    Timestamp = DateTime.Now.AddMinutes(-5),
                    FileCount = 1,
                    TotalPages = 3,
                    PrinterName = "TestPrinter2",
                    FilePaths = new List<string> { "file3.docx" }
                };

                taskHistory.AddTaskRecord(task1);
                taskHistory.AddTaskRecord(task2);

                // Assert - ??????
                var recentTasks = taskHistory.GetRecentTasks(5);
                recentTasks.Should().HaveCount(2);
                
                // ???????????
                recentTasks[0].Timestamp.Should().BeAfter(recentTasks[1].Timestamp);
                recentTasks[0].PrinterName.Should().Be("TestPrinter2");
                recentTasks[1].PrinterName.Should().Be("TestPrinter1");
            }
            finally
            {
                CleanupTestDirectory(tempDir);
            }
        }

        [Fact]
        public void PrinterManagement_ShouldRespectConfiguration()
        {
            // ?????????????????

            // Arrange
            var tempDir = CreateTestDirectory();
            var configFile = Path.Combine(tempDir, "printer_config_test.json");

            try
            {
                var logger = new FileLogger();
                var configService = new JsonConfigurationService(configFile, logger);
                
                // ????????????
                var settings = configService.GetSettings();
                settings.PrinterManagement.HiddenPrinters.Add("TestHiddenPrinter");
                configService.SaveSettings(settings);

                using var printManager = new UnifiedPrintManager(configService, logger);

                // Act
                var availablePrinters = printManager.GetAvailablePrinters();

                // Assert
                availablePrinters.Should().NotContain("TestHiddenPrinter");
            }
            finally
            {
                CleanupTestDirectory(tempDir);
            }
        }

        [Fact]
        public void ErrorRecovery_ServiceFailures_ShouldBeHandledGracefully()
        {
            // ??????????????

            // Arrange
            var tempDir = CreateTestDirectory();
            var invalidConfigFile = Path.Combine(tempDir, "invalid_config.json");
            
            // ???????????
            File.WriteAllText(invalidConfigFile, "invalid json content {{{");

            try
            {
                var logger = new FileLogger();

                // Act & Assert - ??????????????????
                Action act = () =>
                {
                    var configService = new JsonConfigurationService(invalidConfigFile, logger);
                    var settings = configService.GetSettings(); // ????????
                    settings.Should().NotBeNull();
                };
                
                act.Should().NotThrow();
            }
            finally
            {
                CleanupTestDirectory(tempDir);
            }
        }

        [Fact]
        public void ConcurrentOperations_ShouldBeSafe()
        {
            // ??????????
            var tempDir = CreateTestDirectory();
            var configFile = Path.Combine(tempDir, "concurrent_test.json");

            try
            {
                var logger = new FileLogger();
                var configService = new JsonConfigurationService(configFile, logger);
                
                using var taskHistory = new TaskHistoryManager(configService, logger);
                var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)); // 15???

                // Act - ????????
                var tasks = new Task[5];
                for (int i = 0; i < tasks.Length; i++)
                {
                    int taskId = i;
                    tasks[i] = Task.Run(async () =>
                    {
                        try
                        {
                            for (int j = 0; j < 10 && !cts.Token.IsCancellationRequested; j++)
                            {
                                var record = new PrintTaskRecord
                                {
                                    Timestamp = DateTime.Now,
                                    FileCount = 1,
                                    TotalPages = 1,
                                    PrinterName = $"Printer{taskId}",
                                    FilePaths = new List<string> { $"file{taskId}_{j}.pdf" }
                                };
                                
                                taskHistory.AddTaskRecord(record);
                                
                                // ????????????
                                await Task.Delay(10, cts.Token);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // ????????
                        }
                        catch (Exception ex) 
                        { 
                            exceptions.Add(ex); 
                        }
                    }, cts.Token);
                }

                // ??????????????
                try
                {
                    Task.WaitAll(tasks, TimeSpan.FromSeconds(20));
                }
                catch (AggregateException)
                {
                    // ????????
                }

                // Assert - ????????
                var unexpectedExceptions = exceptions.Where(ex => !(ex is OperationCanceledException)).ToList();
                unexpectedExceptions.Should().BeEmpty();
                
                // ????????????
                var recentTasks = taskHistory.GetRecentTasks(100);
                recentTasks.Should().NotBeEmpty();
            }
            finally
            {
                CleanupTestDirectory(tempDir);
            }
        }

        [Fact]
        public void MemoryUsage_ExtendedOperations_ShouldNotLeak()
        {
            // ??????

            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            var tempDir = CreateTestDirectory();
            var configFile = Path.Combine(tempDir, "memory_test.json");

            try
            {
                // Act - ??????
                for (int iteration = 0; iteration < 10; iteration++)
                {
                    var logger = new FileLogger();
                    var configService = new JsonConfigurationService(configFile, logger);
                    
                    using (var printManager = new UnifiedPrintManager(configService, logger))
                    using (var taskHistory = new TaskHistoryManager(configService, logger))
                    {
                        // ??????
                        var printers = printManager.GetAvailablePrinters();
                        var supportedTypes = printManager.GetSupportedFileTypes();
                        
                        for (int i = 0; i < 10; i++)
                        {
                            var record = new PrintTaskRecord
                            {
                                Timestamp = DateTime.Now,
                                FileCount = 1,
                                TotalPages = 1,
                                PrinterName = "TestPrinter",
                                FilePaths = new List<string> { $"test{i}.pdf" }
                            };
                            taskHistory.AddTaskRecord(record);
                        }
                        
                        var tasks = taskHistory.GetRecentTasks(5);
                    }
                }

                // ??????
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var finalMemory = GC.GetTotalMemory(true);

                // Assert - ????????????
                var memoryIncrease = finalMemory - initialMemory;
                memoryIncrease.Should().BeLessThan(50_000_000); // 50MB
            }
            finally
            {
                CleanupTestDirectory(tempDir);
            }
        }

        [Fact]
        public void FileSystem_DirectoryPermissions_ShouldBeHandled()
        {
            // ??????????

            // Arrange
            var logger = new FileLogger();
            var fileOperator = new TimestampFileOperator(logger);

            // Act & Assert - ?????????????????
            var nonExistentBase = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid()}");
            
            try
            {
                Action act = () =>
                {
                    var result = fileOperator.MoveFilesToTimestampDirectory(new string[0], nonExistentBase);
                    result.Should().NotBeNullOrEmpty();
                };
                
                act.Should().NotThrow();
            }
            finally
            {
                CleanupTestDirectory(nonExistentBase);
            }
        }

        /// <summary>
        /// ??????
        /// </summary>
        private string CreateTestDirectory()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"IntegrationTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);
            return tempDir;
        }

        /// <summary>
        /// ??????
        /// </summary>
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
    }
}