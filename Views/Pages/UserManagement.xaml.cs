using Coftea_Capstone.ViewModel;

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
}


