using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Coftea_Capstone.Models;
using Coftea_Capstone.Services;
using Coftea_Capstone.Models.Service;
using System.Collections.ObjectModel;
using Coftea_Capstone.C_;

namespace Coftea_Capstone.ViewModel.Controls
{
    public partial class PaymentPopupViewModel : ObservableObject
    {
        private readonly CartStorageService _cartStorage = new CartStorageService();
        private readonly PayMongoService _payMongoService = new PayMongoService();
        private string? _currentGCashSourceId;
        private string? _currentGCashCheckoutUrl;
        private bool _gcashDeepLinkConfirmed;
        private CancellationTokenSource? _paymentCancellationTokenSource;
        
        private RetryConnectionPopupViewModel GetRetryConnectionPopup()
        {
            return ((App)Application.Current).RetryConnectionPopup;
        }
        
        [ObservableProperty]
        private bool isPaymentVisible = false;

        partial void OnIsPaymentVisibleChanged(bool value)
        {
            System.Diagnostics.Debug.WriteLine($"PaymentPopup IsPaymentVisible changed to: {value}");
        }

        [ObservableProperty]
        private decimal totalAmount;

        [ObservableProperty]
        private decimal amountPaid;

        [ObservableProperty]
        private decimal change;

        [ObservableProperty]
        private string paymentStatus = "Pending";

        [ObservableProperty]
        private string selectedPaymentMethod = "Cash";

        [ObservableProperty]
        private bool isCancelConfirmationVisible = false;

        [ObservableProperty]
        private bool isPaymentPaused = false;

        // Convenience flags used by UI/logic
        public bool IsCashSelected => string.Equals(SelectedPaymentMethod, "Cash", System.StringComparison.OrdinalIgnoreCase);
        public bool IsGCashSelected => string.Equals(SelectedPaymentMethod, "GCash", System.StringComparison.OrdinalIgnoreCase);
        public bool IsBankSelected => string.Equals(SelectedPaymentMethod, "Bank", System.StringComparison.OrdinalIgnoreCase);

        // Property to check if payment can be confirmed (requires internet and sufficient amount for cash)
        public bool CanConfirmPayment
        {
            get
            {
                // For cash payments, need both internet and sufficient amount
                if (IsCashSelected)
                {
                    return Services.NetworkService.HasInternetConnection() && Change >= 0;
                }
                // For GCash and Bank, only need internet (amount is auto-set)
                return Services.NetworkService.HasInternetConnection();
            }
        }

        [ObservableProperty]
        private List<CartItem> cartItems = new();

        [ObservableProperty]
        private ImageSource qrCodeImageSource;

        public PaymentPopupViewModel()
        {
            IsPaymentVisible = false;
            TotalAmount = 0;
            AmountPaid = 0;
            Change = 0;
            PaymentStatus = "Pending";
            CartItems = new List<CartItem>();
            QrCodeImageSource = null;
            
            // Debug: Log that PaymentPopup is initialized as hidden
            System.Diagnostics.Debug.WriteLine("PaymentPopupViewModel initialized with IsPaymentVisible = false");
        }

        public void ShowPayment(decimal total, List<CartItem> items) // Show payment popup with total and cart items
        {
            System.Diagnostics.Debug.WriteLine($"ShowPayment called with total: {total}, items: {items.Count}");
            TotalAmount = total;
            CartItems = items;
            AmountPaid = 0;
            Change = -TotalAmount;
            UpdatePaymentStatus();
            IsPaymentVisible = true;
            System.Diagnostics.Debug.WriteLine($"PaymentPopup IsPaymentVisible set to: {IsPaymentVisible}");

            _currentGCashCheckoutUrl = null;
            _currentGCashSourceId = null;
            QrCodeImageSource = null;
            SelectedPaymentMethod = "Cash";
            _gcashDeepLinkConfirmed = false;

            // Force property change notification
            OnPropertyChanged(nameof(IsPaymentVisible));
            OnPropertyChanged(nameof(QrCodeImageSource));
            OnPropertyChanged(nameof(SelectedPaymentMethod));
        }

        [RelayCommand]
        private void ClosePayment() // Close payment popup
        {
            // Cancel payment processing if it's running
            if (IsProcessingPayment)
            {
                CancelPaymentProcessing();
            }
            
            IsPaymentVisible = false;
            _currentGCashCheckoutUrl = null;
            _currentGCashSourceId = null;
            _gcashDeepLinkConfirmed = false;
        }

        private void CancelPaymentProcessing() // Cancel ongoing payment processing
        {
            if (_paymentCancellationTokenSource != null && !_paymentCancellationTokenSource.Token.IsCancellationRequested)
            {
                _paymentCancellationTokenSource.Cancel();
                System.Diagnostics.Debug.WriteLine("🛑 Payment processing cancelled");
            }
        }

        partial void OnSelectedPaymentMethodChanged(string value)
        {
            OnPropertyChanged(nameof(IsCashSelected));
            OnPropertyChanged(nameof(IsGCashSelected));
            OnPropertyChanged(nameof(IsBankSelected));
            OnPropertyChanged(nameof(CanConfirmPayment));
        }

        [RelayCommand]
        private async Task SelectPaymentMethod(string method) // Cash, GCash, Bank
        {
            SelectedPaymentMethod = method;
            System.Diagnostics.Debug.WriteLine($"Payment method selected: {method}");

            if (IsGCashSelected)
            {
                AmountPaid = TotalAmount;
                Change = 0;
                PaymentStatus = "Preparing GCash checkout...";

                var prepareResult = await EnsureGCashCheckoutAsync(forceNew: true);
                if (!prepareResult.Success)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "GCash Payment",
                        prepareResult.ErrorMessage ?? "Unable to prepare GCash checkout. Please try again.",
                        "OK");

                    SelectedPaymentMethod = "Cash";
                    AmountPaid = 0;
                    Change = -TotalAmount;
                    PaymentStatus = "Pending";
                    QrCodeImageSource = null;
                    return;
                }

                PaymentStatus = "Scan the QR code to continue on GCash";
            }
            else if (IsBankSelected)
            {
                AmountPaid = TotalAmount;
                Change = 0;
                PaymentStatus = "Ready to Confirm";
                QrCodeImageSource = null;
            }
            else
            {
                AmountPaid = 0;
                Change = -TotalAmount;
                UpdatePaymentStatus();
                QrCodeImageSource = null;
            }
        }

        public void UpdateAmountPaid(string amount) // Update amount paid from input
        {
            if (decimal.TryParse(amount, out decimal paid))
            {
                // Prevent negative numbers
                if (paid < 0)
                {
                    paid = 0;
                }
                
                AmountPaid = paid;
                Change = AmountPaid - TotalAmount;
                
                UpdatePaymentStatus();
            }
        }

        private void UpdatePaymentStatus() // Update payment status based on amount and internet connectivity
        {
            // Check internet connection first
            if (!Services.NetworkService.HasInternetConnection())
            {
                PaymentStatus = "No Internet Connection";
                OnPropertyChanged(nameof(CanConfirmPayment));
                return;
            }
            
            // If internet is available, check amount
            if (Change >= 0)
            {
                PaymentStatus = "Ready to Confirm";
            }
            else
            {
                PaymentStatus = "Insufficient Amount";
            }
            OnPropertyChanged(nameof(CanConfirmPayment));
        }

        [RelayCommand]
        private void AddToAmountPaid(object parameter) // Add amount to current amount paid
        {
            if (parameter == null) return;
            
            decimal amount = 0;
            if (parameter is decimal dec)
            {
                amount = dec;
            }
            else if (parameter is string str && decimal.TryParse(str, out decimal parsed))
            {
                amount = parsed;
            }
            else if (parameter is int intVal)
            {
                amount = intVal;
            }
            
            var newAmount = AmountPaid + amount;
            // Prevent negative numbers
            if (newAmount < 0)
            {
                newAmount = 0;
            }
            AmountPaid = newAmount;
            Change = AmountPaid - TotalAmount;
            
            UpdatePaymentStatus();
        }

        [RelayCommand]
        private void SubtractFromAmountPaid(object parameter) // Subtract amount from current amount paid
        {
            if (parameter == null) return;
            
            decimal amount = 0;
            if (parameter is decimal dec)
            {
                amount = dec;
            }
            else if (parameter is string str && decimal.TryParse(str, out decimal parsed))
            {
                amount = parsed;
            }
            else if (parameter is int intVal)
            {
                amount = intVal;
            }
            
            var newAmount = AmountPaid - amount;
            // Prevent negative numbers
            if (newAmount < 0)
            {
                newAmount = 0;
            }
            AmountPaid = newAmount;
            Change = AmountPaid - TotalAmount;
            
            if (Change >= 0)
            {
                PaymentStatus = "Ready to Confirm";
            }
            else
            {
                PaymentStatus = "Insufficient Amount";
            }
        }

        [RelayCommand]
        private void ClearAmountPaid() // Reset amount paid to 0
        {
            AmountPaid = 0;
            Change = -TotalAmount;
            UpdatePaymentStatus();
        }

        [RelayCommand]
        private void CancelPayment() // Show cancel confirmation UI
        {
            // Always show confirmation UI
            IsCancelConfirmationVisible = true;
        }

        [RelayCommand]
        private void ConfirmCancel() // Confirm cancellation - pause if processing, reset if not
        {
            // Always dismiss the confirmation overlay first
            IsCancelConfirmationVisible = false;
            
            // If payment is processing, pause it
            if (IsProcessingPayment)
            {
                PausePaymentProcessing();
            }
            else
            {
                // If not processing, just reset amounts
                AmountPaid = 0;
                Change = -TotalAmount;
                UpdatePaymentStatus();
                System.Diagnostics.Debug.WriteLine("🛑 Payment cancelled - amounts reset");
            }
        }

        [RelayCommand]
        private void DismissCancelConfirmation() // Dismiss cancel confirmation UI
        {
            IsCancelConfirmationVisible = false;
        }

        private void PausePaymentProcessing() // Pause ongoing payment processing
        {
            if (IsProcessingPayment)
            {
                IsPaymentPaused = true;
                PaymentStatus = "Paused";
                CancelPaymentProcessing();
                OnPropertyChanged(nameof(IsProcessingPayment));
                System.Diagnostics.Debug.WriteLine("⏸️ Payment processing paused");
            }
        }

        [RelayCommand]
        private async Task ResumePayment() // Resume paused payment
        {
            if (IsPaymentPaused)
            {
                IsPaymentPaused = false;
                OnPropertyChanged(nameof(IsProcessingPayment));
                // Restart payment processing
                await ConfirmPayment();
            }
        }

        [RelayCommand]
        private void FullyCancelPayment() // Fully cancel paused payment
        {
            if (IsPaymentPaused)
            {
                IsPaymentPaused = false;
                IsCancelConfirmationVisible = false; // Ensure confirmation overlay is dismissed
                AmountPaid = 0;
                Change = -TotalAmount;
                UpdatePaymentStatus();
                System.Diagnostics.Debug.WriteLine("🛑 Payment fully cancelled");
            }
        }

        private int _isProcessingPayment = 0; // Use int for Interlocked operations (0 = false, 1 = true)
        private readonly SemaphoreSlim _paymentSemaphore = new SemaphoreSlim(1, 1); // Thread-safe payment processing
        
        // Helper property to check if payment is processing (for clarity and to avoid IDE confusion)
        public bool IsProcessingPayment => Volatile.Read(ref _isProcessingPayment) != 0;

        [RelayCommand]
        private async Task ConfirmPayment() // Confirm and process payment
        {
            System.Diagnostics.Debug.WriteLine("🔵 ========== ConfirmPayment CALLED ==========");
            System.Diagnostics.Debug.WriteLine($"🔵 TotalAmount: {TotalAmount}, AmountPaid: {AmountPaid}");
            System.Diagnostics.Debug.WriteLine($"🔵 CartItems count: {CartItems?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"🔵 SelectedPaymentMethod: {SelectedPaymentMethod}");
            
            // Check internet connection first - before processing payment
            if (!Services.NetworkService.HasInternetConnection())
            {
                GetRetryConnectionPopup().ShowRetryPopup(
                    ConfirmPayment,
                    "No internet connection detected. Please check your network settings and try again.");
                return;
            }
            
            // For non-cash methods (GCash/Bank), auto-approve regardless of AmountPaid
            var isNonCash = IsGCashSelected || IsBankSelected;
            System.Diagnostics.Debug.WriteLine($"🔵 Is non-cash payment: {isNonCash}");
            if (!isNonCash)
            {
                if (AmountPaid < TotalAmount)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Insufficient Payment",
                        $"Amount paid (₱{AmountPaid:F2}) is less than total (₱{TotalAmount:F2})",
                        "OK");
                    return;
                }
            }

            // Thread-safe check: Try to acquire semaphore (non-blocking)
            if (!await _paymentSemaphore.WaitAsync(0))
            {
                System.Diagnostics.Debug.WriteLine("⚠️ ConfirmPayment aborted because processing is already running");
                return;
            }

            // Set flag atomically
            if (Interlocked.CompareExchange(ref _isProcessingPayment, 1, 0) != 0)
            {
                _paymentSemaphore.Release();
                System.Diagnostics.Debug.WriteLine("⚠️ ConfirmPayment aborted because processing is already running");
                return;
            }
            
            // Create cancellation token source for this payment
            _paymentCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _paymentCancellationTokenSource.Token;
            var paymentCompleted = false;

            try
            {
                // Check if cancelled before starting
                cancellationToken.ThrowIfCancellationRequested();
                
                if (IsGCashSelected)
                {
                    PaymentStatus = _gcashDeepLinkConfirmed
                        ? "GCash payment confirmed"
                        : "GCash payment confirmed (manual)";
                }

                System.Diagnostics.Debug.WriteLine("🔵 About to call ProcessPaymentWithSteps");
                
                // Assign payment method to each cart item BEFORE clearing
                var cartItemsCopy = new List<CartItem>();
                foreach (var item in CartItems)
                {
                    item.PaymentMethod = SelectedPaymentMethod; // Assign the payment method to each item
                    cartItemsCopy.Add(new CartItem
                    {
                        ProductName = item.ProductName,
                        PaymentMethod = item.PaymentMethod,
                        Price = item.Price,
                        SmallQuantity = item.SmallQuantity,
                        MediumQuantity = item.MediumQuantity,
                        LargeQuantity = item.LargeQuantity
                    });
                }
                
                // Process payment with realistic steps (with cancellation support)
                await ProcessPaymentWithSteps(cancellationToken);
                System.Diagnostics.Debug.WriteLine("🔵 ProcessPaymentWithSteps completed");
                
                // Check if cancelled after processing steps
                cancellationToken.ThrowIfCancellationRequested();

                System.Diagnostics.Debug.WriteLine("🔵 About to call SaveTransaction");
                // Save transaction to database and shared store
                var savedTransactions = await SaveTransaction();
                System.Diagnostics.Debug.WriteLine($"🔵 SaveTransaction completed. Saved {savedTransactions?.Count ?? 0} transactions");

                // Enqueue items to processing queue BEFORE clearing cart (in parallel for better performance)
                try
                {
                    System.Diagnostics.Debug.WriteLine($"📋 Enqueueing {CartItems.Count} items to processing queue");
                    var posApp = (App)Application.Current;
                    if (posApp?.POSVM?.ProcessingQueuePopup != null)
                    {
                        // Convert CartItems back to POSPageModel for enqueueing and process in parallel
                        var enqueueTasks = CartItems.Select(async cartItem =>
                        {
                            // Create POSPageModel from CartItem to enqueue to processing queue
                            var posItem = new POSPageModel
                            {
                                ProductID = cartItem.ProductId,
                                ProductName = cartItem.ProductName,
                                ImageSet = cartItem.ImageSet,
                                SmallQuantity = cartItem.SmallQuantity,
                                MediumQuantity = cartItem.MediumQuantity,
                                LargeQuantity = cartItem.LargeQuantity,
                                SmallPrice = cartItem.SmallPrice,
                                MediumPrice = cartItem.MediumPrice,
                                LargePrice = cartItem.LargePrice,
                                SmallAddons = cartItem.SmallAddons ?? new ObservableCollection<InventoryPageModel>(),
                                MediumAddons = cartItem.MediumAddons ?? new ObservableCollection<InventoryPageModel>(),
                                LargeAddons = cartItem.LargeAddons ?? new ObservableCollection<InventoryPageModel>(),
                                InventoryItems = cartItem.InventoryItems ?? new ObservableCollection<InventoryPageModel>()
                            };
                            
                            try
                            {
                                await posApp.POSVM.ProcessingQueuePopup.EnqueueFromCartItem(posItem);
                                System.Diagnostics.Debug.WriteLine($"✅ Enqueued {posItem.ProductName} to processing queue");
                                return (success: true, item: posItem);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"❌ Failed to enqueue {posItem.ProductName}: {ex.Message}");
                                return (success: false, item: posItem);
                            }
                        }).ToArray();
                        
                        var enqueueResults = await Task.WhenAll(enqueueTasks);
                        var successCount = enqueueResults.Count(r => r.success);
                        System.Diagnostics.Debug.WriteLine($"✅ Enqueued {successCount}/{CartItems.Count} items to processing queue");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Error enqueueing items to processing queue: {ex.Message}");
                }

                // Clear cart on successful payment
                try
                {
                    System.Diagnostics.Debug.WriteLine("🧹 Starting cart clear process...");
                    
                    // Clear the actual cart items in the payment popup
                    CartItems.Clear();
                    System.Diagnostics.Debug.WriteLine("✅ PaymentPopup CartItems cleared");
                    
                    // Also clear the cart in the POS page ViewModel
                    var currentApp = (App)Application.Current;
                    System.Diagnostics.Debug.WriteLine($"Current MainPage type: {currentApp?.MainPage?.GetType().Name}");
                    
                    // For Shell-based navigation, access the POS ViewModel directly from App
                    if (currentApp?.POSVM != null)
                    {
                        await currentApp.POSVM.ClearCartAsync();
                        System.Diagnostics.Debug.WriteLine("✅ Cart cleared in POS page ViewModel via App reference");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ POSPageViewModel not found in App");
                        
                        // Fallback: Try to find POS page in Shell
                        if (currentApp?.MainPage is Shell shell)
                        {
                            System.Diagnostics.Debug.WriteLine("Shell found, attempting to access current page...");
                            var currentPage = shell.CurrentPage;
                            System.Diagnostics.Debug.WriteLine($"Current Shell page: {currentPage?.GetType().Name}");
                            
                            if (currentPage is Coftea_Capstone.Views.Pages.PointOfSale posPage)
                            {
                                if (posPage.BindingContext is POSPageViewModel posViewModel)
                                {
                                    await posViewModel.ClearCartAsync();
                                    System.Diagnostics.Debug.WriteLine("✅ Cart cleared in POS page ViewModel via Shell.CurrentPage");
                                }
                            }
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine("✅ Cart clear process completed");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error clearing cart: {ex.Message}\n{ex.StackTrace}");
                }

                // Show success: order-complete popup with auto-disappear
                PaymentStatus = "Payment Confirmed";
                var appInstance = (App)Application.Current;
                
                // Use the actual transaction ID for order number consistency
                var orderNumber = savedTransactions?.FirstOrDefault()?.TransactionId.ToString() ?? "Unknown";
                
                if (appInstance?.OrderCompletePopup != null)
                {
                    await appInstance.OrderCompletePopup.ShowOrderCompleteAsync(orderNumber);
                }
                
                // Show order confirmation popup in bottom right with payment breakdown
                var orderConfirmedPopup = appInstance?.OrderConfirmedPopup;
                if (orderConfirmedPopup != null)
                {
                    await orderConfirmedPopup.ShowOrderConfirmationAsync(orderNumber, TotalAmount, SelectedPaymentMethod, cartItemsCopy);
                }
                
                // No automatic toast here; user can open notifications manually via bell
                
                // Add notifications for each saved transaction (badge only; panel shows details when opened)
                // Process notifications in parallel without blocking the main flow
                var notif = ((App)Application.Current)?.NotificationPopup;
                if (notif != null && savedTransactions != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var notificationTasks = savedTransactions.Select(t =>
                                notif.AddSuccess("Transaction", $"Completed: {t.DrinkName}", $"ID: {t.TransactionId}")
                            ).ToArray();
                            await Task.WhenAll(notificationTasks);
                            System.Diagnostics.Debug.WriteLine($"✅ All {savedTransactions.Count} notifications added");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Error adding notifications: {ex.Message}");
                        }
                    });
                }
                
                        // Refresh today's section (recent orders + today's totals) in Sales Report
                        try
                        {
                            var salesApp = (App)Application.Current;
                            if (salesApp?.SalesReportVM != null)
                            {
                                await salesApp.SalesReportVM.RefreshTodayAsync();
                                System.Diagnostics.Debug.WriteLine("✅ Sales report 'Today' refreshed (orders + totals)");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Could not refresh sales report today: {ex.Message}");
                        }

                // Update Employee Dashboard recent orders + totals immediately
                var appForDashboard = (App)Application.Current;
                var settingsPopup = appForDashboard?.SettingsPopup;
                if (settingsPopup != null && savedTransactions != null)
                {
                    try
                    {
                        // Add each saved transaction to dashboard's recent orders on UI thread
                        foreach (var t in savedTransactions)
                        {
                            var tx = t; // avoid modified closure
                            // Try to resolve product image by name; fall back to default icon
                            string productImage = "drink.png";
                            try
                            {
                                var dbImg = new Models.Database();
                                var prod = await dbImg.GetProductByNameAsync(tx.DrinkName);
                                if (prod != null && !string.IsNullOrWhiteSpace(prod.ImageSet))
                                {
                                    productImage = prod.ImageSet;
                                }
                            }
                            catch { }

                            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                            {
                                settingsPopup.AddRecentOrder(
                                    tx.TransactionId,
                                    tx.DrinkName,
                                    productImage,
                                    tx.Total
                                );
                            });
                        }

                        // Reload dashboard aggregates from DB to ensure consistency
                        await settingsPopup.LoadTodaysMetricsAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Could not refresh employee dashboard: {ex.Message}");
                    }
                }

                // Quick check for pending inventory/processing notifications
                try
                {
                    System.Diagnostics.Debug.WriteLine("🎯 Attempting to refresh top items in Sales Report from payment popup...");
                    var currentApp = (App)Application.Current;
                    if (currentApp?.SalesReportVM != null)
                    {
                        System.Diagnostics.Debug.WriteLine("🎯 SalesReportVM found. Triggering data refresh...");
                        await Task.Delay(150);
                        await currentApp.SalesReportVM.RefreshRecentOrdersAsync();
                        await Task.Delay(150);
                        await currentApp.SalesReportVM.RefreshTodayAsync();
                        await Task.Delay(150);
                        await currentApp.SalesReportVM.LoadDataAsync();
                        System.Diagnostics.Debug.WriteLine("🎯 Sales report data refresh completed after payment");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ SalesReportVM not available during payment refresh");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Error refreshing sales report from payment popup: {ex.Message}");
                }

                try
                {
                    CartItems.Clear();
                    QrCodeImageSource = null;
                }
                catch { }
                
                OnPropertyChanged(nameof(CartItems));
                OnPropertyChanged(nameof(QrCodeImageSource));

                paymentCompleted = true;
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("🛑 Payment processing was cancelled");
                PaymentStatus = "Payment Cancelled";
                // Don't show error popup for cancellation - it's intentional
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Payment confirmation error: {ex.Message}");
                await HandlePaymentFailureAsync("Payment Error", ex.Message);
            }
            finally
            {
                if (paymentCompleted)
                {
                    _ = AutoClosePaymentPopupAsync();
                }

                // Clean up cancellation token
                _paymentCancellationTokenSource?.Dispose();
                _paymentCancellationTokenSource = null;

                // Reset paused flag if payment completed
                if (paymentCompleted)
                {
                    IsPaymentPaused = false;
                }

                // Thread-safe flag reset and semaphore release
                Interlocked.Exchange(ref _isProcessingPayment, 0);
                OnPropertyChanged(nameof(IsProcessingPayment));
                _paymentSemaphore.Release();
                _gcashDeepLinkConfirmed = false;
                _currentGCashCheckoutUrl = null;
                _currentGCashSourceId = null;
                QrCodeImageSource = null;
            }
        }
        private async Task ProcessPaymentWithSteps(CancellationToken cancellationToken = default) // Simulate payment processing steps
        {
            try
            {
                if (SelectedPaymentMethod == "Cash")
                {
                    // Cash payment - simple processing
                    PaymentStatus = "Processing Cash Payment...";
                    await Task.Delay(800, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    PaymentStatus = "Payment Successful";
                }
                else if (SelectedPaymentMethod == "GCash")
                {
                    // GCash payment simulation
                    PaymentStatus = "Initializing GCash Payment...";
                    await Task.Delay(1000, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    PaymentStatus = "Generating QR Code...";
                    await Task.Delay(1200, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    PaymentStatus = "Waiting for Customer Scan...";
                    await Task.Delay(1500, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    PaymentStatus = "Verifying Payment...";
                    await Task.Delay(1000, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    PaymentStatus = "Payment Confirmed";
                    await Task.Delay(500, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                }
                else if (SelectedPaymentMethod == "Bank")
                {
                    // Bank transfer simulation
                    PaymentStatus = "Preparing Bank Transfer...";
                    await Task.Delay(1000, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    PaymentStatus = "Validating Account Details...";
                    await Task.Delay(1200, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    PaymentStatus = "Processing Transfer...";
                    await Task.Delay(1500, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    PaymentStatus = "Verifying Transaction...";
                    await Task.Delay(1000, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    PaymentStatus = "Transfer Completed";
                    await Task.Delay(500, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException)
            {
                PaymentStatus = "Payment Cancelled";
                System.Diagnostics.Debug.WriteLine("🛑 Payment processing cancelled");
                throw; // Re-throw to be handled by caller
            }
            catch (Exception ex)
            {
                PaymentStatus = "Payment Failed";
                System.Diagnostics.Debug.WriteLine($"Payment processing error: {ex.Message}");
                await Task.Delay(1000, cancellationToken);
            }
        }
        private async Task<List<TransactionHistoryModel>> SaveTransaction() // Save transaction to database and in-memory store
        {
            try
            {
                // Block immediately if no internet
                if (!Services.NetworkService.HasInternetConnection())
                {
                    try { await Application.Current.MainPage.DisplayAlert("No Internet", "No internet connection. Please check your network and try again.", "OK"); } catch { }
                    return new List<TransactionHistoryModel>();
                }
                System.Diagnostics.Debug.WriteLine($"🔄 Starting transaction save for {CartItems.Count} items");
                
                var app = (App)Application.Current;
                var transactions = app?.Transactions;
                var database = new Models.Database(); // Will use auto-detected host

                if (transactions != null)
                {
                    // Get the next available transaction ID from the database to ensure persistence
                    var nextId = await database.GetNextTransactionIdAsync();
                    var saved = new List<TransactionHistoryModel>();

                    // Create a single transaction for the entire order
                    var orderTotal = CartItems.Sum(item => item.TotalPrice);
                    var transactionId = nextId;
                    
                    // Create a combined transaction record for the entire order
                    // Calculate size-specific prices for the transaction
                    var smallTotal = CartItems.Sum(item => (item.SmallPrice ?? 0) * item.SmallQuantity);
                    var mediumTotal = CartItems.Sum(item => item.MediumPrice * item.MediumQuantity);
                    var largeTotal = CartItems.Sum(item => item.LargePrice * item.LargeQuantity);
                    var addonTotal = CartItems.Sum(item => Math.Max(0, item.TotalPrice - (((item.SmallPrice ?? 0) * item.SmallQuantity) + (item.MediumPrice * item.MediumQuantity) + (item.LargePrice * item.LargeQuantity))));

                    var totalUnits = CartItems.Sum(item => item.TotalQuantity > 0 ? item.TotalQuantity : Math.Max(1, item.Quantity));

                    var orderTransaction = new TransactionHistoryModel
                    {
                        TransactionId = transactionId,
                        DrinkName = CartItems.Count == 1 ? CartItems[0].ProductName : $"{CartItems.Count} items",
                        Size = CartItems.Count == 1 ? CartItems[0].SelectedSize : "Multiple",
                        Quantity = totalUnits,
                        Price = orderTotal,
                        SmallPrice = (decimal)smallTotal,
                        MediumPrice = (decimal)mediumTotal,
                        LargePrice = (decimal)largeTotal,
                        AddonPrice = (decimal)addonTotal,
                        Vat = 0m,
                        Total = orderTotal,
                        AddOns = string.Join(", ", CartItems.Where(item => !string.IsNullOrWhiteSpace(item.AddOnsDisplay) && item.AddOnsDisplay != "No add-ons").Select(item => $"{item.ProductName}: {item.AddOnsDisplay}")),
                        CustomerName = CartItems.FirstOrDefault()?.CustomerName ?? "",
                        PaymentMethod = SelectedPaymentMethod,
                        Status = "Completed",
                        TransactionDate = DateTime.Now
                    };

                    // Save the combined transaction to database
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await database.SaveTransactionAsync(orderTransaction, CartItems);
                    System.Diagnostics.Debug.WriteLine($"✅ Database save successful for order: {transactionId}");
                    
                    // Deduct inventory for each item in parallel for better performance
                    System.Diagnostics.Debug.WriteLine($"🔧 Starting inventory deduction for {CartItems.Count} cart items");
                    var inventoryDeductionTasks = CartItems.Select(async item =>
                    {
                        System.Diagnostics.Debug.WriteLine($"💾 ========== Queuing inventory deduction for: {item.ProductName} ==========");
                        System.Diagnostics.Debug.WriteLine($"💾 Item details - Small: {item.SmallQuantity}, Medium: {item.MediumQuantity}, Large: {item.LargeQuantity}");
                        try
                        {
                            await DeductInventoryForItemAsync(database, item);
                            System.Diagnostics.Debug.WriteLine($"✅ Inventory deduction successful for: {item.ProductName}");
                            return (success: true, item: item);
                        }
                        catch (Exception itemEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Failed to deduct inventory for {item.ProductName}: {itemEx.Message}");
                            return (success: false, item: item);
                        }
                    }).ToArray();
                    
                    var inventoryResults = await Task.WhenAll(inventoryDeductionTasks);
                    var successCount = inventoryResults.Count(r => r.success);
                    System.Diagnostics.Debug.WriteLine($"🔧 Completed inventory deduction for {successCount}/{CartItems.Count} cart items");
                    
                    // Add to in-memory collection for history popup
                    transactions.Add(orderTransaction);
                    saved.Add(orderTransaction);

                    System.Diagnostics.Debug.WriteLine($"✅ Order transaction {transactionId} saved successfully with {CartItems.Count} items");
                    
                    // Refresh sales report data
                    try 
                    {
                        var currentApp = (App)Application.Current;
                        if (currentApp?.SalesReportVM != null)
                        {
                            await currentApp.SalesReportVM.LoadDataAsync();
                            System.Diagnostics.Debug.WriteLine("✅ Sales report data refreshed after transaction");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error refreshing sales report: {ex.Message}");
                    }
                    
                    // Refresh inventory data
                    try 
                    {
                        var currentApp = (App)Application.Current;
                        if (currentApp?.InventoryVM != null)
                        {
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                await currentApp.InventoryVM.LoadDataAsync();
                                System.Diagnostics.Debug.WriteLine("✅ Inventory data refreshed after transaction");
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error refreshing inventory: {ex.Message}");
                    }
                    
                    return saved;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ App.Transactions is null");
                    await Application.Current.MainPage.DisplayAlert("Error", "Transaction storage not available", "OK");
                }
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("⏰ Transaction save timed out");
                await Application.Current.MainPage.DisplayAlert("Error", "Transaction save timed out. Please try again.", "OK");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ PaymentPopupViewModel error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                
                string errorMessage = "Failed to save transaction";
                if (ex.Message.Contains("foreign key constraint"))
                {
                    errorMessage = "Database error: User session invalid. Please log in again.";
                }
                else if (ex.Message.Contains("Connection"))
                {
                    errorMessage = "Database connection error. Please check your network.";
                }
                else if (ex.Message.Contains("timeout"))
                {
                    errorMessage = "Database operation timed out. Please check your connection.";
                }
                else
                {
                    errorMessage = $"Failed to save transaction: {ex.Message}";
                }
                
                await Application.Current.MainPage.DisplayAlert("Error", errorMessage, "OK");
            }
            return null;
        }

        private async Task DeductInventoryForItemAsync(Models.Database database, CartItem cartItem) // Deduct inventory based on cart item
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔧 DeductInventoryForItemAsync: Starting for {cartItem.ProductName}");
                System.Diagnostics.Debug.WriteLine($"🔧 Cart item quantities - Small: {cartItem.SmallQuantity}, Medium: {cartItem.MediumQuantity}, Large: {cartItem.LargeQuantity}");
                
                // Get product by name to get the product ID
                var product = await database.GetProductByNameAsyncCached(cartItem.ProductName);
                if (product == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Product not found: {cartItem.ProductName}");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"🔧 Found product: {product.ProductName} (ID: {product.ProductID})");

                // Validate and fix product-ingredient connections to ensure consistent deduction
                await database.ValidateAndFixProductIngredientsAsync(product.ProductID);

                // Get all ingredients for this product
                var ingredients = await database.GetProductIngredientsAsync(product.ProductID);
                if (!ingredients.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"❌ No ingredients found for product: {cartItem.ProductName}");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"🔧 Found {ingredients.Count()} ingredients for product: {cartItem.ProductName}");

                // Prepare deductions dictionary to accumulate amounts for the same ingredient
                // Store: convertedAmount, originalUnit, originalAmount
                var deductionsDict = new Dictionary<string, (double convertedAmount, string originalUnit, double originalAmount)>();

                // Helper local function to add deductions for a specific size using per-size amounts
                void AddSizeDeductions(string size, int qty)
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 AddSizeDeductions called: Size={size}, Quantity={qty}");
                    if (qty <= 0) 
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Skipping {size} deductions - quantity is 0 or negative");
                        return;
                    }
                    foreach (var (ingredient, amount, unit, role) in ingredients)
                    {
                        // Only process items with role='ingredient', skip items with role='addon'
                        // Items with role='addon' should only be processed in the addon section below
                        if (role?.Equals("addon", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            System.Diagnostics.Debug.WriteLine($"🔧 Skipping {ingredient.itemName} in AddSizeDeductions - has role='addon', will be processed as addon");
                            continue;
                        }
                        
                        // Choose per-size amount/unit when available; fall back to shared
                        // Use per-size amounts if they exist and are greater than 0, otherwise use shared amount
                        double perServing = size switch
                        {
                            "Small" => (ingredient.InputAmountSmall > 0) ? ingredient.InputAmountSmall : amount,
                            "Medium" => (ingredient.InputAmountMedium > 0) ? ingredient.InputAmountMedium : amount,
                            "Large" => (ingredient.InputAmountLarge > 0) ? ingredient.InputAmountLarge : amount,
                            _ => amount
                        };
                        
                        // Use per-size units if they exist and are not empty, otherwise use shared unit
                        string perUnit = size switch
                        {
                            "Small" => (!string.IsNullOrWhiteSpace(ingredient.InputUnitSmall)) ? ingredient.InputUnitSmall : unit,
                            "Medium" => (!string.IsNullOrWhiteSpace(ingredient.InputUnitMedium)) ? ingredient.InputUnitMedium : unit,
                            "Large" => (!string.IsNullOrWhiteSpace(ingredient.InputUnitLarge)) ? ingredient.InputUnitLarge : unit,
                            _ => unit
                        };
                        
                        // Final fallback to inventory unit if perUnit is still empty
                        if (string.IsNullOrWhiteSpace(perUnit))
                        {
                            perUnit = ingredient.unitOfMeasurement;
                        }

                        var targetUnit = UnitConversionService.Normalize(ingredient.unitOfMeasurement);
                        var converted = ConvertUnits(perServing, perUnit, targetUnit);
                        var total = Math.Round(converted, 6) * qty; // keep small ml/g deductions
                        
                        System.Diagnostics.Debug.WriteLine($"🔧 {ingredient.itemName} ({size}): {perServing} {perUnit} -> {converted} {targetUnit} * {qty} = {total}");
                        System.Diagnostics.Debug.WriteLine($"🔧   Per-size amounts: Small={ingredient.InputAmountSmall}, Medium={ingredient.InputAmountMedium}, Large={ingredient.InputAmountLarge}");
                        System.Diagnostics.Debug.WriteLine($"🔧   Per-size units: Small='{ingredient.InputUnitSmall}', Medium='{ingredient.InputUnitMedium}', Large='{ingredient.InputUnitLarge}'");
                        System.Diagnostics.Debug.WriteLine($"🔧   Shared amount={amount}, Shared unit='{unit}', Inventory unit='{ingredient.unitOfMeasurement}'");
                        
                        // Calculate original total amount before conversion (for logging)
                        var originalTotal = perServing * qty;
                        
                        // Create a unique key that includes size to track per-size deductions
                        var key = $"{ingredient.itemName}|{size}";
                        
                        // Store size-specific deductions
                        if (deductionsDict.ContainsKey(key))
                        {
                            var existing = deductionsDict[key];
                            deductionsDict[key] = (
                                existing.convertedAmount + total,
                                existing.originalUnit, // Keep first unit found
                                existing.originalAmount + originalTotal // Accumulate original amounts
                            );
                        }
                        else
                        {
                            deductionsDict[key] = (total, perUnit, originalTotal);
                        }
                    }
                }

                // Add per-size deductions based on cart quantities
                AddSizeDeductions("Small", cartItem.SmallQuantity);
                AddSizeDeductions("Medium", cartItem.MediumQuantity);
                AddSizeDeductions("Large", cartItem.LargeQuantity);

                // Include selected addons based on configured per-serving amounts
                // Fetch addon link definitions (amount/unit per serving)
                var addonLinks = await database.GetProductAddonsAsync(product.ProductID);
                System.Diagnostics.Debug.WriteLine($"🔧 ADDON DEDUCTION: Found {addonLinks?.Count ?? 0} addon links for product {product.ProductName}");
                
                // Helper function to process addons for a specific size
                void ProcessAddonsForSize(string size, ObservableCollection<InventoryPageModel> sizeAddons, int sizeQuantity)
                {
                    if (sizeQuantity <= 0 || sizeAddons == null || sizeAddons.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"🔧 ADDON DEDUCTION: Skipping {size} size - quantity is {sizeQuantity} or no addons");
                        return;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"🔧 ADDON DEDUCTION: Processing {size} addons for {sizeQuantity} drink(s)");
                    
                    foreach (var cartAddon in sizeAddons)
                    {
                        if (cartAddon == null || !cartAddon.IsSelected) continue;
                        
                        System.Diagnostics.Debug.WriteLine($"🔧 ADDON DEDUCTION: Processing {size} addon: {cartAddon.itemName} (ID: {cartAddon.itemID})");
                        
                        // Find the addon link to get per-serving amount/unit
                        var linkedAddon = addonLinks?.FirstOrDefault(al => al.itemID == cartAddon.itemID);
                        if (linkedAddon == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"   ⚠️ Addon link not found for {cartAddon.itemName}");
                            continue;
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"   ✅ Found addon link: IsSelected={cartAddon.IsSelected}, AddonQuantity={cartAddon.AddonQuantity}");
                        
                        // Treat checked addons with no explicit quantity as 1
                        if (cartAddon.IsSelected && cartAddon.AddonQuantity <= 0)
                        {
                            System.Diagnostics.Debug.WriteLine("   ℹ️ Addon selected with quantity 0 — defaulting to 1");
                            cartAddon.AddonQuantity = 1;
                        }
                        if (!cartAddon.IsSelected || cartAddon.AddonQuantity <= 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"   ⚠️ Addon skipped: not selected or quantity <= 0");
                            continue;
                        }

                        // Use the amount/unit from product_addons table
                        // For Medium and Large, use the configured amount/unit (stored in InputAmountMedium/InputUnitMedium typically)
                        var perServingAmount = size switch
                        {
                            "Small" => (linkedAddon.InputAmountSmall > 0) ? linkedAddon.InputAmountSmall : (linkedAddon.InputAmount > 0 ? linkedAddon.InputAmount : 0),
                            "Medium" => (linkedAddon.InputAmountMedium > 0) ? linkedAddon.InputAmountMedium : (linkedAddon.InputAmount > 0 ? linkedAddon.InputAmount : 0),
                            "Large" => (linkedAddon.InputAmountLarge > 0) ? linkedAddon.InputAmountLarge : (linkedAddon.InputAmount > 0 ? linkedAddon.InputAmount : 0),
                            _ => (linkedAddon.InputAmount > 0 ? linkedAddon.InputAmount : 0)
                        };
                        
                        var perServingUnit = size switch
                        {
                            "Small" => (!string.IsNullOrWhiteSpace(linkedAddon.InputUnitSmall)) ? linkedAddon.InputUnitSmall : (linkedAddon.InputUnit ?? cartAddon.unitOfMeasurement),
                            "Medium" => (!string.IsNullOrWhiteSpace(linkedAddon.InputUnitMedium)) ? linkedAddon.InputUnitMedium : (linkedAddon.InputUnit ?? cartAddon.unitOfMeasurement),
                            "Large" => (!string.IsNullOrWhiteSpace(linkedAddon.InputUnitLarge)) ? linkedAddon.InputUnitLarge : (linkedAddon.InputUnit ?? cartAddon.unitOfMeasurement),
                            _ => (linkedAddon.InputUnit ?? cartAddon.unitOfMeasurement)
                        };

                        System.Diagnostics.Debug.WriteLine($"   📊 Per-serving ({size}): {perServingAmount} {perServingUnit}");

                        if (perServingAmount <= 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"   ⚠️ Per-serving amount is 0, skipping addon");
                            continue; // nothing to deduct for this addon
                        }

                        // Convert to inventory base unit for deduction
                        var inventoryBaseUnit = UnitConversionService.Normalize(cartAddon.unitOfMeasurement);
                        var convertedAmount = ConvertUnits(perServingAmount, perServingUnit, inventoryBaseUnit);

                        System.Diagnostics.Debug.WriteLine($"   🔄 Converting: {perServingAmount} {perServingUnit} → {convertedAmount} {inventoryBaseUnit}");

                        // Deduct addon ONLY for this specific size
                        // AddonQuantity is per drink, so multiply by size quantity only
                        System.Diagnostics.Debug.WriteLine($"   📦 {size} drinks: {sizeQuantity}, Addon quantity per drink: {cartAddon.AddonQuantity}");
                        
                        var sizeAddonDeduction = convertedAmount * cartAddon.AddonQuantity * (double)sizeQuantity;
                        var originalAddonAmount = perServingAmount * cartAddon.AddonQuantity * (double)sizeQuantity;
                        System.Diagnostics.Debug.WriteLine($"   ➖ DEDUCTING for {size}: {sizeAddonDeduction} {inventoryBaseUnit} ({convertedAmount} x {cartAddon.AddonQuantity} x {sizeQuantity})");
                        
                        // Create a unique key that includes size to track per-size deductions
                        var addonKey = $"{cartAddon.itemName}|{size}";
                        
                        // Accumulate addon amounts for the same ingredient and size
                        if (deductionsDict.ContainsKey(addonKey))
                        {
                            var existing = deductionsDict[addonKey];
                            deductionsDict[addonKey] = (
                                existing.convertedAmount + sizeAddonDeduction,
                                existing.originalUnit, // Keep first unit found
                                existing.originalAmount + originalAddonAmount
                            );
                            System.Diagnostics.Debug.WriteLine($"   📝 Accumulated total for {cartAddon.itemName} ({size}): {deductionsDict[addonKey].convertedAmount}");
                        }
                        else
                        {
                            deductionsDict[addonKey] = (sizeAddonDeduction, perServingUnit, originalAddonAmount);
                            System.Diagnostics.Debug.WriteLine($"   📝 First deduction for {cartAddon.itemName} ({size}): {sizeAddonDeduction}");
                        }
                    }
                }
                
                // Process Small addons separately
                if (cartItem.SmallAddons != null && cartItem.SmallQuantity > 0)
                {
                    ProcessAddonsForSize("Small", cartItem.SmallAddons, cartItem.SmallQuantity);
                }
                
                // Process Medium addons separately
                if (cartItem.MediumAddons != null && cartItem.MediumQuantity > 0)
                {
                    ProcessAddonsForSize("Medium", cartItem.MediumAddons, cartItem.MediumQuantity);
                }
                
                // Process Large addons separately
                if (cartItem.LargeAddons != null && cartItem.LargeQuantity > 0)
                {
                    ProcessAddonsForSize("Large", cartItem.LargeAddons, cartItem.LargeQuantity);
                }
                
                // Legacy: Process old InventoryItems if any (for backward compatibility - applies to all sizes)
                if (addonLinks != null && addonLinks.Count > 0 && cartItem.InventoryItems != null && cartItem.InventoryItems.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 ADDON DEDUCTION (Legacy): Processing old InventoryItems");
                    
                    foreach (var linkedAddon in addonLinks)
                    {
                        var cartAddon = cartItem.InventoryItems.FirstOrDefault(ai => ai.itemID == linkedAddon.itemID);
                        if (cartAddon == null || !cartAddon.IsSelected || cartAddon.AddonQuantity <= 0) continue;
                        
                        var perServingAmount = linkedAddon.InputAmount > 0 ? linkedAddon.InputAmount : 0;
                        var perServingUnit = linkedAddon.InputUnit ?? cartAddon.unitOfMeasurement;
                        
                        if (perServingAmount <= 0) continue;
                        
                        var inventoryBaseUnit = UnitConversionService.Normalize(cartAddon.unitOfMeasurement);
                        var convertedAmount = ConvertUnits(perServingAmount, perServingUnit, inventoryBaseUnit);
                        
                        // Legacy: multiply by total drinks (old behavior)
                        var totalDrinks = cartItem.SmallQuantity + cartItem.MediumQuantity + cartItem.LargeQuantity;
                        if (totalDrinks > 0)
                        {
                            var totalAddonDeduction = convertedAmount * cartAddon.AddonQuantity * (double)totalDrinks;
                            var originalAddonAmount = perServingAmount * cartAddon.AddonQuantity * (double)totalDrinks;
                            // Legacy: Use item name only (no size separation for legacy addons)
                            var legacyKey = cartAddon.itemName;
                            if (deductionsDict.ContainsKey(legacyKey))
                            {
                                var existing = deductionsDict[legacyKey];
                                deductionsDict[legacyKey] = (
                                    existing.convertedAmount + totalAddonDeduction,
                                    existing.originalUnit,
                                    existing.originalAmount + originalAddonAmount
                                );
                            }
                            else
                            {
                                deductionsDict[legacyKey] = (totalAddonDeduction, perServingUnit, originalAddonAmount);
                            }
                        }
                    }
                }

                // Add automatic cups and straws per size (1 per serving)
                // These are already size-specific, so they'll be tracked with size
                if (cartItem.SmallQuantity > 0)
                    await AddAutomaticCupAndStrawForSize(deductionsDict, "Small", cartItem.SmallQuantity);
                if (cartItem.MediumQuantity > 0)
                    await AddAutomaticCupAndStrawForSize(deductionsDict, "Medium", cartItem.MediumQuantity);
                if (cartItem.LargeQuantity > 0)
                    await AddAutomaticCupAndStrawForSize(deductionsDict, "Large", cartItem.LargeQuantity);

                // Convert dictionary back to list for database call with original amounts/units and size
                // Extract size from key (format: "itemName|size") and item name
                var deductions = deductionsDict.Select(kvp => 
                {
                    var parts = kvp.Key.Split('|');
                    var itemName = parts[0];
                    var size = parts.Length > 1 ? parts[1] : null;
                    return (itemName, kvp.Value.convertedAmount, kvp.Value.originalUnit, kvp.Value.originalAmount, size ?? "");
                }).ToList();

                System.Diagnostics.Debug.WriteLine($"🔧 Final deductions for {cartItem.ProductName}:");
                foreach (var deduction in deductions)
                {
                    System.Diagnostics.Debug.WriteLine($"   - {deduction.Item1}: {deduction.Item2} (original: {deduction.Item4} {deduction.Item3})");
                }

                // Deduct inventory
                System.Diagnostics.Debug.WriteLine($"🔧 Checking deductions.Any(): {deductions.Any()}");
                System.Diagnostics.Debug.WriteLine($"🔧 Deductions count: {deductions.Count}");
                
                if (deductions.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 About to call DeductInventoryAsync with {deductions.Count} deductions for product: {cartItem.ProductName}");
                    try
                    {
                        var affectedRows = await database.DeductInventoryAsync(deductions, cartItem.ProductName);
                        System.Diagnostics.Debug.WriteLine($"✅ DeductInventoryAsync completed. Affected rows: {affectedRows} for {cartItem.ProductName}");
                    }
                    catch (Exception deductEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ DeductInventoryAsync threw exception: {deductEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"❌ Deduct exception stack: {deductEx.StackTrace}");
                        throw;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ No deductions to process for {cartItem.ProductName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR deducting inventory for {cartItem.ProductName}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
                }
                // Don't throw - we don't want to fail the transaction if inventory deduction fails
                // But we should log this prominently
            }
        }

        private async Task AddAutomaticCupAndStrawForSize(Dictionary<string, (double convertedAmount, string originalUnit, double originalAmount)> deductionsDict, string size, int quantity) // Add cup and straw deductions based on size
        {
            try
            {
                var database = new Models.Database();
                
                // Add appropriate cup based on size
                var normalizedSize = (size ?? string.Empty).Trim().ToLowerInvariant();
                string cupName = normalizedSize switch
                {
                    "small" => "Small Cup",
                    "medium" => "Medium Cup", 
                    "large" => "Large Cup",
                    _ => "Medium Cup" // Default to medium
                };

                var cupItem = await database.GetInventoryItemByNameCachedAsync(cupName);
                if (cupItem != null)
                {
                    // Cups are in "pcs" - no conversion needed, original and converted are the same
                    var originalUnit = "pcs";
                    var cupKey = $"{cupName}|{size}";
                    if (deductionsDict.ContainsKey(cupKey))
                    {
                        var existing = deductionsDict[cupKey];
                        deductionsDict[cupKey] = (
                            existing.convertedAmount + quantity,
                            existing.originalUnit,
                            existing.originalAmount + quantity
                        );
                    }
                    else
                    {
                        deductionsDict[cupKey] = (quantity, originalUnit, quantity);
                    }
                }

                // Add straw (1 per item)
                var strawItem = await database.GetInventoryItemByNameCachedAsync("Straw");
                if (strawItem != null)
                {
                    // Straws are in "pcs" - no conversion needed, original and converted are the same
                    var originalUnit = "pcs";
                    var strawKey = $"Straw|{size}";
                    if (deductionsDict.ContainsKey(strawKey))
                    {
                        var existing = deductionsDict[strawKey];
                        deductionsDict[strawKey] = (
                            existing.convertedAmount + quantity,
                            existing.originalUnit,
                            existing.originalAmount + quantity
                        );
                    }
                    else
                    {
                        deductionsDict[strawKey] = (quantity, originalUnit, quantity);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding automatic cup and straw: {ex.Message}");
            }
        }

        private static double ConvertUnits(double amount, string fromUnit, string toUnit) // Convert amount from one unit to another
        {
            if (string.IsNullOrWhiteSpace(toUnit)) return amount;
            
            // Use UnitConversionService for consistent unit conversion
            return UnitConversionService.Convert(amount, fromUnit, toUnit);
        }

        private static string NormalizeUnit(string unit) // Normalize unit strings to standard short forms
        {
            if (string.IsNullOrWhiteSpace(unit)) return string.Empty;

            // Use UnitConversionService for consistent normalization
            return UnitConversionService.Normalize(unit);
        }

        private static bool IsMassUnit(string unit) // Check if unit is a mass unit
        {
            var normalized = NormalizeUnit(unit);
            return normalized == "kg" || normalized == "g";
        }

        private static bool IsVolumeUnit(string unit) // Check if unit is a volume unit
        {
            var normalized = NormalizeUnit(unit);
            return normalized == "L" || normalized == "ml";
        }

        private static bool IsCountUnit(string unit) // Check if unit is a count/pieces unit
        {
            var normalized = NormalizeUnit(unit);
            return normalized == "pcs" || normalized == "pc";
        }

        private async Task<InventoryValidationResult> ValidateInventoryAvailabilityAsync() // Validate if inventory is sufficient for cart items
        {
            try
            {
                var database = new Models.Database();
                var issues = new List<string>();

                foreach (var item in CartItems)
                {
                    // Get product by name to get the product ID
                var product = await database.GetProductByNameAsyncCached(item.ProductName);
                    if (product == null)
                    {
                        issues.Add($"Product not found: {item.ProductName}");
                        continue;
                    }

                    // Get all ingredients for this product
                    var ingredients = await database.GetProductIngredientsAsync(product.ProductID);
                    
                    // Calculate total quantity across all sizes
                    int totalQuantity = (item.SmallQuantity > 0 ? item.SmallQuantity : 0)
                        + (item.MediumQuantity > 0 ? item.MediumQuantity : 0)
                        + (item.LargeQuantity > 0 ? item.LargeQuantity : 0);
                    if (totalQuantity <= 0)
                    {
                        issues.Add($"Invalid quantity for {item.ProductName}: {totalQuantity}");
                        continue;
                    }

                    // If product has no ingredients, skip ingredient validation (product is always available)
                    if (ingredients == null || !ingredients.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Product {item.ProductName}: No ingredients, skipping ingredient validation");
                        // Still check cups/straws below
                    }
                    else
                    {
                        // Check each ingredient
                        foreach (var (ingredient, amount, unit, role) in ingredients)
                    {
                        // Convert units if needed
                        var convertedAmount = ConvertUnits(amount, unit, ingredient.unitOfMeasurement);
                        
                        // Calculate required amount
                        var requiredAmount = convertedAmount * totalQuantity;
                        
                        // Check if we have enough inventory
                        if (ingredient.itemQuantity < requiredAmount)
                        {
                            var shortage = requiredAmount - ingredient.itemQuantity;
                            issues.Add($"Insufficient {ingredient.itemName}: Need {requiredAmount:F2} {ingredient.unitOfMeasurement}, have {ingredient.itemQuantity:F2} (short by {shortage:F2})");
                        }
                    }

                    // Check automatic cup and straw per size
                    if (item.SmallQuantity > 0)
                        await ValidateAutomaticCupAndStrawForSize(database, issues, "Small", item.SmallQuantity);
                    if (item.MediumQuantity > 0)
                        await ValidateAutomaticCupAndStrawForSize(database, issues, "Medium", item.MediumQuantity);
                    if (item.LargeQuantity > 0)
                        await ValidateAutomaticCupAndStrawForSize(database, issues, "Large", item.LargeQuantity);
                }
                }

                return new InventoryValidationResult
                {
                    IsValid = !issues.Any(),
                    ErrorMessage = issues.Any() ? string.Join("\n", issues) : ""
                };
            }
            catch (Exception ex)
            {
                return new InventoryValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Error validating inventory: {ex.Message}"
                };
            }
        }

        private async Task ValidateAutomaticCupAndStrawForSize(Models.Database database, List<string> issues, string size, int quantity) // Validate cup and straw availability based on size
        {
            try
            {
                // Check appropriate cup based on size
                var normalizedSize2 = (size ?? string.Empty).Trim().ToLowerInvariant();
                string cupName = normalizedSize2 switch
                {
                    "small" => "Small Cup",
                    "medium" => "Medium Cup", 
                    "large" => "Large Cup",
                    _ => "Medium Cup" // Default to medium
                };

                var cupItem = await database.GetInventoryItemByNameCachedAsync(cupName);
                if (cupItem != null && cupItem.itemQuantity < quantity)
                {
                    var shortage = quantity - cupItem.itemQuantity;
                    issues.Add($"Insufficient {cupName}: Need {quantity}, have {cupItem.itemQuantity} (short by {shortage})");
                }

                // Check straw (1 per item)
                var strawItem = await database.GetInventoryItemByNameCachedAsync("Straw");
                if (strawItem != null && strawItem.itemQuantity < quantity)
                {
                    var shortage = quantity - strawItem.itemQuantity;
                    issues.Add($"Insufficient Straw: Need {quantity}, have {strawItem.itemQuantity} (short by {shortage})");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Error validating cup and straw: {ex.Message}");
            }
        }

        public void ConfirmGCashFromDeepLink()
        {
            _gcashDeepLinkConfirmed = true;

            if (!IsProcessingPayment && ConfirmPaymentCommand.CanExecute(null))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await ConfirmPaymentCommand.ExecuteAsync(null);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Error executing ConfirmPayment from deep link: {ex.Message}");
                    }
                });
            }
        }

        private async Task<(bool Success, string? ErrorMessage)> EnsureGCashCheckoutAsync(bool forceNew = false)
        {
            try
            {
                if (!forceNew && !string.IsNullOrWhiteSpace(_currentGCashCheckoutUrl))
                {
                    if (QrCodeImageSource == null)
                    {
                        QrCodeImageSource = QRCodeService.GenerateQRCode(_currentGCashCheckoutUrl, 250);
                    }
                    return (true, null);
                }

                var result = await _payMongoService.CreateGCashSourceAsync(
                    TotalAmount,
                    $"Coftea POS Order {DateTime.Now:yyyyMMddHHmmss}");

                if (!result.Success || string.IsNullOrWhiteSpace(result.CheckoutUrl))
                {
                    _currentGCashCheckoutUrl = null;
                    _currentGCashSourceId = null;
                    QrCodeImageSource = null;
                    return (false, result.ErrorMessage ?? "Unable to create GCash payment link.");
                }

                _currentGCashSourceId = result.SourceId;
                _currentGCashCheckoutUrl = result.CheckoutUrl;
                QrCodeImageSource = QRCodeService.GenerateQRCode(_currentGCashCheckoutUrl, 250);
                _gcashDeepLinkConfirmed = false;
                return (true, null);
            }
            catch (Exception ex)
            {
                _currentGCashCheckoutUrl = null;
                _currentGCashSourceId = null;
                QrCodeImageSource = null;
                return (false, ex.Message);
            }
        }
    }

    public class InventoryValidationResult // Result of inventory validation
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = "";
    }

    public partial class PaymentPopupViewModel
    {
        private async Task AutoClosePaymentPopupAsync()
        {
            try
            {
                await Task.Delay(1000);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (IsPaymentVisible)
                    {
                        ClosePayment();
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Auto-close failed: {ex.Message}");
            }
        }

        private async Task HandlePaymentFailureAsync(string title, string message, bool showAlert = true)
        {
            var displayMessage = string.IsNullOrWhiteSpace(message) ? "Payment failed. Please try again." : message;
            PaymentStatus = displayMessage;

            if (showAlert)
            {
                try
                {
                    await Application.Current.MainPage.DisplayAlert(title, displayMessage, "OK");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Unable to show failure alert: {ex.Message}");
                }
            }
        }
    }
}
