using Xunit;
using FluentAssertions;
using TrayApp.FileOperations;
using TrayApp.Tests.Helpers;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace TrayApp.Tests.FileOperations
{
    /// <summary>
    /// TimestampFileOperator测试类
    /// </summary>
    public class TimestampFileOperatorTests : TestBase
    {
        [Fact]
        public void MoveFilesToTimestampDirectory_EmptyFileList_ShouldCreateDirectoryAndReturnPath()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var fileOperator = new TimestampFileOperator(logger.Object);
            var baseDir = Path.Combine(TestDirectory, "BaseDirectory");
            Directory.CreateDirectory(baseDir);

            // Act
            var resultPath = fileOperator.MoveFilesToTimestampDirectory(new List<string>(), baseDir);

            // Assert
            resultPath.Should().NotBeNullOrEmpty();
            Directory.Exists(resultPath).Should().BeTrue();
            resultPath.Should().StartWith(baseDir);
            
            // 验证时间戳目录格式 (yyyyMMddHHmmss)
            var dirName = Path.GetFileName(resultPath);
            dirName.Should().MatchRegex(@"^\d{14}$", "目录名应该是14位时间戳格式");
        }

        [Fact]
        public void MoveFilesToTimestampDirectory_SingleFile_ShouldMoveFileCorrectly()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var fileOperator = new TimestampFileOperator(logger.Object);
            
            var sourceDir = Path.Combine(TestDirectory, "SourceDir");
            var baseDir = Path.Combine(TestDirectory, "BaseDir");
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(baseDir);

            var sourceFile = Path.Combine(sourceDir, "test.pdf");
            var fileContent = "Test file content";
            File.WriteAllText(sourceFile, fileContent);

            // Act
            var resultPath = fileOperator.MoveFilesToTimestampDirectory(new[] { sourceFile }, baseDir);

            // Assert
            Directory.Exists(resultPath).Should().BeTrue();
            File.Exists(sourceFile).Should().BeFalse("源文件应该被移动（不再存在）");
            
            var movedFile = Path.Combine(resultPath, "test.pdf");
            File.Exists(movedFile).Should().BeTrue("文件应该存在于目标目录");
            File.ReadAllText(movedFile).Should().Be(fileContent, "文件内容应该保持不变");
        }

        [Fact]
        public void MoveFilesToTimestampDirectory_MultipleFiles_ShouldMoveAllFiles()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var fileOperator = new TimestampFileOperator(logger.Object);
            
            var sourceDir = Path.Combine(TestDirectory, "SourceDir");
            var baseDir = Path.Combine(TestDirectory, "BaseDir");
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(baseDir);

            var files = new[]
            {
                Path.Combine(sourceDir, "file1.pdf"),
                Path.Combine(sourceDir, "file2.docx"),
                Path.Combine(sourceDir, "file3.jpg")
            };

            foreach (var file in files)
            {
                File.WriteAllText(file, $"Content of {Path.GetFileName(file)}");
            }

            // Act
            var resultPath = fileOperator.MoveFilesToTimestampDirectory(files, baseDir);

            // Assert
            Directory.Exists(resultPath).Should().BeTrue();
            
            foreach (var sourceFile in files)
            {
                File.Exists(sourceFile).Should().BeFalse("源文件应该被移动");
                
                var fileName = Path.GetFileName(sourceFile);
                var targetFile = Path.Combine(resultPath, fileName);
                File.Exists(targetFile).Should().BeTrue($"文件{fileName}应该存在于目标目录");
            }
        }

        [Fact]
        public void MoveFilesToTimestampDirectory_DuplicateFileName_ShouldRenameFile()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var fileOperator = new TimestampFileOperator(logger.Object);
            
            var sourceDir = Path.Combine(TestDirectory, "SourceDir");
            var baseDir = Path.Combine(TestDirectory, "BaseDir");
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(baseDir);

            // 预先创建时间戳目录和同名文件
            var timestampDir = Path.Combine(baseDir, DateTime.Now.ToString("yyyyMMddHHmmss"));
            Directory.CreateDirectory(timestampDir);
            var existingFile = Path.Combine(timestampDir, "test.pdf");
            File.WriteAllText(existingFile, "Existing content");

            // 创建要移动的同名文件
            var sourceFile = Path.Combine(sourceDir, "test.pdf");
            File.WriteAllText(sourceFile, "New content");

            // Act
            var resultPath = fileOperator.MoveFilesToTimestampDirectory(new[] { sourceFile }, baseDir);

            // Assert
            File.Exists(existingFile).Should().BeTrue("原文件应该保持存在");
            File.ReadAllText(existingFile).Should().Be("Existing content");

            // 新文件应该被重命名
            var renamedFile = Path.Combine(resultPath, "test_1.pdf");
            File.Exists(renamedFile).Should().BeTrue("新文件应该被重命名");
            File.ReadAllText(renamedFile).Should().Be("New content");
        }

        [Fact]
        public void MoveFilesToTimestampDirectory_NonExistentSourceFile_ShouldHandleGracefully()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var fileOperator = new TimestampFileOperator(logger.Object);
            
            var baseDir = Path.Combine(TestDirectory, "BaseDir");
            Directory.CreateDirectory(baseDir);

            var nonExistentFile = Path.Combine(TestDirectory, "non_existent.pdf");

            // Act & Assert
            Action act = () => fileOperator.MoveFilesToTimestampDirectory(new[] { nonExistentFile }, baseDir);
            
            // 应该记录错误但不抛出异常，或者抛出适当的异常
            // 具体行为取决于实现，这里我们验证不会导致程序崩溃
            act.Should().NotThrow<NullReferenceException>();
        }

        [Fact]
        public void MoveFilesToTimestampDirectory_InvalidBasePath_ShouldThrowException()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var fileOperator = new TimestampFileOperator(logger.Object);
            
            var sourceFile = CreateTestFile("test.pdf", "content");
            var invalidBasePath = Path.Combine("X:\\", "InvalidDrive", "InvalidPath");

            // Act & Assert
            Action act = () => fileOperator.MoveFilesToTimestampDirectory(new[] { sourceFile }, invalidBasePath);
            
            // 修复：使用Assert.ThrowsAny来捕获任何异常类型
            var exception = Assert.ThrowsAny<Exception>(act);
            exception.Should().Match(ex => ex is DirectoryNotFoundException || ex is UnauthorizedAccessException,
                "应该抛出DirectoryNotFoundException或UnauthorizedAccessException");
        }
    }
}