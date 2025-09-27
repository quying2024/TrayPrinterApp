using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TrayApp.Core;
using iTextSharp.text.pdf;

namespace TrayApp.Printing
{
    /// <summary>
    /// 外部程序打印管理器，实现IPrintManager接口
    /// 支持调用外部应用程序打印文件，并计算打印页码
    /// </summary>
    public class ExternalPrintManager : IPrintManager
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger _logger;
        private readonly Dictionary<string, IPageCounter> _pageCounters = new Dictionary<string, IPageCounter>();
        private readonly Dictionary<string, int> _printerUsageCount = new Dictionary<string, int>();

        /// <summary>
        /// 当打印完成时触发
        /// </summary>
        public event EventHandler<PrintCompletedEventArgs>? PrintCompleted;

        /// <summary>
        /// 初始化ExternalPrintManager实例
        /// </summary>
        /// <param name="configurationService">配置服务</param>
        /// <param name="logger">日志服务</param>
        public ExternalPrintManager(IConfigurationService configurationService, ILogger logger)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // 注册页码计数器
            RegisterPageCounters();
        }

        /// <summary>
        /// 注册所有页码计数器
        /// </summary>
        private void RegisterPageCounters()
        {
            _pageCounters.Add("PdfPageCounter", new PdfPageCounter(_logger));
            _pageCounters.Add("WordPageCounter", new WordPageCounter(_logger));
            _pageCounters.Add("ImagePageCounter", new ImagePageCounter(_logger));
            _logger.Info($"已注册 {_pageCounters.Count} 种文件页码计数器");
        }

        /// <summary>
        /// 获取可用打印机列表（排除配置中隐藏的打印机）
        /// </summary>
        /// <returns>打印机名称列表</returns>
        public List<string> GetAvailablePrinters()
        {
            var hiddenPrinters = _configurationService.GetHiddenPrinters();
            var availablePrinters = new List<string>();

            try
            {
                // 获取所有已安装的打印机
                foreach (var printer in PrinterSettings.InstalledPrinters)
                {
                    var printerName = printer.ToString() ?? string.Empty;
                    
                    // 排除隐藏的打印机
                    if (!hiddenPrinters.Contains(printerName, StringComparer.OrdinalIgnoreCase))
                    {
                        availablePrinters.Add(printerName);
                    }
                }

                // 根据使用频率排序
                if (_configurationService.GetSettings().PrinterManagement.DisplayOrder == "UsageFrequency")
                {
                    availablePrinters = availablePrinters
                        .OrderByDescending(p => _printerUsageCount.TryGetValue(p, out int count) ? count : 0)
                        .ToList();
                }

                _logger.Info($"获取可用打印机 {availablePrinters.Count} 台");
                return availablePrinters;
            }
            catch (Exception ex)
            {
                _logger.Error("获取打印机列表失败", ex);
                return new List<string>();
            }
        }

        /// <summary>
        /// 计算文件总页码
        /// </summary>
        /// <param name="filePaths">文件路径列表</param>
        /// <returns>总页码</returns>
        public int CalculateTotalPages(IEnumerable<string> filePaths)
        {
            if (filePaths == null) throw new ArgumentNullException(nameof(filePaths));

            int totalPages = 0;

            foreach (var filePath in filePaths)
            {
                try
                {
                    var extension = Path.GetExtension(filePath).ToLower();
                    var association = _configurationService.GetFileTypeAssociation(extension);

                    if (association == null || string.IsNullOrEmpty(association.PageCounterType))
                    {
                        _logger.Warning($"未找到 {extension} 文件的页码计数器配置，默认按1页计算");
                        totalPages += 1;
                        continue;
                    }

                    if (_pageCounters.TryGetValue(association.PageCounterType, out var counter))
                    {
                        int pages = counter.CountPages(filePath);
                        _logger.Info($"文件 {Path.GetFileName(filePath)} 页码: {pages}");
                        totalPages += pages;
                    }
                    else
                    {
                        _logger.Warning($"未找到页码计数器: {association.PageCounterType}，默认按1页计算");
                        totalPages += 1;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"计算文件页码失败: {filePath}", ex);
                    totalPages += 1; // 出错时默认按1页计算
                }
            }

            return totalPages;
        }

        /// <summary>
        /// 打印文件列表
        /// </summary>
        /// <param name="filePaths">文件路径列表</param>
        /// <param name="printerName">目标打印机名称</param>
        public void PrintFiles(IEnumerable<string> filePaths, string printerName)
        {
            if (filePaths == null) throw new ArgumentNullException(nameof(filePaths));
            if (string.IsNullOrEmpty(printerName)) throw new ArgumentException("打印机名称不能为空", nameof(printerName));

            var fileList = filePaths.ToList();
            if (fileList.Count == 0)
            {
                _logger.Info("没有文件需要打印");
                return;
            }

            try
            {
                _logger.Info($"开始打印 {fileList.Count} 个文件到打印机: {printerName}");
                
                // 更新打印机使用次数
                UpdatePrinterUsageCount(printerName);

                // 计算总页码
                int totalPages = CalculateTotalPages(fileList);

                // 逐个打印文件
                foreach (var filePath in fileList)
                {
                    if (!File.Exists(filePath))
                    {
                        _logger.Error($"文件不存在: {filePath}");
                        continue;
                    }

                    if (!PrintFile(filePath, printerName))
                    {
                        _logger.Error($"文件打印失败: {filePath}");
                    }
                }

                _logger.Info($"所有文件打印完成，总页码: {totalPages}");
                
                // 触发打印完成事件
                PrintCompleted?.Invoke(this, new PrintCompletedEventArgs
                {
                    Success = true,
                    FilePaths = fileList,
                    PrinterName = printerName,
                    TotalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                _logger.Error("批量打印失败", ex);
                PrintCompleted?.Invoke(this, new PrintCompletedEventArgs
                {
                    Success = false,
                    FilePaths = fileList,
                    PrinterName = printerName,
                    TotalPages = 0
                });
            }
        }

        /// <summary>
        /// 打印单个文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="printerName">打印机名称</param>
        /// <returns>打印是否成功</returns>
        private bool PrintFile(string filePath, string printerName)
        {
            try
            {
                var extension = Path.GetExtension(filePath).ToLower();
                var association = _configurationService.GetFileTypeAssociation(extension);

                if (association == null)
                {
                    _logger.Error($"未找到 {extension} 文件类型的打印配置");
                    return false;
                }

                if (!File.Exists(association.ExecutorPath))
                {
                    _logger.Error($"打印程序不存在: {association.ExecutorPath}");
                    return false;
                }

                // 替换参数占位符
                var arguments = association.Arguments
                    .Replace("{FilePath}", $"\"{filePath}\"", StringComparison.OrdinalIgnoreCase)
                    .Replace("{PrinterName}", $"\"{printerName}\"", StringComparison.OrdinalIgnoreCase);

                _logger.Info($"执行打印命令: {association.ExecutorPath} {arguments}");

                // 启动外部打印程序
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = association.ExecutorPath,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.Start();
                    
                    // 读取输出（防止进程阻塞）
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();
                    
                    // 等待进程完成，设置超时时间（5分钟）
                    if (process.WaitForExit(300000)) // 5分钟 = 300,000毫秒
                    {
                        var exitCode = process.ExitCode;
                        var output = outputTask.Result;
                        var error = errorTask.Result;

                        if (!string.IsNullOrEmpty(output))
                            _logger.Debug($"打印程序输出: {output}");
                        
                        if (exitCode == 0)
                        {
                            _logger.Info($"文件打印成功: {Path.GetFileName(filePath)}");
                            return true;
                        }
                        else
                        {
                            _logger.Error($"打印程序退出代码: {exitCode}, 错误输出: {error}");
                        }
                    }
                    else
                    {
                        // 超时，终止进程
                        process.Kill();
                        _logger.Error($"打印超时，已终止进程: {association.ExecutorPath}");
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.Error($"打印文件失败: {filePath}", ex);
                return false;
            }
        }

        /// <summary>
        /// 更新打印机使用次数
        /// </summary>
        private void UpdatePrinterUsageCount(string printerName)
        {
            if (_printerUsageCount.ContainsKey(printerName))
            {
                _printerUsageCount[printerName]++;
            }
            else
            {
                _printerUsageCount[printerName] = 1;
            }
        }
    }

    #region 页码计数器实现

    /// <summary>
    /// 页码计数器接口
    /// </summary>
    public interface IPageCounter
    {
        int CountPages(string filePath);
    }

    /// <summary>
    /// PDF页码计数器
    /// </summary>
    public class PdfPageCounter : IPageCounter
    {
        private readonly ILogger _logger;

        public PdfPageCounter(ILogger logger)
        {
            _logger = logger;
        }

        public int CountPages(string filePath)
        {
            try
            {
                // 使用iTextSharp库读取PDF页码
                using (var reader = new iTextSharp.text.pdf.PdfReader(filePath))
                {
                    return reader.NumberOfPages;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"获取PDF页码失败: {filePath}", ex);
                return 1; // 出错时默认按1页计算
            }
        }
    }

    /// <summary>
    /// Word页码计数器
    /// </summary>
    public class WordPageCounter : IPageCounter
    {
        private readonly ILogger _logger;

        public WordPageCounter(ILogger logger)
        {
            _logger = logger;
        }

        public int CountPages(string filePath)
        {
            Microsoft.Office.Interop.Word.Application? wordApp = null;
            Microsoft.Office.Interop.Word.Document? doc = null;
            
            try
            {
                // 使用Microsoft.Office.Interop.Word库
                wordApp = new Microsoft.Office.Interop.Word.Application();
                wordApp.Visible = false;
                wordApp.DisplayAlerts = Microsoft.Office.Interop.Word.WdAlertLevel.wdAlertsNone;

                doc = wordApp.Documents.Open(
                    fileName: filePath,
                    ReadOnly: true
                );

                int pages = doc.ComputeStatistics(Microsoft.Office.Interop.Word.WdStatistic.wdStatisticPages);
                return pages;
            }
            catch (Exception ex)
            {
                _logger.Error($"获取Word页码失败: {filePath}", ex);
                return 1; // 出错时默认按1页计算
            }
            finally
            {
                // 正确释放COM对象
                try
                {
                    doc?.Close(false);
                    wordApp?.Quit(false);
                    
                    if (doc != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(doc);
                    if (wordApp != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
                }
                catch (Exception ex)
                {
                    _logger.Error("释放Word COM对象失败", ex);
                }
            }
        }
    }

    /// <summary>
    /// 图片页码计数器（默认为1页）
    /// </summary>
    public class ImagePageCounter : IPageCounter
    {
        private readonly ILogger _logger;

        public ImagePageCounter(ILogger logger)
        {
            _logger = logger;
        }

        public int CountPages(string filePath)
        {
            try
            {
                // 图片文件默认按1页计算
                return 1;
            }
            catch (Exception ex)
            {
                _logger.Error($"获取图片页码失败: {filePath}", ex);
                return 1;
            }
        }
    }

    #endregion
}