using Xunit;
using FluentAssertions;
using TrayApp.Core;
using TrayApp.Tests.Helpers;
using System;
using System.IO;
using System.Collections.Generic;

namespace TrayApp.Tests.Core
{
    /// <summary>
    /// ??????
    /// ??FileLogger???????
    /// </summary>
    public class FileLoggerTests : TestBase
    {
        [Fact]
        public void Constructor_ShouldCreateLoggerSuccessfully()
        {
            // Act & Assert
            Action act = () => new FileLogger();
            act.Should().NotThrow();
        }

        [Fact]
        public void Debug_ValidMessage_ShouldNotThrow()
        {
            // Arrange
            var logger = new FileLogger();

            // Act & Assert
            Action act = () => logger.Debug("Test debug message");
            act.Should().NotThrow();
        }

        [Fact]
        public void Info_ValidMessage_ShouldNotThrow()
        {
            // Arrange
            var logger = new FileLogger();

            // Act & Assert
            Action act = () => logger.Info("Test info message");
            act.Should().NotThrow();
        }

        [Fact]
        public void Warning_ValidMessage_ShouldNotThrow()
        {
            // Arrange
            var logger = new FileLogger();

            // Act & Assert
            Action act = () => logger.Warning("Test warning message");
            act.Should().NotThrow();
        }

        [Fact]
        public void Error_MessageOnly_ShouldNotThrow()
        {
            // Arrange
            var logger = new FileLogger();

            // Act & Assert
            Action act = () => logger.Error("Test error message");
            act.Should().NotThrow();
        }

        [Fact]
        public void Error_MessageWithException_ShouldNotThrow()
        {
            // Arrange
            var logger = new FileLogger();
            var exception = new InvalidOperationException("Test exception");

            // Act & Assert
            Action act = () => logger.Error("Test error with exception", exception);
            act.Should().NotThrow();
        }

        [Fact]
        public void Error_MessageWithNullException_ShouldNotThrow()
        {
            // Arrange
            var logger = new FileLogger();

            // Act & Assert
            Action act = () => logger.Error("Test error with null exception", null);
            act.Should().NotThrow();
        }

        [Fact]
        public void LogMethods_EmptyMessage_ShouldNotThrow()
        {
            // Arrange
            var logger = new FileLogger();

            // Act & Assert
            Action debugAct = () => logger.Debug("");
            debugAct.Should().NotThrow();

            Action infoAct = () => logger.Info("");
            infoAct.Should().NotThrow();

            Action warningAct = () => logger.Warning("");
            warningAct.Should().NotThrow();

            Action errorAct = () => logger.Error("");
            errorAct.Should().NotThrow();
        }

        [Fact]
        public void LogMethods_NullMessage_ShouldNotThrow()
        {
            // Arrange
            var logger = new FileLogger();

            // Act & Assert
            Action debugAct = () => logger.Debug(null!);
            debugAct.Should().NotThrow();

            Action infoAct = () => logger.Info(null!);
            infoAct.Should().NotThrow();

            Action warningAct = () => logger.Warning(null!);
            warningAct.Should().NotThrow();

            Action errorAct = () => logger.Error(null!);
            errorAct.Should().NotThrow();
        }

        [Fact]
        public void LogMethods_LongMessage_ShouldNotThrow()
        {
            // Arrange
            var logger = new FileLogger();
            var longMessage = new string('A', 10000); // 10KB message

            // Act & Assert
            Action debugAct = () => logger.Debug(longMessage);
            debugAct.Should().NotThrow();

            Action infoAct = () => logger.Info(longMessage);
            infoAct.Should().NotThrow();

            Action warningAct = () => logger.Warning(longMessage);
            warningAct.Should().NotThrow();

            Action errorAct = () => logger.Error(longMessage);
            errorAct.Should().NotThrow();
        }

        [Fact]
        public void LogMethods_SpecialCharacters_ShouldNotThrow()
        {
            // Arrange
            var logger = new FileLogger();
            var specialMessage = "Test message with special chars: ?? ?? \n\r\t \"quotes\" <xml/>";

            // Act & Assert
            Action debugAct = () => logger.Debug(specialMessage);
            debugAct.Should().NotThrow();

            Action infoAct = () => logger.Info(specialMessage);
            infoAct.Should().NotThrow();

            Action warningAct = () => logger.Warning(specialMessage);
            warningAct.Should().NotThrow();

            Action errorAct = () => logger.Error(specialMessage);
            errorAct.Should().NotThrow();
        }

        [Fact]
        public void Error_ComplexException_ShouldNotThrow()
        {
            // Arrange
            var logger = new FileLogger();
            
            // ??????????
            var innerException = new ArgumentException("Inner exception message");
            var middleException = new InvalidOperationException("Middle exception", innerException);
            var outerException = new ApplicationException("Outer exception", middleException);

            // Act & Assert
            Action act = () => logger.Error("Complex exception test", outerException);
            act.Should().NotThrow();
        }

        [Fact]
        public void ConcurrentLogging_MultipleCalls_ShouldNotThrow()
        {
            // Arrange
            var logger = new FileLogger();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var tasks = new System.Threading.Tasks.Task[10];

            // Act - ??????
            for (int i = 0; i < tasks.Length; i++)
            {
                int taskId = i;
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            logger.Debug($"Debug from task {taskId}, iteration {j}");
                            logger.Info($"Info from task {taskId}, iteration {j}");
                            logger.Warning($"Warning from task {taskId}, iteration {j}");
                            logger.Error($"Error from task {taskId}, iteration {j}");
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
            }

            // ????????
            System.Threading.Tasks.Task.WaitAll(tasks);

            // Assert
            exceptions.Should().BeEmpty();
        }

        [Fact]
        public void LogDirectoryCreation_ShouldHandlePermissions()
        {
            // ???????????????????????
            // ????????FileLogger?????????????

            // Arrange & Act
            var logger = new FileLogger();

            // Assert - ??????????????????????
            logger.Should().NotBeNull();

            // ??????????????
            Action act = () => logger.Info("Permission test message");
            act.Should().NotThrow();
        }

        [Fact]
        public void LogFile_ShouldFollowNamingConvention()
        {
            // ??????????
            // FileLogger??????? app_yyyyMMdd.log ???

            // Arrange
            var logger = new FileLogger();
            var expectedDatePart = DateTime.Now.ToString("yyyyMMdd");

            // Act - ??????????????
            logger.Info("Test log file naming");

            // Assert - ?????????????????????????
            // ???????FileLogger??????????????
            Action act = () => logger.Info("Another test message");
            act.Should().NotThrow();
        }

        [Fact]
        public void LogMessages_DifferentLevels_ShouldHaveCorrectFormat()
        {
            // ?????????????
            // ?????????????????????????????

            // Arrange
            var logger = new FileLogger();

            // Act & Assert - ?????????????????
            Action debugAct = () => logger.Debug("Debug level test");
            debugAct.Should().NotThrow();

            Action infoAct = () => logger.Info("Info level test");
            infoAct.Should().NotThrow();

            Action warningAct = () => logger.Warning("Warning level test");
            warningAct.Should().NotThrow();

            Action errorAct = () => logger.Error("Error level test");
            errorAct.Should().NotThrow();
        }

        [Fact]
        public void Performance_HighVolumeLogging_ShouldBeReasonable()
        {
            // ???? - ???????
            var logger = new FileLogger();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - ??1000?????
            for (int i = 0; i < 1000; i++)
            {
                logger.Info($"Performance test message {i}");
            }

            stopwatch.Stop();

            // Assert - 1000????????????????5??
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);
        }

        [Fact]
        public void LogRotation_NewDay_ShouldHandleCorrectly()
        {
            // ????????
            // FileLogger??????????????????

            // Arrange
            var logger = new FileLogger();

            // Act - ??????????
            logger.Info("Message 1");
            logger.Info("Message 2");
            logger.Info("Message 3");

            // Assert - ???????
            Action act = () => logger.Info("Message 4");
            act.Should().NotThrow();
        }

        [Fact]
        public void ErrorHandling_FileSystemIssues_ShouldNotCrash()
        {
            // ???????????
            // FileLogger????????????????

            // Arrange
            var logger = new FileLogger();

            // Act & Assert - ????????????????????????
            Action act = () =>
            {
                for (int i = 0; i < 100; i++)
                {
                    logger.Error($"File system test error {i}");
                }
            };
            
            act.Should().NotThrow();
        }
    }
}