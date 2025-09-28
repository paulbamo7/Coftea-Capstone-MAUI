using System.Globalization;

namespace Coftea_Capstone.ViewModel.Others
{
    public class ChangeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal change)
            {
                if (change >= 0)
                    return Colors.Green;
                else
                    return Colors.Red;
            }
            return Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
