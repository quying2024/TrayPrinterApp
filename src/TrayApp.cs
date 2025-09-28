using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using TrayApp.Core;
using TrayApp.FolderMonitor;
using TrayApp.Printing;
using TrayApp.FileOperations;
using TrayApp.Configuration;
using TrayApp.TaskHistory;
using TrayApp.UI;

namespace TrayApp
{
    /// <summary>
    /// 应用程序核心协调类
    /// </summary>
    public class AppCore : IDisposable
    {
        private readonly IConfigurationService _configurationService;
        private readonly IFolderMonitor _folderMonitor;
        private readonly IPrintManager _printManager;
        private readonly IFileOperator _fileOperator;
        private readonly ITaskHistoryManager _taskHistoryManager;
        private readonly TrayIconManager _trayIconManager;
        private readonly ILogger _logger;
        private string _watchPath = string.Empty;

        /// <summary>
        /// 初始化AppCore实例
        /// </summary>
        public AppCore()
        {
            // 创建日志服务（先于其他服务创建）
            _logger = new FileLogger();
            
            try
            {
                _logger.Info("===== 打印店自动打印系统启动 =====");

                // 创建核心服务
                _configurationService = new JsonConfigurationService("config/appsettings.json", _logger);
                _folderMonitor = new FileSystemWatcherMonitor(_logger);
                _printManager = new UnifiedPrintManager(_configurationService, _logger);  // 使用新的统一打印管理器
                _fileOperator = new TimestampFileOperator(_logger);
                _taskHistoryManager = new TaskHistoryManager(_configurationService, _logger);
                _trayIconManager = new TrayIconManager(_logger, _taskHistoryManager, _configurationService);

                // 订阅事件
                SubscribeEvents();

                _logger.Info("应用核心服务初始化完成（使用统一PDF打印架构）");
            }
            catch (Exception ex)
            {
                _logger.Error("应用核心初始化失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        private void SubscribeEvents()
        {
            // 文件夹监视事件
            _folderMonitor.FilesBatchReady += OnFilesBatchReady;
            
            // 打印完成事件
            _printManager.PrintCompleted += OnPrintCompleted;
            
            // 托盘图标事件
            _trayIconManager.ExitRequested += OnExitRequested;
            _trayIconManager.ConfigurationUpdated += OnConfigurationUpdated;
        }

        /// <summary>
        /// 启动应用
        /// </summary>
        public void Start()
        {
            try
            {
                StartMonitoring();
                
                // 启动Windows消息循环
                Application.Run();
            }
            catch (Exception ex)
            {
                _logger.Error("应用启动失败", ex);
                MessageBox.Show($"应用启动失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Stop();
            }
        }

        /// <summary>
        /// 开始监视文件夹
        /// </summary>
        private void StartMonitoring()
        {
            // 获取配置的监视路径
            _watchPath = _configurationService.GetWatchPath();
            
            // 确保监视目录存在
            if (!System.IO.Directory.Exists(_watchPath))
            {
                _logger.Warning($"监视目录不存在，创建目录: {_watchPath}");
                System.IO.Directory.CreateDirectory(_watchPath);
            }

            // 启动文件夹监视
            var fileTypes = _configurationService.GetMonitoredFileTypes();
            var batchTimeout = _configurationService.GetBatchTimeoutSeconds();
            
            _folderMonitor.StartMonitoring(_watchPath, batchTimeout, fileTypes);
            _logger.Info($"应用已启动，正在监视目录: {_watchPath}");
            
            // 更新托盘提示
            _trayIconManager.UpdateTrayTooltip();
        }

        /// <summary>
        /// 停止应用
        /// </summary>
        public void Stop()
        {
            // 停止文件夹监视
            _folderMonitor.StopMonitoring();
            
            _logger.Info("应用已停止");
            _logger.Info("===== 打印店自动打印系统退出 =====");
            
            // 退出应用消息循环
            Application.Exit();
        }

        /// <summary>
        /// 处理批量文件就绪事件
        /// </summary>
        private void OnFilesBatchReady(object sender, FileBatchEventArgs e)
        {
            if (e.FilePaths == null || e.FilePaths.Count == 0)
            {
                _logger.Info("接收到空的文件批次，忽略处理");
                return;
            }

            _logger.Info($"接收到文件批次: {e.FilePaths.Count} 个文件");
            
            // 使用Task.Run在后台线程处理，避免跨线程调用问题
            Task.Run(() =>
            {
                ProcessFileBatch(e.FilePaths);
            });
        }

        /// <summary>
        /// 处理文件批次
        /// </summary>
        private void ProcessFileBatch(System.Collections.Generic.List<string> filePaths)
        {
            try
            {
                // 获取可用打印机
                var printers = _printManager.GetAvailablePrinters();
                if (printers.Count == 0)
                {
                    _logger.Error("没有可用打印机，无法处理文件批次");
                    MessageBox.Show("没有可用的打印机，请检查打印机配置", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 显示打印机选择对话框
                string? selectedPrinter = _trayIconManager.ShowPrinterSelectionDialog(printers, filePaths.Count);
                if (string.IsNullOrEmpty(selectedPrinter))
                {
                    _logger.Info("用户取消了打印机选择，文件批次处理中止");
                    return;
                }

                // 打印文件
                _printManager.PrintFiles(filePaths, selectedPrinter);
            }
            catch (Exception ex)
            {
                _logger.Error("处理文件批次失败", ex);
                MessageBox.Show($"处理文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 处理打印完成事件
        /// </summary>
        private void OnPrintCompleted(object sender, PrintCompletedEventArgs e)
        {
            if (e.Success)
            {
                _logger.Info($"打印任务成功完成，共 {e.FileCount} 个文件");
                
                // 移动文件到时间戳目录
                string targetDir = _fileOperator.MoveFilesToTimestampDirectory(e.FilePaths, _watchPath);
                
                // 记录任务历史
                _taskHistoryManager.AddTaskRecord(new PrintTaskRecord
                {
                    FileCount = e.FilePaths.Count,
                    TotalPages = e.TotalPages,
                    PrinterName = e.PrinterName,
                    FilePaths = e.FilePaths
                });
                
                // 更新托盘提示
                _trayIconManager.UpdateTrayTooltip();
            }
            else
            {
                _logger.Error("打印任务失败");
                MessageBox.Show("打印任务执行失败，请查看日志获取详细信息", "打印失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 处理退出请求
        /// </summary>
        private void OnExitRequested(object sender, EventArgs e)
        {
            Stop();
        }

        /// <summary>
        /// 处理配置更新事件
        /// </summary>
        private void OnConfigurationUpdated(object sender, EventArgs e)
        {
            try
            {
                _logger.Info("配置已更新，重新启动监视服务");
                
                // 停止当前监视
                _folderMonitor.StopMonitoring();
                
                // 重新启动监视
                StartMonitoring();
                
                _logger.Info("监视服务已重新启动");
            }
            catch (Exception ex)
            {
                _logger.Error("重新启动监视服务失败", ex);
                MessageBox.Show("配置更新后重新启动监视服务失败，请重启应用程序", "警告", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _folderMonitor?.Dispose();
            _taskHistoryManager?.Dispose();
            _trayIconManager?.Dispose();
        }
    }

    /// <summary>
    /// 应用程序入口点
    /// </summary>
    static class Program
    {
        /// <summary>
        /// 主入口点
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 启用视觉样式
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 确保单实例运行
            using (var mutex = new System.Threading.Mutex(true, "{8F6F0AC4-B9A1-45FD-A8CF-72F04E6BDE8F}", out bool createdNew))
            {
                if (createdNew)
                {
                    try
                    {
                        // 创建并启动应用核心
                        var appCore = new AppCore();
                        appCore.Start();
                        
                        // 启动Windows Forms消息循环
                        Application.Run();
                        
                        // 清理资源
                        appCore.Dispose();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"应用启动失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    // 已经有实例在运行，显示提示
                    MessageBox.Show("打印店自动打印系统已在运行中", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
