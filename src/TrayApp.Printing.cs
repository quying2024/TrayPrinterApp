using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TrayApp.Core;
using TrayApp.Printing.Core;
using TrayApp.Printing.Engines;
using TrayApp.Printing.Converters;

namespace TrayApp.Printing
{
    /// <summary>
    /// 新一代统一打印管理器
    /// 核心理念：所有文件先转换为PDF，然后通过统一的PDF打印引擎输出
    /// </summary>
    public class UnifiedPrintManager : IPrintManager, IDisposable
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger _logger;
        private readonly IPdfPrintEngine _pdfPrintEngine;
        private readonly IConverterFactory _converterFactory;
        private readonly Dictionary<string, int> _printerUsageCount = new Dictionary<string, int>();
        private bool _disposed = false;

        /// <summary>
        /// 当打印完成时触发
        /// </summary>
        public event EventHandler<PrintCompletedEventArgs>? PrintCompleted;

        /// <summary>
        /// 初始化统一打印管理器
        /// </summary>
        public UnifiedPrintManager(IConfigurationService configurationService, ILogger logger)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 初始化核心组件
            _pdfPrintEngine = new PdfiumPrintEngine(_logger);
            _converterFactory = new ConverterFactory(_logger);

            // 注册所有转换器
            RegisterConverters();

            _logger.Info("统一打印管理器已初始化");
        }

        /// <summary>
        /// 注册所有文件转换器
        /// </summary>
        private void RegisterConverters()
        {
            try
            {
                // 注册PDF转换器（直通）
                _converterFactory.RegisterConverter(new PdfConverter(_logger));

                // 注册图片转换器
                _converterFactory.RegisterConverter(new ImageToPdfConverter(_logger));

                // 注册Word转换器
                _converterFactory.RegisterConverter(new WordToPdfConverter(_logger));

                var supportedExtensions = _converterFactory.GetSupportedExtensions().ToList();
                _logger.Info($"已注册文件转换器，支持格式: {string.Join(", ", supportedExtensions)}");
            }
            catch (Exception ex)
            {
                _logger.Error("注册文件转换器失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 获取可用打印机列表（排除配置中隐藏的打印机）
        /// </summary>
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
        /// 使用新的转换器架构计算页数
        /// </summary>
        public int CalculateTotalPages(IEnumerable<string> filePaths)
        {
            if (filePaths == null) throw new ArgumentNullException(nameof(filePaths));

            int totalPages = 0;

            foreach (var filePath in filePaths)
            {
                try
                {
                    var extension = Path.GetExtension(filePath).ToLower();
                    var converter = _converterFactory.GetConverter(extension);

                    if (converter == null)
                    {
                        _logger.Warning($"不支持的文件类型: {extension}，默认按1页计算");
                        totalPages += 1;
                        continue;
                    }

                    int pages = converter.CountPages(filePath);
                    _logger.Info($"文件 {Path.GetFileName(filePath)} 页码: {pages}");
                    totalPages += pages;
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
        /// 新架构：转换为PDF后统一打印
        /// </summary>
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

            var startTime = DateTime.Now;
            var successfulFiles = new List<string>();
            var failedFiles = new List<string>();
            int totalPages = 0;

            try
            {
                _logger.Info($"开始统一打印流程: {fileList.Count} 个文件到打印机 {printerName}");
                
                // 更新打印机使用次数
                UpdatePrinterUsageCount(printerName);

                // 逐个处理文件：转换 -> 打印
                foreach (var filePath in fileList)
                {
                    if (!File.Exists(filePath))
                    {
                        _logger.Error($"文件不存在: {filePath}");
                        failedFiles.Add(filePath);
                        continue;
                    }

                    var result = ProcessAndPrintFile(filePath, printerName);
                    if (result.success)
                    {
                        successfulFiles.Add(filePath);
                        totalPages += result.pages;
                        _logger.Info($"文件打印成功: {Path.GetFileName(filePath)} ({result.pages}页)");
                    }
                    else
                    {
                        failedFiles.Add(filePath);
                        _logger.Error($"文件打印失败: {Path.GetFileName(filePath)}");
                    }
                }

                var duration = DateTime.Now - startTime;
                var isFullSuccess = failedFiles.Count == 0;

                _logger.Info($"打印流程完成 - 成功: {successfulFiles.Count}, 失败: {failedFiles.Count}, " +
                           $"总页数: {totalPages}, 耗时: {duration.TotalSeconds:F1}秒");

                // 触发打印完成事件
                PrintCompleted?.Invoke(this, new PrintCompletedEventArgs
                {
                    Success = isFullSuccess,
                    FilePaths = successfulFiles, // 只返回成功的文件
                    PrinterName = printerName,
                    TotalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                _logger.Error("批量打印过程发生异常", ex);
                PrintCompleted?.Invoke(this, new PrintCompletedEventArgs
                {
                    Success = false,
                    FilePaths = new List<string>(),
                    PrinterName = printerName,
                    TotalPages = 0
                });
            }
        }

        /// <summary>
        /// 处理并打印单个文件
        /// 核心流程：文件 -> 转换器 -> PDF流 -> PDF打印引擎 -> 打印机
        /// </summary>
        private (bool success, int pages) ProcessAndPrintFile(string filePath, string printerName)
        {
            Stream? pdfStream = null;
            
            try
            {
                var extension = Path.GetExtension(filePath).ToLower();
                var converter = _converterFactory.GetConverter(extension);

                if (converter == null)
                {
                    _logger.Error($"不支持的文件类型: {extension}");
                    return (false, 0);
                }

                _logger.Debug($"开始处理文件: {Path.GetFileName(filePath)} (类型: {extension})");

                // 第一步：转换为PDF流
                pdfStream = converter.ConvertToPdfStream(filePath);
                
                if (pdfStream == null || pdfStream.Length == 0)
                {
                    _logger.Error($"文件转换为PDF失败: {filePath}");
                    return (false, 0);
                }

                _logger.Debug($"文件转换为PDF成功: {Path.GetFileName(filePath)}, PDF大小: {pdfStream.Length} bytes");

                // 第二步：计算页数
                int pages = converter.CountPages(filePath);

                // 第三步：使用PDF打印引擎打印
                bool printSuccess = _pdfPrintEngine.PrintPdf(pdfStream, printerName);

                if (printSuccess)
                {
                    _logger.Debug($"PDF打印成功: {Path.GetFileName(filePath)}");
                    return (true, pages);
                }
                else
                {
                    _logger.Error($"PDF打印失败: {Path.GetFileName(filePath)}");
                    return (false, 0);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"处理文件时发生异常: {filePath}", ex);
                return (false, 0);
            }
            finally
            {
                // 释放PDF流资源
                pdfStream?.Dispose();
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

        /// <summary>
        /// 获取支持的文件类型
        /// </summary>
        public IEnumerable<string> GetSupportedFileTypes()
        {
            return _converterFactory.GetSupportedExtensions();
        }

        /// <summary>
        /// 检查文件类型是否支持
        /// </summary>
        public bool IsFileTypeSupported(string fileExtension)
        {
            return _converterFactory.IsSupported(fileExtension);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                try
                {
                    _pdfPrintEngine?.Dispose();
                    _logger?.Info("统一打印管理器已释放");
                }
                catch (Exception ex)
                {
                    _logger?.Error("释放打印管理器资源时发生错误", ex);
                }
            }

            _disposed = true;
        }
    }
}