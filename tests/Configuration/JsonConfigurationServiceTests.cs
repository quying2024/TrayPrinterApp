using Xunit;
using FluentAssertions;
using TrayApp.Configuration;
using TrayApp.Core;
using TrayApp.Tests.Helpers;
using Newtonsoft.Json;
using System.IO;

namespace TrayApp.Tests.Configuration
{
    /// <summary>
    /// JsonConfigurationService测试类
    /// </summary>
    public class JsonConfigurationServiceTests : TestBase
    {
        [Fact]
        public void LoadConfiguration_ValidConfigFile_ShouldLoadCorrectly()
        {
            // Arrange
            var testConfig = @"{
                ""Monitoring"": {
                    ""WatchPath"": ""C:\\TestFolder"",
                    ""BatchTimeoutSeconds"": 5,
                    ""FileTypes"": ["".pdf"", "".docx""]
                },
                ""PrinterManagement"": {
                    ""HiddenPrinters"": [""TestPrinter1"", ""TestPrinter2""],
                    ""DisplayOrder"": ""UsageFrequency""
                }
            }";
            
            var configPath = CreateTestConfigFile(testConfig);
            var logger = MockFactory.CreateMockLogger();

            // Act
            var configService = new JsonConfigurationService(configPath, logger.Object);
            var settings = configService.GetSettings();

            // Assert
            settings.Should().NotBeNull();
            settings.Monitoring.WatchPath.Should().Be("C:\\TestFolder");
            settings.Monitoring.BatchTimeoutSeconds.Should().Be(5);
            settings.Monitoring.FileTypes.Should().Contain(".pdf").And.Contain(".docx");
            settings.PrinterManagement.HiddenPrinters.Should().Contain("TestPrinter1").And.Contain("TestPrinter2");
        }

        [Fact]
        public void LoadConfiguration_NonExistentFile_ShouldCreateDefaultConfig()
        {
            // Arrange
            var nonExistentPath = GetTestDataPath("non_existent_config.json");
            var logger = MockFactory.CreateMockLogger();

            // Act
            var configService = new JsonConfigurationService(nonExistentPath, logger.Object);
            var settings = configService.GetSettings();

            // Assert
            settings.Should().NotBeNull();
            File.Exists(nonExistentPath).Should().BeTrue("配置文件应该被自动创建");
            settings.Monitoring.FileTypes.Should().NotBeEmpty("默认配置应包含文件类型");
        }

        [Fact]
        public void LoadConfiguration_MalformedJson_ShouldFallbackToDefault()
        {
            // Arrange
            var malformedConfig = @"{ ""Monitoring"": { ""WatchPath"":"; // 故意的格式错误
            var configPath = CreateTestConfigFile(malformedConfig);
            var logger = MockFactory.CreateMockLogger();

            // Act
            var configService = new JsonConfigurationService(configPath, logger.Object);
            var settings = configService.GetSettings();

            // Assert
            settings.Should().NotBeNull();
            settings.Monitoring.FileTypes.Should().NotBeEmpty("应该回退到默认配置");
        }

        [Fact]
        public void GetHiddenPrinters_ShouldReturnConfiguredPrinters()
        {
            // Arrange
            var testConfig = @"{
                ""PrinterManagement"": {
                    ""HiddenPrinters"": [""HiddenPrinter1"", ""HiddenPrinter2""]
                }
            }";
            
            var configPath = CreateTestConfigFile(testConfig);
            var logger = MockFactory.CreateMockLogger();
            var configService = new JsonConfigurationService(configPath, logger.Object);

            // Act
            var hiddenPrinters = configService.GetHiddenPrinters();

            // Assert
            hiddenPrinters.Should().Contain("HiddenPrinter1").And.Contain("HiddenPrinter2");
            hiddenPrinters.Should().HaveCount(2);
        }

        [Fact]
        public void GetFileTypeAssociation_ExistingType_ShouldReturnAssociation()
        {
            // Arrange
            var testConfig = @"{
                ""FileTypeAssociations"": {
                    "".pdf"": {
                        ""ExecutorPath"": ""test_reader.exe"",
                        ""Arguments"": ""/print \""{FilePath}\"""",
                        ""PageCounterType"": ""PdfPageCounter""
                    }
                }
            }";
            
            var configPath = CreateTestConfigFile(testConfig);
            var logger = MockFactory.CreateMockLogger();
            var configService = new JsonConfigurationService(configPath, logger.Object);

            // Act
            var association = configService.GetFileTypeAssociation(".pdf");

            // Assert
            association.Should().NotBeNull();
            association!.ExecutorPath.Should().Be("test_reader.exe");
            association.PageCounterType.Should().Be("PdfPageCounter");
        }

        [Fact]
        public void GetFileTypeAssociation_NonExistentType_ShouldReturnNull()
        {
            // Arrange
            var logger = MockFactory.CreateMockLogger();
            var configService = new JsonConfigurationService(GetTestDataPath("empty_config.json"), logger.Object);

            // Act
            var association = configService.GetFileTypeAssociation(".xyz");

            // Assert
            association.Should().BeNull();
        }

        [Fact]
        public void SaveSettings_ValidSettings_ShouldPersistCorrectly()
        {
            // Arrange
            var configPath = GetTestDataPath("save_test_config.json");
            var logger = MockFactory.CreateMockLogger();
            var configService = new JsonConfigurationService(configPath, logger.Object);
            
            var newSettings = MockFactory.CreateTestAppSettings();
            newSettings.Monitoring.WatchPath = "C:\\NewTestPath";

            // Act
            configService.SaveSettings(newSettings);
            
            // 重新加载验证
            var reloadedService = new JsonConfigurationService(configPath, logger.Object);
            var reloadedSettings = reloadedService.GetSettings();

            // Assert
            reloadedSettings.Monitoring.WatchPath.Should().Be("C:\\NewTestPath");
            File.Exists(configPath).Should().BeTrue();
        }
    }
}