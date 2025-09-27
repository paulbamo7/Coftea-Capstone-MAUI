using System.Globalization;

namespace Coftea_Capstone.ViewModel.Others
{
    public class CupItemOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCupItem)
            {
                return isCupItem ? 0.6 : 1.0; // Cup items are dimmed to show they're not selectable
            }
            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
