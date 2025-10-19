using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Coftea_Capstone.Models.Service;
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
        private bool isUpdateInventoryDetailsVisible = false;

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
        private double maximumQuantity = 0;

        [ObservableProperty]
        private double maximumUoMQuantity = 0;

        [ObservableProperty]
        private double uoMQuantity = 0;

        [ObservableProperty]
        private string selectedMinimumUoM;

        [ObservableProperty]
        private string selectedMaximumUoM;

        [ObservableProperty]
        private string selectedUoM;

        [ObservableProperty]
        private string imagePath = string.Empty;

        // Reference to the UpdateInventoryDetails control for reset functionality
        public Views.Controls.UpdateInventoryDetails UpdateInventoryDetailsControl { get; set; }

        [ObservableProperty]
        private ImageSource selectedImageSource;

        // Conversion display properties
        [ObservableProperty]
        private string convertedQuantityDisplay = string.Empty;

        [ObservableProperty]
        private string convertedMinimumDisplay = string.Empty;

        [ObservableProperty]
        private string convertedMaximumDisplay = string.Empty;

        [ObservableProperty]
        private bool showConversionInfo = false;

        public ObservableCollection<string> UoMOptions { get; set; } = new ObservableCollection<string> // All possible UoM options
        {
            "Pieces (pcs)",
            "Kilograms (kg)",
            "Grams (g)",
            "Liters (L)",
            "Milliliters (ml)"
        };

        public ObservableCollection<string> Categories { get; set; } = new ObservableCollection<string> // Predefined categories
        {
            "Syrups",
            "Powdered",
            "Fruit Series",
            "Sinkers & etc.",
            "Liquid",
            "Others"
        };

        public AddItemToInventoryViewModel()
        {
            _database = new Database(); // Will use auto-detected host
        }

        partial void OnItemCategoryChanged(string value) // Triggered when item category changes
        {
            // Toggle UI sections based on selected category
            // Pieces-only: only quantity field is shown
            IsPiecesOnlyCategory = value == "Others" || value == "Other";
            // UoM-only categories: show only UoM fields (e.g., Syrups, Powdered, etc.)
            IsUoMOnlyCategory = value == "Syrups" || value == "Powdered" || value == "Fruit Series" || value == "Sinkers & etc." || value == "Liquid";
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

            // Update conversion displays when category changes
            UpdateConversionDisplays();
        }

        partial void OnUoMQuantityChanged(double value) // Triggered when UoM quantity changes
        {
            UpdateConversionDisplays();
        }

        partial void OnSelectedUoMChanged(string value) // Triggered when selected UoM changes
        {
            UpdateConversionDisplays();
        }

        partial void OnMinimumUoMQuantityChanged(double value) // Triggered when minimum UoM quantity changes
        {
            UpdateConversionDisplays();
        }

        partial void OnSelectedMinimumUoMChanged(string value) // Triggered when selected minimum UoM changes
        {
            UpdateConversionDisplays();
        }

        partial void OnSelectedMaximumUoMChanged(string value) // Triggered when selected maximum UoM changes
        {
            UpdateConversionDisplays();
        }

        private void UpdateUoMOptionsForCategory(string category) // Update allowed UoM options based on category
        {
            var cat = category?.Trim() ?? string.Empty;
            // Define allowed sets
            var liquid = new[] { "Liters (L)", "Milliliters (ml)" };
            var weight = new[] { "Kilograms (kg)", "Grams (g)" };
            var pieces = new[] { "Pieces (pcs)" };
            var sinkers = new[] { "Pieces (pcs)", "Liters (L)", "Milliliters (ml)", "Kilograms (kg)", "Grams (g)" };

            IEnumerable<string> allowed;
            if (cat == "Syrups" || cat == "Fruit Series" || cat == "Liquid")
            {
                allowed = liquid;
            }
            else if (cat == "Powdered")
            {
                allowed = weight;
            }
            else if (cat == "Sinkers & etc.")
            {
                allowed = sinkers;
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
            if (!UoMOptions.Contains(SelectedUoM)) // Coerce to first valid option if current selection is invalid
            {
                SelectedUoM = UoMOptions.FirstOrDefault();
            }
            if (!UoMOptions.Contains(SelectedMinimumUoM)) // Coerce to first valid option if current selection is invalid
            {
                SelectedMinimumUoM = UoMOptions.FirstOrDefault();
            }
            if (!UoMOptions.Contains(SelectedMaximumUoM)) // Coerce to first valid option if current selection is invalid
            {
                SelectedMaximumUoM = UoMOptions.FirstOrDefault();
            }
        }

        private void UpdateConversionDisplays() // Update conversion display strings
        {
            // Update quantity conversion display
            if (UoMQuantity > 0 && !string.IsNullOrWhiteSpace(SelectedUoM))
            {
                var (convertedValue, convertedUnit) = UnitConversionService.ConvertToBestUnit(UoMQuantity, SelectedUoM);
                var originalUnit = UnitConversionService.Normalize(SelectedUoM);
                
                if (convertedUnit != originalUnit)
                {
                    ConvertedQuantityDisplay = $"{UoMQuantity} {UnitConversionService.FormatUnit(originalUnit)} = {convertedValue:F2} {UnitConversionService.FormatUnit(convertedUnit)}";
                    ShowConversionInfo = true;
                }
                else
                {
                    ConvertedQuantityDisplay = string.Empty;
                    ShowConversionInfo = false;
                }
            }
            else
            {
                ConvertedQuantityDisplay = string.Empty;
                ShowConversionInfo = false;
            }

            // Update minimum quantity conversion display
            if (MinimumUoMQuantity > 0 && !string.IsNullOrWhiteSpace(SelectedMinimumUoM)) // Triggered when selected minimum UoM changes
            {
                var (convertedValue, convertedUnit) = UnitConversionService.ConvertToBestUnit(MinimumUoMQuantity, SelectedMinimumUoM);
                var originalUnit = UnitConversionService.Normalize(SelectedMinimumUoM);
                
                if (convertedUnit != originalUnit)
                {
                    ConvertedMinimumDisplay = $"{MinimumUoMQuantity} {UnitConversionService.FormatUnit(originalUnit)} = {convertedValue:F2} {UnitConversionService.FormatUnit(convertedUnit)}";
                }
                else
                {
                    ConvertedMinimumDisplay = string.Empty;
                }
            }
            else
            {
                ConvertedMinimumDisplay = string.Empty;
            }

            // Update maximum quantity conversion display
            if (MaximumUoMQuantity > 0 && !string.IsNullOrWhiteSpace(SelectedMaximumUoM))
            {
                var (convertedValue, convertedUnit) = UnitConversionService.ConvertToBestUnit(MaximumUoMQuantity, SelectedMaximumUoM);
                var originalUnit = UnitConversionService.Normalize(SelectedMaximumUoM);
                
                if (convertedUnit != originalUnit)
                {
                    ConvertedMaximumDisplay = $"{MaximumUoMQuantity} {UnitConversionService.FormatUnit(originalUnit)} = {convertedValue:F2} {UnitConversionService.FormatUnit(convertedUnit)}";
                }
                else
                {
                    ConvertedMaximumDisplay = string.Empty;
                }
            }
            else
            {
                ConvertedMaximumDisplay = string.Empty;
            }
        }

        public void BeginEdit(int itemId) // Start editing an existing item
        {
            _editingItemId = itemId;
        }

        [RelayCommand]
        private void CloseAddItemToInventory() // Close the add item overlay
        {
            IsAddItemToInventoryVisible = false;
            ResetForm();
        }

        [RelayCommand]
        private void CloseUpdateInventoryDetails() // Close the update item overlay
        {
            IsUpdateInventoryDetailsVisible = false;
            
            // Reset the UpdateInventoryDetails control's internal state
            if (UpdateInventoryDetailsControl != null)
            {
                UpdateInventoryDetailsControl.ResetForm();
            }
            
            ResetForm();
            _editingItemId = null;
        }

        [RelayCommand]
        public async Task AddItem() // Add or update inventory item
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

            // Determine final quantity with automatic unit conversion
            var finalQuantity = UoMQuantity;
            var finalUnit = SelectedUoM;

            // Convert to best unit for storage if needed
            if (!string.IsNullOrWhiteSpace(SelectedUoM) && UoMQuantity > 0)
            {
                var (convertedValue, convertedUnit) = UnitConversionService.ConvertToBestUnit(UoMQuantity, SelectedUoM);
                finalQuantity = convertedValue;
                finalUnit = UnitConversionService.FormatUnit(convertedUnit);
            }

            // Use the same properties that the UI is binding to
            var minimumThreshold = MinimumQuantity;
            var maximumThreshold = MaximumQuantity;

            // Default rule: if maximum is not provided, set it to the current stock quantity
            if (maximumThreshold <= 0)
            {
                maximumThreshold = finalQuantity;
            }

            // Enforce business rule: current stock must be greater than the relevant minimum
            // Use strict < (not <=) so equal values (e.g., 500 ml vs 0.5 L → 500 ml) are allowed
            if (minimumThreshold > 0 && finalQuantity < minimumThreshold)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Current stock must be greater than the minimum threshold.", "OK");
                return;
            }

            // Enforce business rule: current stock must not exceed the maximum storage capacity
            if (maximumThreshold > 0 && finalQuantity > maximumThreshold)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Current stock ({finalQuantity:F2} {finalUnit}) exceeds the maximum storage capacity ({maximumThreshold:F2} {finalUnit}).", "OK");
                return;
            }

            var inventoryItem = new InventoryPageModel 
            {
                itemName = ItemName,
                itemCategory = ItemCategory,
                itemDescription = ItemDescription,
                itemQuantity = finalQuantity,
                unitOfMeasurement = finalUnit,
                minimumQuantity = minimumThreshold,
                maximumQuantity = maximumThreshold,
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
                    if (_editingItemId.HasValue)
                    {
                        var app = (App)Application.Current;
                        app?.SuccessCardPopup?.Show(
                            "Inventory Item Updated",
                            $"{inventoryItem.itemName} has been updated",
                            $"ID: {inventoryItem.itemID}",
                            1500);
                    }
                    else
                    {
                        // Fetch the new item ID by reading back the last inserted row if not returned by API
                        // Assuming SaveInventoryItemAsync returns rowsAffected only, reload to get ID
                        var latestItems = await _database.GetInventoryItemsAsync();
                        var created = latestItems.OrderByDescending(i => i.itemID).FirstOrDefault(i => i.itemName == inventoryItem.itemName);
                        int createdId = created?.itemID ?? 0;
                        var app = (App)Application.Current;
                        app?.SuccessCardPopup?.Show(
                            "Inventory Item Added",
                            $"{inventoryItem.itemName} has been added",
                            $"ID: {createdId}",
                            1500);
                    }
                    // Notify listeners (e.g., Inventory page) to refresh
                    MessagingCenter.Send(this, "InventoryChanged");
                    ResetForm();
                    // Close both add and update overlays after successful save
                    IsAddItemToInventoryVisible = false;
                    IsUpdateInventoryDetailsVisible = false;
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
        public async Task PickImageAsync() // Pick an image from device storage
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
        private void ClearImage() // Clear the selected image
        {
            ImagePath = string.Empty;
            SelectedImageSource = null;
        }

        private void ResetForm() // Reset all form fields to default
        {
            ItemName = string.Empty;
            ItemCategory = string.Empty;
            ItemDescription = string.Empty;
            ItemQuantity = 0;
            UoMQuantity = 0;
            MinimumQuantity = 0;
            MinimumUoMQuantity = 0;
            MaximumQuantity = 0;
            MaximumUoMQuantity = 0;
            SelectedUoM = null;
            SelectedMinimumUoM = null;
            SelectedMaximumUoM = null;
            ImagePath = string.Empty;
            SelectedImageSource = null;
            ConvertedQuantityDisplay = string.Empty;
            ConvertedMinimumDisplay = string.Empty;
            ConvertedMaximumDisplay = string.Empty;
            ShowConversionInfo = false;
            _editingItemId = null;
        }
    }
}
