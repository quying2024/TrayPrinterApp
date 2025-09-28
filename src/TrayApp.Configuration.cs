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
        /// <param name="configFileName">配置文件名（可选路径）</param>
        /// <param name="logger">日志服务</param>
        public JsonConfigurationService(string configFileName, ILogger logger)
        {
            _logger = logger;
            
            // 确定配置文件的完整路径，优先使用用户数据目录
            _configFilePath = GetConfigFilePath(configFileName);
            _settings = LoadOrCreateDefaultSettings();
        }

        /// <summary>
        /// 获取配置文件的完整路径
        /// </summary>
        private string GetConfigFilePath(string configFileName)
        {
            try
            {
                // 优先使用用户数据目录（普通用户权限可写）
                var userConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TrayPrinterApp");
                Directory.CreateDirectory(userConfigDir);
                return Path.Combine(userConfigDir, Path.GetFileName(configFileName));
            }
            catch
            {
                // 如果失败，尝试使用公共应用程序数据目录
                try
                {
                    var commonConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TrayPrinterApp");
                    Directory.CreateDirectory(commonConfigDir);
                    return Path.Combine(commonConfigDir, Path.GetFileName(configFileName));
                }
                catch
                {
                    // 最后尝试应用程序目录下的config子目录
                    var appConfigDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
                    Directory.CreateDirectory(appConfigDir);
                    return Path.Combine(appConfigDir, Path.GetFileName(configFileName));
                }
            }
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
        /// 获取打印设置
        /// </summary>
        /// <returns>打印设置</returns>
        public PrintSettings GetPrintSettings()
        {
            return _settings.PrintSettings;
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
                    FileTypes = new List<string> { ".pdf", ".docx", ".doc", ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".tif", ".webp" }
                },
                PrinterManagement = new PrinterManagementSettings
                {
                    HiddenPrinters = new List<string> { "Microsoft Print to PDF", "Fax", "OneNote for Windows 10" },
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