using Coftea_Capstone.C_;
using Coftea_Capstone.Pages;
using Coftea_Capstone.Models;

namespace Coftea_Capstone
{
    public partial class App : Application
    {
        public static string dbPath;
        public static UserInfoModel CurrentUser { get; set; }
        public App()
        {
            InitializeComponent();

            dbPath = Path.Combine(FileSystem.AppDataDirectory, "coftea.db3");
           /* if (File.Exists(dbPath))
            {
                File.Delete(dbPath);  // deletes the database file
            }*/

            var NavigatePage = new NavigationPage(new LoginPage());
            MainPage = NavigatePage;

            SeedDatabase();

        }
        public static void SetCurrentUser(UserInfoModel user)
        {
            CurrentUser = user;
        }

        private async void SeedDatabase()
        {
            try
            {
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "coftea.db3");
                var database = new Database(dbPath);
                System.Diagnostics.Debug.WriteLine($"DB Path: {dbPath}");
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
                    await database.SaveProductAsync(new POSPageModel
                    {
                        Name = "Matcha Tea",
                        Price = 110,
                        Status = "Available",
                        Image = "drink.png"
                    });
                    await database.SaveProductAsync(new POSPageModel
                    {
                        Name = "Strawberry Tea",
                        Price = 130,
                        Status = "Available",
                        Image = "drink.png"
                    });
                    await database.SaveProductAsync(new POSPageModel
                    {
                        Name = "Matcha Caffe",
                        Price = 100,
                        Status = "Available",
                        Image = "drink.png"
                    });
                    await database.SaveProductAsync(new POSPageModel
                    {
                        Name = "Strawberry Milktea",
                        Price = 105,
                        Status = "Available",
                        Image = "drink.png"
                    });
                    await database.SaveProductAsync(new POSPageModel
                    {
                        Name = "Strawberry Milk",
                        Price = 135,
                        Status = "Available",
                        Image = "drink.png"
                    });

                }
                var user = await database.GetAllUsersAsync();
               
                if (!user.Any())
                {
                    await database.AddUserAsync(new UserInfoModel
                    {
                        Email = "paul",
                        Password = "1234",
                        FirstName = "Test",
                        LastName = "User",
                        IsAdmin = false,
                        Birthday = DateTime.Now,
                        PhoneNumber = "0000000000",
                        Address = "Default Address"
                    });
                    await database.AddUserAsync(new UserInfoModel
                    {
                        Email = "james",
                        Password = "1234",
                        FirstName = "Test",
                        LastName = "User",
                        IsAdmin = true,
                        Birthday = DateTime.Now,
                        PhoneNumber = "0000000000",
                        Address = "Default Address"
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
