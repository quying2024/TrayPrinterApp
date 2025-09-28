using Xunit;
using FluentAssertions;
using TrayApp.Configuration;
using TrayApp.Core;
using TrayApp.Tests.Helpers;
using System;
using System.IO;

namespace TrayApp.Tests.Configuration
{
    /// <summary>
    /// JSON配置服务测试类
    /// 测试配置文件的读取、写入和默认值处理
    /// </summary>
    public class JsonConfigurationServiceTests : TestBase
    {
        [Fact]
        public void Constructor_ValidConfigFile_ShouldLoadSuccessfully()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var configFileName = $"test_config_{Guid.NewGuid()}.json";

            // Act
            var configService = new JsonConfigurationService(configFileName, logger.Object);

            // Assert
            configService.Should().NotBeNull();
            configService.GetSettings().Should().NotBeNull();
        }

        [Fact]
        public void GetWatchPath_DefaultConfig_ShouldReturnValidPath()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var configFileName = $"watch_path_test_{Guid.NewGuid()}.json";
            var configService = new JsonConfigurationService(configFileName, logger.Object);

            // Act
            var watchPath = configService.GetWatchPath();

            // Assert
            watchPath.Should().NotBeNullOrEmpty();
            watchPath.Should().EndWith("PrintJobs");
        }

        [Fact]
        public void GetBatchTimeoutSeconds_DefaultConfig_ShouldReturnThreeSeconds()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var configFileName = $"timeout_test_{Guid.NewGuid()}.json";
            var configService = new JsonConfigurationService(configFileName, logger.Object);

            // Act
            var timeout = configService.GetBatchTimeoutSeconds();

            // Assert
            timeout.Should().Be(3);
        }

        [Fact]
        public void GetMonitoredFileTypes_DefaultConfig_ShouldReturnSupportedTypes()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var configFileName = $"file_types_test_{Guid.NewGuid()}.json";
            var configService = new JsonConfigurationService(configFileName, logger.Object);

            // Act
            var fileTypes = configService.GetMonitoredFileTypes();

            // Assert
            fileTypes.Should().NotBeEmpty();
            fileTypes.Should().Contain(".pdf");
            fileTypes.Should().Contain(".docx");
            fileTypes.Should().Contain(".doc");
            fileTypes.Should().Contain(".jpg");
            fileTypes.Should().Contain(".png");
        }

        [Fact]
        public void GetHiddenPrinters_DefaultConfig_ShouldReturnDefaultHiddenPrinters()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var configFileName = $"hidden_printers_test_{Guid.NewGuid()}.json";
            var configService = new JsonConfigurationService(configFileName, logger.Object);

            // Act
            var hiddenPrinters = configService.GetHiddenPrinters();

            // Assert
            hiddenPrinters.Should().NotBeEmpty();
            hiddenPrinters.Should().Contain("Microsoft Print to PDF");
            hiddenPrinters.Should().Contain("Fax");
        }

        [Fact]
        public void GetPrintSettings_DefaultConfig_ShouldReturnValidSettings()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var configFileName = $"print_settings_test_{Guid.NewGuid()}.json";
            var configService = new JsonConfigurationService(configFileName, logger.Object);

            // Act
            var printSettings = configService.GetPrintSettings();

            // Assert
            printSettings.Should().NotBeNull();
            printSettings.DefaultCopies.Should().Be(1);
            printSettings.FitToPage.Should().BeTrue();
            printSettings.KeepAspectRatio.Should().BeTrue();
            printSettings.DefaultDpi.Should().Be(300);
        }

        [Fact]
        public void GetTaskHistorySettings_DefaultConfig_ShouldReturnValidSettings()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var configFileName = $"history_settings_test_{Guid.NewGuid()}.json";
            var configService = new JsonConfigurationService(configFileName, logger.Object);

            // Act
            var historySettings = configService.GetTaskHistorySettings();

            // Assert
            historySettings.Should().NotBeNull();
            historySettings.MaxRecords.Should().Be(5);
            historySettings.StoragePath.Should().Be("task_history.json");
        }

        [Fact]
        public void SaveSettings_ValidSettings_ShouldPersistChanges()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var configFileName = $"save_test_{Guid.NewGuid()}.json";
            var configService = new JsonConfigurationService(configFileName, logger.Object);

            var newSettings = TestMockFactory.CreateTestAppSettings();
            newSettings.Monitoring.WatchPath = @"C:\CustomTestPath";
            newSettings.Monitoring.BatchTimeoutSeconds = 10;
            newSettings.PrintSettings.DefaultCopies = 2;

            // Act
            configService.SaveSettings(newSettings);

            // Create new instance to verify persistence
            var newConfigService = new JsonConfigurationService(configFileName, logger.Object);

            // Assert
            newConfigService.GetWatchPath().Should().Be(@"C:\CustomTestPath");
            newConfigService.GetBatchTimeoutSeconds().Should().Be(10);
            newConfigService.GetPrintSettings().DefaultCopies.Should().Be(2);
        }

        [Fact]
        public void GetSettings_AfterSave_ShouldReturnUpdatedSettings()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var configFileName = $"updated_settings_test_{Guid.NewGuid()}.json";
            var configService = new JsonConfigurationService(configFileName, logger.Object);

            var originalSettings = configService.GetSettings();
            originalSettings.PrintSettings.DefaultDpi = 600;
            originalSettings.PrintSettings.FitToPage = false;

            // Act
            configService.SaveSettings(originalSettings);
            var updatedSettings = configService.GetSettings();

            // Assert
            updatedSettings.PrintSettings.DefaultDpi.Should().Be(600);
            updatedSettings.PrintSettings.FitToPage.Should().BeFalse();
        }

        [Fact]
        public void Constructor_NonExistentConfigFile_ShouldCreateDefaultConfig()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var configFileName = $"non_existent_{Guid.NewGuid()}.json";

            // Act
            var configService = new JsonConfigurationService(configFileName, logger.Object);
            var settings = configService.GetSettings();

            // Assert
            settings.Should().NotBeNull();
            settings.Monitoring.Should().NotBeNull();
            settings.PrinterManagement.Should().NotBeNull();
            settings.PrintSettings.Should().NotBeNull();
            settings.TaskHistory.Should().NotBeNull();
            settings.Logging.Should().NotBeNull();
        }
    }
}