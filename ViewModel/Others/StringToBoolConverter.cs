using System.Globalization;

namespace Coftea_Capstone.ViewModel.Others
{
    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If parameter is provided, use it for string comparison
            if (parameter is string parameterValue)
            {
                if (value is string stringValue)
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
            
            // If no parameter, check if string is not null/empty
            if (value is string str)
            {
                return !string.IsNullOrWhiteSpace(str);
            }
            
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
