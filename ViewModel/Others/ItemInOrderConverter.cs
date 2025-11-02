using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Coftea_Capstone.ViewModel.Others
{
    public class ItemInOrderConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // values[0] = EditableItems.Count (to trigger re-evaluation)
                // values[1] = itemID
                if (values == null || values.Length < 2) return false;
                
                var itemId = System.Convert.ToInt32(values[1]);
                
                // Get the ViewModel from Application.Current
                var app = Application.Current as Coftea_Capstone.App;
                var viewModel = app?.CreatePurchaseOrderPopup;
                
                if (viewModel != null)
                {
                    return viewModel.IsItemInOrder(itemId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ItemInOrderConverter: {ex.Message}");
            }
            
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

