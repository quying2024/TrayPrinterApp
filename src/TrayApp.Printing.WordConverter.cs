using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using TrayApp.Core;
using TrayApp.Printing.Core;

namespace TrayApp.Printing.Converters
{
    /// <summary>
    /// Word ? PDF ?????? Office Interop/COM?
    /// ???
    /// - ????? Microsoft Word?
    /// - ?????? Office ?????32 ? Office ??? 64 ?????????????

    /// - ??/?????????????????????????? Office ??????
    /// </summary>
    public class WordToPdfConverter : IFileConverter, IDisposable
    {
        private readonly ILogger _logger;
        private bool _disposed;

        public IEnumerable<string> SupportedExtensions => new[] { ".doc", ".docx" };

        public WordToPdfConverter(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Stream ConvertToPdfStream(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("filePath ????", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException($"?????: {filePath}");

            EnsureBitnessCompatibility();

            object? wordApp = null;
            object? documents = null;
            object? document = null;
            string tempPdfPath = Path.Combine(Path.GetTempPath(), $"TrayApp_Word_{Guid.NewGuid()}.pdf");

            // ??????? 0x800A03EC ??
            var originalCulture = Thread.CurrentThread.CurrentCulture;
            var originalUICulture = Thread.CurrentThread.CurrentUICulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            try
            {
                _logger.Debug($"??? Word ??? PDF: {Path.GetFileName(filePath)}");

                var wordType = Type.GetTypeFromProgID("Word.Application");
                if (wordType == null)
                    throw new InvalidOperationException("???? Word COM ????????? Microsoft Office Word?????????????");

                wordApp = Activator.CreateInstance(wordType);
                dynamic word = wordApp!;

                // ???/??
                TryIgnore(() => word.Visible = false);
                TryIgnore(() => word.DisplayAlerts = 0 /* wdAlertsNone */);
                TryIgnore(() => word.ScreenUpdating = false);

                _logger.Debug("Word ?????????");

                // ?????Documents ???????????
                documents = ((dynamic)word).Documents;
                document = ((dynamic)documents).Open(filePath, false, true, false);
                dynamic doc = document!;

                _logger.Debug($"Word ?????: {Path.GetFileName(filePath)}");

                // ???????????? Interop ???
                const int wdExportFormatPDF = 17;                // WdExportFormat.wdExportFormatPDF
                const int wdOptimizeForPrint = 0;                // WdExportOptimizeFor.wdExportOptimizeForPrint
                const int wdExportAllDocument = 0;               // WdExportRange.wdExportAllDocument
                const int wdExportDocumentWithMarkup = 7;        // WdExportItem.wdExportDocumentWithMarkup
                const int wdExportCreateNoBookmarks = 0;         // WdExportCreateBookmarks.wdExportCreateNoBookmarks

                doc.ExportAsFixedFormat(
                    tempPdfPath,
                    wdExportFormatPDF,
                    false,                 // OpenAfterExport
                    wdOptimizeForPrint,    // OptimizeFor
                    wdExportAllDocument,   // Range
                    1,                     // From (ignored)
                    1,                     // To   (ignored)
                    wdExportDocumentWithMarkup,
                    true,                  // IncludeDocProps
                    true,                  // KeepIRM
                    wdExportCreateNoBookmarks,
                    true,                  // DocStructureTags
                    true,                  // BitmapMissingFonts
                    true,                  // UseISO19005_1 (PDF/A)
                    Type.Missing
                );

                _logger.Info($"Word ? PDF ??: {Path.GetFileName(filePath)} -> {Path.GetFileName(tempPdfPath)}");

                var pdfBytes = File.ReadAllBytes(tempPdfPath);
                return new MemoryStream(pdfBytes);
            }
            catch (COMException comEx)
            {
                _logger.Error($"Word COM ????: {filePath}, HRESULT: 0x{comEx.HResult:X8}", comEx);
                throw new InvalidOperationException($"Word ????: {comEx.Message}", comEx);
            }
            catch (Exception ex)
            {
                _logger.Error($"Word ? PDF ??: {filePath}", ex);
                throw;
            }
            finally
            {
                // ?????document -> documents -> wordApp
                try
                {
                    if (document != null)
                    {
                        TryIgnore(() => ((dynamic)document).Close(false));
                        Marshal.ReleaseComObject(document);
                    }
                }
                catch (Exception ex) { _logger.Warning($"?? document ??: {ex.Message}"); }
                finally { document = null; }

                try
                {
                    if (documents != null)
                    {
                        Marshal.ReleaseComObject(documents);
                    }
                }
                catch (Exception ex) { _logger.Warning($"?? documents ??: {ex.Message}"); }
                finally { documents = null; }

                try
                {
                    if (wordApp != null)
                    {
                        TryIgnore(() => ((dynamic)wordApp).Quit(false));
                        Marshal.ReleaseComObject(wordApp);
                    }
                }
                catch (Exception ex) { _logger.Warning($"?? word ????: {ex.Message}"); }
                finally { wordApp = null; }

                if (!string.IsNullOrEmpty(tempPdfPath) && File.Exists(tempPdfPath))
                {
                    TryIgnore(() =>
                    {
                        File.Delete(tempPdfPath);
                        _logger.Debug($"?????PDF??: {tempPdfPath}");
                    });
                }

                // ???????? RCW??? WINWORD.EXE ????
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // ????
                Thread.CurrentThread.CurrentCulture = originalCulture;
                Thread.CurrentThread.CurrentUICulture = originalUICulture;

                _logger.Debug("Word Interop ?????");
            }
        }

        public int CountPages(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return 1;

            EnsureBitnessCompatibility();

            object? wordApp = null;
            object? documents = null;
            object? document = null;

            // ????
            var originalCulture = Thread.CurrentThread.CurrentCulture;
            var originalUICulture = Thread.CurrentThread.CurrentUICulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            try
            {
                _logger.Debug($"?? Word ??: {Path.GetFileName(filePath)}");

                var wordType = Type.GetTypeFromProgID("Word.Application");
                if (wordType == null)
                {
                    _logger.Warning("???? Word COM ????????? 1");
                    return 1;
                }

                wordApp = Activator.CreateInstance(wordType);
                dynamic word = wordApp!;
                TryIgnore(() => word.Visible = false);
                TryIgnore(() => word.DisplayAlerts = 0);
                TryIgnore(() => word.ScreenUpdating = false);

                documents = ((dynamic)word).Documents;
                document = ((dynamic)documents).Open(filePath, false, true, false);

                const int wdStatisticPages = 2; // WdStatistic.wdStatisticPages
                int pageCount = (int)((dynamic)document).ComputeStatistics(wdStatisticPages);

                _logger.Debug($"Word ??: {pageCount}");
                return Math.Max(1, pageCount);
            }
            catch (COMException ex)
            {
                _logger.Error($"?? Word ?????COM?: {filePath}", ex);
                return 1;
            }
            catch (Exception ex)
            {
                _logger.Error($"?? Word ????: {filePath}", ex);
                return 1;
            }
            finally
            {
                try
                {
                    if (document != null)
                    {
                        TryIgnore(() => ((dynamic)document).Close(false));
                        Marshal.ReleaseComObject(document);
                    }
                }
                catch (Exception ex) { _logger.Warning($"?? document ??: {ex.Message}"); }
                finally { document = null; }

                try
                {
                    if (documents != null)
                    {
                        Marshal.ReleaseComObject(documents);
                    }
                }
                catch (Exception ex) { _logger.Warning($"?? documents ??: {ex.Message}"); }
                finally { documents = null; }

                try
                {
                    if (wordApp != null)
                    {
                        TryIgnore(() => ((dynamic)wordApp).Quit(false));
                        Marshal.ReleaseComObject(wordApp);
                    }
                }
                catch (Exception ex) { _logger.Warning($"?? word ????: {ex.Message}"); }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                Thread.CurrentThread.CurrentCulture = originalCulture;
                Thread.CurrentThread.CurrentUICulture = originalUICulture;
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
                _logger?.Debug("WordToPdfConverter Dispose");
            }
            _disposed = true;
        }

        private static void TryIgnore(Action action)
        {
            try { action(); } catch { /* ignore */ }
        }

        private void EnsureBitnessCompatibility()
        {
            // 32 ? Office + 64 ??? => ?????
            var isProc64 = Environment.Is64BitProcess;

            // ?????????????2016/2019/365 Office16?
            string officeX86 = @"C:\Program Files (x86)\Microsoft Office\root\Office16\WINWORD.EXE";
            string officeX64 = @"C:\Program Files\Microsoft Office\root\Office16\WINWORD.EXE";

            if (File.Exists(officeX86) && !File.Exists(officeX64) && isProc64)
            {
                throw new InvalidOperationException(
                    "??????? 32 ? Office??????? 64 ???????? x86 ??/???" +
                    "???? 64 ? Office ????");
            }
        }
    }
}