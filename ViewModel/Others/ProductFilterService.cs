using System;
using System.Collections.Generic;
using System.Linq;
using Coftea_Capstone.C_;
using Coftea_Capstone.Models;

namespace Coftea_Capstone.ViewModel
{
	public static class ProductFilterService
	{
		public static IEnumerable<POSPageModel> Apply(
			IEnumerable<POSPageModel> products,
			string selectedMainCategory,
			string selectedSubcategory)
		{
			if (products == null) return Enumerable.Empty<POSPageModel>();
			IEnumerable<POSPageModel> filtered = products;

			var main = (selectedMainCategory ?? string.Empty).Trim();
			var sub = (selectedSubcategory ?? string.Empty).Trim();

			if (!string.IsNullOrWhiteSpace(main) && !string.Equals(main, "All", StringComparison.OrdinalIgnoreCase))
			{
				if (string.Equals(main, "Fruit/Soda", StringComparison.OrdinalIgnoreCase))
				{
					filtered = filtered.Where(p => p != null && (
						(!string.IsNullOrWhiteSpace(p.Subcategory) &&
							(p.Subcategory.Trim().Equals("Fruit", StringComparison.OrdinalIgnoreCase) ||
							 p.Subcategory.Trim().Equals("Soda", StringComparison.OrdinalIgnoreCase))) ||
						(!string.IsNullOrWhiteSpace(p.Category) &&
							(p.Category.Trim().Equals("Fruit", StringComparison.OrdinalIgnoreCase) ||
							 p.Category.Trim().Equals("Soda", StringComparison.OrdinalIgnoreCase) ||
							 p.Category.Trim().Equals("Fruit/Soda", StringComparison.OrdinalIgnoreCase)))));

					if (!string.IsNullOrWhiteSpace(sub))
					{
						filtered = filtered.Where(p => p != null && (
							(!string.IsNullOrWhiteSpace(p.Subcategory) && p.Subcategory.Trim().Equals(sub, StringComparison.OrdinalIgnoreCase)) ||
							(!string.IsNullOrWhiteSpace(p.Category) && p.Category.Trim().Equals(sub, StringComparison.OrdinalIgnoreCase))));
					}
				}
                else
                {
                    // First filter by main category strictly against Category
                    filtered = filtered.Where(p => p != null && (!string.IsNullOrWhiteSpace(p.Category) && p.Category.Trim().Equals(main, StringComparison.OrdinalIgnoreCase)));

                    // Then, if a subcategory is provided, narrow further
                    if (!string.IsNullOrWhiteSpace(sub))
                    {
                        filtered = filtered.Where(p => p != null && (!string.IsNullOrWhiteSpace(p.Subcategory) && p.Subcategory.Trim().Equals(sub, StringComparison.OrdinalIgnoreCase)));
                    }
                }
			}

			return filtered;
		}
	}
}


