using System;
using System.Globalization;

namespace Coftea_Capstone.ViewModel.Others
{
    public class IsPositiveConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return false;
            }

            switch (value)
            {
                case bool boolean:
                    return boolean;
                case decimal dec:
                    return dec >= 0;
                case int i:
                    return i > 0;
                case long l:
                    return l > 0;
                case double d:
                    return d > 0;
                case float f:
                    return f > 0;
                case IConvertible convertible:
                    try
                    {
                        return convertible.ToDouble(culture) > 0;
                    }
                    catch
                    {
                        return false;
                    }
                default:
                    return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
