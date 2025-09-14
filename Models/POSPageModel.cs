using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coftea_Capstone.C_
{
    public class POSPageModel : ObservableObject
    {
       
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal SmallPrice { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal LargePrice { get; set; }
        /*public string Status { get; set; }*/
        public string ImageSet { get; set; }
        public string Category { get; set; }

        public bool HasSmall { get; set; } = true;
        public bool HasLarge { get; set; } = true;

        public string SelectedSize { get; set; } // "Small" or "Large"
        /*public string MenuCategory { get; set; }*/
    }
}
