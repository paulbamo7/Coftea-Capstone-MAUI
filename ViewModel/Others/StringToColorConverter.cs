using System.Globalization;
using Microsoft.Maui.Graphics;

namespace Coftea_Capstone.ViewModel.Others
{
    public class StringToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorString && !string.IsNullOrEmpty(colorString))
            {
                try
                {
                    // Try to parse as hex color
                    if (colorString.StartsWith("#"))
                    {
                        return Color.FromArgb(colorString);
                    }
                    
                    // Try to parse as named color
                    return Color.FromArgb(colorString);
                }
                catch
                {
                    // Fallback to default color if parsing fails
                    return Colors.Purple;
                }
            }
            
            // Default color if no color code is provided
            return Colors.Purple;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
