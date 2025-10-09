using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Maui.Storage;
using Coftea_Capstone.C_;

namespace Coftea_Capstone.Services
{
    public class CartStorageService
    {
        private const string CartFileName = "cart_state.json";

        private static string GetCartFilePath()
            => Path.Combine(FileSystem.AppDataDirectory, CartFileName);

        public async Task SaveCartAsync(ObservableCollection<POSPageModel> cartItems)
        {
            try
            {
                var dto = cartItems?.Select(p => new CartItemDto
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName,
                    SmallPrice = p.SmallPrice,
                    MediumPrice = p.MediumPrice,
                    LargePrice = p.LargePrice,
                    ImageSet = p.ImageSet,
                    SmallQuantity = p.SmallQuantity,
                    MediumQuantity = p.MediumQuantity,
                    LargeQuantity = p.LargeQuantity,
                    Category = p.Category,
                    Subcategory = p.Subcategory,
                    ProductDescription = p.ProductDescription,
                    HasSmall = p.HasSmall,
                    HasMedium = p.HasMedium,
                    HasLarge = p.HasLarge
                }).ToList() ?? new List<CartItemDto>();

                var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
                {
                    WriteIndented = false
                });

                var path = GetCartFilePath();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllTextAsync(path, json);
            }
            catch
            {
                // Intentionally swallow — cart persistence should never crash the app
            }
        }

        public async Task<ObservableCollection<POSPageModel>> LoadCartAsync()
        {
            try
            {
                var path = GetCartFilePath();
                if (!File.Exists(path))
                    return new ObservableCollection<POSPageModel>();

                var json = await File.ReadAllTextAsync(path);
                var dto = JsonSerializer.Deserialize<List<CartItemDto>>(json) ?? new List<CartItemDto>();

                var items = new ObservableCollection<POSPageModel>(dto.Select(d => new POSPageModel
                {
                    ProductID = d.ProductID,
                    ProductName = d.ProductName,
                    SmallPrice = d.SmallPrice,
                    MediumPrice = d.MediumPrice,
                    LargePrice = d.LargePrice,
                    ImageSet = d.ImageSet,
                    SmallQuantity = d.SmallQuantity,
                    MediumQuantity = d.MediumQuantity,
                    LargeQuantity = d.LargeQuantity,
                    Category = d.Category,
                    Subcategory = d.Subcategory,
                    ProductDescription = d.ProductDescription,
                    HasSmall = d.HasSmall,
                    HasMedium = d.HasMedium,
                    HasLarge = d.HasLarge
                }));

                return items;
            }
            catch
            {
                return new ObservableCollection<POSPageModel>();
            }
        }

        private class CartItemDto
        {
            public int ProductID { get; set; }
            public string ProductName { get; set; }
            public decimal SmallPrice { get; set; }
            public decimal MediumPrice { get; set; }
            public decimal LargePrice { get; set; }
            public string ImageSet { get; set; }
            public int SmallQuantity { get; set; }
            public int MediumQuantity { get; set; }
            public int LargeQuantity { get; set; }
            public string Category { get; set; }
            public string Subcategory { get; set; }
            public string ProductDescription { get; set; }
            public bool HasSmall { get; set; }
            public bool HasMedium { get; set; }
            public bool HasLarge { get; set; }
        }
    }
}


