using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Coftea_Capstone.ViewModel
{
    public partial class EditInventoryPopupViewModel : ObservableObject
    {
        private readonly Database _database;
        private readonly AddItemToInventoryViewModel _addItemToInventoryViewModel;

        [ObservableProperty]
        private bool isEditInventoryPopupVisible = false;

        [ObservableProperty]
        private ObservableCollection<InventoryPageModel> items = new();

        [ObservableProperty]
        private ObservableCollection<InventoryPageModel> allItems = new();

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private string selectedCategory = "All";

        public EditInventoryPopupViewModel(AddItemToInventoryViewModel addItemToInventoryViewModel)
        {
            _database = new Database(); // Will use auto-detected host
            _addItemToInventoryViewModel = addItemToInventoryViewModel;
        }

        [RelayCommand]
        private async Task LoadInventoryAsync() // Load inventory items from database
        {
            IsLoading = true;
            StatusMessage = "Loading inventory...";
            try
            {
                var list = await _database.GetInventoryItemsAsync();
                AllItems.Clear();
                Items.Clear();
                foreach (var it in list)
                {
                    AllItems.Add(it);
                    Items.Add(it);
                }
                StatusMessage = $"Loaded {Items.Count} items";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task EditItem(InventoryPageModel item) // Open EditInventoryPopup with selected item
        {
            if (item == null) return;
            IsEditInventoryPopupVisible = false;

            // Pull latest from DB by ID to mirror Add flow behavior
            var fresh = await _database.GetInventoryItemByIdAsync(item.itemID) ?? item;

            // Seed AddItemToInventory with DB values and open UpdateInventoryDetails panel
            _addItemToInventoryViewModel.IsAddItemToInventoryVisible = false;
            _addItemToInventoryViewModel.IsUpdateInventoryDetailsVisible = true;
            
            // Reset the UpdateInventoryDetails form to clear any previous data
            if (_addItemToInventoryViewModel.UpdateInventoryDetailsControl != null)
            {
                _addItemToInventoryViewModel.UpdateInventoryDetailsControl.ResetForm();
            }
            
            _addItemToInventoryViewModel.ItemName = fresh.itemName;
            _addItemToInventoryViewModel.ItemDescription = fresh.itemDescription;
            
            // Set category FIRST to trigger the category change logic and populate UoM options
            _addItemToInventoryViewModel.ItemCategory = fresh.itemCategory;
            
            // Now set the quantities and UoM after category is set
            _addItemToInventoryViewModel.UoMQuantity = fresh.itemQuantity;
            _addItemToInventoryViewModel.SelectedUoM = fresh.unitOfMeasurement;
            _addItemToInventoryViewModel.MinimumQuantity = fresh.minimumQuantity;
            _addItemToInventoryViewModel.MaximumQuantity = fresh.maximumQuantity;
            
            // For UoM-only categories, also set the UoM-specific fields
            if (fresh.itemCategory == "Syrups" || fresh.itemCategory == "Powdered" || fresh.itemCategory == "Fruit Series" || fresh.itemCategory == "Sinkers & etc.")
            {
                _addItemToInventoryViewModel.MinimumUoMQuantity = fresh.minimumQuantity;
                _addItemToInventoryViewModel.SelectedMinimumUoM = fresh.unitOfMeasurement;
                _addItemToInventoryViewModel.MaximumUoMQuantity = fresh.maximumQuantity;
                _addItemToInventoryViewModel.SelectedMaximumUoM = fresh.unitOfMeasurement;
            }
            
            // Initialize the UpdateInventoryDetails control with the current stock value AFTER all properties are set
            if (_addItemToInventoryViewModel.UpdateInventoryDetailsControl != null)
            {
                _addItemToInventoryViewModel.UpdateInventoryDetailsControl.InitializeStockValue(
                    fresh.itemQuantity, 
                    fresh.unitOfMeasurement);
            }
            
            _addItemToInventoryViewModel.ImagePath = fresh.ImageSet;
            // Only set SelectedImageSource if there's actually an image path (not placeholder)
            if (!string.IsNullOrWhiteSpace(fresh.ImageSet))
            {
                _addItemToInventoryViewModel.SelectedImageSource = ImageSource.FromFile(fresh.ImageSet);
            }
            else
            {
                _addItemToInventoryViewModel.SelectedImageSource = null;
            }
            _addItemToInventoryViewModel.BeginEdit(fresh.itemID);
        }

        [RelayCommand]
        private void CloseEditInventoryPopup() // Close the popup
        {
            IsEditInventoryPopupVisible = false;
        }

        public async Task ShowEditInventoryPopup() // Show the popup and load inventory
        {
            IsEditInventoryPopupVisible = true;
            await LoadInventoryAsync();
        }

        [RelayCommand]
        private void FilterByCategory(string category) // Filter inventory items by category
        {
            SelectedCategory = category;
            Items.Clear();
            if (string.IsNullOrEmpty(category) || category == "All")
            {
                foreach (var it in AllItems) Items.Add(it);
                return;
            }
           
            // - Ingredients: show Syrups, Powdered, Fruit Series, Sinkers & etc., Liquid
            // - Supplies: show Others category (supplies like cups, straws, etc.)
            bool isIngredients = string.Equals(category, "Ingredients", StringComparison.OrdinalIgnoreCase);
            bool isSupplies = string.Equals(category, "Supplies", StringComparison.OrdinalIgnoreCase);

            foreach (var it in AllItems)
            {
                var cat = (it.itemCategory ?? string.Empty).Trim();
                if (isIngredients)
                {
                    // Show ingredient categories
                    var ingredientCategories = new[] { "Syrups", "Powdered", "Fruit Series", "Sinkers & etc.", "Liquid" };
                    if (ingredientCategories.Any(ic => string.Equals(cat, ic, StringComparison.OrdinalIgnoreCase)))
                        Items.Add(it);
                }
                else if (isSupplies)
                {
                    // Show supplies categories (Supplies and Others)
                    if (string.Equals(cat, "Supplies", StringComparison.OrdinalIgnoreCase) || 
                        string.Equals(cat, "Others", StringComparison.OrdinalIgnoreCase))
                        Items.Add(it);
                }
                else if (string.Equals(cat, category, StringComparison.OrdinalIgnoreCase))
                {
                    // Direct category match (Syrups, Powdered, etc.)
                    Items.Add(it);
                }
            }
        }

        [RelayCommand]
        private async Task DeleteItem(InventoryPageModel item) // Delete selected inventory item
        {
            if (item == null) return;

            // Check for dependencies first
            try
            {
                bool hasDependencies = await _database.HasInventoryProductDependenciesAsync(item.itemID);
                if (hasDependencies)
                {
                    await Application.Current.MainPage.DisplayAlert("Cannot Delete", 
                        "This inventory item cannot be deleted because it is currently used in one or more products. Please remove it from all products first.", "OK");
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking dependencies: {ex.Message}");
                // Continue with deletion attempt
            }

            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Confirm Delete",
                $"Delete '{item.itemName}'?",
                "Yes", "No");
            if (!confirm) return;

            try
            {
                var rows = await _database.DeleteInventoryItemAsync(item.itemID);
                if (rows > 0)
                {
                    AllItems.Remove(item);
                    Items.Remove(item);
                    MessagingCenter.Send(this, "InventoryChanged");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to delete item.", "OK");
                }
            }
            catch (InvalidOperationException ex)
            {
                // Handle foreign key constraint violations with user-friendly message
                await Application.Current.MainPage.DisplayAlert("Cannot Delete", 
                    "This inventory item cannot be deleted because it is currently used in one or more products. Please remove it from all products first.", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}


