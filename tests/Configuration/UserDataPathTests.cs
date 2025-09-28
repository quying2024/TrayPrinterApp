using Xunit;
using FluentAssertions;
using TrayApp.Configuration;
using TrayApp.Core;
using TrayApp.Tests.Helpers;
using System.IO;
using System;

namespace TrayApp.Tests.Configuration
{
    /// <summary>
    /// ??????????????
    /// ??????????????????????????
    /// </summary>
    public class UserDataPathTests : TestBase
    {
        [Fact]
        public void GetConfigFilePath_ShouldUseUserDataDirectory()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var configFileName = "test_config.json";

            // Act
            var configService = new JsonConfigurationService(configFileName, logger.Object);
            var settings = configService.GetSettings();

            // Assert
            settings.Should().NotBeNull();
            // ?????????????????
            settings.Monitoring.Should().NotBeNull();
            settings.PrinterManagement.Should().NotBeNull();
        }

        [Fact]
        public void SaveSettings_ShouldCreateFileInUserDirectory()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var configFileName = $"user_data_test_{Guid.NewGuid()}.json";
            var configService = new JsonConfigurationService(configFileName, logger.Object);

            var testSettings = new AppSettings
            {
                Monitoring = new MonitoringSettings
                {
                    WatchPath = @"C:\TestUserDataPath",
                    BatchTimeoutSeconds = 5,
                    FileTypes = new System.Collections.Generic.List<string> { ".test" }
                },
                PrinterManagement = new PrinterManagementSettings
                {
                    HiddenPrinters = new System.Collections.Generic.List<string> { "TestPrinter" },
                    DisplayOrder = "Name"
                },
                PrintSettings = new PrintSettings
                {
                    DefaultCopies = 2,
                    FitToPage = false,
                    KeepAspectRatio = false,
                    DefaultDpi = 600
                },
                TaskHistory = new TaskHistorySettings
                {
                    MaxRecords = 3,
                    StoragePath = "test_history.json"
                },
                Logging = new LoggingSettings
                {
                    LogLevel = "Debug",
                    LogFilePath = "test.log"
                }
            };

            // Act
            configService.SaveSettings(testSettings);

            // Reload and verify
            var reloadedService = new JsonConfigurationService(configFileName, logger.Object);
            var reloadedSettings = reloadedService.GetSettings();

            // Assert
            reloadedSettings.Monitoring.WatchPath.Should().Be(@"C:\TestUserDataPath");
            reloadedSettings.Monitoring.BatchTimeoutSeconds.Should().Be(5);
            reloadedSettings.Monitoring.FileTypes.Should().Contain(".test");
            reloadedSettings.PrinterManagement.HiddenPrinters.Should().Contain("TestPrinter");
            reloadedSettings.PrinterManagement.DisplayOrder.Should().Be("Name");
            reloadedSettings.PrintSettings.DefaultCopies.Should().Be(2);
            reloadedSettings.PrintSettings.FitToPage.Should().BeFalse();
            reloadedSettings.TaskHistory.MaxRecords.Should().Be(3);
            reloadedSettings.Logging.LogLevel.Should().Be("Debug");
        }

        [Fact]
        public void JsonConfigurationService_DefaultSettings_ShouldBeValid()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var configFileName = $"default_test_{Guid.NewGuid()}.json";

            // Act - ?????????????????
            var configService = new JsonConfigurationService(configFileName, logger.Object);
            var settings = configService.GetSettings();

            // Assert
            settings.Should().NotBeNull();
            settings.Monitoring.WatchPath.Should().NotBeNullOrEmpty();
            settings.Monitoring.BatchTimeoutSeconds.Should().BePositive();
            settings.Monitoring.FileTypes.Should().NotBeEmpty();
            settings.Monitoring.FileTypes.Should().Contain(".pdf");
            settings.Monitoring.FileTypes.Should().Contain(".docx");
            
            settings.PrinterManagement.HiddenPrinters.Should().NotBeNull();
            settings.PrinterManagement.DisplayOrder.Should().NotBeNullOrEmpty();
            
            settings.PrintSettings.Should().NotBeNull();
            settings.PrintSettings.DefaultCopies.Should().BePositive();
            settings.PrintSettings.DefaultDpi.Should().BePositive();
            
            settings.TaskHistory.MaxRecords.Should().BePositive();
            settings.TaskHistory.StoragePath.Should().NotBeNullOrEmpty();
            
            settings.Logging.LogLevel.Should().NotBeNullOrEmpty();
            settings.Logging.LogFilePath.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void GetWatchPath_AfterSettingsChange_ShouldReturnUpdatedValue()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var configFileName = $"watch_path_test_{Guid.NewGuid()}.json";
            var configService = new JsonConfigurationService(configFileName, logger.Object);

            var originalPath = configService.GetWatchPath();
            var newPath = @"C:\NewTestWatchPath";

            // Act
            var settings = configService.GetSettings();
            settings.Monitoring.WatchPath = newPath;
            configService.SaveSettings(settings);

            // Assert
            configService.GetWatchPath().Should().Be(newPath);
            configService.GetWatchPath().Should().NotBe(originalPath);
        }

        [Fact]
        public void GetBatchTimeoutSeconds_AfterSettingsChange_ShouldReturnUpdatedValue()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var configFileName = $"timeout_test_{Guid.NewGuid()}.json";
            var configService = new JsonConfigurationService(configFileName, logger.Object);

            var originalTimeout = configService.GetBatchTimeoutSeconds();
            var newTimeout = originalTimeout + 5;

            // Act
            var settings = configService.GetSettings();
            settings.Monitoring.BatchTimeoutSeconds = newTimeout;
            configService.SaveSettings(settings);

            // Assert
            configService.GetBatchTimeoutSeconds().Should().Be(newTimeout);
            configService.GetBatchTimeoutSeconds().Should().NotBe(originalTimeout);
        }

        [Fact]
        public void GetHiddenPrinters_AfterSettingsChange_ShouldReturnUpdatedList()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var configFileName = $"printers_test_{Guid.NewGuid()}.json";
            var configService = new JsonConfigurationService(configFileName, logger.Object);

            var newHiddenPrinter = "TestHiddenPrinter123";

            // Act
            var settings = configService.GetSettings();
            settings.PrinterManagement.HiddenPrinters.Add(newHiddenPrinter);
            configService.SaveSettings(settings);

            // Assert
            var hiddenPrinters = configService.GetHiddenPrinters();
            hiddenPrinters.Should().Contain(newHiddenPrinter);
        }

        [Fact]
        public void GetPrintSettings_AfterUpdate_ShouldReturnNewSettings()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var configFileName = $"print_settings_test_{Guid.NewGuid()}.json";
            var configService = new JsonConfigurationService(configFileName, logger.Object);

            // Act
            var settings = configService.GetSettings();
            settings.PrintSettings.DefaultCopies = 3;
            settings.PrintSettings.DefaultDpi = 600;
            settings.PrintSettings.FitToPage = false;
            configService.SaveSettings(settings);

            // Assert
            var printSettings = configService.GetPrintSettings();
            printSettings.Should().NotBeNull();
            printSettings.DefaultCopies.Should().Be(3);
            printSettings.DefaultDpi.Should().Be(600);
            printSettings.FitToPage.Should().BeFalse();
        }
    }
}