using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls.Xaml;
using System.ComponentModel;

namespace Coftea_Capstone.Views.Controls
{
    public partial class SimplePieChart : ContentView
    {
        private EventHandler _sizeChangedHandler;
        private PropertyChangedEventHandler _propertyChangedHandler;
        public static readonly BindableProperty ShowLegendProperty =
            BindableProperty.Create(nameof(ShowLegend), typeof(bool), typeof(SimplePieChart), true);

        public bool ShowLegend
        {
            get => (bool)GetValue(ShowLegendProperty);
            set => SetValue(ShowLegendProperty, value);
        }
        public static readonly BindableProperty ItemsSourceProperty = 
            BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable<object>), typeof(SimplePieChart), 
                propertyChanged: OnItemsSourceChanged);

        public static readonly BindableProperty Item1NameProperty = 
            BindableProperty.Create(nameof(Item1Name), typeof(string), typeof(SimplePieChart), "Item 1");

        public static readonly BindableProperty Item2NameProperty = 
            BindableProperty.Create(nameof(Item2Name), typeof(string), typeof(SimplePieChart), "Item 2");

        public static readonly BindableProperty Item3NameProperty = 
            BindableProperty.Create(nameof(Item3Name), typeof(string), typeof(SimplePieChart), "Item 3");

        // Order count properties
        public static readonly BindableProperty Item1CountProperty = 
            BindableProperty.Create(nameof(Item1Count), typeof(int), typeof(SimplePieChart), 0);
        
        public static readonly BindableProperty Item2CountProperty = 
            BindableProperty.Create(nameof(Item2Count), typeof(int), typeof(SimplePieChart), 0);
        
        public static readonly BindableProperty Item3CountProperty = 
            BindableProperty.Create(nameof(Item3Count), typeof(int), typeof(SimplePieChart), 0);

        // Trend indicator properties
        public static readonly BindableProperty Item1HasTrendProperty = 
            BindableProperty.Create(nameof(Item1HasTrend), typeof(bool), typeof(SimplePieChart), false);
        
        public static readonly BindableProperty Item2HasTrendProperty = 
            BindableProperty.Create(nameof(Item2HasTrend), typeof(bool), typeof(SimplePieChart), false);
        
        public static readonly BindableProperty Item3HasTrendProperty = 
            BindableProperty.Create(nameof(Item3HasTrend), typeof(bool), typeof(SimplePieChart), false);

        public IEnumerable<object> ItemsSource
        {
            get => (IEnumerable<object>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public string Item1Name
        {
            get => (string)GetValue(Item1NameProperty);
            set => SetValue(Item1NameProperty, value);
        }

        public string Item2Name
        {
            get => (string)GetValue(Item2NameProperty);
            set => SetValue(Item2NameProperty, value);
        }

        public string Item3Name
        {
            get => (string)GetValue(Item3NameProperty);
            set => SetValue(Item3NameProperty, value);
        }

        public int Item1Count
        {
            get => (int)GetValue(Item1CountProperty);
            set => SetValue(Item1CountProperty, value);
        }

        public int Item2Count
        {
            get => (int)GetValue(Item2CountProperty);
            set => SetValue(Item2CountProperty, value);
        }

        public int Item3Count
        {
            get => (int)GetValue(Item3CountProperty);
            set => SetValue(Item3CountProperty, value);
        }

        public bool Item1HasTrend
        {
            get => (bool)GetValue(Item1HasTrendProperty);
            set => SetValue(Item1HasTrendProperty, value);
        }

        public bool Item2HasTrend
        {
            get => (bool)GetValue(Item2HasTrendProperty);
            set => SetValue(Item2HasTrendProperty, value);
        }

        public bool Item3HasTrend
        {
            get => (bool)GetValue(Item3HasTrendProperty);
            set => SetValue(Item3HasTrendProperty, value);
        }

        public SimplePieChart()
        {
            InitializeComponent();
            
            // Subscribe to property changes to update the chart
            _propertyChangedHandler = (s, e) =>
            {
                if (e.PropertyName == nameof(Item1Count) || 
                    e.PropertyName == nameof(Item2Count) || 
                    e.PropertyName == nameof(Item3Count))
                {
                    UpdateChart();
                }
            };
            PropertyChanged += _propertyChangedHandler;

            // Ensure the chart remains a perfect circle regardless of layout constraints
            _sizeChangedHandler = (s, e) =>
            {
                EnsureSquareAndRedraw();

                // Redraw slices using the new square size to avoid elliptical distortion
                if (ItemsSource != null && ItemsSource.Any())
                {
                    UpdatePieChart(ItemsSource.Take(3).ToList());
                }
            };
            SizeChanged += _sizeChangedHandler;

            // Set drawable for GraphicsView
            if (PieChartCanvas != null)
            {
                PieChartCanvas.Drawable = new DonutDrawable(this);
                PieChartCanvas.Invalidate();
            }
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            if (Handler == null)
            {
                // Detach handlers to avoid retaining the view
                try { if (_propertyChangedHandler != null) PropertyChanged -= _propertyChangedHandler; } catch { }
                try { if (_sizeChangedHandler != null) SizeChanged -= _sizeChangedHandler; } catch { }
                _propertyChangedHandler = null;
                _sizeChangedHandler = null;
            }
        }

        private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is SimplePieChart chart)
            {
                chart.UpdateChart();
            }
        }

        private void UpdateChart()
        {
            if (ItemsSource == null || !ItemsSource.Any())
            {
                Item1Name = "No Data";
                Item2Name = "";
                Item3Name = "";
                Item1Count = 0;
                Item2Count = 0;
                Item3Count = 0;
                Item1HasTrend = false;
                Item2HasTrend = false;
                Item3HasTrend = false;
            InvalidateCanvas();
            return;
            }

            var items = ItemsSource.Take(3).ToList();
            var trendItems = items.OfType<Coftea_Capstone.Models.TrendItem>().ToList();
            
            if (trendItems.Count >= 1)
            {
                Item1Name = GetItemName(trendItems[0]);
                Item1Count = trendItems[0].Count;
                Item1HasTrend = trendItems[0].Count > 0; // Simple trend logic
            }
            if (trendItems.Count >= 2)
            {
                Item2Name = GetItemName(trendItems[1]);
                Item2Count = trendItems[1].Count;
                Item2HasTrend = trendItems[1].Count > 0;
            }
            if (trendItems.Count >= 3)
            {
                Item3Name = GetItemName(trendItems[2]);
                Item3Count = trendItems[2].Count;
                Item3HasTrend = trendItems[2].Count > 0;
            }

            // Update slice visibility and create proper pie chart
            EnsureSquareAndRedraw();
            UpdatePieChart(items);
            InvalidateCanvas();
        }

        private void HideAllSlices() { }

        private void UpdatePieChart(List<object> items)
        {
            if (items.Count == 0) return;

            // Get trend items and calculate total
            var trendItems = items.OfType<Coftea_Capstone.Models.TrendItem>().ToList();
            if (trendItems.Count == 0) return;

            var totalCount = trendItems.Sum(item => item.Count);
            if (totalCount == 0) return;

            // No-op: visibility handled by drawing

            // Calculate pie slice angles based on data proportions
            // Ensure all angles add up to 360 degrees
            double currentAngle = 0;
            
            if (trendItems.Count >= 1)
            {
                var item1Percentage = (double)trendItems[0].Count / totalCount;
                var angle1 = item1Percentage * 360;
                _slice1Start = currentAngle;
                _slice1Sweep = angle1;
                currentAngle += angle1;
            }

            if (trendItems.Count >= 2)
            {
                var item2Percentage = (double)trendItems[1].Count / totalCount;
                var angle2 = item2Percentage * 360;
                _slice2Start = currentAngle;
                _slice2Sweep = angle2;
                currentAngle += angle2;
            }

            if (trendItems.Count >= 3)
            {
                var item3Percentage = (double)trendItems[2].Count / totalCount;
                var angle3 = item3Percentage * 360;
                _slice3Start = currentAngle;
                _slice3Sweep = angle3;
                currentAngle += angle3;
            }

            // Ensure all angles add up to exactly 360 degrees
            if (currentAngle < 360)
            {
                var remainingAngle = 360 - currentAngle;
                if (trendItems.Count == 1)
                {
                    _slice1Start = 0;
                    _slice1Sweep = 360;
                }
                else if (trendItems.Count == 2)
                {
                    var item2Percentage = (double)trendItems[1].Count / totalCount;
                    var angle2 = item2Percentage * 360;
                    _slice2Start = currentAngle - angle2;
                    _slice2Sweep = angle2;
                }
            }
        }

        private void InvalidateCanvas()
        {
            PieChartCanvas?.Invalidate();
        }

        private void EnsureSquareAndRedraw()
        {
            // Keep the chart area square to avoid ellipse distortion from parent layout
            if (ChartContainer == null || PieChartGrid == null)
                return;

            var availableWidth = ChartContainer.Width > 0 ? ChartContainer.Width : ChartContainer.WidthRequest;
            var availableHeight = ChartContainer.Height > 0 ? ChartContainer.Height : ChartContainer.HeightRequest;

            if (availableWidth <= 0 || availableHeight <= 0)
                return;

            var side = Math.Min(availableWidth, availableHeight);

            // Set both to the same dimension to enforce 1:1 aspect
            PieChartGrid.WidthRequest = side;
            PieChartGrid.HeightRequest = side;
            PieChartCanvas.WidthRequest = side;
            PieChartCanvas.HeightRequest = side;
        }

        private string GetItemName(object item)
        {
            if (item is Coftea_Capstone.Models.TrendItem trendItem)
                return trendItem.Name;
            
            return item?.ToString() ?? "Unknown";
        }

        // Store slice angles for drawing
        private double _slice1Start, _slice1Sweep, _slice2Start, _slice2Sweep, _slice3Start, _slice3Sweep;

        private class DonutDrawable : IDrawable
        {
            private readonly SimplePieChart _owner;
            public DonutDrawable(SimplePieChart owner) { _owner = owner; }
            public void Draw(ICanvas canvas, RectF dirtyRect)
            {
                var side = Math.Min(dirtyRect.Width, dirtyRect.Height);
                var centerX = (float)(dirtyRect.X + dirtyRect.Width / 2);
                var centerY = (float)(dirtyRect.Y + dirtyRect.Height / 2);

                var outerRadius = (float)(side * 0.375);
                var innerRadius = (float)(side * 0.1875);

                // Draw slices in order
                DrawSlice(canvas, centerX, centerY, outerRadius, innerRadius, (float)_owner._slice1Start, (float)_owner._slice1Sweep, Color.FromArgb("#90EE90"));
                DrawSlice(canvas, centerX, centerY, outerRadius, innerRadius, (float)_owner._slice2Start, (float)_owner._slice2Sweep, Color.FromArgb("#F5DEB3"));
                DrawSlice(canvas, centerX, centerY, outerRadius, innerRadius, (float)_owner._slice3Start, (float)_owner._slice3Sweep, Color.FromArgb("#8B4513"));
            }

			private void DrawSlice(ICanvas canvas, float cx, float cy, float outerR, float innerR, float startDeg, float sweepDeg, Color color)
			{
				if (sweepDeg <= 0) return;
				canvas.SaveState();
				canvas.FillColor = color;
				
				// Build a donut slice path by approximating arcs with line segments
				var path = CreateDonutSlicePath(cx, cy, outerR, innerR, startDeg - 90f, sweepDeg, 48);
				canvas.FillPath(path);
				canvas.RestoreState();
			}

			private static PathF CreateDonutSlicePath(float cx, float cy, float outerR, float innerR, float startDeg, float sweepDeg, int segments)
			{
				var path = new PathF();
				float startRad = DegreesToRadians(startDeg);
				float endRad = DegreesToRadians(startDeg + sweepDeg);
				if (endRad < startRad)
				{
					endRad += MathF.Tau;
				}
				float sweepRad = endRad - startRad;
				int steps = Math.Max(1, segments);
				
				// Outer arc (start to end)
				for (int i = 0; i <= steps; i++)
				{
					float t = (float)i / steps;
					float a = startRad + sweepRad * t;
					float x = cx + outerR * MathF.Cos(a);
					float y = cy + outerR * MathF.Sin(a);
					if (i == 0)
					{
						path.MoveTo(x, y);
					}
					else
					{
						path.LineTo(x, y);
					}
				}
				
				// Inner arc (end back to start)
				for (int i = steps; i >= 0; i--)
				{
					float t = (float)i / steps;
					float a = startRad + sweepRad * t;
					float x = cx + innerR * MathF.Cos(a);
					float y = cy + innerR * MathF.Sin(a);
					path.LineTo(x, y);
				}
				
				path.Close();
				return path;
			}

			private static float DegreesToRadians(float degrees)
			{
				return degrees * (MathF.PI / 180f);
			}
        }
    }
}
