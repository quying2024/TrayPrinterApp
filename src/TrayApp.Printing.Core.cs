using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace TrayApp.Printing.Core
{
    /// <summary>
    /// ???PDF?????
    /// ??????????????
    /// </summary>
    public interface IFileConverter
    {
        /// <summary>
        /// ????????
        /// </summary>
        IEnumerable<string> SupportedExtensions { get; }

        /// <summary>
        /// ??????PDF?
        /// </summary>
        /// <param name="filePath">?????</param>
        /// <returns>PDF???</returns>
        Stream ConvertToPdfStream(string filePath);

        /// <summary>
        /// ??????????????
        /// </summary>
        /// <param name="filePath">?????</param>
        /// <returns>??</returns>
        int CountPages(string filePath);
    }

    /// <summary>
    /// PDF??????
    /// ???PDF????
    /// </summary>
    public interface IPdfPrintEngine : IDisposable
    {
        /// <summary>
        /// ??PDF???????
        /// </summary>
        /// <param name="pdfStream">PDF???</param>
        /// <param name="printerName">?????</param>
        /// <param name="copies">????</param>
        /// <returns>??????</returns>
        bool PrintPdf(Stream pdfStream, string printerName, int copies = 1);

        /// <summary>
        /// ??PDF????????
        /// </summary>
        /// <param name="pdfFilePath">PDF????</param>
        /// <param name="printerName">?????</param>
        /// <param name="copies">????</param>
        /// <returns>??????</returns>
        bool PrintPdf(string pdfFilePath, string printerName, int copies = 1);
    }

    /// <summary>
    /// ???????
    /// ??????????????
    /// </summary>
    public interface IConverterFactory
    {
        /// <summary>
        /// ????????????
        /// </summary>
        /// <param name="fileExtension">?????</param>
        /// <returns>??????????????null</returns>
        IFileConverter? GetConverter(string fileExtension);

        /// <summary>
        /// ?????
        /// </summary>
        /// <param name="converter">?????</param>
        void RegisterConverter(IFileConverter converter);

        /// <summary>
        /// ????????????
        /// </summary>
        IEnumerable<string> GetSupportedExtensions();

        /// <summary>
        /// ?????????????
        /// </summary>
        /// <param name="fileExtension">?????</param>
        /// <returns>????</returns>
        bool IsSupported(string fileExtension);
    }

    /// <summary>
    /// ????
    /// </summary>
    public class PrintResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int PagesPrinted { get; set; }
        public TimeSpan PrintDuration { get; set; }
    }

    /// <summary>
    /// ????
    /// </summary>
    public class PrintOptions
    {
        public string PrinterName { get; set; } = string.Empty;
        public int Copies { get; set; } = 1;
        public bool FitToPage { get; set; } = true;
        public bool KeepAspectRatio { get; set; } = true;
        public int DpiX { get; set; } = 300;
        public int DpiY { get; set; } = 300;
    }
}