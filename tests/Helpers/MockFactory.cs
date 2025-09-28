using Moq;
using TrayApp.Core;
using System.Collections.Generic;

namespace TrayApp.Tests.Helpers
{
    /// <summary>
    /// Mock对象工厂，提供常用的Mock实例
    /// </summary>
    public static class TestMockFactory
    {
        /// <summary>
        /// 创建Mock配置服务
        /// </summary>
        public static Mock<IConfigurationService> CreateMockConfigurationService()
        {
            var mock = new Mock<IConfigurationService>();
            
            // 设置默认返回值
            mock.Setup(x => x.GetWatchPath()).Returns(@"C:\TestWatch");
            mock.Setup(x => x.GetBatchTimeoutSeconds()).Returns(3);
            mock.Setup(x => x.GetMonitoredFileTypes()).Returns(new List<string> { ".pdf", ".docx", ".jpg" });
            mock.Setup(x => x.GetHiddenPrinters()).Returns(new List<string> { "TestHiddenPrinter" });
            mock.Setup(x => x.GetTaskHistorySettings()).Returns(new TaskHistorySettings 
            { 
                MaxRecords = 5, 
                StoragePath = "test_history.json" 
            });

            return mock;
        }

        /// <summary>
        /// 创建Mock日志服务
        /// </summary>
        public static Mock<ILogger> CreateMockLogger()
        {
            var mock = new Mock<ILogger>();
            // 默认所有日志方法都不做任何操作
            return mock;
        }

        /// <summary>
        /// 创建测试用的AppSettings
        /// </summary>
        public static AppSettings CreateTestAppSettings()
        {
            return new AppSettings
            {
                Monitoring = new MonitoringSettings
                {
                    WatchPath = @"C:\TestWatch",
                    BatchTimeoutSeconds = 3,
                    FileTypes = new List<string> { ".pdf", ".docx", ".jpg" }
                },
                PrinterManagement = new PrinterManagementSettings
                {
                    HiddenPrinters = new List<string> { "TestHiddenPrinter" },
                    DisplayOrder = "UsageFrequency"
                },
                FileTypeAssociations = new Dictionary<string, FileTypeAssociation>
                {
                    [".pdf"] = new FileTypeAssociation
                    {
                        ExecutorPath = "test_pdf_reader.exe",
                        Arguments = "/print \"{FilePath}\" \"{PrinterName}\"",
                        PageCounterType = "PdfPageCounter"
                    }
                },
                TaskHistory = new TaskHistorySettings
                {
                    MaxRecords = 5,
                    StoragePath = "test_history.json"
                },
                Logging = new LoggingSettings
                {
                    LogLevel = "Info",
                    LogFilePath = "test_app.log"
                }
            };
        }
    }
}