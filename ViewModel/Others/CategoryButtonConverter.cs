using System.Globalization;

namespace Coftea_Capstone.ViewModel.Others
{
    public class CategoryButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string selectedCategory && parameter is string category)
            {
                return selectedCategory == category ? "#8B4513" : "#F5E6D8";
            }
            return "#F5E6D8";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
