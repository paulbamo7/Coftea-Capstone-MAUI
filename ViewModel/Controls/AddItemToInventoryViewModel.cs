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
        private int? _editingItemId;

        [ObservableProperty]
        private bool isAddItemToInventoryVisible = false;

        [ObservableProperty]
        private string itemName = string.Empty;

        [ObservableProperty]
        private string itemCategory = string.Empty;

        [ObservableProperty]
        private bool isPiecesOnlyCategory = false;

        [ObservableProperty]
        private bool isSyrupsCategory = false;

        [ObservableProperty]
        private bool isUoMOnlyCategory = false;

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
            "Milliliters (ml)"
        };

        public ObservableCollection<string> Categories { get; set; } = new ObservableCollection<string>
        {
            "Syrups",
            "Powdered",
            "Fruit Series",
            "Sinkers & etc.",
            "Others"
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

        partial void OnItemCategoryChanged(string value)
        {
            // Toggle UI sections based on selected category
            // Pieces-only: only quantity field is shown
            IsPiecesOnlyCategory = value == "Others" || value == "Other";
            // UoM-only categories: show only UoM fields (e.g., Syrups, Powdered, etc.)
            IsUoMOnlyCategory = value == "Syrups" || value == "Powdered" || value == "Fruit Series" || value == "Sinkers & etc.";
            // Backward compatibility flag for existing bindings using Syrups
            IsSyrupsCategory = value == "Syrups";

            // Update allowed UoM options based on category
            UpdateUoMOptionsForCategory(value);

            if (IsPiecesOnlyCategory)
            {
                // Ensure UoM is set to pieces when only quantity is needed
                SelectedUoM = "Pieces (pcs)";
            }
            else if (IsUoMOnlyCategory)
            {
                // Clear UoM to force user to choose appropriate liquid UoM
                if (string.IsNullOrWhiteSpace(SelectedUoM) || SelectedUoM == "Pieces (pcs)")
                {
                    SelectedUoM = string.Empty;
                }
            }
        }

        private void UpdateUoMOptionsForCategory(string category)
        {
            var cat = category?.Trim() ?? string.Empty;
            // Define allowed sets
            var liquid = new[] { "Liters (L)", "Milliliters (ml)" };
            var weight = new[] { "Kilograms (kg)", "Grams (g)" };
            var pieces = new[] { "Pieces (pcs)" };

            IEnumerable<string> allowed;
            if (cat == "Syrups" || cat == "Fruit Series")
            {
                allowed = liquid;
            }
            else if (cat == "Powdered" || cat == "Sinkers & etc.")
            {
                allowed = weight;
            }
            else if (IsPiecesOnlyCategory)
            {
                allowed = pieces;
            }
            else
            {
                // default to all
                allowed = new[] { "Pieces (pcs)", "Kilograms (kg)", "Grams (g)", "Liters (L)", "Milliliters (ml)" };
            }

            // Refresh collection only if changed
            var newList = allowed.ToList();
            bool different = UoMOptions.Count != newList.Count || UoMOptions.Where((t, i) => t != newList[i]).Any();
            if (different)
            {
                UoMOptions.Clear();
                foreach (var u in newList) UoMOptions.Add(u);
            }

            // Coerce selections to valid values
            if (!UoMOptions.Contains(SelectedUoM))
            {
                SelectedUoM = UoMOptions.FirstOrDefault();
            }
            if (!UoMOptions.Contains(SelectedMinimumUoM))
            {
                SelectedMinimumUoM = UoMOptions.FirstOrDefault();
            }
        }

        public void BeginEdit(int itemId)
        {
            _editingItemId = itemId;
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

            // Quantity validation
            if (IsPiecesOnlyCategory)
            {
                if (UoMQuantity <= 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Item quantity must be greater than 0.", "OK");
                    return;
                }
            }
            else
            {
                // UoM-only categories must also provide quantity now
                if (UoMQuantity <= 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Please provide a stock quantity.", "OK");
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(SelectedUoM))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please select a unit of measurement.", "OK");
                return;
            }

            // Determine final quantity (default to 1 when not pieces-only)
            var finalQuantity = UoMQuantity;

            // Determine the relevant minimum by category type
            var minimumThreshold = IsPiecesOnlyCategory ? MinimumQuantity : MinimumUoMQuantity;

            // Enforce business rule: current stock must be greater than the relevant minimum
            if (minimumThreshold > 0 && finalQuantity <= minimumThreshold)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Current stock must be greater than the minimum threshold.", "OK");
                return;
            }

            var inventoryItem = new InventoryPageModel
            {
                itemName = ItemName,
                itemCategory = ItemCategory,
                itemDescription = ItemDescription,
                itemQuantity = finalQuantity,
                unitOfMeasurement = SelectedUoM,
                minimumQuantity = minimumThreshold,
                ImageSet = ImagePath
            };

            try
            {
                int rowsAffected;
                if (_editingItemId.HasValue)
                {
                    inventoryItem.itemID = _editingItemId.Value;
                    rowsAffected = await _database.UpdateInventoryItemAsync(inventoryItem);
                }
                else
                {
                    rowsAffected = await _database.SaveInventoryItemAsync(inventoryItem);
                }
                if (rowsAffected > 0)
                {
                    var msg = _editingItemId.HasValue ? "Inventory item updated successfully!" : "Inventory item added successfully!";
                    await Application.Current.MainPage.DisplayAlert("Success", msg, "OK");
                    // Notify listeners (e.g., Inventory page) to refresh
                    MessagingCenter.Send(this, "InventoryChanged");
                    ResetForm();
                    IsAddItemToInventoryVisible = false;
                    _editingItemId = null;
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to save inventory item.", "OK");
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

        [RelayCommand]
        private void ClearImage()
        {
            ImagePath = string.Empty;
            SelectedImageSource = null;
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
