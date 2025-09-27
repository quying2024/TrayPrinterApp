using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TrayApp.Core;

namespace TrayApp.FolderMonitor
{
    /// <summary>
    /// 文件系统监视实现，支持批量文件收集
    /// 当监视目录中有新文件创建时，会等待指定的超时时间，收集所有新增文件后触发事件
    /// </summary>
    public class FileSystemWatcherMonitor : IFolderMonitor
    {
        private readonly ILogger _logger;
        private FileSystemWatcher? _fileSystemWatcher;
        private readonly List<string> _pendingFiles = new List<string>();
        private System.Timers.Timer? _batchTimer;
        private int _batchTimeoutSeconds;
        private HashSet<string> _monitoredFileTypes = new HashSet<string>();
        private readonly object _lockObj = new object();

        /// <summary>
        /// 当批量文件准备好处理时触发
        /// </summary>
        public event EventHandler<FileBatchEventArgs>? FilesBatchReady;

        /// <summary>
        /// 初始化FileSystemWatcherMonitor实例
        /// </summary>
        /// <param name="logger">日志服务</param>
        public FileSystemWatcherMonitor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 开始监视指定目录
        /// </summary>
        /// <param name="path">要监视的目录路径</param>
        /// <param name="batchTimeoutSeconds">批量收集超时时间（秒）</param>
        /// <param name="fileTypes">要监视的文件类型（如[".pdf", ".docx"]）</param>
        public void StartMonitoring(string path, int batchTimeoutSeconds, IEnumerable<string> fileTypes)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("监视路径不能为空", nameof(path));

            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"监视路径不存在: {path}");

            if (batchTimeoutSeconds < 1)
                throw new ArgumentException("批量超时时间必须大于0", nameof(batchTimeoutSeconds));

            StopMonitoring(); // 确保之前的监视已停止

            _batchTimeoutSeconds = batchTimeoutSeconds;
            _monitoredFileTypes = new HashSet<string>(fileTypes.Select(t => t.ToLower()));

            // 初始化文件系统监视
            _fileSystemWatcher = new FileSystemWatcher
            {
                Path = path,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
                IncludeSubdirectories = false,
                Filter = "*.*" // 先监视所有文件，后续再筛选类型
            };

            // 订阅文件创建事件
            _fileSystemWatcher.Created += OnFileCreated;

            // 初始化批量计时器
            _batchTimer = new System.Timers.Timer(batchTimeoutSeconds * 1000);
            _batchTimer.Elapsed += OnBatchTimerElapsed;
            _batchTimer.AutoReset = false; // 只触发一次

            _fileSystemWatcher.EnableRaisingEvents = true;
            _logger.Info($"开始监视目录: {path}, 批量超时: {batchTimeoutSeconds}秒, 文件类型: {string.Join(",", fileTypes)}");
        }

        /// <summary>
        /// 停止监视目录
        /// </summary>
        public void StopMonitoring()
        {
            lock (_lockObj)
            {
                if (_fileSystemWatcher != null)
                {
                    _fileSystemWatcher.EnableRaisingEvents = false;
                    _fileSystemWatcher.Created -= OnFileCreated;
                    _fileSystemWatcher.Dispose();
                    _fileSystemWatcher = null;
                }

                if (_batchTimer != null)
                {
                    _batchTimer.Stop();
                    _batchTimer.Elapsed -= OnBatchTimerElapsed;
                    _batchTimer.Dispose();
                    _batchTimer = null;
                }

                _pendingFiles.Clear();
            }

            _logger.Info("已停止目录监视");
        }

        /// <summary>
        /// 处理文件创建事件
        /// </summary>
        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                // 获取文件扩展名并转换为小写
                var fileExtension = Path.GetExtension(e.FullPath).ToLower();

                // 检查文件类型是否在监视列表中
                if (!_monitoredFileTypes.Contains(fileExtension))
                {
                    _logger.Debug($"忽略不监视的文件类型: {e.FullPath}");
                    return;
                }

                // 检查文件是否可访问（可能还在被写入）
                if (!IsFileReady(e.FullPath))
                {
                    _logger.Debug($"文件尚未准备好: {e.FullPath}，将稍后重试");
                    // 稍后重试检查
                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        await System.Threading.Tasks.Task.Delay(1000);
                        if (IsFileReady(e.FullPath))
                        {
                            AddFileToBatch(e.FullPath);
                        }
                        else
                        {
                            _logger.Warning($"无法访问文件: {e.FullPath}");
                        }
                    });
                    return;
                }

                AddFileToBatch(e.FullPath);
            }
            catch (Exception ex)
            {
                _logger.Error($"处理文件创建事件失败: {e.FullPath}", ex);
            }
        }

        /// <summary>
        /// 将文件添加到批量处理队列
        /// </summary>
        private void AddFileToBatch(string filePath)
        {
            lock (_lockObj)
            {
                // 避免重复添加
                if (!_pendingFiles.Contains(filePath))
                {
                    _pendingFiles.Add(filePath);
                    _logger.Info($"文件已添加到批量队列: {filePath}, 当前队列大小: {_pendingFiles.Count}");

                    // 重置批量计时器
                    _batchTimer?.Stop();
                    _batchTimer?.Start();
                }
            }
        }

        /// <summary>
        /// 批量计时器超时处理
        /// </summary>
        private void OnBatchTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (_lockObj)
            {
                if (_pendingFiles.Count > 0)
                {
                    _logger.Info($"批量处理超时，共 {_pendingFiles.Count} 个文件准备就绪");
                    
                    // 触发批量文件就绪事件
                    FilesBatchReady?.Invoke(this, new FileBatchEventArgs
                    {
                        FilePaths = new List<string>(_pendingFiles)
                    });

                    // 清空队列
                    _pendingFiles.Clear();
                }
            }
        }

        /// <summary>
        /// 检查文件是否已准备好（不被其他进程锁定）
        /// </summary>
        private bool IsFileReady(string filePath)
        {
            try
            {
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return stream.Length > 0;
                }
            }
            catch (IOException)
            {
                // 文件被锁定，返回false
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error($"检查文件就绪状态失败: {filePath}", ex);
                return false;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            StopMonitoring();
        }
    }
}