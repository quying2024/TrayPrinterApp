using System;
using System.Collections.Generic;
using System.IO;

namespace TrayApp.Core
{
    /// <summary>
    /// 应用配置模型
    /// </summary>
    public class AppSettings
    {
        public MonitoringSettings Monitoring { get; set; } = new MonitoringSettings();
        public PrinterManagementSettings PrinterManagement { get; set; } = new PrinterManagementSettings();
        public Dictionary<string, FileTypeAssociation> FileTypeAssociations { get; set; } = new Dictionary<string, FileTypeAssociation>();
        public TaskHistorySettings TaskHistory { get; set; } = new TaskHistorySettings();
        public LoggingSettings Logging { get; set; } = new LoggingSettings();
    }

    public class MonitoringSettings
    {
        public string WatchPath { get; set; } = string.Empty;
        public int BatchTimeoutSeconds { get; set; } = 3;
        public List<string> FileTypes { get; set; } = new List<string>();
    }

    public class PrinterManagementSettings
    {
        public List<string> HiddenPrinters { get; set; } = new List<string>();
        public string DisplayOrder { get; set; } = "UsageFrequency";
    }

    public class FileTypeAssociation
    {
        public string ExecutorPath { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string PageCounterType { get; set; } = string.Empty;
    }

    public class TaskHistorySettings
    {
        public int MaxRecords { get; set; } = 5;
        public string StoragePath { get; set; } = "task_history.json";
    }

    public class LoggingSettings
    {
        public string LogLevel { get; set; } = "Info";
        public string LogFilePath { get; set; } = "app.log";
    }

    /// <summary>
    /// 配置服务接口
    /// </summary>
    public interface IConfigurationService
    {
        AppSettings GetSettings();
        void SaveSettings(AppSettings settings);
        string GetWatchPath();
        int GetBatchTimeoutSeconds();
        List<string> GetMonitoredFileTypes();
        List<string> GetHiddenPrinters();
        FileTypeAssociation? GetFileTypeAssociation(string fileExtension);
        TaskHistorySettings GetTaskHistorySettings();
    }

    /// <summary>
    /// 文件夹监视接口
    /// </summary>
    public interface IFolderMonitor : IDisposable
    {
        event EventHandler<FileBatchEventArgs> FilesBatchReady;
        void StartMonitoring(string path, int batchTimeoutSeconds, IEnumerable<string> fileTypes);
        void StopMonitoring();
    }

    public class FileBatchEventArgs : EventArgs
    {
        public List<string> FilePaths { get; set; } = new List<string>();
    }

    /// <summary>
    /// 打印管理接口
    /// </summary>
    public interface IPrintManager
    {
        event EventHandler<PrintCompletedEventArgs> PrintCompleted;
        List<string> GetAvailablePrinters();
        int CalculateTotalPages(IEnumerable<string> filePaths);
        void PrintFiles(IEnumerable<string> filePaths, string printerName);
    }

    public class PrintCompletedEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public List<string> FilePaths { get; set; } = new List<string>();
        public string PrinterName { get; set; } = string.Empty;
        public int TotalPages { get; set; }
        public int FileCount => FilePaths.Count; // 添加缺失的FileCount属性，自动计算文件数量
    }

    /// <summary>
    /// 文件操作接口
    /// </summary>
    public interface IFileOperator
    {
        string MoveFilesToTimestampDirectory(IEnumerable<string> filePaths, string baseDirectory);
    }

    /// <summary>
    /// 任务历史记录接口
    /// </summary>
    public interface ITaskHistoryManager : IDisposable
    {
        void AddTaskRecord(PrintTaskRecord record);
        List<PrintTaskRecord> GetRecentTasks(int count);
    }

    public class PrintTaskRecord
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public int FileCount { get; set; }
        public int TotalPages { get; set; }
        public string PrinterName { get; set; } = string.Empty;
        public List<string> FilePaths { get; set; } = new List<string>();
    }

    /// <summary>
    /// 日志接口
    /// </summary>
    public interface ILogger
    {
        void Debug(string message);
        void Info(string message);
        void Warning(string message);
        void Error(string message, Exception? ex = null);
    }

    /// <summary>
    /// 文件日志实现
    /// </summary>
    public class FileLogger : ILogger
    {
        private readonly string _logPath;
        private readonly object _lockObj = new object();

        public FileLogger()
        {
            // Prefer CommonApplicationData so non-admin users can write logs after installation
            try
            {
                var logsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TrayPrinterApp", "logs");
                Directory.CreateDirectory(logsDir);
                _logPath = Path.Combine(logsDir, $"app_{DateTime.Now:yyyyMMdd}.log");
            }
            catch
            {
                // Fallback to per-user LocalApplicationData if CommonApplicationData not available
                try
                {
                    var userLogs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TrayPrinterApp", "logs");
                    Directory.CreateDirectory(userLogs);
                    _logPath = Path.Combine(userLogs, $"app_{DateTime.Now:yyyyMMdd}.log");
                }
                catch
                {
                    // Last resort: use application base directory (may be read-only under Program Files)
                    var fallbackDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                    Directory.CreateDirectory(fallbackDir);
                    _logPath = Path.Combine(fallbackDir, $"app_{DateTime.Now:yyyyMMdd}.log");
                }
            }
        }

        public void Debug(string message)
        {
            WriteLog("DEBUG", message);
        }

        public void Info(string message)
        {
            WriteLog("INFO", message);
        }

        public void Warning(string message)
        {
            WriteLog("WARNING", message);
        }

        public void Error(string message, Exception? ex = null)
        {
            var errorMessage = ex != null ? $"{message}\n{ex}" : message;
            WriteLog("ERROR", errorMessage);
        }

        private void WriteLog(string level, string message)
        {
            lock (_lockObj)
            {
                try
                {
                    var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                    File.AppendAllText(_logPath, logEntry + Environment.NewLine);
                }
                catch
                {
                    // 忽略日志写入失败，避免循环错误
                }
            }
        }
    }
}