using Coftea_Capstone.ViewModel;
using Coftea_Capstone.Views.Controls;
using Microsoft.Maui.Graphics;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Coftea_Capstone.Views.Pages;

public partial class SalesReport : ContentPage
{
	public SalesReport()
	{
		InitializeComponent();
		
		// Create and set the SalesReportPageViewModel
		var settingsPopup = ((App)Application.Current).SettingsPopup;
		var viewModel = new SalesReportPageViewModel(settingsPopup);
		BindingContext = viewModel;
		
		// Set RetryConnectionPopup binding context
		RetryConnectionPopup.BindingContext = ((App)Application.Current).RetryConnectionPopup;
		
		// Initialize the view model
		_ = viewModel.InitializeAsync();

		// Wire up charts to reflect the data when BindingContext changes
		BindingContextChanged += OnBindingContextChanged;
	}

	private void OnBindingContextChanged(object sender, EventArgs e)
	{
		UpdateCharts();
		if (BindingContext is INotifyPropertyChanged npc)
		{
			npc.PropertyChanged -= ViewModelOnPropertyChanged;
			npc.PropertyChanged += ViewModelOnPropertyChanged;
		}
	}

	private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(ViewModel.SalesReportPageViewModel.TopCoffeeToday)
			|| e.PropertyName == nameof(ViewModel.SalesReportPageViewModel.TopMilkteaToday)
			|| e.PropertyName == nameof(ViewModel.SalesReportPageViewModel.TopCoffeeWeekly)
			|| e.PropertyName == nameof(ViewModel.SalesReportPageViewModel.TopMilkteaWeekly))
		{
			UpdateCharts();
		}
	}

	private void UpdateCharts()
	{
		if (BindingContext is not ViewModel.SalesReportPageViewModel vm) return;

		// Build pie slices
		var coffeePie = new SimplePieChartDrawable();
		foreach (var item in vm.TopCoffeeToday)
		{
			coffeePie.Slices.Add(new PieSlice { Label = item.Name, Value = item.Count, Color = GetColorForLabel(item.Name) });
		}
		CoffeePie.Drawable = coffeePie;

		var milkTeaPie = new SimplePieChartDrawable();
		foreach (var item in vm.TopMilkteaToday)
		{
			milkTeaPie.Slices.Add(new PieSlice { Label = item.Name, Value = item.Count, Color = GetColorForLabel(item.Name) });
		}
		MilkTeaPie.Drawable = milkTeaPie;

		// Build bar items
		var coffeeBar = new SimpleBarChartDrawable();
		foreach (var item in vm.TopCoffeeWeekly)
		{
			coffeeBar.Items.Add(new BarItem { Label = item.Name, Value = item.Count, Color = GetColorForLabel(item.Name) });
		}
		CoffeeBar.Drawable = coffeeBar;

		var milkTeaBar = new SimpleBarChartDrawable();
		foreach (var item in vm.TopMilkteaWeekly)
		{
			milkTeaBar.Items.Add(new BarItem { Label = item.Name, Value = item.Count, Color = GetColorForLabel(item.Name) });
		}
		MilkTeaBar.Drawable = milkTeaBar;
	}



	private static Color GetColorForLabel(string label)
	{
		// Simple fixed palette mapping
		return label switch
		{
			"Salted Caramel" => Color.FromRgb(210, 150, 100),
			"Vanilla Blanca" => Color.FromRgb(240, 220, 200),
			"Chocolate" => Color.FromRgb(120, 80, 60),
			"Matcha" => Color.FromRgb(140, 190, 120),
			"Brown Sugar" => Color.FromRgb(160, 110, 70),
			"Hokkaido" => Color.FromRgb(200, 160, 120),
			_ => Colors.SlateGray
		};
	}
}