using System.Globalization;

namespace Coftea_Capstone.ViewModel.Others
{
    public class SizeButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string selectedSize && parameter is string buttonSize)
            {
                return selectedSize == buttonSize ? "Black" : "Black"; // Black text for both selected and unselected
            }
            return "Black";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
