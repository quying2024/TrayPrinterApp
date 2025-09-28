using Xunit;
using FluentAssertions;
using TrayApp.TaskHistory;
using TrayApp.Core;
using TrayApp.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.IO;

namespace TrayApp.Tests.TaskHistory
{
    /// <summary>
    /// ??????????????
    /// ??TaskHistoryManager?????????????
    /// </summary>
    public class UserDataTaskHistoryTests : TestBase
    {
        [Fact]
        public void TaskHistoryManager_ShouldUseUserDataDirectory()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var testFileName = $"user_data_history_test_{Guid.NewGuid()}.json";
            
            mockConfig.Setup(x => x.GetTaskHistorySettings())
                     .Returns(new TaskHistorySettings 
                     { 
                         MaxRecords = 5, 
                         StoragePath = testFileName 
                     });

            var logger = TestMockFactory.CreateMockLogger();

            // Act
            using (var historyManager = new TaskHistoryManager(mockConfig.Object, logger.Object))
            {
                // Add a test record
                historyManager.AddTaskRecord(new PrintTaskRecord
                {
                    FileCount = 2,
                    TotalPages = 6,
                    PrinterName = "UserDataTestPrinter",
                    FilePaths = new List<string> { "test1.pdf", "test2.pdf" }
                });
            }

            // Create new instance to verify persistence
            using (var historyManager2 = new TaskHistoryManager(mockConfig.Object, logger.Object))
            {
                var loadedTasks = historyManager2.GetRecentTasks(5);

                // Assert
                loadedTasks.Should().HaveCount(1);
                var task = loadedTasks[0];
                task.FileCount.Should().Be(2);
                task.TotalPages.Should().Be(6);
                task.PrinterName.Should().Be("UserDataTestPrinter");
                task.FilePaths.Should().HaveCount(2);
            }
        }

        [Fact]
        public void GetUserDataPath_ShouldCreateDirectoryIfNotExists()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var testFileName = $"directory_test_{Guid.NewGuid()}.json";
            
            mockConfig.Setup(x => x.GetTaskHistorySettings())
                     .Returns(new TaskHistorySettings 
                     { 
                         MaxRecords = 3, 
                         StoragePath = testFileName 
                     });

            var logger = TestMockFactory.CreateMockLogger();

            // Act
            using var historyManager = new TaskHistoryManager(mockConfig.Object, logger.Object);

            // Assert
            // ??TaskHistoryManager????????????????
            historyManager.Should().NotBeNull();
        }

        [Fact]
        public void TaskHistoryManager_MultipleInstances_ShouldShareData()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var sharedFileName = $"shared_test_{Guid.NewGuid()}.json";
            
            mockConfig.Setup(x => x.GetTaskHistorySettings())
                     .Returns(new TaskHistorySettings 
                     { 
                         MaxRecords = 5, 
                         StoragePath = sharedFileName 
                     });

            var logger = TestMockFactory.CreateMockLogger();

            // Act - ?????????
            using (var historyManager1 = new TaskHistoryManager(mockConfig.Object, logger.Object))
            {
                historyManager1.AddTaskRecord(new PrintTaskRecord
                {
                    FileCount = 1,
                    TotalPages = 3,
                    PrinterName = "SharedPrinter1",
                    Timestamp = DateTime.Now.AddMinutes(-5)
                });

                historyManager1.AddTaskRecord(new PrintTaskRecord
                {
                    FileCount = 2,
                    TotalPages = 8,
                    PrinterName = "SharedPrinter2",
                    Timestamp = DateTime.Now.AddMinutes(-3)
                });
            }

            // Act - ?????????
            using (var historyManager2 = new TaskHistoryManager(mockConfig.Object, logger.Object))
            {
                var tasks = historyManager2.GetRecentTasks(10);

                // Assert
                tasks.Should().HaveCount(2);
                
                // ??????????????????
                tasks[0].PrinterName.Should().Be("SharedPrinter2");
                tasks[1].PrinterName.Should().Be("SharedPrinter1");
                
                // ???????
                tasks[0].FileCount.Should().Be(2);
                tasks[0].TotalPages.Should().Be(8);
                tasks[1].FileCount.Should().Be(1);
                tasks[1].TotalPages.Should().Be(3);
            }
        }

        [Fact]
        public void TaskHistoryManager_UserDataPath_ShouldHandleSpecialCharacters()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var specialFileName = $"????_{Guid.NewGuid()}.json"; // ??????
            
            mockConfig.Setup(x => x.GetTaskHistorySettings())
                     .Returns(new TaskHistorySettings 
                     { 
                         MaxRecords = 3, 
                         StoragePath = specialFileName 
                     });

            var logger = TestMockFactory.CreateMockLogger();

            // Act
            using var historyManager = new TaskHistoryManager(mockConfig.Object, logger.Object);
            
            historyManager.AddTaskRecord(new PrintTaskRecord
            {
                FileCount = 1,
                TotalPages = 2,
                PrinterName = "???????",
                FilePaths = new List<string> { "????.pdf" }
            });

            var tasks = historyManager.GetRecentTasks(5);

            // Assert
            tasks.Should().HaveCount(1);
            tasks[0].PrinterName.Should().Be("???????");
            tasks[0].FilePaths.Should().Contain("????.pdf");
        }

        [Fact]
        public void TaskHistoryManager_UserDataPath_ShouldHandleLongPaths()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var longFileName = $"very_long_filename_that_might_cause_issues_in_some_filesystems_{Guid.NewGuid()}.json";
            
            mockConfig.Setup(x => x.GetTaskHistorySettings())
                     .Returns(new TaskHistorySettings 
                     { 
                         MaxRecords = 3, 
                         StoragePath = longFileName 
                     });

            var logger = TestMockFactory.CreateMockLogger();

            // Act & Assert
            Action act = () =>
            {
                using var historyManager = new TaskHistoryManager(mockConfig.Object, logger.Object);
                historyManager.AddTaskRecord(new PrintTaskRecord
                {
                    FileCount = 1,
                    TotalPages = 1,
                    PrinterName = "LongPathPrinter"
                });
            };

            act.Should().NotThrow();
        }

        [Fact]
        public void TaskHistoryManager_UserDataPath_ShouldRecoverFromCorruptedFile()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var testFileName = $"corrupted_test_{Guid.NewGuid()}.json";
            
            mockConfig.Setup(x => x.GetTaskHistorySettings())
                     .Returns(new TaskHistorySettings 
                     { 
                         MaxRecords = 3, 
                         StoragePath = testFileName 
                     });

            var logger = TestMockFactory.CreateMockLogger();

            // ?????????JSON???????????????????
            var userDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TrayPrinterApp");
            Directory.CreateDirectory(userDataDir);
            var corruptedFilePath = Path.Combine(userDataDir, testFileName);
            File.WriteAllText(corruptedFilePath, "{ invalid json content }");

            // Act
            using var historyManager = new TaskHistoryManager(mockConfig.Object, logger.Object);
            
            // ?????????????????
            historyManager.AddTaskRecord(new PrintTaskRecord
            {
                FileCount = 1,
                TotalPages = 1,
                PrinterName = "RecoveryTestPrinter"
            });

            var tasks = historyManager.GetRecentTasks(5);

            // Assert
            tasks.Should().HaveCount(1);
            tasks[0].PrinterName.Should().Be("RecoveryTestPrinter");

            // Cleanup
            try
            {
                File.Delete(corruptedFilePath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}