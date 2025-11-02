using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Coftea_Capstone.ViewModel.Others
{
    public class CategoryAndSubcategoryVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return false;
            
            // values[0] = Category, values[1] = Subcategory
            var category = values[0] as string;
            var subcategory = values[1] as string;
            
            // Check if category is Coffee or Fruit/Soda
            if (string.IsNullOrWhiteSpace(category)) return false;
            
            var normalizedCategory = category.Trim();
            bool isAllowedCategory = normalizedCategory.Equals("Coffee", StringComparison.OrdinalIgnoreCase) ||
                                    normalizedCategory.Equals("Fruit/Soda", StringComparison.OrdinalIgnoreCase) ||
                                    normalizedCategory.Equals("FruitSoda", StringComparison.OrdinalIgnoreCase) ||
                                    normalizedCategory.Equals("Fruit Soda", StringComparison.OrdinalIgnoreCase);
            
            // Only show if category is allowed AND subcategory is not null/empty
            return isAllowedCategory && !string.IsNullOrWhiteSpace(subcategory);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

