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
        [PrimaryKey, AutoIncrement]
        public int ProductID { get; set; }
        [NotNull]
        public string Name { get; set; } = string.Empty;
        [NotNull]
        public double Price { get; set; }
        [NotNull]
        public string Status { get; set; } = string.Empty;
        [NotNull]
        public string Image { get; set; } = string.Empty;
       
        /*[NotNull]
        public string MenuCategory { get; set; } = string.Empty;*/
    }
}
