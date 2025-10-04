using System.Globalization;

namespace Coftea_Capstone.ViewModel.Others
{
    public class NotificationColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string notificationType)
            {
                return notificationType switch
                {
                    "Success" => "#4CAF50",
                    "Error" => "#F44336",
                    "Info" => "#2196F3",
                    _ => "#2196F3"
                };
            }
            return "#2196F3";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
