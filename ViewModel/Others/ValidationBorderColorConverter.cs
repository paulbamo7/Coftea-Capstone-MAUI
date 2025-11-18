using System.Globalization;
using Microsoft.Maui.Graphics;

namespace Coftea_Capstone.ViewModel.Others
{
    public class ValidationBorderColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If there's a validation message (non-empty string), return red color
            if (value is string validationMessage)
            {
                return !string.IsNullOrWhiteSpace(validationMessage) ? Color.FromArgb("#D9534F") : Color.FromArgb("#8B7355");
            }
            
            // Default color when no validation message
            return Color.FromArgb("#8B7355");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

