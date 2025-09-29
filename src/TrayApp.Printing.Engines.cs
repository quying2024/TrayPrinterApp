using System;
using System.IO;
using System.Drawing;
using System.Drawing.Printing;
using System.Drawing.Imaging;
using PdfiumViewer;
using TrayApp.Core;
using TrayApp.Printing.Core;

namespace TrayApp.Printing.Engines
{
    /// <summary>
    /// ??PdfiumViewer.Core + System.Drawing.Printing?PDF????
    /// ??.NET 8????PDF????
    /// </summary>
    public class PdfiumPrintEngine : IPdfPrintEngine, IDisposable
    {
        private readonly ILogger _logger;
        private bool _disposed = false;
        private PdfDocument? _currentDocument = null;
        private int _currentPageIndex = 0;
        private string _currentPrinterName = string.Empty;
        private int _totalCopies = 1;
        private int _currentCopy = 0;

        public PdfiumPrintEngine(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// ????PDF????????
        /// </summary>
        public bool PrintPdf(Stream pdfStream, string printerName, int copies = 1)
        {
            if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
            if (string.IsNullOrEmpty(printerName)) throw new ArgumentException("?????????", nameof(printerName));

            try
            {
                _logger.Info($"????PDF????: {printerName}, ??: {copies}");

                // ????????
                if (pdfStream.CanSeek)
                {
                    pdfStream.Position = 0;
                }

                using (var pdfDocument = PdfDocument.Load(pdfStream))
                {
                    return PrintPdfDocument(pdfDocument, printerName, copies);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"????PDF??: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// ?????PDF????????
        /// </summary>
        public bool PrintPdf(string pdfFilePath, string printerName, int copies = 1)
        {
            if (string.IsNullOrEmpty(pdfFilePath)) throw new ArgumentException("PDF????????", nameof(pdfFilePath));
            if (string.IsNullOrEmpty(printerName)) throw new ArgumentException("?????????", nameof(printerName));

            if (!File.Exists(pdfFilePath))
            {
                _logger.Error($"PDF?????: {pdfFilePath}");
                return false;
            }

            try
            {
                _logger.Info($"????PDF??: {Path.GetFileName(pdfFilePath)} ????: {printerName}, ??: {copies}");

                using (var pdfDocument = PdfDocument.Load(pdfFilePath))
                {
                    return PrintPdfDocument(pdfDocument, printerName, copies);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"??PDF????: {pdfFilePath}, {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// ??System.Drawing.Printing??PDF??
        /// </summary>
        private bool PrintPdfDocument(PdfDocument pdfDocument, string printerName, int copies)
        {
            try
            {
                // ?????????
                _currentDocument = pdfDocument;
                _currentPageIndex = 0;
                _currentPrinterName = printerName;
                _totalCopies = Math.Max(1, Math.Min(copies, 999));
                _currentCopy = 0;

                using (var printDocument = new PrintDocument())
                {
                    // ?????
                    printDocument.PrinterSettings.PrinterName = printerName;
                    printDocument.PrinterSettings.Copies = 1; // ??????????
                    printDocument.DocumentName = "TrayPrinterApp PDF";

                    // ????????
                    if (!printDocument.PrinterSettings.IsValid)
                    {
                        _logger.Error($"?????????: {printerName}");
                        return false;
                    }

                    // ??????
                    printDocument.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                    
                    // ????????????????
                    printDocument.PrintController = new StandardPrintController();

                    // ??????
                    printDocument.PrintPage += OnPrintPage;
                    printDocument.EndPrint += OnEndPrint;

                    _logger.Debug($"???? - ???: {printerName}, ??: {pdfDocument.PageCount}, ??: {copies}");

                    // ??????
                    for (_currentCopy = 0; _currentCopy < _totalCopies; _currentCopy++)
                    {
                        _currentPageIndex = 0; // ????
                        printDocument.Print();
                    }

                    _logger.Info($"PDF???? - ??: {pdfDocument.PageCount}, ??: {copies}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"??PDF????: {ex.Message}", ex);
                return false;
            }
            finally
            {
                // ???????
                _currentDocument = null;
                _currentPageIndex = 0;
                _currentPrinterName = string.Empty;
            }
        }

        /// <summary>
        /// ????????? - ??PdfiumViewer.Core??
        /// </summary>
        private void OnPrintPage(object? sender, PrintPageEventArgs e)
        {
            try
            {
                if (_currentDocument == null || e.Graphics == null)
                {
                    e.Cancel = true;
                    return;
                }

                if (_currentPageIndex >= _currentDocument.PageCount)
                {
                    e.HasMorePages = false;
                    return;
                }

                // ????????
                var printArea = e.MarginBounds;
                var dpiX = e.Graphics.DpiX;
                var dpiY = e.Graphics.DpiY;

                _logger.Debug($"???? {_currentPageIndex + 1}/{_currentDocument.PageCount}, " +
                             $"????: {printArea.Width}x{printArea.Height}, " +
                             $"DPI: {dpiX}x{dpiY}");

                // ??????
                var pageSize = _currentDocument.PageSizes[_currentPageIndex];
                var pageWidth = pageSize.Width;
                var pageHeight = pageSize.Height;

                // ?????????????
                var scaleX = printArea.Width / pageWidth;
                var scaleY = printArea.Height / pageHeight;
                var scale = Math.Min(scaleX, scaleY); // ?????

                // ?????????
                var renderWidth = (int)(pageWidth * scale);
                var renderHeight = (int)(pageHeight * scale);
                var x = printArea.X + (printArea.Width - renderWidth) / 2;
                var y = printArea.Y + (printArea.Height - renderHeight) / 2;

                // ??PdfiumViewer.Core???????
                using (var image = _currentDocument.Render(_currentPageIndex, renderWidth, renderHeight, dpiX, dpiY, false))
                {
                    // ????????????????
                    e.Graphics.DrawImage(image, x, y, renderWidth, renderHeight);
                    
                    _logger.Debug($"?????? - ??: {renderWidth}x{renderHeight}, ??: ({x},{y})");
                }

                _currentPageIndex++;
                e.HasMorePages = _currentPageIndex < _currentDocument.PageCount;
            }
            catch (Exception ex)
            {
                _logger.Error($"?????????: {ex.Message}", ex);
                e.Cancel = true;
            }
        }

        /// <summary>
        /// ?????????
        /// </summary>
        private void OnEndPrint(object? sender, PrintEventArgs e)
        {
            _logger.Debug($"?????? - ?? {_currentCopy + 1}/{_totalCopies}");
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
                // ????
                _currentDocument?.Dispose();
                _currentDocument = null;
                _logger?.Debug("PdfiumPrintEngine???");
            }

            _disposed = true;
        }
    }
}