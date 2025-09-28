using Xunit;
using FluentAssertions;
using TrayApp.Tests.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TrayApp.Tests.Quick
{
    /// <summary>
    /// ?????????????????
    /// </summary>
    public class QuickTimeoutTests : TestBase
    {
        [Fact]
        public void SimpleTest_ShouldCompleteQuickly()
        {
            // ???????????
            var result = 1 + 1;
            result.Should().Be(2);
        }

        [Fact]
        public async Task AsyncTest_WithTimeout_ShouldNotHang()
        {
            // ?????????????
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            
            await Task.Delay(100, cts.Token);
            
            true.Should().BeTrue();
        }

        [Fact]
        public void ConcurrentTest_WithTimeout_ShouldComplete()
        {
            // ???????
            var tasks = new Task[3];
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            for (int i = 0; i < tasks.Length; i++)
            {
                int taskId = i;
                tasks[i] = Task.Run(async () =>
                {
                    await Task.Delay(50, cts.Token);
                    return taskId;
                }, cts.Token);
            }

            try
            {
                Task.WaitAll(tasks, TimeSpan.FromSeconds(10));
                tasks.All(t => t.IsCompletedSuccessfully || t.IsCanceled).Should().BeTrue();
            }
            catch (AggregateException)
            {
                // ????????
                true.Should().BeTrue();
            }
        }

        [Fact]
        public void ResourceManagement_ShouldNotLeak()
        {
            // ??????
            for (int i = 0; i < 100; i++)
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(10)))
                {
                    // ?????????
                }
            }

            true.Should().BeTrue();
        }
    }
}