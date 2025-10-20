using Microsoft.Maui.Graphics;
using System.Diagnostics;

namespace Coftea_Capstone.Services
{
    public static class ImageLoadingService
    {
        /// <summary>
        /// Creates an optimized ImageSource for better performance on devices
        /// </summary>
        public static ImageSource CreateOptimizedImageSource(string imagePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imagePath))
                {
                    Debug.WriteLine("🖼️ ImageLoadingService: Empty image path provided");
                    return ImageSource.FromFile("coftea_logo.png");
                }

                // Validate the image file
                if (!IsValidImageFile(imagePath))
                {
                    Debug.WriteLine($"🖼️ ImageLoadingService: Invalid image file: {imagePath}");
                    return ImageSource.FromFile("coftea_logo.png");
                }

                // Create the ImageSource
                var imageSource = ImageSource.FromFile(imagePath);
                Debug.WriteLine($"🖼️ ImageLoadingService: Successfully created ImageSource for: {imagePath}");
                
                return imageSource;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"🖼️ ImageLoadingService: Error creating ImageSource for {imagePath}: {ex.Message}");
                return ImageSource.FromFile("coftea_logo.png");
            }
        }

        /// <summary>
        /// Validates if an image file is valid and safe to load
        /// </summary>
        private static bool IsValidImageFile(string imagePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                {
                    Debug.WriteLine($"🖼️ ImageLoadingService: File does not exist: {imagePath}");
                    return false;
                }

                // Check file size (avoid loading very large images that might cause memory issues)
                var fileInfo = new FileInfo(imagePath);
                const long maxFileSize = 10 * 1024 * 1024; // 10MB limit
                
                if (fileInfo.Length > maxFileSize)
                {
                    Debug.WriteLine($"🖼️ ImageLoadingService: File too large: {imagePath} ({fileInfo.Length} bytes)");
                    return false;
                }

                // Check file extension
                var extension = Path.GetExtension(imagePath).ToLowerInvariant();
                var validExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" };
                
                if (!validExtensions.Contains(extension))
                {
                    Debug.WriteLine($"🖼️ ImageLoadingService: Invalid file extension: {extension}");
                    return false;
                }

                // Additional validation: check if file is not empty
                if (fileInfo.Length == 0)
                {
                    Debug.WriteLine($"🖼️ ImageLoadingService: File is empty: {imagePath}");
                    return false;
                }

                Debug.WriteLine($"🖼️ ImageLoadingService: File validation passed: {imagePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"🖼️ ImageLoadingService: Error validating file {imagePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the appropriate fallback image based on platform
        /// </summary>
        public static ImageSource GetFallbackImage()
        {
            try
            {
                // Try to load the default logo
                var fallbackImage = ImageSource.FromFile("coftea_logo.png");
                Debug.WriteLine("🖼️ ImageLoadingService: Using coftea_logo.png as fallback");
                return fallbackImage;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"🖼️ ImageLoadingService: Error loading fallback image: {ex.Message}");
                
                // If even the fallback fails, try a simple approach
                try
                {
                    return ImageSource.FromFile("dotnet_bot.png");
                }
                catch
                {
                    // Last resort - return null and let the UI handle it
                    Debug.WriteLine("🖼️ ImageLoadingService: All fallback attempts failed");
                    return null;
                }
            }
        }

        /// <summary>
        /// Preloads and caches an image for better performance
        /// </summary>
        public static async Task<ImageSource> PreloadImageAsync(string imagePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imagePath))
                    return GetFallbackImage();

                // Validate the image first
                if (!IsValidImageFile(imagePath))
                    return GetFallbackImage();

                // Create the ImageSource
                var imageSource = CreateOptimizedImageSource(imagePath);
                
                // For HTTP images, we might want to preload them
                if (imagePath.StartsWith("http"))
                {
                    Debug.WriteLine($"🖼️ ImageLoadingService: Preloading HTTP image: {imagePath}");
                    // HTTP images are handled by the platform automatically
                }

                Debug.WriteLine($"🖼️ ImageLoadingService: Successfully preloaded image: {imagePath}");
                return imageSource;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"🖼️ ImageLoadingService: Error preloading image {imagePath}: {ex.Message}");
                return GetFallbackImage();
            }
        }
    }
}
