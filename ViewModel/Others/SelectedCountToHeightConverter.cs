using System;
using System.Globalization;
using System.Linq;
using Microsoft.Maui.Controls;
using System.Collections;
using System.Collections.ObjectModel;
using Coftea_Capstone.Models;

namespace Coftea_Capstone.ViewModel.Others
{
    public class IndexOfInCollectionMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return 0;

            var collection = values[0] as IList;
            var item = values[1];
            if (collection == null || item == null) return 0;

            int index = 0;
            foreach (var it in collection)
            {
                if (ReferenceEquals(it, item) || (it?.Equals(item) ?? false))
                {
                    return index + 1; // 1-based
                }
                index++;
            }
            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

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