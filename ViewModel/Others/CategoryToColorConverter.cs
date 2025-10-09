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
                    "Transaction" => "#EAD7C6",
                    "Ingredients" => "#EBCFBF",
                    "Addons" => "#E3C2A6",
                    "Supplies" => "#F0D2B8",
                    "Syrups" => "#F2D9C1",
                    "Powdered" => "#F4E1D2",
                    "Fruit Series" => "#FFEAD6",
                    "Sinkers & etc." => "#F3D9D0",
                    "Others" => "#F1DEC7",
                    _ => "#FFEAD6" // Default lighter accent
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
