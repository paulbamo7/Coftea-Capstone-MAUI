using System.Globalization;

namespace Coftea_Capstone.ViewModel.Others
{
    public class CategoryToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string category)
            {
                return category switch
                {
                    "Transaction" => "#5B4F45",
                    "Ingredients" => "#A57C5C",
                    "Addons" => "#8B4513",
                    "Supplies" => "#D2691E",
                    "Syrups" => "#CD853F",
                    "Powdered" => "#DEB887",
                    "Fruit Series" => "#F4A460",
                    "Sinkers & etc." => "#BC8F8F",
                    "Others" => "#D2B48C",
                    _ => "#5B4F45" // Default color
                };
            }
            return "#5B4F45";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
