using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace Coftea_Capstone
{
    partial class InventoryPageModel : ObservableObject
    {
        [PrimaryKey, AutoIncrement]
        public int itemID { get; set; }

        [NotNull]
        public int itemQuantity { get; set; }

        [NotNull]
        public string itemName { get; set; } = string.Empty;

        [NotNull]
        public string itemCategory { get; set; } = string.Empty;
    }
}
