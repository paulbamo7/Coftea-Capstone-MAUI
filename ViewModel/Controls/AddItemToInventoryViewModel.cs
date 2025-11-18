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

        public string CategoryDisplayText => string.IsNullOrWhiteSpace(ItemCategory) ? "Select Category" : ItemCategory;

        [ObservableProperty]
        private bool isCategoryDropdownVisible = false;

        public bool IsAnyDropdownVisible => IsCategoryDropdownVisible;

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

        // Validation message properties
        [ObservableProperty]
        private string itemNameValidationMessage = string.Empty;

        [ObservableProperty]
        private string categoryValidationMessage = string.Empty;

        [ObservableProperty]
        private string quantityValidationMessage = string.Empty;

        [ObservableProperty]
        private string uoMValidationMessage = string.Empty;

        [ObservableProperty]
        private string minimumQuantityValidationMessage = string.Empty;

        [ObservableProperty]
        private string maximumQuantityValidationMessage = string.Empty;

        [ObservableProperty]
        private string generalValidationMessage = string.Empty;

        private void ClearValidationMessages()
        {
            ItemNameValidationMessage = string.Empty;
            CategoryValidationMessage = string.Empty;
            QuantityValidationMessage = string.Empty;
            UoMValidationMessage = string.Empty;
            MinimumQuantityValidationMessage = string.Empty;
            MaximumQuantityValidationMessage = string.Empty;
            GeneralValidationMessage = string.Empty;
        }

        partial void OnItemNameChanged(string value)
        {
            if (!string.IsNullOrEmpty(ItemNameValidationMessage))
            {
                ItemNameValidationMessage = string.Empty;
            }
        }


        partial void OnMinimumQuantityChanged(double value)
        {
            if (!string.IsNullOrEmpty(MinimumQuantityValidationMessage))
            {
                MinimumQuantityValidationMessage = string.Empty;
            }
        }

        partial void OnMaximumQuantityChanged(double value)
        {
            if (!string.IsNullOrEmpty(MaximumQuantityValidationMessage))
            {
                MaximumQuantityValidationMessage = string.Empty;
            }
        }

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
            "Supplies",
            "Others"
        };

        public AddItemToInventoryViewModel()
        {
            _database = new Database(); // Will use auto-detected host
        }

        [RelayCommand]
        private void ShowCategoryPicker()
        {
            IsCategoryDropdownVisible = !IsCategoryDropdownVisible;
        }

        [RelayCommand]
        private void SelectCategory(string category)
        {
            if (!string.IsNullOrWhiteSpace(category) && Categories.Contains(category))
            {
                ItemCategory = category;
                IsCategoryDropdownVisible = false;
            }
        }

        [RelayCommand]
        private void CloseCategoryDropdown()
        {
            IsCategoryDropdownVisible = false;
        }

        partial void OnItemCategoryChanged(string value) // Triggered when item category changes
        {
            // Clear validation message
            if (!string.IsNullOrEmpty(CategoryValidationMessage))
            {
                CategoryValidationMessage = string.Empty;
            }

            // Notify display text change
            OnPropertyChanged(nameof(CategoryDisplayText));

            // Validate that selected category is in the Categories list
            if (!string.IsNullOrWhiteSpace(value) && !Categories.Contains(value))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Invalid category selected: {value}, resetting to null");
                ItemCategory = string.Empty;
                return;
            }

            // Toggle UI sections based on selected category
            // Pieces-only: only quantity field is shown (includes Supplies for cups/straws)
            var newIsPiecesOnly = value == "Others" || value == "Other" || value == "Supplies";
            // UoM-only categories: show only UoM fields (e.g., Syrups, Powdered, etc.)
            var newIsUoMOnly = value == "Syrups" || value == "Powdered" || value == "Fruit Series" || value == "Sinkers & etc." || value == "Liquid";
            
            // Only update if changed to avoid unnecessary notifications
            if (IsPiecesOnlyCategory != newIsPiecesOnly)
            {
                IsPiecesOnlyCategory = newIsPiecesOnly;
            }
            if (IsUoMOnlyCategory != newIsUoMOnly)
            {
                IsUoMOnlyCategory = newIsUoMOnly;
            }
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
            // Clear validation message
            if (!string.IsNullOrEmpty(QuantityValidationMessage))
            {
                QuantityValidationMessage = string.Empty;
            }
            UpdateConversionDisplays();
        }

        partial void OnSelectedUoMChanged(string value) // Validate UoM selection
        {
            // Clear validation message
            if (!string.IsNullOrEmpty(UoMValidationMessage))
            {
                UoMValidationMessage = string.Empty;
            }

            // Validate that selected UoM is in the UoMOptions list
            if (!string.IsNullOrWhiteSpace(value) && !UoMOptions.Contains(value))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Invalid UoM selected: {value}, resetting to null");
                SelectedUoM = null;
                return;
            }
            UpdateConversionDisplays();
        }

        partial void OnMinimumUoMQuantityChanged(double value) // Triggered when minimum UoM quantity changes
        {
            UpdateConversionDisplays();
        }

        partial void OnSelectedMinimumUoMChanged(string value) // Validate minimum UoM selection
        {
            // Validate that selected minimum UoM is in the UoMOptions list
            if (!string.IsNullOrWhiteSpace(value) && !UoMOptions.Contains(value))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Invalid minimum UoM selected: {value}, resetting to null");
                SelectedMinimumUoM = null;
                return;
            }
            UpdateConversionDisplays();
        }

        partial void OnSelectedMaximumUoMChanged(string value) // Validate maximum UoM selection
        {
            // Validate that selected maximum UoM is in the UoMOptions list
            if (!string.IsNullOrWhiteSpace(value) && !UoMOptions.Contains(value))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Invalid maximum UoM selected: {value}, resetting to null");
                SelectedMaximumUoM = null;
                return;
            }
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
        private async Task SaveItem() // Save item immediately in edit mode
        {
            System.Diagnostics.Debug.WriteLine("🔧 SaveItem called - saving current item details immediately");
            await AddItem();
        }

        [RelayCommand]
        public async Task AddItem() // Add or update inventory item
        {
            ClearValidationMessages();

            // Block immediately if no internet for DB-backed save
            if (!Services.NetworkService.HasInternetConnection())
            {
                GeneralValidationMessage = "No internet connection. Please check your network and try again.";
                return;
            }
            if (string.IsNullOrWhiteSpace(ItemName))
            {
                ItemNameValidationMessage = "Item name is required.";
                return;
            }
            // Reject names with no letters (e.g., only digits)
            if (!ItemName.Any(char.IsLetter))
            {
                ItemNameValidationMessage = "Item name must contain letters (e.g., 'IPhone 14'). Pure numbers are not allowed.";
                return;
            }

            if (string.IsNullOrWhiteSpace(ItemCategory))
            {
                CategoryValidationMessage = "Item category is required.";
                return;
            }

            // Quantity validation
            if (IsPiecesOnlyCategory)
            {
                if (UoMQuantity <= 0)
                {
                    QuantityValidationMessage = "Item quantity must be greater than 0.";
                    return;
                }
            }
            else
            {
                // UoM-only categories must also provide quantity now
                if (UoMQuantity <= 0)
                {
                    QuantityValidationMessage = "Please provide a stock quantity.";
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(SelectedUoM))
            {
                UoMValidationMessage = "Please select a unit of measurement.";
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

            // Get minimum and maximum quantities WITHOUT converting them
            // The values should already be in the same unit as the stock quantity
            var minimumThreshold = MinimumQuantity;
            var maximumThreshold = IsUoMOnlyCategory ? MaximumUoMQuantity : MaximumQuantity;
            var minimumUnit = SelectedMinimumUoM ?? SelectedUoM;
            var maximumUnit = IsUoMOnlyCategory ? (SelectedMaximumUoM ?? SelectedUoM) : (SelectedMaximumUoM ?? SelectedUoM);
            
            System.Diagnostics.Debug.WriteLine($"🔧 AddItem validation - Stock: {finalQuantity} {finalUnit}");
            System.Diagnostics.Debug.WriteLine($"🔧 AddItem validation - Min: {minimumThreshold} {minimumUnit}");
            System.Diagnostics.Debug.WriteLine($"🔧 AddItem validation - Max: {maximumThreshold} {maximumUnit}");

            // Only validate unit compatibility - DO NOT convert the values
            // Ensure minimum unit matches final unit (if set)
            if (minimumThreshold > 0 && !string.IsNullOrWhiteSpace(minimumUnit) && !string.IsNullOrWhiteSpace(finalUnit))
            {
                var normalizedMinimumUnit = UnitConversionService.Normalize(minimumUnit);
                var normalizedFinalUnit = UnitConversionService.Normalize(finalUnit);
                
                if (!string.IsNullOrWhiteSpace(normalizedMinimumUnit) && !string.IsNullOrWhiteSpace(normalizedFinalUnit))
                {
                    if (!UnitConversionService.AreCompatibleUnits(normalizedMinimumUnit, normalizedFinalUnit))
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Incompatible minimum units: {normalizedMinimumUnit} vs {normalizedFinalUnit}");
                        MinimumQuantityValidationMessage = $"Minimum quantity unit ({minimumUnit}) is not compatible with stock quantity unit ({finalUnit}).";
                        return;
                    }
                    // Ensure units match - if they don't, require user to use same unit
                    if (!string.Equals(normalizedMinimumUnit, normalizedFinalUnit, StringComparison.OrdinalIgnoreCase))
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Minimum unit ({normalizedMinimumUnit}) differs from final unit ({normalizedFinalUnit}) - requiring match");
                        MinimumQuantityValidationMessage = $"Minimum quantity must use the same unit as stock quantity ({finalUnit}).";
                        return;
                    }
                }
            }

            // Only validate unit compatibility for maximum - DO NOT convert the values
            if (maximumThreshold > 0 && !string.IsNullOrWhiteSpace(maximumUnit) && !string.IsNullOrWhiteSpace(finalUnit))
            {
                var normalizedMaximumUnit = UnitConversionService.Normalize(maximumUnit);
                var normalizedFinalUnit = UnitConversionService.Normalize(finalUnit);
                
                if (!string.IsNullOrWhiteSpace(normalizedMaximumUnit) && !string.IsNullOrWhiteSpace(normalizedFinalUnit))
                {
                    if (!UnitConversionService.AreCompatibleUnits(normalizedMaximumUnit, normalizedFinalUnit))
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Incompatible maximum units: {normalizedMaximumUnit} vs {normalizedFinalUnit}");
                        MaximumQuantityValidationMessage = $"Maximum quantity unit ({maximumUnit}) is not compatible with stock quantity unit ({finalUnit}).";
                        return;
                    }
                    // Ensure units match - if they don't, require user to use same unit
                    if (!string.Equals(normalizedMaximumUnit, normalizedFinalUnit, StringComparison.OrdinalIgnoreCase))
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Maximum unit ({normalizedMaximumUnit}) differs from final unit ({normalizedFinalUnit}) - requiring match");
                        MaximumQuantityValidationMessage = $"Maximum quantity must use the same unit as stock quantity ({finalUnit}).";
                        return;
                    }
                }
            }

            // Default rule: if maximum is not provided, set it to the current stock quantity
            if (maximumThreshold <= 0)
            {
                maximumThreshold = finalQuantity;
            }

            // Enforce business rule: current stock must be greater than the relevant minimum
            // Use strict < (not <=) so equal values (e.g., 500 ml vs 0.5 L → 500 ml) are allowed
            if (minimumThreshold > 0 && finalQuantity < minimumThreshold)
            {
                QuantityValidationMessage = "Current stock must be greater than the minimum threshold.";
                return;
            }

            // Enforce business rule: current stock must not exceed the maximum storage capacity
            if (maximumThreshold > 0 && finalQuantity > maximumThreshold)
            {
                QuantityValidationMessage = $"Current stock ({finalQuantity:F2} {finalUnit}) exceeds the maximum storage capacity ({maximumThreshold:F2} {finalUnit}).";
                return;
            }

            var inventoryItem = new InventoryPageModel 
            {
                itemName = ItemName,
                itemCategory = ItemCategory,
                itemDescription = ItemDescription,
                itemQuantity = finalQuantity,
                unitOfMeasurement = finalUnit,
                minimumQuantity = minimumThreshold,  // Now converted to same unit as stock
                maximumQuantity = maximumThreshold,  // Now converted to same unit as stock
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
                    var app = (App)Application.Current;
                    if (_editingItemId.HasValue)
                    {
                        app?.SuccessCardPopup?.Show(
                            "Inventory Item Updated",
                            $"{inventoryItem.itemName} has been updated",
                            $"ID: {inventoryItem.itemID}",
                            1500);
                        
                        // Add notification
                        await app?.NotificationPopup?.AddNotification(
                            "Inventory Updated",
                            $"{inventoryItem.itemName} has been updated successfully",
                            $"ID: {inventoryItem.itemID}",
                            "Info");
                    }
                    else
                    {
                        // Fetch the new item ID by reading back the last inserted row if not returned by API
                        // Assuming SaveInventoryItemAsync returns rowsAffected only, reload to get ID
                        var latestItems = await _database.GetInventoryItemsAsync();
                        var created = latestItems.OrderByDescending(i => i.itemID).FirstOrDefault(i => i.itemName == inventoryItem.itemName);
                        int createdId = created?.itemID ?? 0;
                        app?.SuccessCardPopup?.Show(
                            "Inventory Item Added",
                            $"{inventoryItem.itemName} has been added",
                            $"ID: {createdId}",
                            1500);
                        
                        // Add notification
                        await app?.NotificationPopup?.AddNotification(
                            "Inventory Item Added",
                            $"{inventoryItem.itemName} has been added to inventory",
                            $"ID: {createdId}",
                            "Info");
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
                    GeneralValidationMessage = "Failed to save inventory item.";
                }
            }
            catch (Exception ex)
            {
                GeneralValidationMessage = $"Failed to add inventory item: {ex.Message}";
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
                    // Save the image to app data directory and get the filename
                    var fileName = await Services.ImagePersistenceService.SaveImageAsync(result.FullPath);
                    
                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        ImagePath = fileName; // Store only the filename
                        SelectedImageSource = ImageSource.FromFile(Services.ImagePersistenceService.GetImagePath(fileName));
                    }
                    else
                    {
                        GeneralValidationMessage = "Failed to save image. Please try again.";
                    }
                }
            }
            catch (Exception ex)
            {
                GeneralValidationMessage = ex.Message;
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
