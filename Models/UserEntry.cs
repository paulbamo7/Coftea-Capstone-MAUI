using CommunityToolkit.Mvvm.ComponentModel;

namespace Coftea_Capstone.Models
{
    public class UserEntry : ObservableObject
    {
        private bool _canAccessInventory;
        private bool _canAccessSalesReport;
        private static Database _database = new();

        public int Id { get; set; }
        public string Username { get; set; }
        public string LastActive { get; set; }
        public string DateAdded { get; set; }
        public bool IsAdmin { get; set; } 
        
        public bool CanAccessInventory 
        { 
            get => IsAdmin ? true : _canAccessInventory; // Admin always has access
            set 
            {
                if (!IsAdmin && SetProperty(ref _canAccessInventory, value)) // Only update if not admin
                {
                    // Update database when property changes
                    _ = Task.Run(async () => await UpdateDatabaseAsync());
                }
            }
        }
        public bool CanAccessSalesReport 
        { 
            get => IsAdmin ? true : _canAccessSalesReport; // Ensure admin always has access
            set 
            {
                if (!IsAdmin && SetProperty(ref _canAccessSalesReport, value))
                {
                    // Update database when property changes
                    _ = Task.Run(async () => await UpdateDatabaseAsync());
                }
            }
        }

        private async Task UpdateDatabaseAsync()
        {
            try
            {
                if (!IsAdmin)
                {
                    await _database.UpdateUserAccessAsync(Id, _canAccessInventory, _canAccessSalesReport);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating user access: {ex.Message}");
            }
        }
    }
}
