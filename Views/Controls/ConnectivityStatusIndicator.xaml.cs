using Coftea_Capstone.ViewModel.Controls;

namespace Coftea_Capstone.Views.Controls;

public partial class ConnectivityStatusIndicator : ContentView
{
	public ConnectivityStatusIndicator()
	{
		InitializeComponent();
		BindingContext = new ConnectivityStatusIndicatorViewModel();
	}
}

