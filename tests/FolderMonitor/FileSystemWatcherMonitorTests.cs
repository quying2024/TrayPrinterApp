using Xunit;
using FluentAssertions;
using TrayApp.FolderMonitor;
using TrayApp.Core;
using TrayApp.Tests.Helpers;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TrayApp.Tests.FolderMonitor
{
    /// <summary>
    /// FileSystemWatcherMonitor测试类
    /// </summary>
    public class FileSystemWatcherMonitorTests : TestBase
    {
        [Fact]
        public async Task StartMonitoring_SingleFile_ShouldTriggerEvent()
        {
            // Arrange
            var logger = MockFactory.CreateMockLogger();
            var monitor = new FileSystemWatcherMonitor(logger.Object);
            var watchDir = Path.Combine(TestDirectory, "WatchFolder");
            Directory.CreateDirectory(watchDir);

            FileBatchEventArgs? capturedArgs = null;
            monitor.FilesBatchReady += (sender, args) => capturedArgs = args;

            // Act
            monitor.StartMonitoring(watchDir, 2, new[] { ".pdf" }); // 2秒超时
            
            // 创建测试文件
            var testFile = Path.Combine(watchDir, "test.pdf");
            await File.WriteAllTextAsync(testFile, "Test PDF Content");
            
            // 等待批处理超时
            await Task.Delay(3000);

            // Assert
            capturedArgs.Should().NotBeNull();
            capturedArgs!.FilePaths.Should().ContainSingle();
            capturedArgs.FilePaths[0].Should().Be(testFile);

            // Cleanup
            monitor.StopMonitoring();
        }

        [Fact]
        public async Task StartMonitoring_MultipleBatchFiles_ShouldCombineInBatch()
        {
            // Arrange
            var logger = MockFactory.CreateMockLogger();
            var monitor = new FileSystemWatcherMonitor(logger.Object);
            var watchDir = Path.Combine(TestDirectory, "BatchWatchFolder");
            Directory.CreateDirectory(watchDir);

            FileBatchEventArgs? capturedArgs = null;
            monitor.FilesBatchReady += (sender, args) => capturedArgs = args;

            // Act
            monitor.StartMonitoring(watchDir, 3, new[] { ".pdf", ".docx" }); // 3秒超时

            // 在3秒内创建多个文件
            var file1 = Path.Combine(watchDir, "file1.pdf");
            var file2 = Path.Combine(watchDir, "file2.docx");
            var file3 = Path.Combine(watchDir, "file3.pdf");

            await File.WriteAllTextAsync(file1, "Content 1");
            await Task.Delay(500);
            await File.WriteAllTextAsync(file2, "Content 2");
            await Task.Delay(500);
            await File.WriteAllTextAsync(file3, "Content 3");
            
            // 等待批处理超时
            await Task.Delay(4000);

            // Assert
            capturedArgs.Should().NotBeNull();
            capturedArgs!.FilePaths.Should().HaveCount(3);
            capturedArgs.FilePaths.Should().Contain(file1);
            capturedArgs.FilePaths.Should().Contain(file2);
            capturedArgs.FilePaths.Should().Contain(file3);

            // Cleanup
            monitor.StopMonitoring();
        }

        [Fact]
        public async Task StartMonitoring_FileTypeFilter_ShouldOnlyIncludeMatchingFiles()
        {
            // Arrange
            var logger = MockFactory.CreateMockLogger();
            var monitor = new FileSystemWatcherMonitor(logger.Object);
            var watchDir = Path.Combine(TestDirectory, "FilterWatchFolder");
            Directory.CreateDirectory(watchDir);

            FileBatchEventArgs? capturedArgs = null;
            monitor.FilesBatchReady += (sender, args) => capturedArgs = args;

            // Act
            monitor.StartMonitoring(watchDir, 2, new[] { ".pdf" }); // 只监视PDF文件

            // 创建不同类型的文件
            var pdfFile = Path.Combine(watchDir, "test.pdf");
            var txtFile = Path.Combine(watchDir, "test.txt");
            
            await File.WriteAllTextAsync(pdfFile, "PDF Content");
            await File.WriteAllTextAsync(txtFile, "TXT Content");
            
            // 等待批处理超时
            await Task.Delay(3000);

            // Assert
            capturedArgs.Should().NotBeNull();
            capturedArgs!.FilePaths.Should().ContainSingle();
            capturedArgs.FilePaths[0].Should().Be(pdfFile);
            capturedArgs.FilePaths.Should().NotContain(txtFile);

            // Cleanup
            monitor.StopMonitoring();
        }

        [Fact]
        public void StartMonitoring_InvalidDirectory_ShouldThrowException()
        {
            // Arrange
            var logger = MockFactory.CreateMockLogger();
            var monitor = new FileSystemWatcherMonitor(logger.Object);
            var invalidDir = Path.Combine(TestDirectory, "NonExistentFolder");

            // Act & Assert
            Action act = () => monitor.StartMonitoring(invalidDir, 3, new[] { ".pdf" });
            act.Should().Throw<DirectoryNotFoundException>();
        }

        [Fact]
        public void StartMonitoring_EmptyPath_ShouldThrowException()
        {
            // Arrange
            var logger = MockFactory.CreateMockLogger();
            var monitor = new FileSystemWatcherMonitor(logger.Object);

            // Act & Assert
            Action act = () => monitor.StartMonitoring("", 3, new[] { ".pdf" });
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void StopMonitoring_WhenRunning_ShouldStopGracefully()
        {
            // Arrange
            var logger = MockFactory.CreateMockLogger();
            var monitor = new FileSystemWatcherMonitor(logger.Object);
            var watchDir = Path.Combine(TestDirectory, "StopTestFolder");
            Directory.CreateDirectory(watchDir);

            monitor.StartMonitoring(watchDir, 3, new[] { ".pdf" });

            // Act
            Action act = () => monitor.StopMonitoring();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public async Task FilesBatchReady_MultipleSubscribers_ShouldNotifyAll()
        {
            // Arrange
            var logger = MockFactory.CreateMockLogger();
            var monitor = new FileSystemWatcherMonitor(logger.Object);
            var watchDir = Path.Combine(TestDirectory, "MultiSubscriberFolder");
            Directory.CreateDirectory(watchDir);

            var subscriber1Called = false;
            var subscriber2Called = false;

            monitor.FilesBatchReady += (sender, args) => subscriber1Called = true;
            monitor.FilesBatchReady += (sender, args) => subscriber2Called = true;

            // Act
            monitor.StartMonitoring(watchDir, 2, new[] { ".pdf" });
            
            var testFile = Path.Combine(watchDir, "multi_test.pdf");
            await File.WriteAllTextAsync(testFile, "Test Content");
            
            await Task.Delay(3000);

            // Assert
            subscriber1Called.Should().BeTrue();
            subscriber2Called.Should().BeTrue();

            // Cleanup
            monitor.StopMonitoring();
        }
    }
}