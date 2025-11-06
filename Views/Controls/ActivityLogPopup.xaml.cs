using Coftea_Capstone.ViewModel.Controls;
using Microsoft.Maui.Controls;

namespace Coftea_Capstone.Views.Controls;

public partial class ActivityLogPopup : ContentView
{
    public ActivityLogPopup()
    {
        InitializeComponent();
    }

    private void OnActivityStartDateSelected(object sender, DateChangedEventArgs e)
    {
        // Update end date minimum when start date changes
        if (BindingContext is ActivityLogPopupViewModel viewModel)
        {
            if (ActivityEndDatePicker != null && e.NewDate > ActivityEndDatePicker.Date)
            {
                ActivityEndDatePicker.Date = e.NewDate;
                viewModel.FilterEndDate = e.NewDate;
            }
        }
    }
}
