using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Office.Interop.Word;
using TrayApp.Core;
using TrayApp.Printing.Core;

namespace TrayApp.Printing.Converters
{
    /// <summary>
    /// Word???PDF???
    /// ?? DOC, DOCX ??
    /// ?? Microsoft Office Interop ??
    /// </summary>
    public class WordToPdfConverter : IFileConverter, IDisposable
    {
        private readonly ILogger _logger;
        private bool _disposed = false;

        public IEnumerable<string> SupportedExtensions => new[] { ".doc", ".docx" };

        public WordToPdfConverter(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Stream ConvertToPdfStream(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("????????", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException($"?????: {filePath}");

            Microsoft.Office.Interop.Word.Application? wordApp = null;
            Document? document = null;
            string tempPdfPath = "";

            try
            {
                _logger.Debug($"????Word???PDF: {Path.GetFileName(filePath)}");

                // ????PDF????
                tempPdfPath = Path.Combine(Path.GetTempPath(), $"TrayApp_Word_{Guid.NewGuid()}.pdf");

                // ??Word????
                wordApp = new Microsoft.Office.Interop.Word.Application
                {
                    Visible = false,
                    DisplayAlerts = WdAlertLevel.wdAlertsNone,
                    ScreenUpdating = false
                };

                _logger.Debug("Word???????");

                // ??Word??
                document = wordApp.Documents.Open(
                    FileName: filePath,
                    ConfirmConversions: false,
                    ReadOnly: true,
                    AddToRecentFiles: false,
                    PasswordDocument: Type.Missing,
                    PasswordTemplate: Type.Missing,
                    Revert: false,
                    WritePasswordDocument: Type.Missing,
                    WritePasswordTemplate: Type.Missing,
                    Format: WdOpenFormat.wdOpenFormatAuto
                );

                _logger.Debug($"Word?????: {Path.GetFileName(filePath)}");

                // ???PDF
                document.ExportAsFixedFormat(
                    OutputFileName: tempPdfPath,
                    ExportFormat: WdExportFormat.wdExportFormatPDF,
                    OpenAfterExport: false,
                    OptimizeFor: WdExportOptimizeFor.wdExportOptimizeForPrint,
                    Range: WdExportRange.wdExportAllDocument,
                    Item: WdExportItem.wdExportDocumentWithMarkup,
                    IncludeDocProps: true,
                    KeepIRM: true,
                    CreateBookmarks: WdExportCreateBookmarks.wdExportCreateNoBookmarks,
                    DocStructureTags: true,
                    BitmapMissingFonts: true
                );

                _logger.Info($"Word???PDF??: {Path.GetFileName(filePath)} -> {Path.GetFileName(tempPdfPath)}");

                // ?????PDF??????
                var pdfBytes = File.ReadAllBytes(tempPdfPath);
                return new MemoryStream(pdfBytes);
            }
            catch (COMException comEx)
            {
                _logger.Error($"Word COM????: {filePath}, HRESULT: 0x{comEx.HResult:X8}", comEx);
                throw new InvalidOperationException($"Word??????: {comEx.Message}", comEx);
            }
            catch (Exception ex)
            {
                _logger.Error($"Word?PDF??: {filePath}", ex);
                throw;
            }
            finally
            {
                // ????
                try
                {
                    // ????
                    if (document != null)
                    {
                        document.Close(SaveChanges: false);
                        Marshal.ReleaseComObject(document);
                    }

                    // ??Word????
                    if (wordApp != null)
                    {
                        wordApp.Quit(SaveChanges: false);
                        Marshal.ReleaseComObject(wordApp);
                    }

                    // ????PDF??
                    if (!string.IsNullOrEmpty(tempPdfPath) && File.Exists(tempPdfPath))
                    {
                        try
                        {
                            File.Delete(tempPdfPath);
                            _logger.Debug($"?????PDF??: {tempPdfPath}");
                        }
                        catch (Exception)
                        {
                            _logger.Warning($"????PDF????: {tempPdfPath}");
                        }
                    }

                    _logger.Debug("Word??????");
                }
                catch (Exception ex)
                {
                    _logger.Error("??Word???????", ex);
                }

                // ?????????COM??
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        public int CountPages(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return 1;

            Microsoft.Office.Interop.Word.Application? wordApp = null;
            Document? document = null;

            try
            {
                _logger.Debug($"????Word????: {Path.GetFileName(filePath)}");

                // ??Word????
                wordApp = new Microsoft.Office.Interop.Word.Application
                {
                    Visible = false,
                    DisplayAlerts = WdAlertLevel.wdAlertsNone,
                    ScreenUpdating = false
                };

                // ????
                document = wordApp.Documents.Open(
                    FileName: filePath,
                    ConfirmConversions: false,
                    ReadOnly: true,
                    AddToRecentFiles: false
                );

                // ????
                int pageCount = document.ComputeStatistics(WdStatistic.wdStatisticPages);
                
                _logger.Debug($"Word????: {pageCount}");
                return Math.Max(1, pageCount);
            }
            catch (Exception ex)
            {
                _logger.Error($"??Word??????: {filePath}", ex);
                return 1; // ???????1?
            }
            finally
            {
                try
                {
                    document?.Close(SaveChanges: false);
                    wordApp?.Quit(SaveChanges: false);

                    if (document != null) Marshal.ReleaseComObject(document);
                    if (wordApp != null) Marshal.ReleaseComObject(wordApp);
                }
                catch (Exception ex)
                {
                    _logger.Error("??Word????", ex);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
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
                _logger?.Debug("WordToPdfConverter???");
            }

            _disposed = true;
        }
    }
}