using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
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

        public EditInventoryPopupViewModel(AddItemToInventoryViewModel addItemToInventoryViewModel)
        {
            _database = new Database(); // Will use auto-detected host
            _addItemToInventoryViewModel = addItemToInventoryViewModel;
        }

        [RelayCommand]
        private async Task LoadInventoryAsync()
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
        private async Task EditItem(InventoryPageModel item)
        {
            if (item == null) return;
            IsEditInventoryPopupVisible = false;

            // Pull latest from DB by ID to mirror Add flow behavior
            var fresh = await _database.GetInventoryItemByIdAsync(item.itemID) ?? item;

            // Seed AddItemToInventory with DB values and open UpdateInventoryDetails panel
            _addItemToInventoryViewModel.IsAddItemToInventoryVisible = false;
            _addItemToInventoryViewModel.IsUpdateInventoryDetailsVisible = true;
            _addItemToInventoryViewModel.ItemName = fresh.itemName;
            _addItemToInventoryViewModel.ItemCategory = fresh.itemCategory;
            _addItemToInventoryViewModel.ItemDescription = fresh.itemDescription;
            _addItemToInventoryViewModel.UoMQuantity = fresh.itemQuantity;
            _addItemToInventoryViewModel.SelectedUoM = fresh.unitOfMeasurement;
            _addItemToInventoryViewModel.MinimumQuantity = fresh.minimumQuantity;
            _addItemToInventoryViewModel.ImagePath = fresh.ImageSet;
            _addItemToInventoryViewModel.SelectedImageSource = fresh.ImageSource;
            _addItemToInventoryViewModel.BeginEdit(fresh.itemID);
        }

        [RelayCommand]
        private void CloseEditInventoryPopup()
        {
            IsEditInventoryPopupVisible = false;
        }

        public async Task ShowEditInventoryPopup()
        {
            IsEditInventoryPopupVisible = true;
            await LoadInventoryAsync();
        }

        [RelayCommand]
        private void FilterByCategory(string category)
        {
            Items.Clear();
            if (string.IsNullOrEmpty(category) || category == "All")
            {
                foreach (var it in AllItems) Items.Add(it);
                return;
            }
            foreach (var it in AllItems)
            {
                if (string.Equals(it.itemCategory, category, StringComparison.OrdinalIgnoreCase))
                {
                    Items.Add(it);
                }
            }
        }

        [RelayCommand]
        private async Task DeleteItem(InventoryPageModel item)
        {
            if (item == null) return;
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
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}


