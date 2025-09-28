using System;
using System.IO;
using PdfiumViewer;
using System.Drawing.Printing;
using TrayApp.Core;
using TrayApp.Printing.Core;

namespace TrayApp.Printing.Engines
{
    /// <summary>
    /// ??PdfiumViewer?PDF????
    /// ?????PDF????
    /// </summary>
    public class PdfiumPrintEngine : IPdfPrintEngine, IDisposable
    {
        private readonly ILogger _logger;
        private bool _disposed = false;

        public PdfiumPrintEngine(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// ??PDF???????
        /// </summary>
        public bool PrintPdf(Stream pdfStream, string printerName, int copies = 1)
        {
            if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
            if (string.IsNullOrEmpty(printerName)) throw new ArgumentException("?????????", nameof(printerName));

            try
            {
                _logger.Info($"????PDF?????: {printerName}, ??: {copies}");

                // ?????????
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
                _logger.Error($"??PDF???: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// ??PDF????????
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
        /// ??PDF?????????
        /// </summary>
        private bool PrintPdfDocument(PdfDocument pdfDocument, string printerName, int copies)
        {
            try
            {
                using (var printDocument = pdfDocument.CreatePrintDocument())
                {
                    // ?????
                    printDocument.PrinterSettings.PrinterName = printerName;
                    printDocument.PrinterSettings.Copies = (short)Math.Max(1, Math.Min(copies, 999));

                    // ?????????
                    if (!printDocument.PrinterSettings.IsValid)
                    {
                        _logger.Error($"?????????: {printerName}");
                        return false;
                    }

                    // ??????
                    printDocument.PrinterSettings.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                    
                    // ??????????????
                    printDocument.PrintController = new StandardPrintController();

                    _logger.Debug($"???? - ???: {printerName}, ??: {pdfDocument.PageCount}, ??: {copies}");

                    // ????
                    printDocument.Print();

                    _logger.Info($"PDF?????? - ??: {pdfDocument.PageCount}, ??: {copies}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"??PDF????: {ex.Message}", ex);
                return false;
            }
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
                // ??????
                _logger?.Debug("PdfiumPrintEngine???");
            }

            _disposed = true;
        }
    }
}