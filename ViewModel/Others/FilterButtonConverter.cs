using System.Globalization;

namespace Coftea_Capstone.ViewModel.Others
{
    public class FilterButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string selectedFilter && parameter is string filterOption)
            {
                return selectedFilter == filterOption;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
