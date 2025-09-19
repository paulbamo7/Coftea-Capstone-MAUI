using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coftea_Capstone.ViewModel
{
    public partial class AddItemToInventoryViewModel : ObservableObject
    {
        private readonly Database _database;

        [ObservableProperty]
        private bool isAddItemToInventoryVisible = false;

        [ObservableProperty]
        private string itemName = string.Empty;

        [ObservableProperty]
        private string itemCategory = string.Empty;

        [ObservableProperty]
        private string itemDescription = string.Empty;

        [ObservableProperty]
        private double itemQuantity = 0;

        [ObservableProperty]
        private string unitOfMeasurement = string.Empty;

        [ObservableProperty]
        private double minimumQuantity = 0;

        [ObservableProperty]
        private double minimumUoMQuantity = 0;

        [ObservableProperty]
        private double uoMQuantity = 0;

        [ObservableProperty]
        private string selectedMinimumUoM;

        [ObservableProperty]
        private string selectedUoM;

        [ObservableProperty]
        private string imagePath = string.Empty;

        [ObservableProperty]
        private ImageSource selectedImageSource;

        public ObservableCollection<string> UoMOptions { get; set; } = new ObservableCollection<string>
        {
            "Pieces (pcs)",
            "Kilograms (kg)",
            "Grams (g)",
            "Liters (L)",
            "Milliliters (ml)",
            "Cups (cups)",
            "Tablespoons (tbsp)",
            "Teaspoons (tsp)",
            "Ounces (oz)",
            "Pounds (lbs)"
        };

        public AddItemToInventoryViewModel()
        {
            _database = new Database(
                host: "0.0.0.0",
                database: "coftea_db",
                user: "root",
                password: ""
            );
        }

        [RelayCommand]
        private void CloseAddItemToInventory()
        {
            IsAddItemToInventoryVisible = false;
            ResetForm();
        }

        [RelayCommand]
        public async Task AddItem()
        {
            if (string.IsNullOrWhiteSpace(ItemName))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Item name is required.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(ItemCategory))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Item category is required.", "OK");
                return;
            }

            if (UoMQuantity <= 0)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Item quantity must be greater than 0.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedUoM))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please select a unit of measurement.", "OK");
                return;
            }

            var inventoryItem = new InventoryPageModel
            {
                itemName = ItemName,
                itemCategory = ItemCategory,
                itemDescription = ItemDescription,
                itemQuantity = UoMQuantity,
                unitOfMeasurement = SelectedUoM,
                minimumQuantity = MinimumQuantity,
                ImageSet = ImagePath
            };

            try
            {
                int rowsAffected = await _database.SaveInventoryItemAsync(inventoryItem);
                if (rowsAffected > 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Success", "Inventory item added successfully!", "OK");
                    ResetForm();
                    IsAddItemToInventoryVisible = false;
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to add inventory item.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to add inventory item: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task PickImageAsync()
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select an image",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    ImagePath = result.FullPath;
                    SelectedImageSource = ImageSource.FromFile(result.FullPath);
                }
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private void ResetForm()
        {
            ItemName = string.Empty;
            ItemCategory = string.Empty;
            ItemDescription = string.Empty;
            ItemQuantity = 0;
            UoMQuantity = 0;
            MinimumQuantity = 0;
            MinimumUoMQuantity = 0;
            SelectedUoM = null;
            SelectedMinimumUoM = null;
            ImagePath = string.Empty;
            SelectedImageSource = null;
        }
    }
}
