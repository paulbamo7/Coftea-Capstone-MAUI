using System.Globalization;

namespace Coftea_Capstone.ViewModel.Others
{
    public class NotificationTextIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string notificationType)
            {
                return notificationType switch
                {
                    "Success" => "✓",
                    "Error" => "⚠",
                    "Info" => "ℹ",
                    _ => "ℹ"
                };
            }
            return "ℹ";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
