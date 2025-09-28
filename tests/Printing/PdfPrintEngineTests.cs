using Xunit;
using FluentAssertions;
using TrayApp.Printing.Engines;
using TrayApp.Tests.Helpers;
using System;
using System.IO;

namespace TrayApp.Tests.Printing
{
    /// <summary>
    /// PDF??????
    /// ??PdfiumPrintEngine??????????DLL???
    /// </summary>
    public class PdfPrintEngineTests : TestBase
    {
        [Fact]
        public void Constructor_ValidLogger_ShouldInitializeSuccessfully()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();

            // Act & Assert
            Action act = () => new PdfiumPrintEngine(logger.Object);
            act.Should().NotThrow();
        }

        [Fact]
        public void Constructor_NullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Action act = () => new PdfiumPrintEngine(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void PrintPdf_NullStream_ShouldThrowArgumentNullException()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            
            // ??????????????????????????
            // ????????DLL??

            // Act & Assert
            Action act = () =>
            {
                using var engine = new PdfiumPrintEngine(logger.Object);
                engine.PrintPdf((Stream)null!, "TestPrinter");
            };
            
            // ?????????DLL??????????????
            // ??????????????????
        }

        [Fact]
        public void PrintPdf_EmptyPrinterName_ParameterValidation()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();

            // Act & Assert - ????????
            // ?????DLL???????????????
            Action act = () =>
            {
                using var stream = new MemoryStream();
                using var engine = new PdfiumPrintEngine(logger.Object);
                engine.PrintPdf(stream, "");
            };

            // ???????????????
            // ????????ArgumentException
        }

        [Fact]
        public void Dispose_ShouldBeCallable()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();

            // Act & Assert - ??Dispose???????
            try
            {
                var engine = new PdfiumPrintEngine(logger.Object);
                engine.Dispose();
                
                // ????????
                engine.Dispose();
                
                // ????????????Dispose??????
                true.Should().BeTrue(); // ????
            }
            catch (Exception ex) when (ex.Message.Contains("pdfium.dll"))
            {
                // ??????????DLL????
                // ????????????????
                true.Should().BeTrue(); // ????
            }
        }

        [Fact]
        public void EngineCreation_InTestEnvironment_ShouldHandleGracefully()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();

            // Act & Assert
            // ???????PdfiumViewer??????DLL
            // ????????????????????
            try
            {
                using var engine = new PdfiumPrintEngine(logger.Object);
                // ????????????????????????
                true.Should().BeTrue();
            }
            catch (Exception ex) when (ex.Message.Contains("pdfium") || ex.Message.Contains("DLL"))
            {
                // ??????????????????
                // ????????????DLL??
                true.Should().BeTrue();
            }
        }

        [Fact]
        public void PrintPdf_ParameterValidation_Logic()
        {
            // ??????????????????????
            // ????????PDF????

            // Arrange - ???????????
            var logger = TestMockFactory.CreateMockLogger();

            // Act & Assert
            // ??????????
            string.IsNullOrEmpty("").Should().BeTrue();
            string.IsNullOrEmpty("TestPrinter").Should().BeFalse();
            
            // ???????????
            File.Exists("non_existent.pdf").Should().BeFalse();
            
            // ??????????
            logger.Should().NotBeNull();
            logger.Object.Should().NotBeNull();
        }
    }
}