using System.Globalization;

namespace Coftea_Capstone.ViewModel.Others
{
    public class CategoryTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string selectedCategory && parameter is string category)
            {
                return selectedCategory == category ? "#FFFFFF" : "#8B4513";
            }
            return "#8B4513";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
