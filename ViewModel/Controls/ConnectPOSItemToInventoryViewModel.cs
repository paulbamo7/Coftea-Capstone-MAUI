using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using Coftea_Capstone.ViewModel.Controls;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class ConnectPOSItemToInventoryViewModel : ObservableObject
    {

        // Event to trigger AddProduct in parent VM
        public event Action ConfirmPreviewRequested;
        [ObservableProperty] private bool isConnectPOSToInventoryVisible;
        [ObservableProperty] private bool isPreviewVisible;

        public ObservableCollection<Ingredient> Ingredients { get; set; } = new();

        // Preview-bound properties (populated from parent VM)
        [ObservableProperty] private ImageSource selectedImageSource;
        [ObservableProperty] private string productName;
        [ObservableProperty] private string selectedCategory;
        [ObservableProperty] private decimal smallPrice;
        [ObservableProperty] private decimal largePrice;
        [ObservableProperty] private string productDescription;

        // Event to notify AddItem popup
        public event Action ReturnRequested;

        [RelayCommand]
        private void ReturnToAddItemToPOS()
        {
            IsConnectPOSToInventoryVisible = false;
            ReturnRequested?.Invoke();
        }

        [RelayCommand]
        private void CloseConnectPOSToInventory()
        {
            IsConnectPOSToInventoryVisible = false;
        }
        [RelayCommand]
        private void ShowPreview()
        {
            IsPreviewVisible = true;
        }

        [RelayCommand]
        private void ClosePreview()
        {
            IsPreviewVisible = false;
        }

        [RelayCommand]
        private void ConfirmPreview()
        {
            IsConnectPOSToInventoryVisible = false; // close overlay
            IsPreviewVisible = false;
            ConfirmPreviewRequested?.Invoke();
        }
        public class Ingredient
        {
            public string Name { get; set; }
            public int Amount { get; set; }
            public bool Selected { get; set; }
        }
    }
}
