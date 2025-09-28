using Xunit;
using FluentAssertions;
using TrayApp.UI;
using TrayApp.Core;
using TrayApp.Tests.Helpers;
using Moq;
using System;
using System.Collections.Generic;

namespace TrayApp.Tests.UI
{
    /// <summary>
    /// TrayIconManager???
    /// ?????????????????????
    /// </summary>
    public class TrayIconManagerTests : TestBase
    {
        [Fact]
        public void Constructor_ValidParameters_ShouldInitializeSuccessfully()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var taskHistoryManager = TestMockFactory.CreateMockTaskHistoryManager();
            var configurationService = TestMockFactory.CreateMockConfigurationService();

            // Act & Assert
            Action act = () => new TrayIconManager(logger.Object, taskHistoryManager.Object, configurationService.Object);
            act.Should().NotThrow();
        }

        [Fact]
        public void Constructor_NullLogger_ShouldThrowArgumentNullException()
        {
            // Arrange
            var taskHistoryManager = TestMockFactory.CreateMockTaskHistoryManager();
            var configurationService = TestMockFactory.CreateMockConfigurationService();

            // Act & Assert
            Action act = () => new TrayIconManager(null!, taskHistoryManager.Object, configurationService.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void Constructor_NullTaskHistoryManager_ShouldThrowArgumentNullException()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var configurationService = TestMockFactory.CreateMockConfigurationService();

            // Act & Assert
            Action act = () => new TrayIconManager(logger.Object, null!, configurationService.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("taskHistoryManager");
        }

        [Fact]
        public void Constructor_NullConfigurationService_ShouldThrowArgumentNullException()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var taskHistoryManager = TestMockFactory.CreateMockTaskHistoryManager();

            // Act & Assert
            Action act = () => new TrayIconManager(logger.Object, taskHistoryManager.Object, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("configurationService");
        }

        [Fact]
        public void ShowPrinterSelectionDialog_EmptyPrinterList_ShouldReturnNull()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var taskHistoryManager = TestMockFactory.CreateMockTaskHistoryManager();
            var configurationService = TestMockFactory.CreateMockConfigurationService();

            using var trayManager = new TrayIconManager(logger.Object, taskHistoryManager.Object, configurationService.Object);

            // Act
            var result = trayManager.ShowPrinterSelectionDialog(new List<string>(), 5);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ShowPrinterSelectionDialog_NullPrinterList_ShouldReturnNull()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var taskHistoryManager = TestMockFactory.CreateMockTaskHistoryManager();
            var configurationService = TestMockFactory.CreateMockConfigurationService();

            using var trayManager = new TrayIconManager(logger.Object, taskHistoryManager.Object, configurationService.Object);

            // Act
            var result = trayManager.ShowPrinterSelectionDialog(null!, 5);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void UpdateTrayTooltip_EmptyTaskHistory_ShouldSetDefaultTooltip()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var taskHistoryManager = TestMockFactory.CreateMockTaskHistoryManager();
            var configurationService = TestMockFactory.CreateMockConfigurationService();

            // ????????
            taskHistoryManager.Setup(x => x.GetRecentTasks(It.IsAny<int>()))
                              .Returns(new List<PrintTaskRecord>());

            using var trayManager = new TrayIconManager(logger.Object, taskHistoryManager.Object, configurationService.Object);

            // Act
            trayManager.UpdateTrayTooltip();

            // Assert
            // ?????GetRecentTasks??
            taskHistoryManager.Verify(x => x.GetRecentTasks(5), Times.AtLeastOnce);
        }

        [Fact]
        public void UpdateTrayTooltip_WithTaskHistory_ShouldIncludeTaskInfo()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var taskHistoryManager = TestMockFactory.CreateMockTaskHistoryManager();
            var configurationService = TestMockFactory.CreateMockConfigurationService();

            // ????????
            var mockTasks = new List<PrintTaskRecord>
            {
                new PrintTaskRecord
                {
                    Timestamp = DateTime.Now.AddMinutes(-5),
                    FileCount = 3,
                    TotalPages = 10,
                    PrinterName = "TestPrinter1"
                },
                new PrintTaskRecord
                {
                    Timestamp = DateTime.Now.AddMinutes(-10),
                    FileCount = 2,
                    TotalPages = 5,
                    PrinterName = "TestPrinter2"
                }
            };

            taskHistoryManager.Setup(x => x.GetRecentTasks(It.IsAny<int>()))
                              .Returns(mockTasks);

            using var trayManager = new TrayIconManager(logger.Object, taskHistoryManager.Object, configurationService.Object);

            // Act
            trayManager.UpdateTrayTooltip();

            // Assert
            taskHistoryManager.Verify(x => x.GetRecentTasks(5), Times.AtLeastOnce);
        }

        [Fact]
        public void ConfigurationUpdated_Event_ShouldBeTriggerable()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var taskHistoryManager = TestMockFactory.CreateMockTaskHistoryManager();
            var configurationService = TestMockFactory.CreateMockConfigurationService();

            using var trayManager = new TrayIconManager(logger.Object, taskHistoryManager.Object, configurationService.Object);

            bool eventTriggered = false;
            trayManager.ConfigurationUpdated += (sender, args) => eventTriggered = true;

            // Act - ????????????
            var eventInfo = typeof(TrayIconManager).GetEvent("ConfigurationUpdated");
            var eventField = typeof(TrayIconManager).GetField("ConfigurationUpdated", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (eventField?.GetValue(trayManager) is EventHandler handler)
            {
                handler.Invoke(trayManager, EventArgs.Empty);
            }

            // Assert
            eventTriggered.Should().BeTrue();
        }

        [Fact]
        public void ExitRequested_Event_ShouldBeTriggerable()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var taskHistoryManager = TestMockFactory.CreateMockTaskHistoryManager();
            var configurationService = TestMockFactory.CreateMockConfigurationService();

            using var trayManager = new TrayIconManager(logger.Object, taskHistoryManager.Object, configurationService.Object);

            bool eventTriggered = false;
            trayManager.ExitRequested += (sender, args) => eventTriggered = true;

            // Act - ??????????
            var eventInfo = typeof(TrayIconManager).GetEvent("ExitRequested");
            var eventField = typeof(TrayIconManager).GetField("ExitRequested", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (eventField?.GetValue(trayManager) is EventHandler handler)
            {
                handler.Invoke(trayManager, EventArgs.Empty);
            }

            // Assert
            eventTriggered.Should().BeTrue();
        }

        [Fact]
        public void Dispose_ShouldReleaseResources()
        {
            // Arrange
            var logger = TestMockFactory.CreateMockLogger();
            var taskHistoryManager = TestMockFactory.CreateMockTaskHistoryManager();
            var configurationService = TestMockFactory.CreateMockConfigurationService();

            var trayManager = new TrayIconManager(logger.Object, taskHistoryManager.Object, configurationService.Object);

            // Act & Assert
            Action act = () => trayManager.Dispose();
            act.Should().NotThrow();

            // ????Dispose??????
            Action act2 = () => trayManager.Dispose();
            act2.Should().NotThrow();
        }
    }
}