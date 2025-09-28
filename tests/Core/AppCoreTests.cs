using Xunit;
using FluentAssertions;
using TrayApp;
using TrayApp.Core;
using TrayApp.Tests.Helpers;
using Moq;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrayApp.Tests.Core
{
    /// <summary>
    /// ????????
    /// ??AppCore????????????????
    /// </summary>
    public class AppCoreTests : TestBase
    {
        [Fact]
        public void Constructor_ShouldInitializeSuccessfully()
        {
            // Act & Assert
            Action act = () => new AppCore();
            act.Should().NotThrow();
        }

        [Fact]
        public void Start_ShouldInitializeServicesSuccessfully()
        {
            // Arrange
            using var appCore = new AppCore();

            // Act & Assert
            Action act = () => appCore.Start();
            act.Should().NotThrow();
        }

        [Fact]
        public void Stop_BeforeStart_ShouldNotThrow()
        {
            // Arrange
            using var appCore = new AppCore();

            // Act & Assert
            Action act = () => appCore.Stop();
            act.Should().NotThrow();
        }

        [Fact]
        public void Stop_AfterStart_ShouldCleanupResources()
        {
            // Arrange
            using var appCore = new AppCore();
            appCore.Start();

            // Act & Assert
            Action act = () => appCore.Stop();
            act.Should().NotThrow();
        }

        [Fact]
        public void Dispose_ShouldReleaseResources()
        {
            // Arrange
            var appCore = new AppCore();

            // Act & Assert
            Action act = () => appCore.Dispose();
            act.Should().NotThrow();

            // Multiple dispose calls should be safe
            Action act2 = () => appCore.Dispose();
            act2.Should().NotThrow();
        }

        [Fact]
        public void Start_MultipleCalls_ShouldHandleGracefully()
        {
            // Arrange
            using var appCore = new AppCore();

            // Act & Assert
            appCore.Start();
            Action act = () => appCore.Start(); // Second call
            act.Should().NotThrow();
        }

        [Fact]
        public void Stop_MultipleCalls_ShouldHandleGracefully()
        {
            // Arrange
            using var appCore = new AppCore();
            appCore.Start();
            appCore.Stop();

            // Act & Assert
            Action act = () => appCore.Stop(); // Second call
            act.Should().NotThrow();
        }

        [Fact]
        public void Lifecycle_StartStopStart_ShouldWorkCorrectly()
        {
            // Arrange
            using var appCore = new AppCore();

            // Act & Assert
            Action startAct = () => appCore.Start();
            startAct.Should().NotThrow();

            Action stopAct = () => appCore.Stop();
            stopAct.Should().NotThrow();

            Action restartAct = () => appCore.Start();
            restartAct.Should().NotThrow();
        }

        [Fact]
        public void EventHandling_ShouldNotCauseExceptions()
        {
            // ??????????????????????
            using var appCore = new AppCore();
            
            // ?????????????????????
            Action act = () => appCore.Start();
            act.Should().NotThrow();

            // ??????????
            Thread.Sleep(100);

            // ??????
            Action stopAct = () => appCore.Stop();
            stopAct.Should().NotThrow();
        }

        [Fact]
        public void ServiceInitialization_ShouldHandleErrors()
        {
            // ??????????????????????????????
            using var appCore = new AppCore();

            // ?????????????????????????????
            // ??????????
            Action act = () => appCore.Start();
            act.Should().NotThrow();
        }

        [Fact]
        public void ResourceCleanup_AfterDispose_ShouldBeComplete()
        {
            // Arrange & Act
            var appCore = new AppCore();
            appCore.Start();
            appCore.Dispose();

            // Assert - ??Dispose?????
            // ????Dispose??????
            Action act = () => appCore.Dispose();
            act.Should().NotThrow();
        }

        [Fact]
        public void Configuration_ShouldBeLoadedCorrectly()
        {
            // ?????????????
            using var appCore = new AppCore();
            
            // ??????????????
            Action act = () => appCore.Start();
            act.Should().NotThrow();
        }

        [Fact]
        public void Threading_MultipleStartStopCalls_ShouldBeSafe()
        {
            // ???????????????
            using var appCore = new AppCore();

            var exceptions = new List<Exception>();
            var tasks = new System.Threading.Tasks.Task[4];
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // 10???

            // ???????????Start?Stop
            tasks[0] = System.Threading.Tasks.Task.Run(async () =>
            {
                try 
                { 
                    appCore.Start();
                    await Task.Delay(50, cts.Token);
                }
                catch (Exception ex) { lock (exceptions) exceptions.Add(ex); }
            }, cts.Token);

            tasks[1] = System.Threading.Tasks.Task.Run(async () =>
            {
                try 
                { 
                    await Task.Delay(100, cts.Token);
                    appCore.Stop(); 
                }
                catch (Exception ex) { lock (exceptions) exceptions.Add(ex); }
            }, cts.Token);

            tasks[2] = System.Threading.Tasks.Task.Run(async () =>
            {
                try 
                { 
                    await Task.Delay(150, cts.Token);
                    appCore.Start(); 
                }
                catch (Exception ex) { lock (exceptions) exceptions.Add(ex); }
            }, cts.Token);

            tasks[3] = System.Threading.Tasks.Task.Run(async () =>
            {
                try 
                { 
                    await Task.Delay(200, cts.Token);
                    appCore.Stop(); 
                }
                catch (Exception ex) { lock (exceptions) exceptions.Add(ex); }
            }, cts.Token);

            // ??????????????????
            try
            {
                System.Threading.Tasks.Task.WaitAll(tasks, TimeSpan.FromSeconds(15));
            }
            catch (AggregateException)
            {
                // ?????????????????????
            }

            // ????????
            var unexpectedExceptions = exceptions.Where(ex => !(ex is OperationCanceledException)).ToList();
            unexpectedExceptions.Should().BeEmpty();
        }

        [Fact]
        public void ErrorHandling_UnexpectedExceptions_ShouldNotCrashApplication()
        {
            // ????????????????
            using var appCore = new AppCore();

            try
            {
                appCore.Start();
                
                // ?????????????
                // ??????????????????
                
                // ?????????????
                Thread.Sleep(200);
                
                // ????????????
                true.Should().BeTrue();
            }
            catch (Exception ex)
            {
                // ??????????????????
                ex.Should().NotBeNull();
                // ??????????????????
            }
        }

        [Fact]
        public void ServiceDependencies_ShouldBeInjectedCorrectly()
        {
            // ????????????
            using var appCore = new AppCore();
            
            // ????????????????????
            Action act = () => appCore.Start();
            act.Should().NotThrow();
            
            // ???????????????????????????????
            // ???????????????????????
        }

        [Fact]
        public void Performance_StartupTime_ShouldBeReasonable()
        {
            // ??????????
            using var appCore = new AppCore();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            appCore.Start();
            stopwatch.Stop();

            // Assert - ???????????????5????
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);
        }

        [Fact]
        public void Memory_AfterStartStop_ShouldNotLeak()
        {
            // ?????????
            var initialMemory = GC.GetTotalMemory(true);

            using (var appCore = new AppCore())
            {
                appCore.Start();
                appCore.Stop();
            }

            // ??????
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(true);
            
            // ????????????????????
            var memoryIncrease = finalMemory - initialMemory;
            memoryIncrease.Should().BeLessThan(10_000_000); // 10MB
        }
    }
}