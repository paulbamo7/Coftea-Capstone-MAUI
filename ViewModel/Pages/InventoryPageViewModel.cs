using Coftea_Capstone.C_;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Coftea_Capstone.ViewModel.Controls;

namespace Coftea_Capstone.ViewModel
{
    public partial class InventoryPageViewModel : ObservableObject
    {
        public AddItemToPOSViewModel AddItemToInventoryPopup { get; } = new AddItemToPOSViewModel();
        public SettingsPopUpViewModel SettingsPopup { get; set; }

        private readonly Database _database;
        public InventoryPageViewModel(SettingsPopUpViewModel settingsPopup)
        {
            _database = new Database(
                host: "192.168.1.4",
                database: "coftea_db",
                user: "maui",
                password: "password123"
            );
            SettingsPopup = settingsPopup;
        }
        [RelayCommand]
        private void AddInventoryItem(InventoryPageViewModel inventory)
        {

        }
        [RelayCommand]
        private void EditInventoryItem(InventoryPageViewModel inventory)
        {

        }
        [RelayCommand]
        private void RemoveInventoryItem(InventoryPageViewModel inventory)
        {

        }   
        
    }
}
