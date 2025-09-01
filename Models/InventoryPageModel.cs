using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Coftea_Capstone
{
    partial class InventoryItem : ObservableObject
    {
        [ObservableProperty]
        private int itemID;
        private int itemQuantity;
        private string itemName;
        private string itemCategory;
    }
}
