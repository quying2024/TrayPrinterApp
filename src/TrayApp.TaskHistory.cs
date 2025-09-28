using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TrayApp.Core;

namespace TrayApp.TaskHistory
{
    /// <summary>
    /// 任务历史管理器，实现ITaskHistoryManager接口
    /// 负责记录和查询打印任务历史
    /// </summary>
    public class TaskHistoryManager : ITaskHistoryManager, IDisposable
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger _logger;
        private readonly List<PrintTaskRecord> _taskHistory = new List<PrintTaskRecord>();
        private readonly object _lockObj = new object();
        private string _storagePath = "task_history.json";
        private int _maxRecords = 5;
        private bool _disposed = false;

        /// <summary>
        /// 初始化TaskHistoryManager实例
        /// </summary>
        /// <param name="configurationService">配置服务</param>
        /// <param name="logger">日志服务</param>
        public TaskHistoryManager(IConfigurationService configurationService, ILogger logger)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // 从配置加载设置
            LoadSettings();
            
            // 加载历史记录
            LoadHistory();
        }

        /// <summary>
        /// 从配置加载设置
        /// </summary>
        private void LoadSettings()
        {
            var settings = _configurationService.GetTaskHistorySettings();
            _maxRecords = settings.MaxRecords;
            
            // 确保历史记录文件保存在用户数据目录
            _storagePath = GetUserDataPath(settings.StoragePath);
            
            if (_maxRecords < 1)
            {
                _logger.Warning($"无效的最大记录数: {_maxRecords}，使用默认值5");
                _maxRecords = 5;
            }
            
            _logger.Info($"任务历史配置: 最大记录数={_maxRecords}, 存储路径={_storagePath}");
        }

        /// <summary>
        /// 获取用户数据目录中的文件路径
        /// </summary>
        private string GetUserDataPath(string fileName)
        {
            try
            {
                // 优先使用用户数据目录（普通用户权限可写）
                var userDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TrayPrinterApp");
                Directory.CreateDirectory(userDataDir);
                return Path.Combine(userDataDir, Path.GetFileName(fileName));
            }
            catch
            {
                // 如果失败，尝试使用公共应用程序数据目录
                try
                {
                    var commonDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TrayPrinterApp");
                    Directory.CreateDirectory(commonDataDir);
                    return Path.Combine(commonDataDir, Path.GetFileName(fileName));
                }
                catch
                {
                    // 最后尝试应用程序目录
                    return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(fileName));
                }
            }
        }

        /// <summary>
        /// 加载历史记录
        /// </summary>
        private void LoadHistory()
        {
            lock (_lockObj)
            {
                try
                {
                    if (File.Exists(_storagePath))
                    {
                        string json = File.ReadAllText(_storagePath);
                        var history = JsonConvert.DeserializeObject<List<PrintTaskRecord>>(json) ?? new List<PrintTaskRecord>();
                        
                        // 按时间戳排序（最新的在前）
                        _taskHistory.AddRange(history.OrderByDescending(r => r.Timestamp));
                        
                        // 确保不超过最大记录数
                        TrimHistory();
                        
                        _logger.Info($"已加载 {_taskHistory.Count} 条任务历史记录");
                    }
                    else
                    {
                        _logger.Info("任务历史文件不存在，将创建新文件");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("加载任务历史失败", ex);
                    // 清除损坏的历史记录
                    _taskHistory.Clear();
                }
            }
        }

        /// <summary>
        /// 保存历史记录到文件
        /// </summary>
        private void SaveHistory()
        {
            lock (_lockObj)
            {
                try
                {
                    // 保存完整的记录信息
                    string json = JsonConvert.SerializeObject(_taskHistory, Formatting.Indented);
                    File.WriteAllText(_storagePath, json);
                    _logger.Debug($"已保存 {_taskHistory.Count} 条任务历史记录");
                }
                catch (Exception ex)
                {
                    _logger.Error("保存任务历史失败", ex);
                }
            }
        }

        /// <summary>
        /// 修剪历史记录，确保不超过最大记录数
        /// </summary>
        private void TrimHistory()
        {
            if (_taskHistory.Count > _maxRecords)
            {
                int removeCount = _taskHistory.Count - _maxRecords;
                _taskHistory.RemoveRange(_maxRecords, removeCount);
                _logger.Debug($"已修剪 {removeCount} 条旧任务历史记录");
            }
        }

        /// <summary>
        /// 添加任务记录
        /// </summary>
        /// <param name="record">任务记录</param>
        public void AddTaskRecord(PrintTaskRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (record.FileCount <= 0)
            {
                _logger.Warning("忽略无效的任务记录（文件数为0）");
                return;
            }

            lock (_lockObj)
            {
                // 添加新记录到开头
                _taskHistory.Insert(0, record);
                
                // 修剪历史记录
                TrimHistory();
                
                // 保存到文件
                SaveHistory();
                
                _logger.Info($"已添加任务记录: {record.FileCount}个文件, {record.TotalPages}页, 打印机: {record.PrinterName}");
            }
        }

        /// <summary>
        /// 获取最近的任务记录
        /// </summary>
        /// <param name="count">要获取的记录数</param>
        /// <returns>任务记录列表（按时间倒序）</returns>
        public List<PrintTaskRecord> GetRecentTasks(int count)
        {
            lock (_lockObj)
            {
                if (count <= 0)
                {
                    return new List<PrintTaskRecord>();
                }
                
                count = Math.Min(count, _taskHistory.Count);
                return _taskHistory.Take(count).ToList();
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // 释放托管资源
                lock (_lockObj)
                {
                    // 保存最后的历史记录
                    try
                    {
                        SaveHistory();
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error("释放资源时保存历史记录失败", ex);
                    }
                }
            }

            _disposed = true;
        }
    }
}