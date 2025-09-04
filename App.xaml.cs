using Coftea_Capstone.C_;
using Coftea_Capstone.Pages;
using Coftea_Capstone.Models;

namespace Coftea_Capstone
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            var NavigatePage = new NavigationPage(new LoginPage());
            MainPage = NavigatePage;
            SeedDatabase();

        }

        private async void SeedDatabase()
        {
            try
            {
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "coftea.db3");
                var database = new Database(dbPath);

                var products = await database.GetProductsAsync();
                System.Diagnostics.Debug.WriteLine($"Products count: {products.Count}");
                if (!products.Any())
                {
                    await database.SaveProductAsync(new POSPageModel
                    {   
                        Name = "Chocolate Tea",
                        Price = 120,                       
                        Status = "Available",
                        Image = "drink.png"
                    });
                
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Seeding failed: {ex.Message}");
            }
        }
    }
}
