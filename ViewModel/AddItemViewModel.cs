using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coftea_Capstone.ViewModel
{
    public partial class AddItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string productName;

        [ObservableProperty]
        private string price;

        [ObservableProperty]
        private string selectedSize;
    }
}
