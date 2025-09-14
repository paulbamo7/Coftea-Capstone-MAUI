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
        public AddItemToPOSViewModel AddItemToInventoryPopup { get; } = new AddItemToPOSViewModel();

        private readonly Database _database;
        public InventoryPageViewModel()
        {
            _database = new Database(
                host: "192.168.1.4",
                database: "coftea_db",
                user: "maui",
                password: "password123"
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
