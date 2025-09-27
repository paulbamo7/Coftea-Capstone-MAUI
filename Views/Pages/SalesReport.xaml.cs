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
		if (e.PropertyName == nameof(ViewModel.SalesReportPageViewModel.CurrentCategoryToday)
			|| e.PropertyName == nameof(ViewModel.SalesReportPageViewModel.CurrentCategoryWeekly))
		{
			UpdateCharts();
		}
	}

	private void UpdateCharts()
	{
		if (BindingContext is not ViewModel.SalesReportPageViewModel vm) return;

		// Update current category pie chart
		var currentCategoryPie = new SimplePieChartDrawable();
		foreach (var item in vm.CurrentCategoryToday)
		{
			currentCategoryPie.Slices.Add(new PieSlice { Label = item.Name, Value = item.Count, Color = GetColorForLabel(item.Name) });
		}
		
		// Find the GraphicsView elements by name
		var pieChart = this.FindByName<GraphicsView>("CurrentCategoryPie");
		var barChart = this.FindByName<GraphicsView>("CurrentCategoryBar");
		
		if (pieChart != null)
		{
			pieChart.Drawable = currentCategoryPie;
		}

		// Update current category bar chart
		var currentCategoryBar = new SimpleBarChartDrawable();
		foreach (var item in vm.CurrentCategoryWeekly)
		{
			currentCategoryBar.Items.Add(new BarItem { Label = item.Name, Value = item.Count, Color = GetColorForLabel(item.Name) });
		}
		
		if (barChart != null)
		{
			barChart.Drawable = currentCategoryBar;
		}
	}



	private static Color GetColorForLabel(string label)
	{
		// Simple fixed palette mapping
		return label switch
		{
			"Salted Caramel" => Color.FromRgb(210, 150, 100),
			"Vanilla Blanca" => Color.FromRgb(240, 220, 200),
			"Chocolate" => Color.FromRgb(120, 80, 60),
			"Java Chip" => Color.FromRgb(100, 60, 40),
			"Americano Black" => Color.FromRgb(80, 50, 30),
			"Americano White" => Color.FromRgb(180, 140, 100),
			"Cafe Latte" => Color.FromRgb(200, 160, 120),
			"Cappuccino" => Color.FromRgb(160, 120, 80),
			"Mocha Frappe" => Color.FromRgb(140, 100, 60),
			"Caramel Frappe" => Color.FromRgb(220, 180, 140),
			"Strawberry Frappe" => Color.FromRgb(255, 150, 150),
			"Orange Soda" => Color.FromRgb(255, 165, 0),
			"Lemon Soda" => Color.FromRgb(255, 255, 0),
			"Grape Soda" => Color.FromRgb(128, 0, 128),
			"Matcha" => Color.FromRgb(140, 190, 120),
			"Brown Sugar" => Color.FromRgb(160, 110, 70),
			"Hokkaido" => Color.FromRgb(200, 160, 120),
			"Taro" => Color.FromRgb(180, 120, 180),
			"Wintermelon" => Color.FromRgb(120, 200, 120),
			_ => Colors.SlateGray
		};
	}
}