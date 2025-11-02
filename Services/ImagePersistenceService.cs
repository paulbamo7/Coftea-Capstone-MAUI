using Microsoft.Maui.Storage;
using System.IO;

namespace Coftea_Capstone.Services
{
    public static class ImagePersistenceService
    {
        private static readonly string AppImagesDirectory = Path.Combine(FileSystem.AppDataDirectory, "ProductImages");

        /// <summary>
        /// Ensures the app images directory exists
        /// </summary>
        public static async Task EnsureImagesDirectoryExistsAsync()
        {
            if (!Directory.Exists(AppImagesDirectory))
            {
                Directory.CreateDirectory(AppImagesDirectory);
            }
        }

        /// <summary>
        /// Copies an image from the source path to the app data directory and returns the filename
        /// </summary>
        /// <param name="sourcePath">The source image path</param>
        /// <returns>The filename of the copied image, or empty string if failed</returns>
        public static async Task<string> SaveImageAsync(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                return string.Empty;

            try
            {
                await EnsureImagesDirectoryExistsAsync();

                // Generate a unique filename to avoid conflicts
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(sourcePath)}";
                var destinationPath = Path.Combine(AppImagesDirectory, fileName);

                // Copy the file to app data directory
                File.Copy(sourcePath, destinationPath, true);

                return fileName; // Return only the filename, not the full path
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving image: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the full path to an image file in the app data directory
        /// </summary>
        /// <param name="fileName">The filename stored in the database</param>
        /// <returns>The full path to the image file</returns>
        public static string GetImagePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            return Path.Combine(AppImagesDirectory, fileName);
        }

        /// <summary>
        /// Checks if an image file exists in the app data directory
        /// </summary>
        /// <param name="fileName">The filename to check</param>
        /// <returns>True if the file exists, false otherwise</returns>
        public static bool ImageExists(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var fullPath = GetImagePath(fileName);
            return File.Exists(fullPath);
        }

        /// <summary>
        /// Deletes an image file from the app data directory
        /// </summary>
        /// <param name="fileName">The filename to delete</param>
        public static void DeleteImage(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            try
            {
                var fullPath = GetImagePath(fileName);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting image: {ex.Message}");
            }
        }

        /// <summary>
        /// Restores image from database bytes to app data directory
        /// </summary>
        public static async Task<bool> RestoreImageFromBytesAsync(string fileName, byte[] imageBytes)
        {
            if (string.IsNullOrWhiteSpace(fileName) || imageBytes == null || imageBytes.Length == 0)
                return false;

            try
            {
                await EnsureImagesDirectoryExistsAsync();
                var imagePath = GetImagePath(fileName);
                await File.WriteAllBytesAsync(imagePath, imageBytes);
                System.Diagnostics.Debug.WriteLine($"✅ Restored image to app data: {fileName}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error restoring image: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Migrates old image paths to the new system
        /// This method can be called during app startup to handle existing data
        /// </summary>
        public static async Task MigrateOldImagesAsync()
        {
            try
            {
                await EnsureImagesDirectoryExistsAsync();
                
                // This method can be extended to handle migration of existing image paths
                // For now, it just ensures the directory exists
                System.Diagnostics.Debug.WriteLine("Image migration completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during image migration: {ex.Message}");
            }
        }
    }
}
