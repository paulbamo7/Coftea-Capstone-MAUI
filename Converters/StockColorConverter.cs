using System.Globalization;

namespace Coftea_Capstone.Converters
{
    public class StockColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string alertText)
            {
                if (alertText.Contains("CRITICAL"))
                {
                    return Colors.Red;
                }
                else if (alertText.Contains("LOW"))
                {
                    return Colors.Orange;
                }
                else if (alertText.Contains("✅"))
                {
                    return Colors.Green;
                }
                else if (alertText.Contains("❌"))
                {
                    return Colors.Red;
                }
            }
            
            return Colors.Black; // Default color
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
