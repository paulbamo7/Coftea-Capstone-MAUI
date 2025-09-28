using System.Globalization;

namespace Coftea_Capstone.ViewModel.Others
{
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "Ready to Confirm" => Colors.Green,
                    "Insufficient Amount" => Colors.Red,
                    "Processing..." => Colors.Orange,
                    "Payment Confirmed" => Colors.Green,
                    _ => Colors.Black
                };
            }
            return Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
