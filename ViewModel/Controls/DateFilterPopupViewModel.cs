using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class DateFilterPopupViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isVisible = false;

        [ObservableProperty]
        private DateTime startDate = DateTime.Today;

        [ObservableProperty]
        private DateTime endDate = DateTime.Today;

        public DateTime Today => DateTime.Today;

        private Action<DateTime?, DateTime?>? _onDateSelected;

        public DateFilterPopupViewModel()
        {
            // Initialize with today's date
            StartDate = DateTime.Today;
            EndDate = DateTime.Today;
        }

        public void Show(Action<DateTime?, DateTime?> onDateSelected, DateTime? currentStartDate, DateTime? currentEndDate)
        {
            _onDateSelected = onDateSelected;
            
            // Set current dates if provided, otherwise use today
            if (currentStartDate.HasValue)
                StartDate = currentStartDate.Value;
            else
                StartDate = DateTime.Today;

            if (currentEndDate.HasValue)
            {
                // Subtract 1 day because endDate includes the next day
                var actualEndDate = currentEndDate.Value.AddDays(-1);
                EndDate = actualEndDate;
            }
            else
                EndDate = DateTime.Today;

            IsVisible = true;
        }

        [RelayCommand]
        private void Close()
        {
            IsVisible = false;
        }

        [RelayCommand]
        private void SelectQuickFilter(string filter)
        {
            var today = DateTime.Today;

            switch (filter)
            {
                case "Today":
                    StartDate = today;
                    EndDate = today;
                    break;
                case "Yesterday":
                    StartDate = today.AddDays(-1);
                    EndDate = today.AddDays(-1);
                    break;
                case "This Week":
                    StartDate = today.AddDays(-(int)today.DayOfWeek);
                    EndDate = today;
                    break;
                case "This Month":
                    StartDate = new DateTime(today.Year, today.Month, 1);
                    EndDate = today;
                    break;
                case "All Dates":
                    // For "All Dates", we'll set dates to a wide range
                    // The callback will handle null values
                    StartDate = new DateTime(2020, 1, 1);
                    EndDate = today;
                    break;
            }
        }

        [RelayCommand]
        private void ApplyDateFilter()
        {
            DateTime? startDate = StartDate;
            DateTime? endDate = EndDate.AddDays(1); // Add 1 day to include the end date

            // Check if "All Dates" was selected (start date is very early)
            if (StartDate.Year == 2020 && StartDate.Month == 1 && StartDate.Day == 1 && EndDate == Today)
            {
                startDate = null;
                endDate = null;
            }

            _onDateSelected?.Invoke(startDate, endDate);
            IsVisible = false;
        }
    }
}

