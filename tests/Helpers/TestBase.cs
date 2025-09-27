using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace TrayApp.Tests.Helpers
{
    /// <summary>
    /// 测试基类，提供通用的测试设施
    /// </summary>
    public abstract class TestBase : IDisposable
    {
        protected readonly string TestDirectory;
        protected readonly string TestDataDirectory;
        private bool _disposed = false;

        protected TestBase()
        {
            // 为每个测试创建独立的临时目录
            TestDirectory = Path.Combine(Path.GetTempPath(), $"TrayAppTest_{Guid.NewGuid():N}");
            TestDataDirectory = Path.Combine(TestDirectory, "TestData");
            
            Directory.CreateDirectory(TestDirectory);
            Directory.CreateDirectory(TestDataDirectory);
        }

        /// <summary>
        /// 获取测试数据文件路径
        /// </summary>
        protected string GetTestDataPath(string fileName)
        {
            return Path.Combine(TestDataDirectory, fileName);
        }

        /// <summary>
        /// 创建测试文件
        /// </summary>
        protected string CreateTestFile(string fileName, string content = "Test Content")
        {
            var filePath = GetTestDataPath(fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        /// <summary>
        /// 创建测试配置文件
        /// </summary>
        protected string CreateTestConfigFile(string configJson)
        {
            var configPath = GetTestDataPath("test_config.json");
            File.WriteAllText(configPath, configJson);
            return configPath;
        }

        /// <summary>
        /// 等待指定时间（用于异步测试）
        /// </summary>
        protected async Task WaitForAsync(TimeSpan timeout)
        {
            await Task.Delay(timeout);
        }

        /// <summary>
        /// 清理测试资源
        /// </summary>
        public virtual void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    if (Directory.Exists(TestDirectory))
                    {
                        Directory.Delete(TestDirectory, true);
                    }
                }
                catch
                {
                    // 忽略清理失败，避免影响测试结果
                }
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}