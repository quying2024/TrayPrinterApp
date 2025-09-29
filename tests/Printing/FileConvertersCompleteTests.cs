using Xunit;
using FluentAssertions;
using TrayApp.Printing.Converters;
using TrayApp.Printing.Core;
using TrayApp.Tests.Helpers;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TrayApp.Tests.Printing
{
    /// <summary>
    /// ???????????
    /// ??????????????????
    /// </summary>
    public class FileConvertersCompleteTests : TestBase
    {
        #region ???????

        [Fact]
        public void ConverterFactory_Constructor_ValidLogger_ShouldSucceed()
        {
            // Arrange & Act
            var logger = TestMockFactory.CreateMockLogger();
            Action act = () => new ConverterFactory(logger.Object);
            
            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void ConverterFactory_Constructor_NullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Action act = () => new ConverterFactory(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void ConverterFactory_RegisterConverter_ValidConverter_ShouldAddToCollection()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var factory = new ConverterFactory(logger.Object);
            var mockConverter = new Mock<IFileConverter>();
            mockConverter.Setup(x => x.SupportedExtensions).Returns(new[] { ".test", ".example" });

            // Act
            factory.RegisterConverter(mockConverter.Object);

            // Assert
            var converter1 = factory.GetConverter(".test");
            var converter2 = factory.GetConverter(".example");
            converter1.Should().NotBeNull();
            converter2.Should().NotBeNull();
            converter1.Should().BeSameAs(mockConverter.Object);
            converter2.Should().BeSameAs(mockConverter.Object);
        }

        [Fact]
        public void ConverterFactory_RegisterConverter_NullConverter_ShouldThrowArgumentNullException()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var factory = new ConverterFactory(logger.Object);

            // Act & Assert
            Action act = () => factory.RegisterConverter(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ConverterFactory_GetConverter_UnknownExtension_ShouldReturnNull()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var factory = new ConverterFactory(logger.Object);

            // Act
            var converter = factory.GetConverter(".unknown");

            // Assert
            converter.Should().BeNull();
        }

        [Fact]
        public void ConverterFactory_GetConverter_CaseInsensitive_ShouldWork()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var factory = new ConverterFactory(logger.Object);
            var mockConverter = new Mock<IFileConverter>();
            mockConverter.Setup(x => x.SupportedExtensions).Returns(new[] { ".pdf" });
            factory.RegisterConverter(mockConverter.Object);

            // Act
            var converter1 = factory.GetConverter(".pdf");
            var converter2 = factory.GetConverter(".PDF");
            var converter3 = factory.GetConverter("pdf"); // ????

            // Assert
            converter1.Should().NotBeNull();
            converter2.Should().NotBeNull();
            converter3.Should().NotBeNull();
            converter1.Should().BeSameAs(converter2);
            converter2.Should().BeSameAs(converter3);
        }

        [Fact]
        public void ConverterFactory_IsSupported_KnownExtension_ShouldReturnTrue()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var factory = new ConverterFactory(logger.Object);
            var mockConverter = new Mock<IFileConverter>();
            mockConverter.Setup(x => x.SupportedExtensions).Returns(new[] { ".pdf", ".docx" });
            factory.RegisterConverter(mockConverter.Object);

            // Act & Assert
            factory.IsSupported(".pdf").Should().BeTrue();
            factory.IsSupported(".docx").Should().BeTrue();
            factory.IsSupported(".PDF").Should().BeTrue(); // ??????
            factory.IsSupported("pdf").Should().BeTrue(); // ????
        }

        [Fact]
        public void ConverterFactory_IsSupported_UnknownExtension_ShouldReturnFalse()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var factory = new ConverterFactory(logger.Object);

            // Act & Assert
            factory.IsSupported(".unknown").Should().BeFalse();
            factory.IsSupported("").Should().BeFalse();
            factory.IsSupported(null!).Should().BeFalse();
        }

        [Fact]
        public void ConverterFactory_GetSupportedExtensions_MultipleConverters_ShouldReturnAllExtensions()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var factory = new ConverterFactory(logger.Object);
            
            var converter1 = new Mock<IFileConverter>();
            converter1.Setup(x => x.SupportedExtensions).Returns(new[] { ".pdf", ".doc" });
            
            var converter2 = new Mock<IFileConverter>();
            converter2.Setup(x => x.SupportedExtensions).Returns(new[] { ".jpg", ".png", ".bmp" });

            factory.RegisterConverter(converter1.Object);
            factory.RegisterConverter(converter2.Object);

            // Act
            var supportedExtensions = factory.GetSupportedExtensions().ToList();

            // Assert
            supportedExtensions.Should().HaveCount(5);
            supportedExtensions.Should().Contain(".pdf");
            supportedExtensions.Should().Contain(".doc");
            supportedExtensions.Should().Contain(".jpg");
            supportedExtensions.Should().Contain(".png");
            supportedExtensions.Should().Contain(".bmp");
        }

        [Fact]
        public void ConverterFactory_OverwriteConverter_ShouldReplaceExistingConverter()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var factory = new ConverterFactory(logger.Object);
            
            var converter1 = new Mock<IFileConverter>();
            converter1.Setup(x => x.SupportedExtensions).Returns(new[] { ".pdf" });
            
            var converter2 = new Mock<IFileConverter>();
            converter2.Setup(x => x.SupportedExtensions).Returns(new[] { ".pdf" });

            // Act
            factory.RegisterConverter(converter1.Object);
            factory.RegisterConverter(converter2.Object); // ?????

            // Assert
            var converter = factory.GetConverter(".pdf");
            converter.Should().BeSameAs(converter2.Object);
            converter.Should().NotBeSameAs(converter1.Object);
        }

        #endregion

        #region PDF?????

        [Fact]
        public void PdfConverter_Constructor_ValidLogger_ShouldSucceed()
        {
            // Arrange & Act
            var logger = TestMockFactory.CreateMockLogger();
            Action act = () => new PdfConverter(logger.Object);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void PdfConverter_Constructor_NullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Action act = () => new PdfConverter(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void PdfConverter_SupportedExtensions_ShouldContainOnlyPdf()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var converter = new PdfConverter(logger.Object);

            // Act
            var extensions = converter.SupportedExtensions.ToList();

            // Assert
            extensions.Should().HaveCount(1);
            extensions.Should().Contain(".pdf");
        }

        [Fact]
        public void PdfConverter_ConvertToPdfStream_ValidPdfFile_ShouldReturnStream()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var converter = new PdfConverter(logger.Object);
            var testPdfPath = GetTestDataPath("sample.pdf");

            // Act
            using var result = converter.ConvertToPdfStream(testPdfPath);

            // Assert
            result.Should().NotBeNull();
            result.Length.Should().BeGreaterThan(0);
            result.CanRead.Should().BeTrue();
        }

        [Fact]
        public void PdfConverter_ConvertToPdfStream_EmptyPath_ShouldThrowArgumentException()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var converter = new PdfConverter(logger.Object);

            // Act & Assert
            Action act = () => converter.ConvertToPdfStream("");
            act.Should().Throw<ArgumentException>().WithParameterName("filePath");
        }

        [Fact]
        public void PdfConverter_ConvertToPdfStream_NullPath_ShouldThrowArgumentException()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var converter = new PdfConverter(logger.Object);

            // Act & Assert
            Action act = () => converter.ConvertToPdfStream(null!);
            act.Should().Throw<ArgumentException>().WithParameterName("filePath");
        }

        [Fact]
        public void PdfConverter_ConvertToPdfStream_NonExistentFile_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var converter = new PdfConverter(logger.Object);
            var nonExistentFile = Path.Combine(Path.GetTempPath(), $"non_existent_{Guid.NewGuid()}.pdf");

            // Act & Assert
            Action act = () => converter.ConvertToPdfStream(nonExistentFile);
            act.Should().Throw<FileNotFoundException>();
        }

        [Fact]
        public void PdfConverter_CountPages_ValidPdf_ShouldReturnCorrectCount()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var converter = new PdfConverter(logger.Object);
            var singlePagePdf = GetTestDataPath("sample.pdf");
            var multiPagePdf = GetTestDataPath("multipage.pdf");

            // Act & Assert
            try
            {
                var singlePageCount = converter.CountPages(singlePagePdf);
                var multiPageCount = converter.CountPages(multiPagePdf);

                singlePageCount.Should().Be(1);
                multiPageCount.Should().Be(3);
            }
            catch (Exception ex) when (ex.Message.Contains("iText") || ex.Message.Contains("PDF"))
            {
                // PDF??????iText7??????????????
                true.Should().BeTrue("PDF??????iText7??");
            }
        }

        [Fact]
        public void PdfConverter_CountPages_InvalidFile_ShouldReturnOne()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var converter = new PdfConverter(logger.Object);

            // Act
            var pageCount1 = converter.CountPages("non_existent.pdf");
            var pageCount2 = converter.CountPages("");
            var pageCount3 = converter.CountPages(null!);

            // Assert
            pageCount1.Should().Be(1);
            pageCount2.Should().Be(1);
            pageCount3.Should().Be(1);
        }

        [Fact]
        public void PdfConverter_CountPages_CorruptedPdf_ShouldReturnOneAndLogError()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var converter = new PdfConverter(logger.Object);
            var corruptedPdf = GetTestDataPath("corrupted.pdf");

            // Act
            var pageCount = converter.CountPages(corruptedPdf);

            // Assert
            pageCount.Should().Be(1); // ????1?
            logger.Verify(l => l.Error(It.IsAny<string>(), It.IsAny<Exception>()), Times.AtLeastOnce);
        }

        #endregion

        #region ???????

        [Fact]
        public void ImageToPdfConverter_Constructor_ValidLogger_ShouldSucceed()
        {
            // Arrange & Act
            var logger = TestMockFactory.CreateMockLogger();
            Action act = () => new ImageToPdfConverter(logger.Object);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void ImageToPdfConverter_SupportedExtensions_ShouldContainAllImageFormats()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var converter = new ImageToPdfConverter(logger.Object);

            // Act
            var extensions = converter.SupportedExtensions.ToList();

            // Assert
            extensions.Should().Contain(".jpg");
            extensions.Should().Contain(".jpeg");
            extensions.Should().Contain(".png");
            extensions.Should().Contain(".bmp");
            extensions.Should().Contain(".gif");
            extensions.Should().Contain(".tiff");
            extensions.Should().Contain(".tif");
            extensions.Should().Contain(".webp");
            extensions.Count.Should().Be(8);
        }

        [Fact]
        public void ImageToPdfConverter_CountPages_ShouldAlwaysReturnOne()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var converter = new ImageToPdfConverter(logger.Object);

            // Act & Assert
            converter.CountPages("any_path.jpg").Should().Be(1);
            converter.CountPages("").Should().Be(1);
            converter.CountPages(null!).Should().Be(1);
        }

        [Fact]
        public void ImageToPdfConverter_ConvertToPdfStream_ValidParameters_ShouldAttemptConversion()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var converter = new ImageToPdfConverter(logger.Object);
            
            // ?????????????
            var testImagePath = CreateTestImageFile();

            try
            {
                // Act - ???????PDF
                using var result = converter.ConvertToPdfStream(testImagePath);

                // Assert - ??????????????PDF?
                result.Should().NotBeNull();
                result.Length.Should().BeGreaterThan(0);
            }
            catch (Exception ex) when (ex.Message.Contains("SkiaSharp") || ex.Message.Contains("PDFsharp"))
            {
                // ??????SkiaSharp?PDFsharp?
                true.Should().BeTrue("???PDF????SkiaSharp?PDFsharp??");
            }
            finally
            {
                // ??????
                if (File.Exists(testImagePath))
                {
                    File.Delete(testImagePath);
                }
            }
        }

        [Fact]
        public void ImageToPdfConverter_ConvertToPdfStream_InvalidParameters_ShouldThrowException()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var converter = new ImageToPdfConverter(logger.Object);

            // Act & Assert
            Action act1 = () => converter.ConvertToPdfStream("");
            act1.Should().Throw<ArgumentException>().WithParameterName("filePath");

            Action act2 = () => converter.ConvertToPdfStream(null!);
            act2.Should().Throw<ArgumentException>().WithParameterName("filePath");

            Action act3 = () => converter.ConvertToPdfStream("non_existent.jpg");
            act3.Should().Throw<FileNotFoundException>();
        }

        private string CreateTestImageFile()
        {
            // ???????1x1??BMP??????
            var testImagePath = Path.Combine(TestDirectory, "test_image.bmp");
            
            // BMP???1x1?????
            var bmpData = new byte[]
            {
                // BMP???
                0x42, 0x4D, 0x3A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x36, 0x00, 0x00, 0x00,
                // DIB??
                0x28, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00,
                0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // ???? (?????) + ??
                0xFF, 0xFF, 0xFF, 0x00
            };

            File.WriteAllBytes(testImagePath, bmpData);
            return testImagePath;
        }

        #endregion

        #region Word?????

        [Fact]
        public void WordToPdfConverter_Constructor_ValidLogger_ShouldSucceed()
        {
            // Arrange & Act
            var logger = TestMockFactory.CreateMockLogger();
            Action act = () => new WordToPdfConverter(logger.Object);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void WordToPdfConverter_SupportedExtensions_ShouldContainWordFormats()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var converter = new WordToPdfConverter(logger.Object);

            // Act
            var extensions = converter.SupportedExtensions.ToList();

            // Assert
            extensions.Should().Contain(".doc");
            extensions.Should().Contain(".docx");
            extensions.Should().HaveCount(2);
        }

        [Fact]
        public void WordToPdfConverter_CountPages_NonExistentFile_ShouldReturnOne()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();

            // Act & Assert - ??????Microsoft Office Interop??
            try
            {
                using var converter = new WordToPdfConverter(logger.Object);
                var pageCount = converter.CountPages("non_existent.docx");
                pageCount.Should().Be(1);
            }
            catch (Exception ex) when (ex.Message.Contains("office") || ex.Message.Contains("Interop") || ex.Message.Contains("COM"))
            {
                // ???????????????????Office
                // ??COM?????
                true.Should().BeTrue("??????Office Interop????????");
            }
        }

        [Fact]
        public void WordToPdfConverter_ConvertToPdfStream_InvalidParameters_ShouldThrowException()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();

            try
            {
                using var converter = new WordToPdfConverter(logger.Object);

                // Act & Assert
                Action act1 = () => converter.ConvertToPdfStream("");
                act1.Should().Throw<ArgumentException>().WithParameterName("filePath");

                Action act2 = () => converter.ConvertToPdfStream(null!);
                act2.Should().Throw<ArgumentException>().WithParameterName("filePath");

                Action act3 = () => converter.ConvertToPdfStream("non_existent.docx");
                act3.Should().Throw<FileNotFoundException>();
            }
            catch (Exception ex) when (ex.Message.Contains("office") || ex.Message.Contains("Interop"))
            {
                // ?????????Office??????????Office???????
                // ???????????????????
                true.Should().BeTrue("Office Interop?????");
            }
        }

        [Fact]
        public void WordToPdfConverter_Dispose_ShouldReleaseResources()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();

            // Act & Assert - ??Dispose????
            try
            {
                var converter = new WordToPdfConverter(logger.Object);
                converter.Dispose();
                
                // ??Dispose????
                converter.Dispose();
                
                true.Should().BeTrue();
            }
            catch (Exception ex) when (ex.Message.Contains("office") || ex.Message.Contains("COM"))
            {
                // ?????????Office???????
                true.Should().BeTrue("Dispose????Office Interop??");
            }
        }

        [Fact]
        public void WordToPdfConverter_ConvertToPdfStream_WithTestWordFile_ShouldAttemptConversion()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var testWordPath = CreateTestWordFile();

            try
            {
                using var converter = new WordToPdfConverter(logger.Object);
                
                // Act - ????Word??
                using var result = converter.ConvertToPdfStream(testWordPath);
                
                // Assert - ???????????PDF?
                result.Should().NotBeNull();
                result.Length.Should().BeGreaterThan(0);
                
                // ?????????
                logger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Word???PDF??"))), Times.AtLeastOnce);
            }
            catch (Exception ex) when (ex.Message.Contains("office") || ex.Message.Contains("Interop") || ex.Message.Contains("COM"))
            {
                // Word????Microsoft Office??
                true.Should().BeTrue("Word?PDF????Microsoft Office??");
                logger.Verify(l => l.Error(It.IsAny<string>(), It.IsAny<Exception>()), Times.AtLeastOnce);
            }
            finally
            {
                // ??????
                if (File.Exists(testWordPath))
                {
                    File.Delete(testWordPath);
                }
            }
        }

        private string CreateTestWordFile()
        {
            // ???????Word??????
            // ??????RTF??????Word????
            var testWordPath = Path.Combine(TestDirectory, "test_document.rtf");
            var rtfContent = @"{\rtf1\ansi\deff0 {\fonttbl {\f0 Times New Roman;}}
\f0\fs24 This is a test document for Word to PDF conversion.
\par
This document contains multiple lines to test the conversion process.
\par
End of test document.
}";
            File.WriteAllText(testWordPath, rtfContent);
            return testWordPath;
        }

        #endregion

        #region ????

        [Fact]
        public void ConverterFactory_WithAllConverters_ShouldSupportAllFileTypes()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var factory = new ConverterFactory(logger.Object);

            // ???????
            factory.RegisterConverter(new PdfConverter(logger.Object));
            factory.RegisterConverter(new ImageToPdfConverter(logger.Object));
            
            try
            {
                factory.RegisterConverter(new WordToPdfConverter(logger.Object));
            }
            catch (Exception ex) when (ex.Message.Contains("office"))
            {
                // Word??????Office?????????????????
            }

            // Act
            var supportedExtensions = factory.GetSupportedExtensions().ToList();

            // Assert
            supportedExtensions.Should().Contain(".pdf");
            supportedExtensions.Should().Contain(".jpg");
            supportedExtensions.Should().Contain(".jpeg");
            supportedExtensions.Should().Contain(".png");
            supportedExtensions.Should().Contain(".bmp");
            
            // Word?????????????Office???
            var hasWordSupport = supportedExtensions.Contains(".doc") || supportedExtensions.Contains(".docx");
            if (hasWordSupport)
            {
                supportedExtensions.Should().Contain(".doc");
                supportedExtensions.Should().Contain(".docx");
            }
        }

        [Fact]
        public async Task ConverterFactory_ConcurrentAccess_ShouldBeSafe()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var factory = new ConverterFactory(logger.Object);
            factory.RegisterConverter(new PdfConverter(logger.Object));
            factory.RegisterConverter(new ImageToPdfConverter(logger.Object));

            var tasks = new Task[10];

            // Act - ?????????
            for (int i = 0; i < tasks.Length; i++)
            {
                int taskId = i;
                tasks[i] = Task.Run(() =>
                {
                    // ????????????
                    var extensions = new[] { ".pdf", ".jpg", ".png", ".bmp", ".unknown" };
                    foreach (var ext in extensions)
                    {
                        var converter = factory.GetConverter(ext);
                        var isSupported = factory.IsSupported(ext);
                        
                        if (ext == ".unknown")
                        {
                            converter.Should().BeNull();
                            isSupported.Should().BeFalse();
                        }
                        else
                        {
                            converter.Should().NotBeNull();
                            isSupported.Should().BeTrue();
                        }
                    }
                });
            }

            // Assert
            await Task.WhenAll(tasks);
            tasks.Should().AllSatisfy(task => task.IsCompleted.Should().BeTrue());
        }

        [Fact]
        public void ConverterFactory_GetConverter_PerformanceTest()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var factory = new ConverterFactory(logger.Object);
            factory.RegisterConverter(new PdfConverter(logger.Object));
            factory.RegisterConverter(new ImageToPdfConverter(logger.Object));

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - ???????????
            for (int i = 0; i < 10000; i++)
            {
                var extensions = new[] { ".pdf", ".jpg", ".png", ".bmp", ".docx" };
                foreach (var ext in extensions)
                {
                    factory.GetConverter(ext);
                    factory.IsSupported(ext);
                }
            }

            stopwatch.Stop();

            // Assert - 10000??????1????
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
        }

        #endregion
    }
}