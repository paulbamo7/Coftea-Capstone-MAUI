using System;
using System.IO;
using Microsoft.Maui.Controls;
using ZXing;
using ZXing.Common;
using SkiaSharp;

namespace Coftea_Capstone.Services
{
    public static class QRCodeService
    {
        /// <summary>
        /// Generates a QR code image from text and returns it as an ImageSource
        /// </summary>
        /// <param name="text">The text/data to encode in the QR code</param>
        /// <param name="size">Size of the QR code image (default: 200)</param>
        /// <returns>ImageSource that can be used in Image controls</returns>
        public static ImageSource GenerateQRCode(string text, int size = 200)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return null;
                }

                // Create QR code writer
                var writer = new BarcodeWriterPixelData
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new EncodingOptions
                    {
                        Height = size,
                        Width = size,
                        Margin = 2
                    }
                };

                // Generate QR code
                var pixelData = writer.Write(text);

                // Create SkiaSharp bitmap
                var bitmap = new SKBitmap(new SKImageInfo(pixelData.Width, pixelData.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
                
                // Copy pixel data to bitmap (ZXing returns BGRA format, which matches SkiaSharp Bgra8888)
                var pixelPtr = bitmap.GetPixels();
                System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, pixelPtr, pixelData.Pixels.Length);

                // Convert to PNG and create ImageSource
                using (var image = SKImage.FromBitmap(bitmap))
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                {
                    var bytes = data.ToArray();
                    bitmap.Dispose();
                    return ImageSource.FromStream(() => new MemoryStream(bytes));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error generating QR code: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return null;
            }
        }
    }
}
