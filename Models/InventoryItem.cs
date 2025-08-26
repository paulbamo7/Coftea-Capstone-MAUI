using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Coftea_Capstone.C_
{
    class InventoryItem
    {
        [ObservableProperty]
        private int ItemID { get; set; }
        private int ItemQuantity { get; set; }
        private string ItemName { get; set; }
        private string ItemCategory { get; set; }
                     
    }
}
