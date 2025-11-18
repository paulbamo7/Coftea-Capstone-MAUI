using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coftea_Capstone.C_;
using Coftea_Capstone.Models;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ImageSource = Microsoft.Maui.Controls.ImageSource;

namespace Coftea_Capstone.ViewModel.Controls
{
	public class ProductPickerItem
	{
		public string Name { get; set; } = string.Empty;
		public string Category { get; set; } = "Other";
		public string ImageSet { get; set; } = "product_placeholder.png";
		public string? Subcategory { get; set; }
		
		public ImageSource ImageSource // Convert ImageSet string to ImageSource for binding
		{
			get
			{
				try
				{
					if (string.IsNullOrWhiteSpace(ImageSet))
						return Microsoft.Maui.Controls.ImageSource.FromFile("coftea_logo.png"); 
					
					// Handle HTTP URLs
					if (ImageSet.StartsWith("http"))
						return Microsoft.Maui.Controls.ImageSource.FromUri(new Uri(ImageSet));

					// Handle local file paths (legacy support)
					if (System.IO.Path.IsPathRooted(ImageSet) && System.IO.File.Exists(ImageSet))
						return Microsoft.Maui.Controls.ImageSource.FromFile(ImageSet);

					// Handle app data directory images (new system)
					var appDataPath = Services.ImagePersistenceService.GetImagePath(ImageSet);
					if (System.IO.File.Exists(appDataPath))
						return Microsoft.Maui.Controls.ImageSource.FromFile(appDataPath);

					// Fallback to bundled resource (restoration from database happens when items are loaded)
					return Microsoft.Maui.Controls.ImageSource.FromFile(ImageSet);
				}
				catch
				{
					return Microsoft.Maui.Controls.ImageSource.FromFile("coftea_logo.png");
				}
			}
		}
	}

	public partial class ProductPickerPopupViewModel : ObservableObject
	{
		private readonly Database _database = new Database();

		[ObservableProperty]
		private bool isVisible;

		[ObservableProperty]
		private string selectedCategory = "All";

		[ObservableProperty]
		private string selectedSubcategory = "All";

		public bool IsSubcategoryVisible =>
			!string.IsNullOrWhiteSpace(SelectedCategory) &&
			(SelectedCategory.Equals("Coffee", StringComparison.OrdinalIgnoreCase) ||
			 SelectedCategory.Equals("Fruit/Soda", StringComparison.OrdinalIgnoreCase));

		public ObservableCollection<string> AvailableCategories { get; } = new();
		public ObservableCollection<string> AvailableSubcategories { get; } = new();
		public ObservableCollection<ProductPickerItem> AllProducts { get; } = new();
		public ObservableCollection<ProductPickerItem> FilteredProducts { get; } = new();

		public event Action<string>? OnProductSelected;

		public async Task InitializeAsync()
		{
			if (AvailableCategories.Count == 0)
			{
				AvailableCategories.Add("All");
			}

			var products = await _database.GetProductsAsyncCached();

			var categories = products
				.Select(p => string.IsNullOrWhiteSpace(p.Category) ? "Other" : p.Category.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(c => c);
			foreach (var cat in categories)
			{
				if (!AvailableCategories.Contains(cat))
				{
					AvailableCategories.Add(cat);
				}
			}

			AllProducts.Clear();
			foreach (var p in products.OrderBy(p => p.ProductName))
			{
				if (string.IsNullOrWhiteSpace(p.ProductName))
				{
					continue;
				}

				AllProducts.Add(new ProductPickerItem
				{
					Name = p.ProductName,
					Category = string.IsNullOrWhiteSpace(p.Category) ? "Other" : p.Category.Trim(),
					Subcategory = string.IsNullOrWhiteSpace(p.Subcategory) ? null : p.Subcategory.Trim(),
					ImageSet = !string.IsNullOrWhiteSpace(p.ImageSet) ? p.ImageSet : "product_placeholder.png"
				});
			}

			RefreshSubcategories();
			ApplyFilter();
		}

		private void RefreshSubcategories()
		{
			AvailableSubcategories.Clear();
			if (!IsSubcategoryVisible)
			{
				SelectedSubcategory = "All";
				OnPropertyChanged(nameof(IsSubcategoryVisible));
				return;
			}

			AvailableSubcategories.Add("All");
			var subs = AllProducts
				.Where(p => p.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(p.Subcategory))
				.Select(p => p.Subcategory!)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(s => s);
			foreach (var s in subs)
			{
				AvailableSubcategories.Add(s);
			}

			SelectedSubcategory = "All";
			OnPropertyChanged(nameof(IsSubcategoryVisible));
		}

		private void ApplyFilter()
		{
			FilteredProducts.Clear();

			IEnumerable<ProductPickerItem> query = AllProducts;
			if (!string.IsNullOrWhiteSpace(SelectedCategory) && SelectedCategory != "All")
			{
				query = query.Where(p => p.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase));
			}
			if (IsSubcategoryVisible && !string.IsNullOrWhiteSpace(SelectedSubcategory) && !SelectedSubcategory.Equals("All", StringComparison.OrdinalIgnoreCase))
			{
				query = query.Where(p => string.Equals(p.Subcategory, SelectedSubcategory, StringComparison.OrdinalIgnoreCase));
			}

			foreach (var item in query)
			{
				FilteredProducts.Add(item);
			}
		}

		[RelayCommand]
		private void SelectCategory(string category)
		{
			SelectedCategory = category;
			RefreshSubcategories();
			ApplyFilter();
		}

		[RelayCommand]
		private void SelectSubcategory(string subcategory)
		{
			SelectedSubcategory = subcategory;
			ApplyFilter();
		}

		[RelayCommand]
		private void Close()
		{
			IsVisible = false;
		}

		[RelayCommand]
		private void SelectProduct(string productName)
		{
			if (string.IsNullOrWhiteSpace(productName))
			{
				return;
			}
			IsVisible = false;
			OnProductSelected?.Invoke(productName);
		}
	}
}


