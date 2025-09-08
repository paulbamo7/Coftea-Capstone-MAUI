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
        private readonly Database _database;
        public ObservableCollection<InventoryItem> InventoryItems { get; set; }
        public InventoryPageViewModel(Database database)
        {
            _database = new Database(App.dbPath);
            InventoryItems = new ObservableCollection<InventoryItem>
            {
                new InventoryItem { Name = "Espresso", StockStatus = "Out of Stock", Color = Color.FromRgb(224, 202, 182) },
                new InventoryItem { Name = "Pearl", StockStatus = "Low of Stock", Color = Color.FromRgb(193, 160, 132) },
                new InventoryItem { Name = "Yakult", StockStatus = "Low of Stock", Color = Color.FromRgb(150, 132, 116) },
            };
        }
        public class InventoryItem
        {
            public string Name { get; set; }
            public string StockStatus { get; set; }
            public Color Color { get; set; }
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
