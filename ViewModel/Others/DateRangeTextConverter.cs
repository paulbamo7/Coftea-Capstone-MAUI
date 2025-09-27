using System.Globalization;

namespace Coftea_Capstone.ViewModel.Others
{
    public class DateRangeTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string selectedDateRange && parameter is string dateRange)
            {
                return selectedDateRange == dateRange ? "#FFFFFF" : "#8B4513";
            }
            return "#8B4513";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
