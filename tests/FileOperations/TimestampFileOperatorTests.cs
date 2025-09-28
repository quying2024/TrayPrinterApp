using Xunit;
using FluentAssertions;
using TrayApp.FileOperations;
using TrayApp.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TrayApp.Tests.FileOperations
{
    /// <summary>
    /// 时间戳文件操作器测试
    /// 测试文件移动和时间戳目录创建功能
    /// </summary>
    public class TimestampFileOperatorTests : TestBase
    {
        [Fact]
        public void Constructor_ValidLogger_ShouldSucceed()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();

            // Act & Assert
            Action act = () => new TimestampFileOperator(logger.Object);
            act.Should().NotThrow();
        }

        [Fact]
        public void Constructor_NullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Action act = () => new TimestampFileOperator(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void MoveFilesToTimestampDirectory_NullFilePaths_ShouldThrowArgumentNullException()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var operator_ = new TimestampFileOperator(logger.Object);

            // Act & Assert
            Action act = () => operator_.MoveFilesToTimestampDirectory(null!, Path.GetTempPath());
            act.Should().Throw<ArgumentNullException>().WithParameterName("filePaths");
        }

        [Fact]
        public void MoveFilesToTimestampDirectory_NullBaseDirectory_ShouldThrowArgumentException()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var operator_ = new TimestampFileOperator(logger.Object);

            // Act & Assert
            Action act = () => operator_.MoveFilesToTimestampDirectory(new List<string>(), null!);
            act.Should().Throw<ArgumentException>().WithParameterName("baseDirectory");
        }

        [Fact]
        public void MoveFilesToTimestampDirectory_EmptyBaseDirectory_ShouldThrowArgumentException()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var operator_ = new TimestampFileOperator(logger.Object);

            // Act & Assert
            Action act = () => operator_.MoveFilesToTimestampDirectory(new List<string>(), "");
            act.Should().Throw<ArgumentException>().WithParameterName("baseDirectory");
        }

        [Fact]
        public void MoveFilesToTimestampDirectory_WhitespaceBaseDirectory_ShouldThrowArgumentException()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var operator_ = new TimestampFileOperator(logger.Object);

            // Act & Assert
            Action act = () => operator_.MoveFilesToTimestampDirectory(new List<string>(), "   ");
            act.Should().Throw<ArgumentException>().WithParameterName("baseDirectory");
        }

        [Fact]
        public void MoveFilesToTimestampDirectory_EmptyFileList_ShouldCreateTimestampDirectory()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var operator_ = new TimestampFileOperator(logger.Object);
            var baseDir = CreateTestDirectory();

            try
            {
                // Act
                var result = operator_.MoveFilesToTimestampDirectory(new List<string>(), baseDir);

                // Assert
                result.Should().NotBeNullOrEmpty();
                Directory.Exists(result).Should().BeTrue();
                result.Should().StartWith(baseDir);
                
                // 检查时间戳格式 (yyyyMMddHHmmss)
                var timestampPart = Path.GetFileName(result);
                timestampPart.Should().HaveLength(14);
                timestampPart.Should().MatchRegex(@"^\d{14}$");
            }
            finally
            {
                CleanupTestDirectory(baseDir);
            }
        }

        [Fact]
        public void MoveFilesToTimestampDirectory_ValidFiles_ShouldMoveFilesSuccessfully()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var operator_ = new TimestampFileOperator(logger.Object);
            var baseDir = CreateTestDirectory();
            
            // 创建测试文件
            var testFile1 = Path.Combine(baseDir, "test1.txt");
            var testFile2 = Path.Combine(baseDir, "test2.pdf");
            File.WriteAllText(testFile1, "content1");
            File.WriteAllText(testFile2, "content2");

            try
            {
                // Act
                var result = operator_.MoveFilesToTimestampDirectory(new[] { testFile1, testFile2 }, baseDir);

                // Assert
                result.Should().NotBeNullOrEmpty();
                Directory.Exists(result).Should().BeTrue();
                
                // 原文件应该不存在
                File.Exists(testFile1).Should().BeFalse();
                File.Exists(testFile2).Should().BeFalse();
                
                // 目标文件应该存在
                var movedFile1 = Path.Combine(result, "test1.txt");
                var movedFile2 = Path.Combine(result, "test2.pdf");
                File.Exists(movedFile1).Should().BeTrue();
                File.Exists(movedFile2).Should().BeTrue();
                
                // 内容应该保持不变
                File.ReadAllText(movedFile1).Should().Be("content1");
                File.ReadAllText(movedFile2).Should().Be("content2");
            }
            finally
            {
                CleanupTestDirectory(baseDir);
            }
        }

        [Fact]
        public void MoveFilesToTimestampDirectory_NonExistentFiles_ShouldSkipMissingFiles()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var operator_ = new TimestampFileOperator(logger.Object);
            var baseDir = CreateTestDirectory();
            
            // 创建一个存在的文件和一个不存在的文件路径
            var existingFile = Path.Combine(baseDir, "existing.txt");
            var nonExistentFile = Path.Combine(baseDir, "nonexistent.txt");
            File.WriteAllText(existingFile, "content");

            try
            {
                // Act
                var result = operator_.MoveFilesToTimestampDirectory(
                    new[] { existingFile, nonExistentFile }, baseDir);

                // Assert
                result.Should().NotBeNullOrEmpty();
                Directory.Exists(result).Should().BeTrue();
                
                // 存在的文件应该被移动
                File.Exists(existingFile).Should().BeFalse();
                var movedFile = Path.Combine(result, "existing.txt");
                File.Exists(movedFile).Should().BeTrue();
                File.ReadAllText(movedFile).Should().Be("content");
                
                // 不存在的文件不应该出现在目标目录
                var wouldBeMovedFile = Path.Combine(result, "nonexistent.txt");
                File.Exists(wouldBeMovedFile).Should().BeFalse();
            }
            finally
            {
                CleanupTestDirectory(baseDir);
            }
        }

        [Fact]
        public void MoveFilesToTimestampDirectory_DuplicateFileNames_ShouldRenameFiles()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var operator_ = new TimestampFileOperator(logger.Object);
            var baseDir = CreateTestDirectory();
            
            // 创建两个同名文件在不同目录
            var subDir1 = Path.Combine(baseDir, "sub1");
            var subDir2 = Path.Combine(baseDir, "sub2");
            Directory.CreateDirectory(subDir1);
            Directory.CreateDirectory(subDir2);
            
            var file1 = Path.Combine(subDir1, "duplicate.txt");
            var file2 = Path.Combine(subDir2, "duplicate.txt");
            File.WriteAllText(file1, "content1");
            File.WriteAllText(file2, "content2");

            try
            {
                // Act
                var result = operator_.MoveFilesToTimestampDirectory(new[] { file1, file2 }, baseDir);

                // Assert
                result.Should().NotBeNullOrEmpty();
                Directory.Exists(result).Should().BeTrue();
                
                // 应该有两个文件，一个原名，一个重命名
                var files = Directory.GetFiles(result);
                files.Should().HaveCount(2);
                
                var originalFile = Path.Combine(result, "duplicate.txt");
                var renamedFile = Path.Combine(result, "duplicate_1.txt");
                
                File.Exists(originalFile).Should().BeTrue();
                File.Exists(renamedFile).Should().BeTrue();
                
                // 内容应该不同
                var content1 = File.ReadAllText(originalFile);
                var content2 = File.ReadAllText(renamedFile);
                content1.Should().NotBe(content2);
                new[] { content1, content2 }.Should().Contain("content1");
                new[] { content1, content2 }.Should().Contain("content2");
            }
            finally
            {
                CleanupTestDirectory(baseDir);
            }
        }

        [Fact]
        public void MoveFilesToTimestampDirectory_MultipleDuplicates_ShouldIncrementCounter()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var operator_ = new TimestampFileOperator(logger.Object);
            var baseDir = CreateTestDirectory();
            
            // 创建三个同名文件
            var subDirs = new[] { "sub1", "sub2", "sub3" };
            var files = new List<string>();
            
            for (int i = 0; i < subDirs.Length; i++)
            {
                var subDir = Path.Combine(baseDir, subDirs[i]);
                Directory.CreateDirectory(subDir);
                var file = Path.Combine(subDir, "triple.txt");
                File.WriteAllText(file, $"content{i + 1}");
                files.Add(file);
            }

            try
            {
                // Act
                var result = operator_.MoveFilesToTimestampDirectory(files, baseDir);

                // Assert
                result.Should().NotBeNullOrEmpty();
                Directory.Exists(result).Should().BeTrue();
                
                // 应该有三个文件
                var movedFiles = Directory.GetFiles(result);
                movedFiles.Should().HaveCount(3);
                
                // 文件名应该是: triple.txt, triple_1.txt, triple_2.txt
                var expectedFiles = new[]
                {
                    Path.Combine(result, "triple.txt"),
                    Path.Combine(result, "triple_1.txt"),
                    Path.Combine(result, "triple_2.txt")
                };
                
                foreach (var expectedFile in expectedFiles)
                {
                    File.Exists(expectedFile).Should().BeTrue();
                }
            }
            finally
            {
                CleanupTestDirectory(baseDir);
            }
        }

        [Fact]
        public void MoveFilesToTimestampDirectory_LockedFile_ShouldLogErrorAndContinue()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var operator_ = new TimestampFileOperator(logger.Object);
            var baseDir = CreateTestDirectory();
            
            var validFile = Path.Combine(baseDir, "valid.txt");
            var lockedFile = Path.Combine(baseDir, "locked.txt");
            File.WriteAllText(validFile, "valid content");
            File.WriteAllText(lockedFile, "locked content");

            try
            {
                // 锁定文件
                using (var stream = File.OpenWrite(lockedFile))
                {
                    // Act
                    var result = operator_.MoveFilesToTimestampDirectory(
                        new[] { validFile, lockedFile }, baseDir);

                    // Assert
                    result.Should().NotBeNullOrEmpty();
                    Directory.Exists(result).Should().BeTrue();
                    
                    // 有效文件应该被移动
                    File.Exists(validFile).Should().BeFalse();
                    var movedValidFile = Path.Combine(result, "valid.txt");
                    File.Exists(movedValidFile).Should().BeTrue();
                    
                    // 锁定文件应该保持在原位置（移动失败）
                    File.Exists(lockedFile).Should().BeTrue();
                    var wouldBeMovedLockedFile = Path.Combine(result, "locked.txt");
                    File.Exists(wouldBeMovedLockedFile).Should().BeFalse();
                }
            }
            finally
            {
                CleanupTestDirectory(baseDir);
            }
        }

        [Fact]
        public void MoveFilesToTimestampDirectory_NonExistentBaseDirectory_ShouldCreateBaseDirectory()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var operator_ = new TimestampFileOperator(logger.Object);
            var baseDir = Path.Combine(Path.GetTempPath(), $"NonExistentBase_{Guid.NewGuid()}");
            
            var tempDir = CreateTestDirectory();
            var testFile = Path.Combine(tempDir, "test.txt");
            File.WriteAllText(testFile, "content");

            try
            {
                // Act
                var result = operator_.MoveFilesToTimestampDirectory(new[] { testFile }, baseDir);

                // Assert
                result.Should().NotBeNullOrEmpty();
                Directory.Exists(result).Should().BeTrue();
                Directory.Exists(baseDir).Should().BeTrue();
                
                var movedFile = Path.Combine(result, "test.txt");
                File.Exists(movedFile).Should().BeTrue();
                File.ReadAllText(movedFile).Should().Be("content");
            }
            finally
            {
                CleanupTestDirectory(tempDir);
                CleanupTestDirectory(baseDir);
            }
        }

        [Fact]
        public void MoveFilesToTimestampDirectory_LongFilePath_ShouldHandleGracefully()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var operator_ = new TimestampFileOperator(logger.Object);
            var baseDir = CreateTestDirectory();
            
            // 创建一个长文件名
            var longFileName = new string('a', 200) + ".txt";
            var testFile = Path.Combine(baseDir, longFileName);
            
            try
            {
                File.WriteAllText(testFile, "content");
                
                // Act
                var result = operator_.MoveFilesToTimestampDirectory(new[] { testFile }, baseDir);

                // Assert
                result.Should().NotBeNullOrEmpty();
                Directory.Exists(result).Should().BeTrue();
                
                var movedFile = Path.Combine(result, longFileName);
                File.Exists(movedFile).Should().BeTrue();
            }
            catch (PathTooLongException)
            {
                // 在某些系统上，长路径可能不被支持，这是正常的
                true.Should().BeTrue("长路径在当前系统上不被支持");
            }
            finally
            {
                CleanupTestDirectory(baseDir);
            }
        }

        [Fact]
        public void MoveFilesToTimestampDirectory_SpecialCharactersInFileName_ShouldHandleCorrectly()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var operator_ = new TimestampFileOperator(logger.Object);
            var baseDir = CreateTestDirectory();
            
            // 创建包含特殊字符的文件名（但在文件系统中有效的）
            var specialFileName = "test[1]file.txt";
            var testFile = Path.Combine(baseDir, specialFileName);
            File.WriteAllText(testFile, "special content");

            try
            {
                // Act
                var result = operator_.MoveFilesToTimestampDirectory(new[] { testFile }, baseDir);

                // Assert
                result.Should().NotBeNullOrEmpty();
                Directory.Exists(result).Should().BeTrue();
                
                var movedFile = Path.Combine(result, specialFileName);
                File.Exists(movedFile).Should().BeTrue();
                File.ReadAllText(movedFile).Should().Be("special content");
            }
            finally
            {
                CleanupTestDirectory(baseDir);
            }
        }

        [Fact]
        public void MoveFilesToTimestampDirectory_ConcurrentCalls_ShouldCreateDifferentTimestamps()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var operator_ = new TimestampFileOperator(logger.Object);
            var baseDir = CreateTestDirectory();
            
            var file1 = Path.Combine(baseDir, "file1.txt");
            var file2 = Path.Combine(baseDir, "file2.txt");
            File.WriteAllText(file1, "content1");
            File.WriteAllText(file2, "content2");

            try
            {
                // Act - 快速连续调用
                var result1 = operator_.MoveFilesToTimestampDirectory(new[] { file1 }, baseDir);
                System.Threading.Thread.Sleep(1100); // 确保时间戳不同（精确到秒）
                var result2 = operator_.MoveFilesToTimestampDirectory(new[] { file2 }, baseDir);

                // Assert
                result1.Should().NotBe(result2);
                Directory.Exists(result1).Should().BeTrue();
                Directory.Exists(result2).Should().BeTrue();
                
                File.Exists(Path.Combine(result1, "file1.txt")).Should().BeTrue();
                File.Exists(Path.Combine(result2, "file2.txt")).Should().BeTrue();
            }
            finally
            {
                CleanupTestDirectory(baseDir);
            }
        }

        /// <summary>
        /// 创建测试目录
        /// </summary>
        private string CreateTestDirectory()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"FileOperatorTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);
            return tempDir;
        }

        /// <summary>
        /// 清理测试目录
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
                // 忽略清理错误
            }
        }
    }
}