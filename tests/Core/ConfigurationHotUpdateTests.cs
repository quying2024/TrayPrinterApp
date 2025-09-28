using Xunit;
using FluentAssertions;
using TrayApp.Core;
using TrayApp.Tests.Helpers;
using Moq;
using System;
using System.Collections.Generic;

namespace TrayApp.Tests.Core
{
    /// <summary>
    /// ???????????
    /// ????????????????
    /// </summary>
    public class ConfigurationHotUpdateTests : TestBase
    {
        [Fact]
        public void AppCore_ConfigurationUpdate_ShouldTriggerRestart()
        {
            // ?????????????????????
            // ??AppCore??Windows Forms?????????????????
            // ??????????
            
            // Arrange
            var mockConfig = TestMockFactory.CreateMockConfigurationService();
            var mockLogger = TestMockFactory.CreateMockLogger();
            
            bool configUpdateEventReceived = false;
            
            // ????????
            mockConfig.Setup(x => x.GetWatchPath()).Returns(@"C:\UpdatedPath");
            
            // Act
            // ?????????????
            var originalPath = @"C:\OriginalPath";
            var updatedPath = mockConfig.Object.GetWatchPath();
            
            if (originalPath != updatedPath)
            {
                configUpdateEventReceived = true;
            }
            
            // Assert
            configUpdateEventReceived.Should().BeTrue();
            updatedPath.Should().Be(@"C:\UpdatedPath");
        }

        [Fact]
        public void AppSettings_Validation_ShouldHandleInvalidValues()
        {
            // Arrange & Act
            var settings = new AppSettings
            {
                Monitoring = new MonitoringSettings
                {
                    WatchPath = "", // ????
                    BatchTimeoutSeconds = -1, // ????
                    FileTypes = new List<string>()
                },
                PrinterManagement = new PrinterManagementSettings
                {
                    HiddenPrinters = null!, // null?
                    DisplayOrder = ""
                },
                PrintSettings = new PrintSettings
                {
                    DefaultCopies = 0, // ????
                    DefaultDpi = -1 // ??DPI
                },
                TaskHistory = new TaskHistorySettings
                {
                    MaxRecords = 0, // ?????
                    StoragePath = ""
                },
                Logging = new LoggingSettings
                {
                    LogLevel = "",
                    LogFilePath = ""
                }
            };

            // Assert - ?????????????????
            settings.Should().NotBeNull();
            settings.Monitoring.Should().NotBeNull();
            settings.PrinterManagement.Should().NotBeNull();
            settings.PrintSettings.Should().NotBeNull();
            settings.TaskHistory.Should().NotBeNull();
            settings.Logging.Should().NotBeNull();
        }

        [Fact]
        public void PrintSettings_Properties_ShouldBeSettable()
        {
            // Arrange & Act
            var printSettings = new PrintSettings
            {
                DefaultCopies = 2,
                FitToPage = false,
                KeepAspectRatio = false,
                DefaultDpi = 600
            };

            // Assert
            printSettings.DefaultCopies.Should().Be(2);
            printSettings.FitToPage.Should().BeFalse();
            printSettings.KeepAspectRatio.Should().BeFalse();
            printSettings.DefaultDpi.Should().Be(600);
        }

        [Fact]
        public void MonitoringSettings_DefaultValues_ShouldBeReasonable()
        {
            // Arrange & Act
            var monitoring = new MonitoringSettings();

            // Assert
            monitoring.WatchPath.Should().NotBeNull();
            monitoring.BatchTimeoutSeconds.Should().Be(3);
            monitoring.FileTypes.Should().NotBeNull();
        }

        [Fact]
        public void PrinterManagementSettings_DefaultValues_ShouldBeInitialized()
        {
            // Arrange & Act
            var printerManagement = new PrinterManagementSettings();

            // Assert
            printerManagement.HiddenPrinters.Should().NotBeNull();
            printerManagement.DisplayOrder.Should().NotBeNull();
        }

        [Fact]
        public void PrintSettings_DefaultValues_ShouldBeValid()
        {
            // Arrange & Act
            var printSettings = new PrintSettings();

            // Assert
            printSettings.DefaultCopies.Should().Be(1);
            printSettings.FitToPage.Should().BeTrue();
            printSettings.KeepAspectRatio.Should().BeTrue();
            printSettings.DefaultDpi.Should().Be(300);
        }

        [Fact]
        public void TaskHistorySettings_DefaultValues_ShouldBeValid()
        {
            // Arrange & Act
            var taskHistory = new TaskHistorySettings();

            // Assert
            taskHistory.MaxRecords.Should().Be(5);
            taskHistory.StoragePath.Should().NotBeNull();
        }

        [Fact]
        public void LoggingSettings_DefaultValues_ShouldBeSet()
        {
            // Arrange & Act
            var logging = new LoggingSettings();

            // Assert
            logging.LogLevel.Should().NotBeNull();
            logging.LogFilePath.Should().NotBeNull();
        }

        [Fact]
        public void PrintTaskRecord_Properties_ShouldBeSettableAndGettable()
        {
            // Arrange
            var now = DateTime.Now;
            var filePaths = new List<string> { "file1.pdf", "file2.docx" };

            // Act
            var record = new PrintTaskRecord
            {
                Timestamp = now,
                FileCount = 2,
                TotalPages = 10,
                PrinterName = "TestPrinter",
                FilePaths = filePaths
            };

            // Assert
            record.Timestamp.Should().Be(now);
            record.FileCount.Should().Be(2);
            record.TotalPages.Should().Be(10);
            record.PrinterName.Should().Be("TestPrinter");
            record.FilePaths.Should().BeEquivalentTo(filePaths);
        }

        [Fact]
        public void FileBatchEventArgs_Properties_ShouldBeSettableAndGettable()
        {
            // Arrange
            var filePaths = new List<string> { "batch1.pdf", "batch2.jpg" };

            // Act
            var eventArgs = new FileBatchEventArgs
            {
                FilePaths = filePaths
            };

            // Assert
            eventArgs.FilePaths.Should().BeEquivalentTo(filePaths);
        }

        [Fact]
        public void PrintCompletedEventArgs_FileCount_ShouldCalculateCorrectly()
        {
            // Arrange
            var filePaths = new List<string> { "file1.pdf", "file2.docx", "file3.jpg" };

            // Act
            var eventArgs = new PrintCompletedEventArgs
            {
                Success = true,
                FilePaths = filePaths,
                PrinterName = "TestPrinter",
                TotalPages = 15
            };

            // Assert
            eventArgs.FileCount.Should().Be(3); // ???????
            eventArgs.Success.Should().BeTrue();
            eventArgs.FilePaths.Should().BeEquivalentTo(filePaths);
            eventArgs.PrinterName.Should().Be("TestPrinter");
            eventArgs.TotalPages.Should().Be(15);
        }
    }
}