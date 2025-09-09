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
        public string Name { get; set; }
        public double SmallPrice { get; set; }
        public double LargePrice { get; set; }
        /*public string Status { get; set; }*/
        public string Image { get; set; }
        /*public string MenuCategory { get; set; }*/
    }
}
