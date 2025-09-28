using System.Globalization;

namespace Coftea_Capstone.ViewModel.Others
{
    public class NotificationIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string notificationType)
            {
                return notificationType switch
                {
                    "Success" => "checkmark.png",
                    "Error" => "warning.png", 
                    "Info" => "info.png",
                    _ => "info.png"
                };
            }
            return "info.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
