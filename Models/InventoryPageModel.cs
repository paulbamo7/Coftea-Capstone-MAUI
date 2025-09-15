using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace Coftea_Capstone.Models
{
    public class InventoryPageModel : ObservableObject
    {
        public int itemID { get; set; }

        public string itemName { get; set; }

        public double itemQuantity { get; set; } 
        public string itemCategory { get; set; }
        public string ImageSet { get; set; }
        public ImageSource ImageSource =>
        string.IsNullOrWhiteSpace(ImageSet) ? "placeholder.png" : ImageSource.FromFile(ImageSet);
    }
}
