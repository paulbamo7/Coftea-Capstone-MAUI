using System;
using System.Globalization;
using System.Linq;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using Coftea_Capstone.C_;
using Coftea_Capstone.Models;

namespace Coftea_Capstone.ViewModel.Others
{
    public class SelectedCountToHeightConverter : IValueConverter
    {
        public double CompactHeight { get; set; } = 240; // when 3 or fewer selected
        public double ExpandedHeight { get; set; } = 420; // when more than 3 selected

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ObservableCollection<InventoryPageModel> items)
            {
                int selectedCount = items.Count(i => i.IsSelected);
                return selectedCount > 3 ? ExpandedHeight : CompactHeight;
            }
            return CompactHeight;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


