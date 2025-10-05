using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.Generic;
using System.Linq;

namespace Coftea_Capstone.Views.Controls
{
    public partial class SimplePieChart : ContentView
    {
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
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Item1Count) || 
                    e.PropertyName == nameof(Item2Count) || 
                    e.PropertyName == nameof(Item3Count))
                {
                    UpdateChart();
                }
            };
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
                HideAllSlices();
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
            UpdatePieChart(items);
        }

        private void HideAllSlices()
        {
            Slice1.IsVisible = false;
            Slice2.IsVisible = false;
            Slice3.IsVisible = false;
        }

        private void UpdatePieChart(List<object> items)
        {
            if (items.Count == 0)
            {
                HideAllSlices();
                return;
            }

            // Get trend items and calculate total
            var trendItems = items.OfType<Coftea_Capstone.Models.TrendItem>().ToList();
            if (trendItems.Count == 0)
            {
                HideAllSlices();
                return;
            }

            var totalCount = trendItems.Sum(item => item.Count);
            if (totalCount == 0)
            {
                HideAllSlices();
                return;
            }

            // Show slices based on data availability
            Slice1.IsVisible = trendItems.Count >= 1;
            Slice2.IsVisible = trendItems.Count >= 2;
            Slice3.IsVisible = trendItems.Count >= 3;

            // Calculate pie slice angles based on data proportions
            // Ensure all angles add up to 360 degrees
            double currentAngle = 0;
            
            if (trendItems.Count >= 1)
            {
                var item1Percentage = (double)trendItems[0].Count / totalCount;
                var angle1 = item1Percentage * 360;
                UpdateSlicePath(Slice1, currentAngle, angle1);
                currentAngle += angle1;
            }

            if (trendItems.Count >= 2)
            {
                var item2Percentage = (double)trendItems[1].Count / totalCount;
                var angle2 = item2Percentage * 360;
                UpdateSlicePath(Slice2, currentAngle, angle2);
                currentAngle += angle2;
            }

            if (trendItems.Count >= 3)
            {
                var item3Percentage = (double)trendItems[2].Count / totalCount;
                var angle3 = item3Percentage * 360;
                UpdateSlicePath(Slice3, currentAngle, angle3);
                currentAngle += angle3;
            }

            // Ensure all angles add up to exactly 360 degrees
            if (currentAngle < 360)
            {
                var remainingAngle = 360 - currentAngle;
                if (trendItems.Count == 1)
                {
                    // Single item takes full circle
                    UpdateSlicePath(Slice1, 0, 360);
                    Slice2.IsVisible = false;
                    Slice3.IsVisible = false;
                }
                else if (trendItems.Count == 2)
                {
                    // Two items - distribute remaining angle to second item
                    var item2Percentage = (double)trendItems[1].Count / totalCount;
                    var angle2 = item2Percentage * 360;
                    UpdateSlicePath(Slice2, currentAngle - angle2, angle2);
                    Slice3.IsVisible = false;
                }
            }
        }

        private void UpdateSlicePath(Microsoft.Maui.Controls.Shapes.Path slice, double startAngle, double sweepAngle)
        {
            // Convert angles to radians
            var startRad = startAngle * Math.PI / 180;
            var endRad = (startAngle + sweepAngle) * Math.PI / 180;

            // Center point
            var centerX = 80.0;
            var centerY = 80.0;
            var outerRadius = 60.0;
            var innerRadius = 30.0; // Inner radius for donut hole

            // Calculate outer arc points with proper circular positioning
            var outerStartX = centerX + outerRadius * Math.Cos(startRad - Math.PI / 2);
            var outerStartY = centerY + outerRadius * Math.Sin(startRad - Math.PI / 2);
            var outerEndX = centerX + outerRadius * Math.Cos(endRad - Math.PI / 2);
            var outerEndY = centerY + outerRadius * Math.Sin(endRad - Math.PI / 2);

            // Calculate inner arc points with proper circular positioning
            var innerStartX = centerX + innerRadius * Math.Cos(startRad - Math.PI / 2);
            var innerStartY = centerY + innerRadius * Math.Sin(startRad - Math.PI / 2);
            var innerEndX = centerX + innerRadius * Math.Cos(endRad - Math.PI / 2);
            var innerEndY = centerY + innerRadius * Math.Sin(endRad - Math.PI / 2);

            // Create PathGeometry for donut slice
            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure();
            
            // Start from outer arc start point
            pathFigure.StartPoint = new Point(outerStartX, outerStartY);
            
            // Add outer arc
            var outerArcSegment = new ArcSegment
            {
                Point = new Point(outerEndX, outerEndY),
                Size = new Size(outerRadius, outerRadius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = sweepAngle > 180
            };
            pathFigure.Segments.Add(outerArcSegment);
            
            // Add line to inner arc end point
            pathFigure.Segments.Add(new LineSegment { Point = new Point(innerEndX, innerEndY) });
            
            // Add inner arc (reverse direction)
            var innerArcSegment = new ArcSegment
            {
                Point = new Point(innerStartX, innerStartY),
                Size = new Size(innerRadius, innerRadius),
                SweepDirection = SweepDirection.CounterClockwise,
                IsLargeArc = sweepAngle > 180
            };
            pathFigure.Segments.Add(innerArcSegment);
            
            // Add line back to start to close the path
            pathFigure.Segments.Add(new LineSegment { Point = new Point(outerStartX, outerStartY) });
            
            // Close the path
            pathFigure.IsClosed = true;
            pathGeometry.Figures.Add(pathFigure);
            slice.Data = pathGeometry;
        }

        private string GetItemName(object item)
        {
            if (item is Coftea_Capstone.Models.TrendItem trendItem)
                return trendItem.Name;
            
            return item?.ToString() ?? "Unknown";
        }
    }
}
