using Xunit;
using FluentAssertions;
using TrayApp.Printing;
using TrayApp.Core;
using TrayApp.Tests.Helpers;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrayApp.Tests.Printing
{
    /// <summary>
    /// ?????????
    /// ????UnifiedPrintManager??
    /// </summary>
    public class UnifiedPrintManagerTests : TestBase
    {
        [Fact]
        public void Constructor_ValidParameters_ShouldInitializeSuccessfully()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();

            // Act & Assert
            Action act = () => new UnifiedPrintManager(mockConfig.Object, mockLogger.Object);
            act.Should().NotThrow();
        }

        [Fact]
        public void Constructor_NullConfigurationService_ShouldThrowArgumentNullException()
        {
            // Arrange
            var mockLogger = TestMockFactory.CreateMockLogger();

            // Act & Assert
            Action act = () => new UnifiedPrintManager(null!, mockLogger.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("configurationService");
        }

        [Fact]
        public void Constructor_NullLogger_ShouldThrowArgumentNullException()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();

            // Act & Assert
            Action act = () => new UnifiedPrintManager(mockConfig.Object, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void GetAvailablePrinters_ShouldReturnFilteredPrinterList()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();
            
            mockConfig.Setup(x => x.GetHiddenPrinters())
                     .Returns(new List<string> { "Microsoft Print to PDF" });

            using var printManager = new UnifiedPrintManager(mockConfig.Object, mockLogger.Object);

            // Act
            var printers = printManager.GetAvailablePrinters();

            // Assert
            printers.Should().NotBeNull();
            printers.Should().NotContain("Microsoft Print to PDF");
        }

        [Fact]
        public void CalculateTotalPages_EmptyFileList_ShouldReturnZero()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();

            using var printManager = new UnifiedPrintManager(mockConfig.Object, mockLogger.Object);

            // Act
            var totalPages = printManager.CalculateTotalPages(new List<string>());

            // Assert
            totalPages.Should().Be(0);
        }

        [Fact]
        public void CalculateTotalPages_NullFileList_ShouldThrowArgumentNullException()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();

            using var printManager = new UnifiedPrintManager(mockConfig.Object, mockLogger.Object);

            // Act & Assert
            Action act = () => printManager.CalculateTotalPages(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void PrintFiles_EmptyFileList_ShouldComplete()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();

            using var printManager = new UnifiedPrintManager(mockConfig.Object, mockLogger.Object);

            // Act & Assert
            Action act = () => printManager.PrintFiles(new List<string>(), "TestPrinter");
            act.Should().NotThrow();
        }

        [Fact]
        public void PrintFiles_NullFileList_ShouldThrowArgumentNullException()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();

            using var printManager = new UnifiedPrintManager(mockConfig.Object, mockLogger.Object);

            // Act & Assert
            Action act = () => printManager.PrintFiles(null!, "TestPrinter");
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void PrintFiles_EmptyPrinterName_ShouldThrowArgumentException()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();

            using var printManager = new UnifiedPrintManager(mockConfig.Object, mockLogger.Object);

            // Act & Assert
            Action act = () => printManager.PrintFiles(new List<string> { "test.pdf" }, "");
            act.Should().Throw<ArgumentException>().WithParameterName("printerName");
        }

        [Fact]
        public void GetSupportedFileTypes_ShouldReturnExpectedTypes()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();

            using var printManager = new UnifiedPrintManager(mockConfig.Object, mockLogger.Object);

            // Act
            var supportedTypes = printManager.GetSupportedFileTypes().ToList();

            // Assert
            supportedTypes.Should().NotBeEmpty();
            supportedTypes.Should().Contain(".pdf");
            supportedTypes.Should().Contain(".jpg");
            supportedTypes.Should().Contain(".docx");
        }

        [Fact]
        public void IsFileTypeSupported_PdfFile_ShouldReturnTrue()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();

            using var printManager = new UnifiedPrintManager(mockConfig.Object, mockLogger.Object);

            // Act
            var isSupported = printManager.IsFileTypeSupported(".pdf");

            // Assert
            isSupported.Should().BeTrue();
        }

        [Fact]
        public void IsFileTypeSupported_UnsupportedFile_ShouldReturnFalse()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();

            using var printManager = new UnifiedPrintManager(mockConfig.Object, mockLogger.Object);

            // Act
            var isSupported = printManager.IsFileTypeSupported(".xyz");

            // Assert
            isSupported.Should().BeFalse();
        }

        [Fact]
        public void PrintCompleted_Event_ShouldBeTriggerable()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();

            using var printManager = new UnifiedPrintManager(mockConfig.Object, mockLogger.Object);

            bool eventTriggered = false;
            PrintCompletedEventArgs? eventArgs = null;

            printManager.PrintCompleted += (sender, args) =>
            {
                eventTriggered = true;
                eventArgs = args;
            };

            // Act - ???????????
            var eventField = typeof(UnifiedPrintManager).GetField("PrintCompleted", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (eventField?.GetValue(printManager) is EventHandler<PrintCompletedEventArgs> handler)
            {
                var testEventArgs = new PrintCompletedEventArgs
                {
                    Success = true,
                    FilePaths = new List<string> { "test.pdf" },
                    PrinterName = "TestPrinter",
                    TotalPages = 1
                };
                handler.Invoke(printManager, testEventArgs);
            }

            // Assert
            eventTriggered.Should().BeTrue();
            eventArgs.Should().NotBeNull();
            eventArgs!.Success.Should().BeTrue();
            eventArgs.FileCount.Should().Be(1);
        }

        [Fact]
        public void Dispose_ShouldReleaseResources()
        {
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();

            var printManager = new UnifiedPrintManager(mockConfig.Object, mockLogger.Object);

            // Act & Assert
            Action act = () => printManager.Dispose();
            act.Should().NotThrow();

            // ????Dispose??????
            Action act2 = () => printManager.Dispose();
            act2.Should().NotThrow();
        }
    }
}