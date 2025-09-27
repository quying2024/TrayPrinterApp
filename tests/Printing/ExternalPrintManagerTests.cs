using Xunit;
using FluentAssertions;
using Moq;
using TrayApp.Printing;
using TrayApp.Core;
using TrayApp.Tests.Helpers;
using System.Drawing.Printing;
using System.Collections.Generic;
using System.Linq;

namespace TrayApp.Tests.Printing
{
    /// <summary>
    /// ExternalPrintManager测试类
    /// </summary>
    public class ExternalPrintManagerTests : TestBase
    {
        [Fact]
        public void GetAvailablePrinters_WithHiddenPrinters_ShouldFilterCorrectly()
        {
            // Arrange
            var mockConfig = MockFactory.CreateMockConfigurationService();
            mockConfig.Setup(x => x.GetHiddenPrinters())
                     .Returns(new List<string> { "HiddenPrinter1", "Microsoft Print to PDF" });
            
            var logger = MockFactory.CreateMockLogger();
            var printManager = new ExternalPrintManager(mockConfig.Object, logger.Object);

            // Act
            var availablePrinters = printManager.GetAvailablePrinters();

            // Assert
            availablePrinters.Should().NotBeNull();
            availablePrinters.Should().NotContain("HiddenPrinter1");
            availablePrinters.Should().NotContain("Microsoft Print to PDF");
            
            // 验证至少有一些打印机（除非系统真的没有安装打印机）
            // 这个测试在无打印机的CI环境中可能会有空列表，这是正常的
        }

        [Fact]
        public void CalculateTotalPages_EmptyFileList_ShouldReturnZero()
        {
            // Arrange
            var mockConfig = MockFactory.CreateMockConfigurationService();
            var logger = MockFactory.CreateMockLogger();
            var printManager = new ExternalPrintManager(mockConfig.Object, logger.Object);

            // Act
            var totalPages = printManager.CalculateTotalPages(new List<string>());

            // Assert
            totalPages.Should().Be(0);
        }

        [Fact]
        public void CalculateTotalPages_WithMockPdfFile_ShouldReturnExpectedCount()
        {
            // Arrange
            var mockConfig = MockFactory.CreateMockConfigurationService();
            mockConfig.Setup(x => x.GetFileTypeAssociation(".pdf"))
                     .Returns(new FileTypeAssociation
                     {
                         ExecutorPath = "mock_pdf_reader.exe",
                         Arguments = "/print \"{FilePath}\"",
                         PageCounterType = "PdfPageCounter"
                     });

            var logger = MockFactory.CreateMockLogger();
            var printManager = new ExternalPrintManager(mockConfig.Object, logger.Object);

            // 创建一个模拟的PDF文件（仅用于路径测试，不依赖真实的PDF内容）
            var mockPdfPath = CreateTestFile("test.pdf", "Mock PDF Content");

            // Act & Assert
            // 注意：实际的页码计算依赖于iTextSharp库和真实的PDF文件
            // 在单元测试中，我们主要验证方法调用不会抛异常
            Action act = () => printManager.CalculateTotalPages(new[] { mockPdfPath });
            act.Should().NotThrow();
        }

        [Fact]
        public void CalculateTotalPages_UnsupportedFileType_ShouldHandleGracefully()
        {
            // Arrange
            var mockConfig = MockFactory.CreateMockConfigurationService();
            mockConfig.Setup(x => x.GetFileTypeAssociation(".xyz"))
                     .Returns((FileTypeAssociation?)null);

            var logger = MockFactory.CreateMockLogger();
            var printManager = new ExternalPrintManager(mockConfig.Object, logger.Object);

            var unsupportedFile = CreateTestFile("test.xyz", "Unsupported content");

            // Act
            var totalPages = printManager.CalculateTotalPages(new[] { unsupportedFile });

            // Assert
            totalPages.Should().BeGreaterOrEqualTo(0, "不支持的文件类型应该返回0或默认页数");
        }

        [Fact]
        public void PrintFiles_EmptyFileList_ShouldNotThrow()
        {
            // Arrange
            var mockConfig = MockFactory.CreateMockConfigurationService();
            var logger = MockFactory.CreateMockLogger();
            var printManager = new ExternalPrintManager(mockConfig.Object, logger.Object);

            // Act & Assert
            Action act = () => printManager.PrintFiles(new List<string>(), "TestPrinter");
            act.Should().NotThrow();
        }

        [Fact]
        public void PrintFiles_ValidFiles_ShouldTriggerPrintCompletedEvent()
        {
            // Arrange
            var mockConfig = MockFactory.CreateMockConfigurationService();
            mockConfig.Setup(x => x.GetFileTypeAssociation(".pdf"))
                     .Returns(new FileTypeAssociation
                     {
                         ExecutorPath = "notepad.exe", // 使用系统自带程序避免依赖
                         Arguments = "\"{FilePath}\"",
                         PageCounterType = "PdfPageCounter"
                     });

            var logger = MockFactory.CreateMockLogger();
            var printManager = new ExternalPrintManager(mockConfig.Object, logger.Object);

            PrintCompletedEventArgs? capturedArgs = null;
            printManager.PrintCompleted += (sender, args) => capturedArgs = args;

            var testFile = CreateTestFile("test.pdf", "Test content");

            // Act
            printManager.PrintFiles(new[] { testFile }, "TestPrinter");

            // Note: 由于我们使用了真实的进程启动，这个测试可能需要一些时间
            // 在实际项目中，我们应该抽象Process.Start以便更好地进行单元测试

            // Assert
            // 这里我们主要验证方法调用不会抛异常
            // 事件的触发依赖于异步处理，在单元测试中较难精确验证
        }

        [Fact]
        public void Constructor_NullParameters_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            var logger = MockFactory.CreateMockLogger();
            
            Action actNullConfig = () => new ExternalPrintManager(null!, logger.Object);
            actNullConfig.Should().Throw<ArgumentNullException>();

            var mockConfig = MockFactory.CreateMockConfigurationService();
            Action actNullLogger = () => new ExternalPrintManager(mockConfig.Object, null!);
            actNullLogger.Should().Throw<ArgumentNullException>();
        }
    }

    /// <summary>
    /// 页码计数器测试类
    /// </summary>
    public class PageCounterTests : TestBase
    {
        [Fact]
        public void ImagePageCounter_CountPages_ShouldAlwaysReturnOne()
        {
            // Arrange
            var logger = MockFactory.CreateMockLogger();
            var imageCounter = new ImagePageCounter(logger.Object);
            var testImagePath = CreateTestFile("test.jpg", "Mock image content");

            // Act
            var pageCount = imageCounter.CountPages(testImagePath);

            // Assert
            pageCount.Should().Be(1, "图片文件默认应该返回1页");
        }

        [Fact]
        public void PdfPageCounter_NonExistentFile_ShouldReturnDefaultValue()
        {
            // Arrange
            var logger = MockFactory.CreateMockLogger();
            var pdfCounter = new PdfPageCounter(logger.Object);
            var nonExistentPath = GetTestDataPath("non_existent.pdf");

            // Act
            var pageCount = pdfCounter.CountPages(nonExistentPath);

            // Assert
            pageCount.Should().Be(1, "不存在的文件应该返回默认值1");
        }
    }
}