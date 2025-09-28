using Moq;
using TrayApp.Core;
using System.Collections.Generic;

namespace TrayApp.Tests.Helpers
{
    /// <summary>
    /// Mock对象工厂，提供常用的Mock实例
    /// 适配新的统一PDF打印架构
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
            mock.Setup(x => x.GetMonitoredFileTypes()).Returns(new List<string> { ".pdf", ".docx", ".jpg", ".png" });
            mock.Setup(x => x.GetHiddenPrinters()).Returns(new List<string> { "TestHiddenPrinter" });
            mock.Setup(x => x.GetTaskHistorySettings()).Returns(new TaskHistorySettings 
            { 
                MaxRecords = 5, 
                StoragePath = "test_history.json" 
            });
            mock.Setup(x => x.GetPrintSettings()).Returns(new PrintSettings
            {
                DefaultCopies = 1,
                FitToPage = true,
                KeepAspectRatio = true,
                DefaultDpi = 300
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
        /// 创建Mock任务历史管理器
        /// </summary>
        public static Mock<ITaskHistoryManager> CreateMockTaskHistoryManager()
        {
            var mock = new Mock<ITaskHistoryManager>();
            
            // 设置默认返回值
            mock.Setup(x => x.GetRecentTasks(It.IsAny<int>()))
                .Returns(new List<PrintTaskRecord>());

            return mock;
        }

        /// <summary>
        /// 创建Mock统一打印管理器
        /// </summary>
        public static Mock<IPrintManager> CreateMockUnifiedPrintManager()
        {
            var mock = new Mock<IPrintManager>();
            
            // 设置默认返回值
            mock.Setup(x => x.GetAvailablePrinters())
                .Returns(new List<string> { "TestPrinter1", "TestPrinter2" });
            
            mock.Setup(x => x.CalculateTotalPages(It.IsAny<IEnumerable<string>>()))
                .Returns(1);

            return mock;
        }

        /// <summary>
        /// 创建Mock文件操作器
        /// </summary>
        public static Mock<IFileOperator> CreateMockFileOperator()
        {
            var mock = new Mock<IFileOperator>();
            
            // 设置默认返回值
            mock.Setup(x => x.MoveFilesToTimestampDirectory(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
                .Returns(@"C:\TestDirectory\20231020153022");

            return mock;
        }

        /// <summary>
        /// 创建Mock文件夹监视器
        /// </summary>
        public static Mock<IFolderMonitor> CreateMockFolderMonitor()
        {
            var mock = new Mock<IFolderMonitor>();
            return mock;
        }

        /// <summary>
        /// 创建测试用的AppSettings（新架构）
        /// </summary>
        public static AppSettings CreateTestAppSettings()
        {
            return new AppSettings
            {
                Monitoring = new MonitoringSettings
                {
                    WatchPath = @"C:\TestWatch",
                    BatchTimeoutSeconds = 3,
                    FileTypes = new List<string> { ".pdf", ".docx", ".jpg", ".png" }
                },
                PrinterManagement = new PrinterManagementSettings
                {
                    HiddenPrinters = new List<string> { "TestHiddenPrinter" },
                    DisplayOrder = "UsageFrequency"
                },
                PrintSettings = new PrintSettings
                {
                    DefaultCopies = 1,
                    FitToPage = true,
                    KeepAspectRatio = true,
                    DefaultDpi = 300
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

        /// <summary>
        /// 创建测试用的PrintTaskRecord
        /// </summary>
        public static PrintTaskRecord CreateTestPrintTaskRecord(
            int fileCount = 1, 
            int totalPages = 1, 
            string printerName = "TestPrinter")
        {
            return new PrintTaskRecord
            {
                Timestamp = System.DateTime.Now,
                FileCount = fileCount,
                TotalPages = totalPages,
                PrinterName = printerName,
                FilePaths = new List<string> { "test.pdf" }
            };
        }

        /// <summary>
        /// 创建测试用的FileBatchEventArgs
        /// </summary>
        public static FileBatchEventArgs CreateTestFileBatchEventArgs(params string[] filePaths)
        {
            return new FileBatchEventArgs
            {
                FilePaths = new List<string>(filePaths)
            };
        }
    }
}