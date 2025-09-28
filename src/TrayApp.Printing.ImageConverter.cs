using System;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using TrayApp.Core;
using TrayApp.Printing.Core;

namespace TrayApp.Printing.Converters
{
    /// <summary>
    /// ?????PDF???
    /// ?? JPG, PNG, BMP, GIF, TIFF ???
    /// ?? SkiaSharp ?????PDFsharp ??PDF
    /// </summary>
    public class ImageToPdfConverter : IFileConverter
    {
        private readonly ILogger _logger;

        public IEnumerable<string> SupportedExtensions => new[] 
        { 
            ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".tif", ".webp"
        };

        public ImageToPdfConverter(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Stream ConvertToPdfStream(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("????????", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException($"?????: {filePath}");

            try
            {
                _logger.Debug($"???????PDF: {Path.GetFileName(filePath)}");

                // ??SkiaSharp????
                using (var skBitmap = SKBitmap.Decode(filePath))
                {
                    if (skBitmap == null)
                    {
                        throw new InvalidOperationException($"????????: {filePath}");
                    }

                    // ??PDF??
                    var pdfDocument = new PdfDocument();
                    var page = pdfDocument.AddPage();
                    
                    // ?????? - ???????????????A4
                    var imageWidth = skBitmap.Width;
                    var imageHeight = skBitmap.Height;
                    
                    // A4???? (72 DPI)
                    const double A4_WIDTH = 595.28;  // 210mm
                    const double A4_HEIGHT = 841.89; // 297mm
                    
                    // ?????????A4??
                    var scaleX = A4_WIDTH / imageWidth;
                    var scaleY = A4_HEIGHT / imageHeight;
                    var scale = Math.Min(scaleX, scaleY);
                    
                    // ??????
                    if (scale >= 1.0)
                    {
                        // ????A4?????????
                        page.Width = XUnit.FromPoint(imageWidth * 72.0 / 96.0); // ??96 DPI
                        page.Height = XUnit.FromPoint(imageHeight * 72.0 / 96.0);
                    }
                    else
                    {
                        // ????A4???A4??
                        page.Width = XUnit.FromPoint(A4_WIDTH);
                        page.Height = XUnit.FromPoint(A4_HEIGHT);
                    }

                    // ???????
                    using (var gfx = XGraphics.FromPdfPage(page))
                    {
                        // ?SkiaSharp?????????
                        using (var image = ConvertSkBitmapToXImage(skBitmap))
                        {
                            // ????????????
                            var targetWidth = page.Width.Point;
                            var targetHeight = page.Height.Point;
                            
                            if (scale < 1.0)
                            {
                                // ???????
                                targetWidth = imageWidth * scale;
                                targetHeight = imageHeight * scale;
                            }
                            
                            var x = (page.Width.Point - targetWidth) / 2;
                            var y = (page.Height.Point - targetHeight) / 2;
                            
                            // ?????PDF
                            gfx.DrawImage(image, x, y, targetWidth, targetHeight);
                        }
                    }

                    // ?PDF??????
                    var stream = new MemoryStream();
                    pdfDocument.Save(stream);
                    pdfDocument.Close();
                    
                    stream.Position = 0;
                    
                    _logger.Info($"???PDF??: {Path.GetFileName(filePath)}, ??: {imageWidth}x{imageHeight}");
                    return stream;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"???PDF??: {filePath}", ex);
                throw;
            }
        }

        public int CountPages(string filePath)
        {
            // ???????1?
            return 1;
        }

        /// <summary>
        /// ?SkiaSharp?????PDFsharp??
        /// </summary>
        private XImage ConvertSkBitmapToXImage(SKBitmap skBitmap)
        {
            try
            {
                // ?SKBitmap???PNG????
                using (var image = SKImage.FromBitmap(skBitmap))
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                {
                    var bytes = data.ToArray();
                    
                    // ???????XImage
                    using (var stream = new MemoryStream(bytes))
                    {
                        return XImage.FromStream(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"????????", ex);
                throw;
            }
        }
    }
}