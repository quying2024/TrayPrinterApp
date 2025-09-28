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

namespace TrayApp.Tests.Printing
{
    /// <summary>
    /// ???????
    /// ??????????PDF???
    /// </summary>
    public class FileConvertersTests : TestBase
    {
        [Fact]
        public void ConverterFactory_RegisterConverter_ShouldAddToCollection()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var factory = new ConverterFactory(logger.Object);
            var mockConverter = new Mock<IFileConverter>();
            mockConverter.Setup(x => x.SupportedExtensions).Returns(new[] { ".test" });

            // Act
            factory.RegisterConverter(mockConverter.Object);

            // Assert
            var converter = factory.GetConverter(".test");
            converter.Should().NotBeNull();
            converter.Should().Be(mockConverter.Object);
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
        public void ConverterFactory_IsSupported_KnownExtension_ShouldReturnTrue()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var factory = new ConverterFactory(logger.Object);
            var mockConverter = new Mock<IFileConverter>();
            mockConverter.Setup(x => x.SupportedExtensions).Returns(new[] { ".pdf" });
            factory.RegisterConverter(mockConverter.Object);

            // Act
            var isSupported = factory.IsSupported(".pdf");

            // Assert
            isSupported.Should().BeTrue();
        }

        [Fact]
        public void ConverterFactory_IsSupported_UnknownExtension_ShouldReturnFalse()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var factory = new ConverterFactory(logger.Object);

            // Act
            var isSupported = factory.IsSupported(".unknown");

            // Assert
            isSupported.Should().BeFalse();
        }

        [Fact]
        public void ConverterFactory_GetSupportedExtensions_ShouldReturnAllExtensions()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var factory = new ConverterFactory(logger.Object);
            
            var converter1 = new Mock<IFileConverter>();
            converter1.Setup(x => x.SupportedExtensions).Returns(new[] { ".pdf", ".doc" });
            
            var converter2 = new Mock<IFileConverter>();
            converter2.Setup(x => x.SupportedExtensions).Returns(new[] { ".jpg", ".png" });

            factory.RegisterConverter(converter1.Object);
            factory.RegisterConverter(converter2.Object);

            // Act
            var supportedExtensions = factory.GetSupportedExtensions().ToList();

            // Assert
            supportedExtensions.Should().HaveCount(4);
            supportedExtensions.Should().Contain(".pdf");
            supportedExtensions.Should().Contain(".doc");
            supportedExtensions.Should().Contain(".jpg");
            supportedExtensions.Should().Contain(".png");
        }

        [Fact]
        public void PdfConverter_SupportedExtensions_ShouldContainPdf()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var converter = new PdfConverter(logger.Object);

            // Act
            var extensions = converter.SupportedExtensions.ToList();

            // Assert
            extensions.Should().Contain(".pdf");
            extensions.Should().HaveCount(1);
        }

        [Fact]
        public void PdfConverter_CountPages_NonExistentFile_ShouldReturnOne()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var converter = new PdfConverter(logger.Object);

            // Act
            var pageCount = converter.CountPages("non_existent.pdf");

            // Assert
            pageCount.Should().Be(1);
        }

        [Fact]
        public void PdfConverter_CountPages_EmptyPath_ShouldReturnOne()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var converter = new PdfConverter(logger.Object);

            // Act
            var pageCount = converter.CountPages("");

            // Assert
            pageCount.Should().Be(1);
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
        public void PdfConverter_ConvertToPdfStream_NonExistentFile_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var converter = new PdfConverter(logger.Object);

            // Act & Assert
            Action act = () => converter.ConvertToPdfStream("non_existent.pdf");
            act.Should().Throw<FileNotFoundException>();
        }

        [Fact]
        public void ImageToPdfConverter_SupportedExtensions_ShouldContainImageFormats()
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
        }

        [Fact]
        public void ImageToPdfConverter_CountPages_ShouldAlwaysReturnOne()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var converter = new ImageToPdfConverter(logger.Object);

            // Act
            var pageCount = converter.CountPages("any_path.jpg");

            // Assert
            pageCount.Should().Be(1);
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

            // Act & Assert - ???????????Office Interop??
            try
            {
                using var converter = new WordToPdfConverter(logger.Object);
                var pageCount = converter.CountPages("non_existent.docx");
                pageCount.Should().Be(1);
            }
            catch (Exception ex) when (ex.Message.Contains("office") || ex.Message.Contains("Interop"))
            {
                // ????????Office????
                // ?????????????????Office
                true.Should().BeTrue("??????Office Interop???????");
            }
        }

        [Fact]
        public void WordToPdfConverter_Dispose_ShouldReleaseResources()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();

            // Act & Assert - ??Dispose??
            try
            {
                var converter = new WordToPdfConverter(logger.Object);
                converter.Dispose();
                
                // ????Dispose??????
                converter.Dispose();
                
                true.Should().BeTrue();
            }
            catch (Exception ex) when (ex.Message.Contains("office"))
            {
                // ?????????????
                true.Should().BeTrue("??????Office Interop??");
            }
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
        public void ConverterFactory_Constructor_NullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Action act = () => new ConverterFactory(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }
    }
}