using Microsoft.Maui.Controls;
using Coftea_Capstone.Services;

namespace Coftea_Capstone.Views.Controls
{
    public partial class NotificationDropdown : ContentView
    {
        public NotificationDropdown()
        {
            InitializeComponent();
        }

        private void OnNotificationIconClicked(object sender, EventArgs e)
        {
            // Toggle dropdown visibility
            DropdownPanel.IsVisible = !DropdownPanel.IsVisible;
        }

        private void OnCloseClicked(object sender, EventArgs e)
        {
            // Hide dropdown
            DropdownPanel.IsVisible = false;
        }

        // Method to add new notifications programmatically
        public void AddNotification(string title, string message, string timeAgo, string iconSource)
        {
            var notificationGrid = new Grid
            {
                BackgroundColor = NotificationItemsContainer.Children.Count % 2 == 0 ? Color.FromArgb("#F8E8D7") : Colors.White,
                Padding = new Thickness(15, 10)
            };

            notificationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            notificationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            notificationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var icon = new Image
            {
                Source = iconSource,
                WidthRequest = 30,
                HeightRequest = 30,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(icon, 0);

            var textStack = new VerticalStackLayout
            {
                Margin = new Thickness(10, 0, 0, 0)
            };

            var titleLabel = new Label
            {
                Text = title,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#3E2723")
            };

            var messageLabel = new Label
            {
                Text = message,
                FontSize = 12,
                TextColor = Color.FromArgb("#5B4F45")
            };

            textStack.Children.Add(titleLabel);
            textStack.Children.Add(messageLabel);
            Grid.SetColumn(textStack, 1);

            var timeLabel = new Label
            {
                Text = timeAgo,
                FontSize = 10,
                TextColor = Color.FromArgb("#5B4F45"),
                VerticalOptions = LayoutOptions.Start
            };
            Grid.SetColumn(timeLabel, 2);

            notificationGrid.Children.Add(icon);
            notificationGrid.Children.Add(textStack);
            notificationGrid.Children.Add(timeLabel);

            // Use safe method to add notification grid to prevent IllegalStateException
            if (ViewParentHelper.SafeAddToParent(notificationGrid, NotificationItemsContainer))
            {
                // Move to top of the list if successfully added
                var index = NotificationItemsContainer.Children.IndexOf(notificationGrid);
                if (index > 0)
                {
                    NotificationItemsContainer.Children.RemoveAt(index);
                    NotificationItemsContainer.Children.Insert(0, notificationGrid);
                }
            }
        }

        // Method to clear all notifications
        public void ClearNotifications()
        {
            ViewParentHelper.SafeClearChildren(NotificationItemsContainer);
        }
    }
}
