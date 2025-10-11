using System.Globalization;

namespace Coftea_Capstone.ViewModel.Others
{
    public class FilterButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string selectedFilter && parameter is string filterOption)
            {
                // Trim both values to handle any whitespace issues
                selectedFilter = selectedFilter?.Trim() ?? string.Empty;
                filterOption = filterOption?.Trim() ?? string.Empty;
                
                bool isSelected = string.Equals(selectedFilter, filterOption, StringComparison.OrdinalIgnoreCase);
                System.Diagnostics.Debug.WriteLine($"FilterButtonConverter: selectedFilter='{selectedFilter}', filterOption='{filterOption}', isSelected={isSelected}");
                return isSelected;
            }
            System.Diagnostics.Debug.WriteLine($"FilterButtonConverter: value='{value}', parameter='{parameter}'");
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
