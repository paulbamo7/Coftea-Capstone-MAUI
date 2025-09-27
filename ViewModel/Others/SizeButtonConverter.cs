using System.Globalization;

namespace Coftea_Capstone.ViewModel.Others
{
    public class SizeButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string selectedSize && parameter is string buttonSize)
            {
                return selectedSize == buttonSize ? "#FFD700" : "#E6DECC"; // Gold for selected, light beige for unselected
            }
            return "#E6DECC";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
