using Coftea_Capstone.C_;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coftea_Capstone.ViewModel
{
    public partial class InventoryPageViewModel : ObservableObject
    {
        public SettingsPopUpViewModel SettingsPopup { get; } = new SettingsPopUpViewModel();
        public AddItemToPOSViewModel AddItemToInventoryPopup { get; } = new AddItemToPOSViewModel();

        private readonly Database _database;
        public InventoryPageViewModel()
        {
            _database = new Database(
                host: "localhost",
                database: "coftea_db",
                user: "root",
                password: ""
            );

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
