using Xunit;
using FluentAssertions;
using TrayApp.Configuration;
using TrayApp.Core;
using TrayApp.Tests.Helpers;
using System.IO;
using System.Collections.Generic;

namespace TrayApp.Tests.Configuration
{
    /// <summary>
    /// JsonConfigurationService测试类
    /// </summary>
    public class JsonConfigurationServiceTests : TestBase
    {
        [Fact]
        public void Constructor_ValidConfigPath_ShouldLoadSuccessfully()
        {
            // Arrange
            var configPath = CreateTestConfigFile();
            var logger = TestMockFactory.CreateMockLogger();

            // Act & Assert
            Action act = () => new JsonConfigurationService(configPath, logger.Object);
            act.Should().NotThrow();
        }

        [Fact]
        public void GetWatchPath_FromValidConfig_ShouldReturnCorrectPath()
        {
            // Arrange
            var configPath = CreateTestConfigFile();
            var logger = TestMockFactory.CreateMockLogger();
            var configService = new JsonConfigurationService(configPath, logger.Object);

            // Act
            var watchPath = configService.GetWatchPath();

            // Assert
            watchPath.Should().NotBeNullOrEmpty();
            watchPath.Should().Contain("TestWatch");
        }

        [Fact]
        public void GetBatchTimeoutSeconds_FromValidConfig_ShouldReturnCorrectValue()
        {
            // Arrange
            var configPath = CreateTestConfigFile();
            var logger = TestMockFactory.CreateMockLogger();
            var configService = new JsonConfigurationService(configPath, logger.Object);

            // Act
            var timeout = configService.GetBatchTimeoutSeconds();

            // Assert
            timeout.Should().BePositive();
            timeout.Should().Be(3); // 默认配置值
        }

        [Fact]
        public void GetMonitoredFileTypes_FromValidConfig_ShouldReturnExpectedTypes()
        {
            // Arrange
            var configPath = CreateTestConfigFile();
            var logger = TestMockFactory.CreateMockLogger();
            var configService = new JsonConfigurationService(configPath, logger.Object);

            // Act
            var fileTypes = configService.GetMonitoredFileTypes();

            // Assert
            fileTypes.Should().NotBeNull();
            fileTypes.Should().Contain(".pdf");
            fileTypes.Should().Contain(".docx");
            fileTypes.Should().Contain(".jpg");
        }

        [Fact]
        public void GetHiddenPrinters_FromValidConfig_ShouldReturnHiddenList()
        {
            // Arrange
            var configPath = CreateTestConfigFile();
            var logger = TestMockFactory.CreateMockLogger();
            var configService = new JsonConfigurationService(configPath, logger.Object);

            // Act
            var hiddenPrinters = configService.GetHiddenPrinters();

            // Assert
            hiddenPrinters.Should().NotBeNull();
            hiddenPrinters.Should().Contain("TestHiddenPrinter");
        }

        [Fact]
        public void GetFileTypeAssociation_ExistingType_ShouldReturnAssociation()
        {
            // Arrange
            var configPath = CreateTestConfigFile();
            var logger = TestMockFactory.CreateMockLogger();
            var configService = new JsonConfigurationService(configPath, logger.Object);

            // Act
            var association = configService.GetFileTypeAssociation(".pdf");

            // Assert
            association.Should().NotBeNull();
            association!.ExecutorPath.Should().NotBeNullOrEmpty();
            association.PageCounterType.Should().Be("PdfPageCounter");
        }

        [Fact]
        public void GetFileTypeAssociation_NonExistingType_ShouldReturnNull()
        {
            // Arrange
            var configPath = CreateTestConfigFile();
            var logger = TestMockFactory.CreateMockLogger();
            var configService = new JsonConfigurationService(configPath, logger.Object);

            // Act
            var association = configService.GetFileTypeAssociation(".unknown");

            // Assert
            association.Should().BeNull();
        }

        [Fact]
        public void SaveSettings_ValidSettings_ShouldPersistToFile()
        {
            // Arrange
            var configPath = Path.Combine(TestDirectory, "save_test_config.json");
            var logger = TestMockFactory.CreateMockLogger();
            var testSettings = TestMockFactory.CreateTestAppSettings();
            testSettings.Monitoring.WatchPath = @"C:\ModifiedPath";

            var configService = new JsonConfigurationService(configPath, logger.Object);

            // Act
            configService.SaveSettings(testSettings);

            // Assert
            File.Exists(configPath).Should().BeTrue();
            
            // 验证保存的内容
            var reloadedService = new JsonConfigurationService(configPath, logger.Object);
            reloadedService.GetWatchPath().Should().Be(@"C:\ModifiedPath");
        }

        /// <summary>
        /// 创建测试配置文件
        /// </summary>
        private string CreateTestConfigFile()
        {
            var configPath = Path.Combine(TestDirectory, "test_config.json");
            var testSettings = TestMockFactory.CreateTestAppSettings();
            
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(testSettings, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(configPath, json);
            
            return configPath;
        }
    }
}