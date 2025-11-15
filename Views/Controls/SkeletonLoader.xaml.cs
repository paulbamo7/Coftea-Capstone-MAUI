using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Coftea_Capstone.Views.Controls
{
    public partial class SkeletonLoader : ContentView
    {
        public static readonly BindableProperty SkeletonTypeProperty =
            BindableProperty.Create(nameof(SkeletonType), typeof(SkeletonType), typeof(SkeletonLoader), SkeletonType.Card,
                propertyChanged: OnSkeletonTypeChanged);

        public SkeletonType SkeletonType
        {
            get => (SkeletonType)GetValue(SkeletonTypeProperty);
            set => SetValue(SkeletonTypeProperty, value);
        }

        public SkeletonLoader()
        {
            InitializeComponent();
            GenerateSkeleton();
        }

        private static void OnSkeletonTypeChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is SkeletonLoader loader)
            {
                loader.GenerateSkeleton();
            }
        }

        private void GenerateSkeleton()
        {
            SkeletonGrid.Children.Clear();
            SkeletonGrid.RowDefinitions.Clear();
            SkeletonGrid.ColumnDefinitions.Clear();

            switch (SkeletonType)
            {
                case SkeletonType.Card:
                    GenerateCardSkeleton();
                    break;
                case SkeletonType.Table:
                    GenerateTableSkeleton();
                    break;
                case SkeletonType.List:
                    GenerateListSkeleton();
                    break;
                case SkeletonType.Chart:
                    GenerateChartSkeleton();
                    break;
            }
        }

        private void GenerateCardSkeleton()
        {
            var stack = new VerticalStackLayout { Spacing = 12, Padding = 16 };
            
            // Title skeleton
            stack.Children.Add(CreateSkeletonBox(200, 20, 8));
            
            // Content skeleton
            stack.Children.Add(CreateSkeletonBox(150, 16, 8));
            stack.Children.Add(CreateSkeletonBox(180, 16, 8));
            
            SkeletonGrid.Children.Add(stack);
        }

        private void GenerateTableSkeleton()
        {
            var grid = new Grid
            {
                RowDefinitions = new RowDefinitionCollection
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                },
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                },
                RowSpacing = 8,
                ColumnSpacing = 8
            };

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    var box = CreateSkeletonBox(100, 40, 4);
                    grid.Children.Add(box);
                    Grid.SetRow(box, row);
                    Grid.SetColumn(box, col);
                }
            }

            SkeletonGrid.Children.Add(grid);
        }

        private void GenerateListSkeleton()
        {
            var stack = new VerticalStackLayout { Spacing = 12 };
            
            for (int i = 0; i < 5; i++)
            {
                var item = new HorizontalStackLayout { Spacing = 12, Padding = 8 };
                item.Children.Add(CreateSkeletonBox(50, 50, 8));
                item.Children.Add(new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        CreateSkeletonBox(150, 16, 4),
                        CreateSkeletonBox(100, 12, 4)
                    }
                });
                stack.Children.Add(item);
            }

            SkeletonGrid.Children.Add(stack);
        }

        private void GenerateChartSkeleton()
        {
            var grid = new Grid
            {
                RowDefinitions = new RowDefinitionCollection
                {
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            // Chart area skeleton
            var chartArea = new Grid
            {
                RowDefinitions = new RowDefinitionCollection
                {
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
                },
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                }
            };

            for (int i = 0; i < 5; i++)
            {
                var bar = CreateSkeletonBox(60, 80, 4);
                chartArea.Children.Add(bar);
                Grid.SetRow(bar, i);
                Grid.SetColumn(bar, 1);
            }

            grid.Children.Add(chartArea);
            Grid.SetRow(chartArea, 0);

            // X-axis skeleton
            var xAxis = new HorizontalStackLayout { Spacing = 8, Padding = 8 };
            for (int i = 0; i < 5; i++)
            {
                xAxis.Children.Add(CreateSkeletonBox(40, 12, 4));
            }
            grid.Children.Add(xAxis);
            Grid.SetRow(xAxis, 1);

            SkeletonGrid.Children.Add(grid);
        }

        private BoxView CreateSkeletonBox(double width, double height, double cornerRadius)
        {
            var box = new BoxView
            {
                WidthRequest = width,
                HeightRequest = height,
                Color = Colors.LightGray,
                CornerRadius = cornerRadius
            };

            // Add shimmer animation
            var animation = new Animation
            {
                { 0, 0.5, new Animation(v => box.Opacity = v, 0.3, 0.7) },
                { 0.5, 1, new Animation(v => box.Opacity = v, 0.7, 0.3) }
            };

            animation.Commit(box, "Shimmer", 16, 1500, Easing.Linear, null, () => true);

            return box;
        }
    }

    public enum SkeletonType
    {
        Card,
        Table,
        List,
        Chart
    }
}

