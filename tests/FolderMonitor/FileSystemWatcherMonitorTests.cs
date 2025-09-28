using Xunit;
using FluentAssertions;
using TrayApp.FolderMonitor;
using TrayApp.Core;
using TrayApp.Tests.Helpers;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace TrayApp.Tests.FolderMonitor
{
    /// <summary>
    /// FileSystemWatcherMonitor测试类
    /// </summary>
    public class FileSystemWatcherMonitorTests : TestBase
    {
        [Fact]
        public void StartMonitoring_ValidParameters_ShouldStartSuccessfully()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var monitor = new FileSystemWatcherMonitor(logger.Object);
            
            var watchPath = Path.Combine(TestDirectory, "MonitorTest");
            Directory.CreateDirectory(watchPath);
            
            var fileTypes = new List<string> { ".pdf", ".docx" };

            // Act & Assert
            Action act = () => monitor.StartMonitoring(watchPath, 3, fileTypes);
            act.Should().NotThrow();

            // Cleanup
            monitor.StopMonitoring();
        }

        [Fact]
        public void StartMonitoring_InvalidPath_ShouldThrowException()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var monitor = new FileSystemWatcherMonitor(logger.Object);
            
            var invalidPath = Path.Combine("X:\\", "NonExistentPath");
            var fileTypes = new List<string> { ".pdf" };

            // Act & Assert
            Action act = () => monitor.StartMonitoring(invalidPath, 3, fileTypes);
            act.Should().Throw<DirectoryNotFoundException>();
        }

        [Fact]
        public void StartMonitoring_EmptyPath_ShouldThrowArgumentException()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var monitor = new FileSystemWatcherMonitor(logger.Object);
            
            var fileTypes = new List<string> { ".pdf" };

            // Act & Assert
            Action act = () => monitor.StartMonitoring("", 3, fileTypes);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void StartMonitoring_NegativeTimeout_ShouldThrowArgumentException()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var monitor = new FileSystemWatcherMonitor(logger.Object);
            
            var watchPath = Path.Combine(TestDirectory, "TimeoutTest");
            Directory.CreateDirectory(watchPath);
            var fileTypes = new List<string> { ".pdf" };

            // Act & Assert
            Action act = () => monitor.StartMonitoring(watchPath, -1, fileTypes);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void FilesBatchReady_Event_ShouldTriggerWhenFilesAdded()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var monitor = new FileSystemWatcherMonitor(logger.Object);
            
            var watchPath = Path.Combine(TestDirectory, "EventTest");
            Directory.CreateDirectory(watchPath);
            
            var eventTriggered = false;
            FileBatchEventArgs? capturedArgs = null;
            
            monitor.FilesBatchReady += (sender, args) =>
            {
                eventTriggered = true;
                capturedArgs = args;
            };

            monitor.StartMonitoring(watchPath, 1, new[] { ".pdf" }); // 1秒超时便于测试

            // Act
            var testFile = Path.Combine(watchPath, "test.pdf");
            File.WriteAllText(testFile, "Test content");

            // 等待事件触发
            Thread.Sleep(2000); // 等待超过超时时间

            // Assert
            eventTriggered.Should().BeTrue("文件创建后应该触发事件");
            capturedArgs.Should().NotBeNull();
            capturedArgs!.FilePaths.Should().Contain(testFile);

            // Cleanup
            monitor.StopMonitoring();
        }

        [Fact]
        public void StopMonitoring_RunningMonitor_ShouldStopSuccessfully()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var monitor = new FileSystemWatcherMonitor(logger.Object);
            
            var watchPath = Path.Combine(TestDirectory, "StopTest");
            Directory.CreateDirectory(watchPath);

            monitor.StartMonitoring(watchPath, 3, new[] { ".pdf" });

            // Act & Assert
            Action act = () => monitor.StopMonitoring();
            act.Should().NotThrow();
        }

        [Fact]
        public void FileTypeFilter_NonMonitoredType_ShouldNotTriggerEvent()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var monitor = new FileSystemWatcherMonitor(logger.Object);
            
            var watchPath = Path.Combine(TestDirectory, "FilterTest");
            Directory.CreateDirectory(watchPath);
            
            var eventTriggered = false;
            monitor.FilesBatchReady += (sender, args) => eventTriggered = true;

            monitor.StartMonitoring(watchPath, 1, new[] { ".pdf" }); // 仅监视PDF

            // Act
            var txtFile = Path.Combine(watchPath, "test.txt"); // 创建非监视类型文件
            File.WriteAllText(txtFile, "Text content");

            Thread.Sleep(2000); // 等待

            // Assert
            eventTriggered.Should().BeFalse("非监视文件类型不应触发事件");

            // Cleanup
            monitor.StopMonitoring();
        }

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var monitor = new FileSystemWatcherMonitor(logger.Object);
            
            var watchPath = Path.Combine(TestDirectory, "DisposeTest");
            Directory.CreateDirectory(watchPath);

            monitor.StartMonitoring(watchPath, 3, new[] { ".pdf" });

            // Act & Assert
            Action act = () => monitor.Dispose();
            act.Should().NotThrow();
        }
    }
}