using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TrayApp.Core;
using TrayApp.Printing.Core;

namespace TrayApp.Printing.Converters
{
    /// <summary>
    /// ???????
    /// ???????????
    /// </summary>
    public class ConverterFactory : IConverterFactory
    {
        private readonly Dictionary<string, IFileConverter> _converters = new();
        private readonly ILogger _logger;

        public ConverterFactory(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// ????????????
        /// </summary>
        public IFileConverter? GetConverter(string fileExtension)
        {
            if (string.IsNullOrEmpty(fileExtension))
                return null;

            var extension = fileExtension.StartsWith(".") ? fileExtension.ToLower() : $".{fileExtension.ToLower()}";
            
            _converters.TryGetValue(extension, out var converter);
            return converter;
        }

        /// <summary>
        /// ?????
        /// </summary>
        public void RegisterConverter(IFileConverter converter)
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));

            foreach (var extension in converter.SupportedExtensions)
            {
                var ext = extension.StartsWith(".") ? extension.ToLower() : $".{extension.ToLower()}";
                _converters[ext] = converter;
                _logger.Debug($"??????: {ext} -> {converter.GetType().Name}");
            }
        }

        /// <summary>
        /// ????????????
        /// </summary>
        public IEnumerable<string> GetSupportedExtensions()
        {
            return _converters.Keys.ToList();
        }

        /// <summary>
        /// ?????????????
        /// </summary>
        public bool IsSupported(string fileExtension)
        {
            return GetConverter(fileExtension) != null;
        }
    }

    /// <summary>
    /// PDF?????????
    /// PDF????????????????
    /// </summary>
    public class PdfConverter : IFileConverter
    {
        private readonly ILogger _logger;

        public IEnumerable<string> SupportedExtensions => new[] { ".pdf" };

        public PdfConverter(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Stream ConvertToPdfStream(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("????????", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException($"?????: {filePath}");

            try
            {
                _logger.Debug($"????PDF??: {Path.GetFileName(filePath)}");
                
                // ??PDF????????
                var fileBytes = File.ReadAllBytes(filePath);
                return new MemoryStream(fileBytes);
            }
            catch (Exception ex)
            {
                _logger.Error($"??PDF????: {filePath}", ex);
                throw;
            }
        }

        public int CountPages(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return 1;
            if (!File.Exists(filePath)) return 1;

            try
            {
                using (var reader = new iText.Kernel.Pdf.PdfReader(filePath))
                using (var document = new iText.Kernel.Pdf.PdfDocument(reader))
                {
                    return document.GetNumberOfPages();
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"??PDF????: {filePath}", ex);
                return 1; // ???????1?
            }
        }
    }
}