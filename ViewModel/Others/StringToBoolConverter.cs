using System.Globalization;

namespace Coftea_Capstone.ViewModel.Others
{
    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && parameter is string parameterValue)
            {
                // Handle negation with ! prefix
                if (parameterValue.StartsWith("!"))
                {
                    var targetValue = parameterValue.Substring(1);
                    return !string.Equals(stringValue, targetValue, StringComparison.OrdinalIgnoreCase);
                }
                
                return string.Equals(stringValue, parameterValue, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
