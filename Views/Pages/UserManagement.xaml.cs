using Coftea_Capstone.ViewModel;
using Coftea_Capstone.ViewModel.Controls;

namespace Coftea_Capstone.Views.Pages;

public partial class UserManagement : ContentPage
{
    public UserManagement()
    {
        InitializeComponent();
        if (BindingContext is UserManagementPageViewModel vm)
        {
            _ = vm.InitializeAsync();
        }
    }

    private void OnBellClicked(object sender, EventArgs e)
    {
        var popup = ((App)Application.Current).NotificationPopup;
        popup?.AddSuccess("User Management", "Successfully created user. ID: UM1001", "ID: UM1001");
        popup?.AddSuccess("User Management", "Approved user request. ID: UR995", "ID: UR995");
        popup?.ToggleCommand.Execute(null);
    }
}


