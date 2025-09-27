using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TrayApp.Core;

namespace TrayApp.Configuration
{
    /// <summary>
    /// JSON配置服务实现
    /// 负责读取和写入应用配置，支持配置文件的加载和保存
    /// </summary>
    public class JsonConfigurationService : IConfigurationService
    {
        private readonly string _configFilePath;
        private readonly ILogger _logger;
        private AppSettings _settings;

        /// <summary>
        /// 初始化JsonConfigurationService实例
        /// </summary>
        /// <param name="configFilePath">配置文件路径</param>
        /// <param name="logger">日志服务</param>
        public JsonConfigurationService(string configFilePath, ILogger logger)
        {
            _configFilePath = configFilePath;
            _logger = logger;
            _settings = LoadOrCreateDefaultSettings();
        }

        /// <summary>
        /// 获取应用配置
        /// </summary>
        /// <returns>应用配置对象</returns>
        public AppSettings GetSettings()
        {
            return _settings;
        }

        /// <summary>
        /// 保存应用配置
        /// </summary>
        /// <param name="settings">要保存的配置对象</param>
        public void SaveSettings(AppSettings settings)
        {
            try
            {
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(_configFilePath, json);
                _settings = settings;
                _logger.Info($"配置已保存到 {_configFilePath}");
            }
            catch (Exception ex)
            {
                _logger.Error("保存配置失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 获取监视文件夹路径
        /// </summary>
        /// <returns>监视文件夹路径</returns>
        public string GetWatchPath()
        {
            return _settings.Monitoring.WatchPath;
        }

        /// <summary>
        /// 获取批量处理超时时间（秒）
        /// </summary>
        /// <returns>超时时间（秒）</returns>
        public int GetBatchTimeoutSeconds()
        {
            return _settings.Monitoring.BatchTimeoutSeconds;
        }

        /// <summary>
        /// 获取要监视的文件类型列表
        /// </summary>
        /// <returns>文件类型列表（如[".pdf", ".docx"]）</returns>
        public List<string> GetMonitoredFileTypes()
        {
            return _settings.Monitoring.FileTypes;
        }

        /// <summary>
        /// 获取要隐藏的打印机列表
        /// </summary>
        /// <returns>打印机名称列表</returns>
        public List<string> GetHiddenPrinters()
        {
            return _settings.PrinterManagement.HiddenPrinters;
        }

        /// <summary>
        /// 获取文件类型关联配置
        /// </summary>
        /// <param name="fileExtension">文件扩展名（如".pdf"）</param>
        /// <returns>文件类型关联配置，若不存在则返回null</returns>
        public FileTypeAssociation? GetFileTypeAssociation(string fileExtension)
        {
            if (string.IsNullOrEmpty(fileExtension))
                return null;

            var extension = fileExtension.StartsWith(".") ? fileExtension : $".{fileExtension}";
            _settings.FileTypeAssociations.TryGetValue(extension.ToLower(), out var association);
            return association;
        }

        /// <summary>
        /// 获取任务历史记录配置
        /// </summary>
        /// <returns>任务历史记录配置</returns>
        public TaskHistorySettings GetTaskHistorySettings()
        {
            return _settings.TaskHistory;
        }

        /// <summary>
        /// 加载配置文件，若不存在则创建默认配置
        /// </summary>
        private AppSettings LoadOrCreateDefaultSettings()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? CreateDefaultSettings();
                }
                else
                {
                    _logger.Info($"配置文件不存在，创建默认配置: {_configFilePath}");
                    var defaultSettings = CreateDefaultSettings();
                    SaveSettings(defaultSettings);
                    return defaultSettings;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("加载配置失败，使用默认配置", ex);
                return CreateDefaultSettings();
            }
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        private AppSettings CreateDefaultSettings()
        {
            return new AppSettings
            {
                Monitoring = new MonitoringSettings
                {
                    WatchPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "PrintJobs"),
                    BatchTimeoutSeconds = 3,
                    FileTypes = new List<string> { ".pdf", ".docx", ".jpg", ".png", ".doc" }
                },
                PrinterManagement = new PrinterManagementSettings
                {
                    HiddenPrinters = new List<string> { "Microsoft Print to PDF", "Fax" },
                    DisplayOrder = "UsageFrequency"
                },
                FileTypeAssociations = new Dictionary<string, FileTypeAssociation>
                {
                    {
                        ".pdf", new FileTypeAssociation
                        {
                            ExecutorPath = "C:\\Program Files\\Adobe\\Acrobat DC\\Acrobat\\Acrobat.exe",
                            Arguments = "/t \"{FilePath}\" \"{PrinterName}\"",
                            PageCounterType = "PdfPageCounter"
                        }
                    },
                    {
                        ".docx", new FileTypeAssociation
                        {
                            ExecutorPath = "C:\\Program Files\\Microsoft Office\\root\\Office16\\WINWORD.EXE",
                            Arguments = "/q /n \"{FilePath}\" /mFilePrintDefault /mFileExit",
                            PageCounterType = "WordPageCounter"
                        }
                    },
                    {
                        ".jpg", new FileTypeAssociation
                        {
                            ExecutorPath = "mspaint.exe",
                            Arguments = "/pt \"{FilePath}\" \"{PrinterName}\"",
                            PageCounterType = "ImagePageCounter"
                        }
                    }
                },
                TaskHistory = new TaskHistorySettings
                {
                    MaxRecords = 5,
                    StoragePath = "task_history.json"
                },
                Logging = new LoggingSettings
                {
                    LogLevel = "Info",
                    LogFilePath = "app.log"
                }
            };
        }
    }
}