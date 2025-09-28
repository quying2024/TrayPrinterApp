using Xunit;
using FluentAssertions;
using TrayApp.TaskHistory;
using TrayApp.Core;
using TrayApp.Tests.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace TrayApp.Tests.TaskHistory
{
    /// <summary>
    /// TaskHistoryManager测试类
    /// </summary>
    public class TaskHistoryManagerTests : TestBase
    {
        [Fact]
        public void AddTaskRecord_NewRecord_ShouldAddSuccessfully()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var testStoragePath = GetTestDataPath($"test_history_{Guid.NewGuid()}.json");
            mockConfig.Setup(x => x.GetTaskHistorySettings())
                     .Returns(new TaskHistorySettings 
                     { 
                         MaxRecords = 5, 
                         StoragePath = testStoragePath 
                     });

            var logger = TestMockFactory.CreateMockLogger();
            using var historyManager = new TaskHistoryManager(mockConfig.Object, logger.Object);

            var taskRecord = new PrintTaskRecord
            {
                FileCount = 3,
                TotalPages = 10,
                PrinterName = "TestPrinter",
                FilePaths = new List<string> { "file1.pdf", "file2.pdf", "file3.pdf" }
            };

            // Act
            historyManager.AddTaskRecord(taskRecord);
            var recentTasks = historyManager.GetRecentTasks(1);

            // Assert
            recentTasks.Should().HaveCount(1);
            var addedTask = recentTasks.First();
            addedTask.FileCount.Should().Be(3);
            addedTask.TotalPages.Should().Be(10);
            addedTask.PrinterName.Should().Be("TestPrinter");
            addedTask.FilePaths.Should().HaveCount(3);
        }

        [Fact]
        public void AddTaskRecord_ExceedsMaxRecords_ShouldTrimOldRecords()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var testStoragePath = GetTestDataPath($"test_history_trim_{Guid.NewGuid()}.json");
            mockConfig.Setup(x => x.GetTaskHistorySettings())
                     .Returns(new TaskHistorySettings 
                     { 
                         MaxRecords = 3, 
                         StoragePath = testStoragePath 
                     });

            var logger = TestMockFactory.CreateMockLogger();
            using var historyManager = new TaskHistoryManager(mockConfig.Object, logger.Object);

            // Act - 添加4条记录，超过最大限制3条
            for (int i = 1; i <= 4; i++)
            {
                var record = new PrintTaskRecord
                {
                    FileCount = i,
                    TotalPages = i * 2,
                    PrinterName = $"Printer{i}",
                    Timestamp = DateTime.Now.AddMinutes(-i) // 确保时间顺序
                };
                historyManager.AddTaskRecord(record);
                // 添加小延迟确保时间戳不同
                System.Threading.Thread.Sleep(1);
            }

            var allTasks = historyManager.GetRecentTasks(10);

            // Assert
            allTasks.Should().HaveCount(3, "应该只保留最大记录数");
            
            // 最新的记录应该在前面
            allTasks[0].PrinterName.Should().Be("Printer4");
            allTasks[1].PrinterName.Should().Be("Printer3");
            allTasks[2].PrinterName.Should().Be("Printer2");
            
            // 最早的记录应该被删除
            allTasks.Should().NotContain(t => t.PrinterName == "Printer1");
        }

        [Fact]
        public void GetRecentTasks_RequestMoreThanAvailable_ShouldReturnAllAvailable()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var testStoragePath = GetTestDataPath($"test_history_count_{Guid.NewGuid()}.json");
            // 确保文件不存在
            if (File.Exists(testStoragePath))
            {
                File.Delete(testStoragePath);
            }
            
            mockConfig.Setup(x => x.GetTaskHistorySettings())
                     .Returns(new TaskHistorySettings 
                     { 
                         MaxRecords = 10, 
                         StoragePath = testStoragePath 
                     });

            var logger = TestMockFactory.CreateMockLogger();
            using var historyManager = new TaskHistoryManager(mockConfig.Object, logger.Object);

            // 添加2条记录
            for (int i = 1; i <= 2; i++)
            {
                historyManager.AddTaskRecord(new PrintTaskRecord
                {
                    FileCount = i,
                    PrinterName = $"Printer{i}",
                    Timestamp = DateTime.Now.AddSeconds(-i)
                });
                // 添加小延迟确保时间戳不同
                System.Threading.Thread.Sleep(10);
            }

            // Act - 请求5条记录，但只有2条
            var tasks = historyManager.GetRecentTasks(5);

            // Assert
            tasks.Should().HaveCount(2);
        }

        [Fact]
        public void GetRecentTasks_EmptyHistory_ShouldReturnEmptyList()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var testStoragePath = GetTestDataPath($"empty_history_{Guid.NewGuid()}.json");
            // 确保文件不存在
            if (File.Exists(testStoragePath))
            {
                File.Delete(testStoragePath);
            }
            
            mockConfig.Setup(x => x.GetTaskHistorySettings())
                     .Returns(new TaskHistorySettings 
                     { 
                         MaxRecords = 5, 
                         StoragePath = testStoragePath 
                     });

            var logger = TestMockFactory.CreateMockLogger();
            using var historyManager = new TaskHistoryManager(mockConfig.Object, logger.Object);

            // Act
            var tasks = historyManager.GetRecentTasks(5);

            // Assert
            tasks.Should().NotBeNull();
            tasks.Should().BeEmpty();
        }

        [Fact]
        public void TaskHistoryManager_Persistence_ShouldSaveAndLoadCorrectly()
        {
            // Arrange
            var testStoragePath = GetTestDataPath($"persistence_test_history_{Guid.NewGuid()}.json");
            // 确保文件不存在
            if (File.Exists(testStoragePath))
            {
                File.Delete(testStoragePath);
            }
            
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            mockConfig.Setup(x => x.GetTaskHistorySettings())
                     .Returns(new TaskHistorySettings 
                     { 
                         MaxRecords = 5, 
                         StoragePath = testStoragePath 
                     });

            var logger = TestMockFactory.CreateMockLogger();

            var originalRecord = new PrintTaskRecord
            {
                FileCount = 2,
                TotalPages = 8,
                PrinterName = "PersistencePrinter",
                FilePaths = new List<string> { "persist1.pdf", "persist2.pdf" }
            };

            // Act - 第一个实例添加记录
            using (var historyManager1 = new TaskHistoryManager(mockConfig.Object, logger.Object))
            {
                historyManager1.AddTaskRecord(originalRecord);
            }

            // 第二个实例加载记录
            using (var historyManager2 = new TaskHistoryManager(mockConfig.Object, logger.Object))
            {
                var loadedTasks = historyManager2.GetRecentTasks(5);

                // Assert
                loadedTasks.Should().HaveCount(1);
                var loadedTask = loadedTasks.First();
                loadedTask.FileCount.Should().Be(2);
                loadedTask.TotalPages.Should().Be(8);
                loadedTask.PrinterName.Should().Be("PersistencePrinter");
                loadedTask.FilePaths.Should().HaveCount(2);
            }

            // 注意：由于TaskHistoryManager现在使用用户数据目录，
            // 我们不直接验证testStoragePath文件是否存在，
            // 而是通过第二个实例能够成功加载数据来验证持久化功能
        }

        [Fact]
        public void AddTaskRecord_NullRecord_ShouldThrowArgumentNullException()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var testStoragePath = GetTestDataPath($"null_test_{Guid.NewGuid()}.json");
            mockConfig.Setup(x => x.GetTaskHistorySettings())
                     .Returns(new TaskHistorySettings 
                     { 
                         MaxRecords = 5, 
                         StoragePath = testStoragePath 
                     });
            
            var logger = TestMockFactory.CreateMockLogger();
            using var historyManager = new TaskHistoryManager(mockConfig.Object, logger.Object);

            // Act & Assert
            Action act = () => historyManager.AddTaskRecord(null!);
            
            // 修复：应该抛出ArgumentNullException
            var exception = Assert.Throws<ArgumentNullException>(act);
            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void GetRecentTasks_NegativeCount_ShouldReturnEmptyList()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var testStoragePath = GetTestDataPath($"negative_test_{Guid.NewGuid()}.json");
            mockConfig.Setup(x => x.GetTaskHistorySettings())
                     .Returns(new TaskHistorySettings 
                     { 
                         MaxRecords = 5, 
                         StoragePath = testStoragePath 
                     });
            
            var logger = TestMockFactory.CreateMockLogger();
            using var historyManager = new TaskHistoryManager(mockConfig.Object, logger.Object);

            // Act
            var tasks = historyManager.GetRecentTasks(-1);

            // Assert
            tasks.Should().NotBeNull();
            tasks.Should().BeEmpty();
        }
    }
}