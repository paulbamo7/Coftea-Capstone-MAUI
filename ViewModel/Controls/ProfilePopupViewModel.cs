using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Coftea_Capstone.Models;
using Microsoft.Maui.Controls;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class ProfilePopupViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isProfileVisible = false;

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string fullName = string.Empty;

        [ObservableProperty]
        private string phoneNumber = string.Empty;

        [ObservableProperty]
        private bool isAdmin = false;

        [ObservableProperty]
        private string profileImage = "usericon.png";

        [ObservableProperty]
        private ImageSource profileImageSource = "usericon.png";

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private bool hasError = false;

        [ObservableProperty]
        private bool isImagePickerVisible = false;

        [ObservableProperty]
        private bool canAccessInventory = false;

        [ObservableProperty]
        private bool canAccessSalesReport = false;

        public ProfilePopupViewModel()
        {
            _ = Task.Run(async () => await LoadUserProfile()); // Load profile data on initialization
        }

        public async void ShowProfile() // Open the profile popup
        {
            System.Diagnostics.Debug.WriteLine("ShowProfile called - starting to load user profile");
            await LoadUserProfile();
            System.Diagnostics.Debug.WriteLine($"ShowProfile completed - IsProfileVisible set to true. Current data - Email: {Email}, FullName: {FullName}, PhoneNumber: {PhoneNumber}");
            IsProfileVisible = true;
        }

        [RelayCommand]
        private void CloseProfile()
        {
            IsProfileVisible = false;
        }

        [RelayCommand]
        private async Task SaveProfile() // Save profile changes
        {
            try
            {
                IsLoading = true;
                HasError = false;
                StatusMessage = "Saving profile...";

                System.Diagnostics.Debug.WriteLine($"SaveProfile called with data - Username: {Username}, Email: {Email}, FullName: {FullName}, PhoneNumber: {PhoneNumber}");

                // Validate required fields
                if (string.IsNullOrWhiteSpace(Email))
                {
                    StatusMessage = "Email is required";
                    HasError = true;
                    return;
                }

                // Save to preferences
                await SaveUserProfileToStorage();
                System.Diagnostics.Debug.WriteLine("Profile saved to preferences successfully");

                // Update the global current user if it exists
                if (App.CurrentUser != null)
                {
                    App.CurrentUser.Username = Username;
                    App.CurrentUser.Email = Email;
                    
                    // Parse full name and update FirstName and LastName
                    var nameParts = FullName?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
                    App.CurrentUser.FirstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
                    App.CurrentUser.LastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : string.Empty;
                    App.CurrentUser.FullName = FullName; // Keep the full name as well
                    
                    App.CurrentUser.PhoneNumber = PhoneNumber;
                    App.CurrentUser.ProfileImage = ProfileImage;
                    App.CurrentUser.CanAccessInventory = CanAccessInventory;
                    App.CurrentUser.CanAccessSalesReport = CanAccessSalesReport;
                    
                    System.Diagnostics.Debug.WriteLine("App.CurrentUser updated successfully");
                }

                // Save to database
                await SaveProfileToDatabase();
                System.Diagnostics.Debug.WriteLine("Profile saved to database successfully");

                StatusMessage = "Profile saved successfully!";
                HasError = false;

                // Reload profile data from database to ensure UI shows latest saved data
                await LoadUserProfile();
                
                // Refresh profile display across the app
                RefreshProfileDisplay();

                // Close popup after successful save
                await Task.Delay(1000);
                IsProfileVisible = false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving profile: {ex.Message}";
                HasError = true;
                System.Diagnostics.Debug.WriteLine($"Profile save error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                IsLoading = false;
            }
        }


        [RelayCommand]
        private void ToggleImagePicker() // Show/hide image picker
        {
            IsImagePickerVisible = !IsImagePickerVisible;
            if (IsImagePickerVisible)
            {
                StatusMessage = "Select a new profile image";
                HasError = false;
            }
        }

        [RelayCommand]
        private async Task ChangeProfileImage() // Change profile image
        {
            try
            {
                IsLoading = true;
                HasError = false;

                // Use the device's file picker to select an image
                var customFileType = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.iOS, new[] { "public.image" } },
                        { DevicePlatform.Android, new[] { "image/*" } },
                        { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" } },
                        { DevicePlatform.macOS, new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" } }
                    });

                var options = new PickOptions
                {
                    PickerTitle = "Select Profile Image",
                    FileTypes = customFileType
                };

                var result = await FilePicker.Default.PickAsync(options);
                if (result != null)
                {
                    // Copy the selected image to app data directory
                    var fileName = $"profile_{Guid.NewGuid()}{Path.GetExtension(result.FileName)}";
                    var targetPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                    
                    using var stream = await result.OpenReadAsync();
                    using var fileStream = File.Create(targetPath);
                    await stream.CopyToAsync(fileStream);
                    
                    ProfileImage = fileName;
                    ProfileImageSource = GetProfileImageSource(fileName);

                    // Update the global user profile
                    if (App.CurrentUser != null)
                    {
                        App.CurrentUser.ProfileImage = ProfileImage;
                    }

                    // Save to database
                    await SaveProfileImageToDatabase();

                    StatusMessage = "Profile image updated successfully!";
                    HasError = false;
                    IsImagePickerVisible = false;

                    // Refresh profile display across the app
                    RefreshProfileDisplay();

                    await Task.Delay(2000);
                    StatusMessage = string.Empty;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error changing profile image: {ex.Message}";
                HasError = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveProfileImageToDatabase() // Save profile image to database
        {
            try
            {
                var database = new Models.Database();
                var currentUserId = App.CurrentUser?.ID ?? 1;
                
                // Update user profile image in database
                await database.UpdateUserProfileImageAsync(currentUserId, ProfileImage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving profile image to database: {ex.Message}");
            }
        }

        public async Task LoadUserProfile() // Load profile data
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("LoadUserProfile called");
                
                // Always load fresh data from database to ensure we have the latest information
                await LoadUserFromDatabase();

                System.Diagnostics.Debug.WriteLine($"After LoadUserFromDatabase - Email: {Email}, FullName: {FullName}, PhoneNumber: {PhoneNumber}, Username: {Username}, ProfileImage: {ProfileImage}");

                // Trigger property change notifications to update UI on main thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    RefreshProfileDisplay();
                });

                StatusMessage = string.Empty;
                HasError = false;
                
                System.Diagnostics.Debug.WriteLine("LoadUserProfile completed successfully");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading profile: {ex.Message}";
                HasError = true;
                System.Diagnostics.Debug.WriteLine($"Profile load error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private async Task LoadUserFromDatabase() // Load user data from database
        {
            try
            {
                // Always load fresh data from database to ensure we have complete information
                var database = new Models.Database();
                var currentUserId = App.CurrentUser?.ID ?? 1; // Default to user ID 1 if not set
                var currentUserEmail = App.CurrentUser?.Email;
                
                System.Diagnostics.Debug.WriteLine($"Loading fresh user data from database for ID: {currentUserId}, Email: {currentUserEmail}");
                
                // Load user details from database
                var user = await database.GetUserByIdAsync(currentUserId);
                
                // If user not found by ID, try to find by email
                if (user == null && !string.IsNullOrEmpty(currentUserEmail))
                {
                    System.Diagnostics.Debug.WriteLine($"User not found by ID, trying to find by email: {currentUserEmail}");
                    user = await database.GetUserByEmailAsync(currentUserEmail);
                }
                
                if (user != null)
                {
                    System.Diagnostics.Debug.WriteLine($"User found in database: {user.Email}, {user.FirstName} {user.LastName}, Phone: {user.PhoneNumber}, Username: {user.Username}");
                    
                    // Update properties on main thread to ensure UI updates
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        // Load username from database
                        Username = user.Username ?? string.Empty;
                        
                        // Pre-fill other information from database
                        Email = user.Email ?? string.Empty;
                        
                        // Construct full name from FirstName + LastName
                        var firstName = user.FirstName ?? string.Empty;
                        var lastName = user.LastName ?? string.Empty;
                        FullName = $"{firstName} {lastName}".Trim();
                        
                        PhoneNumber = user.PhoneNumber ?? string.Empty;
                        IsAdmin = user.IsAdmin;
                        ProfileImage = user.ProfileImage ?? "usericon.png";
                        ProfileImageSource = GetProfileImageSource(ProfileImage);
                        
                        System.Diagnostics.Debug.WriteLine($"Profile data set - Username: '{Username}', ProfileImage: '{ProfileImage}'");
                    });
                    
                    // For admin users, always set access to true (admin has all permissions)
                    // For non-admin users, use the database values
                    if (user.IsAdmin)
                    {
                        CanAccessInventory = true;
                        CanAccessSalesReport = true;
                    }
                    else
                    {
                        CanAccessInventory = user.CanAccessInventory;
                        CanAccessSalesReport = user.CanAccessSalesReport;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Profile data loaded from database - Email: {Email}, FullName: {FullName}, Phone: {PhoneNumber}, IsAdmin: {IsAdmin}, CanAccessInventory: {CanAccessInventory}, CanAccessSalesReport: {CanAccessSalesReport}");
                    System.Diagnostics.Debug.WriteLine($"Database values - IsAdmin: {user.IsAdmin}, CanAccessInventory: {user.CanAccessInventory}, CanAccessSalesReport: {user.CanAccessSalesReport}");
                    
                    // Update App.CurrentUser with fresh data from database
                    if (App.CurrentUser != null)
                    {
                        var firstName = user.FirstName ?? string.Empty;
                        var lastName = user.LastName ?? string.Empty;
                        
                        App.CurrentUser.ID = user.ID; // Make sure we have the correct ID
                        App.CurrentUser.Username = user.Username ?? string.Empty;
                        App.CurrentUser.Email = user.Email ?? string.Empty;
                        App.CurrentUser.FirstName = firstName;
                        App.CurrentUser.LastName = lastName;
                        App.CurrentUser.FullName = $"{firstName} {lastName}".Trim();
                        App.CurrentUser.PhoneNumber = user.PhoneNumber ?? string.Empty;
                        App.CurrentUser.ProfileImage = user.ProfileImage ?? "usericon.png";
                        App.CurrentUser.CanAccessInventory = CanAccessInventory;
                        App.CurrentUser.CanAccessSalesReport = CanAccessSalesReport;
                        
                        System.Diagnostics.Debug.WriteLine($"App.CurrentUser updated with ID: {App.CurrentUser.ID}, Username: '{App.CurrentUser.Username}'");
                    }
                    return;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No user found with ID: {currentUserId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading user from database: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        private async Task SaveUserProfileToStorage() // Save profile data to preferences
        {
            try
            {
                // Save to preferences
                Preferences.Set("Username", Username);
                Preferences.Set("Email", Email);
                Preferences.Set("FullName", FullName);
                Preferences.Set("PhoneNumber", PhoneNumber);
                Preferences.Set("IsAdmin", IsAdmin);
                Preferences.Set("ProfileImage", ProfileImage);
                Preferences.Set("CanAccessInventory", CanAccessInventory);
                Preferences.Set("CanAccessSalesReport", CanAccessSalesReport);

                System.Diagnostics.Debug.WriteLine("Profile saved to preferences successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving profile to storage: {ex.Message}");
                throw;
            }
        }

        private async Task SaveProfileToDatabase() // Save profile data to database
        {
            try
            {
                var database = new Models.Database();
                var currentUserId = App.CurrentUser?.ID ?? 1;
                
                System.Diagnostics.Debug.WriteLine($"Saving profile to database for user ID: {currentUserId}");
                System.Diagnostics.Debug.WriteLine($"Data to save - Username: {Username}, Email: {Email}, FullName: {FullName}, PhoneNumber: {PhoneNumber}");
                
                // Update user profile in database
                var rowsAffected = await database.UpdateUserProfileAsync(currentUserId, Username, Email, FullName, PhoneNumber, ProfileImage, CanAccessInventory, CanAccessSalesReport);
                
                System.Diagnostics.Debug.WriteLine($"Database update completed. Rows affected: {rowsAffected}");
                
                if (rowsAffected == 0)
                {
                    throw new Exception("No rows were updated in the database. User might not exist or data is unchanged.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving profile to database: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        [RelayCommand]
        private async Task ResetProfile() // Reset profile changes
        {
            await LoadUserProfile();
            StatusMessage = "Profile reset to saved values";
            HasError = false;
        }

        // Method to refresh profile display across the app
        public void RefreshProfileDisplay() // Notify UI to refresh profile display
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("RefreshProfileDisplay called");
                
                // Force UI update by setting the ProfileImageSource again
                var tempImage = ProfileImage;
                ProfileImageSource = GetProfileImageSource(tempImage);
                
                // Trigger property change notifications for this popup
                OnPropertyChanged(nameof(Username));
                OnPropertyChanged(nameof(Email));
                OnPropertyChanged(nameof(FullName));
                OnPropertyChanged(nameof(PhoneNumber));
                OnPropertyChanged(nameof(IsAdmin));
                OnPropertyChanged(nameof(ProfileImage));
                OnPropertyChanged(nameof(ProfileImageSource));
                OnPropertyChanged(nameof(CanAccessInventory));
                OnPropertyChanged(nameof(CanAccessSalesReport));
                
                System.Diagnostics.Debug.WriteLine($"Property change notifications sent - Username: '{Username}', ProfileImage: '{ProfileImage}'");
                System.Diagnostics.Debug.WriteLine($"Full details - Email: {Email}, FullName: {FullName}, PhoneNumber: {PhoneNumber}, IsAdmin: {IsAdmin}");
                
                // Notify App.CurrentUser changes to trigger UI updates across all pages
                if (App.CurrentUser != null)
                {
                    // Trigger property change on App.CurrentUser if it has OnPropertyChanged method
                    try
                    {
                        var userType = App.CurrentUser.GetType();
                        var method = userType.GetMethod("OnPropertyChanged", 
                            System.Reflection.BindingFlags.Instance | 
                            System.Reflection.BindingFlags.NonPublic | 
                            System.Reflection.BindingFlags.Public,
                            null, 
                            new[] { typeof(string) }, 
                            null);
                        
                        if (method != null)
                        {
                            method.Invoke(App.CurrentUser, new object[] { nameof(App.CurrentUser.Username) });
                            method.Invoke(App.CurrentUser, new object[] { nameof(App.CurrentUser.ProfileImage) });
                            System.Diagnostics.Debug.WriteLine("Triggered property change notifications on App.CurrentUser");
                        }
                    }
                    catch (Exception reflectionEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Could not trigger property change via reflection: {reflectionEx.Message}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("Profile display refreshed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing profile display: {ex.Message}");
            }
        }

        private ImageSource GetProfileImageSource(string imageFileName) // Get ImageSource for profile image
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetProfileImageSource called with: {imageFileName}");
                
                if (string.IsNullOrWhiteSpace(imageFileName) || imageFileName == "usericon.png")
                {
                    System.Diagnostics.Debug.WriteLine("Using default usericon.png");
                    return ImageSource.FromFile("usericon.png");
                }

                // Check if it's a custom profile image in app data
                var appDataPath = Path.Combine(FileSystem.AppDataDirectory, imageFileName);
                System.Diagnostics.Debug.WriteLine($"Checking for image at: {appDataPath}");
                
                if (File.Exists(appDataPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Custom profile image found at: {appDataPath}");
                    return ImageSource.FromFile(appDataPath);
                }

                System.Diagnostics.Debug.WriteLine($"Custom image not found, checking if it's a resource file");
                
                // Check if it's a bundled resource file (e.g., avatar1.png, avatar2.png)
                try
                {
                    var resourceImage = ImageSource.FromFile(imageFileName);
                    System.Diagnostics.Debug.WriteLine($"Using resource image: {imageFileName}");
                    return resourceImage;
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine($"Resource image not found, using default");
                }

                // Fallback to default user icon
                return ImageSource.FromFile("usericon.png");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading profile image: {ex.Message}");
                return ImageSource.FromFile("usericon.png");
            }
        }
    }
}
