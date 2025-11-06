using Coftea_Capstone.ViewModel.Controls;
using Microsoft.Maui.Controls;

namespace Coftea_Capstone.Views.Controls
{
    public partial class DateFilterPopup : ContentView
    {
        public DateFilterPopup()
        {
            InitializeComponent();
        }

        private void OnStartDateSelected(object sender, DateChangedEventArgs e)
        {
            // Update end date minimum when start date changes
            if (BindingContext is DateFilterPopupViewModel viewModel)
            {
                if (EndDatePicker != null && e.NewDate > EndDatePicker.Date)
                {
                    EndDatePicker.Date = e.NewDate;
                    viewModel.EndDate = e.NewDate;
                }
            }
        }
    }
}

