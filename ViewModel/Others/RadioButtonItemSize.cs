using Microsoft.Maui.Controls;
using System;
using System.Globalization;

namespace Coftea_Capstone.ViewModel.Others;

public class RadioButtonItemSize : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
            return count == 0; // True if empty
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

}
