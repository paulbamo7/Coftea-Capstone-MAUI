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
        public double SmallPrice { get; set; }
        public int Quantity { get; set; } = 1;
        public double LargePrice { get; set; }
        /*public string Status { get; set; }*/
        public string ImageSet { get; set; }
        /*public string MenuCategory { get; set; }*/
    }
}
