# 📱 Coftea POS - Offline-First System Guide

## Overview

The Coftea POS system now supports **full offline operation**! You can continue using the system even without an internet connection, and all data will automatically sync to the online MySQL database when connectivity is restored.

## ✨ Key Features

### 1. **Automatic Mode Switching**
- 🌐 **Online Mode**: Uses MySQL database when internet is available
- 📱 **Offline Mode**: Automatically switches to local SQLite database when internet is lost
- 🔄 **Auto-Sync**: Automatically syncs pending operations when internet is restored

### 2. **What Works Offline**

✅ **Fully Functional Offline (No Alerts or Popups):**
- View all products (POS menu)
- Make sales and process transactions
- View inventory items
- Update inventory quantities
- View product-ingredient connections
- View addons
- Process payments
- View sales reports
- Manage inventory

⏸️ **Queued for Sync (Saved Locally):**
- New transactions/sales
- Inventory quantity changes
- Inventory activity logs
- Product updates

❌ **Requires Internet (Will Show Alert if Offline):**
- Login (first time - cached afterwards)
- Password reset via email
- Email-based features

### 3. **Sync Status Indicator**

A real-time indicator shows your connection status:

- 🌐 **Online** (Green) - Connected to online database
- 📱 **Offline** (Orange) - Running in offline mode
- 🔄 **Syncing** - Uploading pending operations
- 🔴 **Badge Number** - Number of pending operations waiting to sync

## 📁 Architecture

### Services

#### 1. **ConnectivityService** (`Services/ConnectivityService.cs`)
- Monitors internet connection status
- Notifies app when connectivity changes
- Tests database reachability

#### 2. **LocalDatabaseService** (`Services/LocalDatabaseService.cs`)
- SQLite local database for offline storage
- Stores products, inventory, transactions, users
- Manages pending operations queue

#### 3. **DatabaseSyncService** (`Services/DatabaseSyncService.cs`)
- Syncs pending operations to online MySQL database
- Pulls latest data from online to local cache
- Handles sync errors and retries

#### 4. **HybridDatabaseService** (`Services/HybridDatabaseService.cs`)
- Unified API that works both online and offline
- Automatically routes to SQLite or MySQL based on connectivity
- Queues operations when offline

### Data Flow

```
┌─────────────────┐
│  User Action    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ HybridDatabase  │◄──────┐
│    Service      │       │
└────────┬────────┘       │
         │                │
    ┌────┴────┐          │
    │         │          │
    ▼         ▼          │
┌──────┐  ┌──────┐      │
│Online│  │Local │      │
│MySQL │  │SQLite│      │
└──┬───┘  └───┬──┘      │
   │          │         │
   │    ┌─────┴──────┐  │
   │    │ Pending    │  │
   │    │ Operations │  │
   │    │   Queue    │  │
   │    └─────┬──────┘  │
   │          │         │
   └──────────┴─────────┘
      Sync Service
```

## 🚀 How It Works

### When Internet is Available

1. App connects to online MySQL database
2. Pulls latest data (products, inventory, users) to local cache
3. All operations are saved to both local and online databases
4. If any pending operations exist, they are synced immediately

### When Internet is Lost

1. System automatically detects loss of connectivity
2. Switches to local SQLite database
3. All operations continue normally, saved locally
4. Operations are queued for later sync
5. Sync indicator shows "Offline" with pending count

### When Internet is Restored

1. System detects connectivity restoration
2. **Automatically pulls latest data from online database** (Products, Inventory, Users)
3. Syncs all pending operations (inventory, products, logs)
4. Clears old synced operations
5. System returns to normal online mode

### Starting Offline Then Going Online

**Scenario:** You start the app with no internet, then internet becomes available.

**What Happens:**
1. App starts in offline mode with empty local database
2. You can't do much without data (no products, no inventory)
3. When internet is restored:
   - ✅ **Automatic Pull**: System automatically downloads all data from online database
   - ✅ **Products loaded**: All POS products become available
   - ✅ **Inventory loaded**: All inventory items appear
   - ✅ **Users loaded**: User data is cached locally
4. You can now work normally, and if internet drops again, you'll have the cached data!

## 🗄️ Local Database Tables

### Products (`LocalProduct`)
- `productID` - Product identifier
- `productName` - Product name
- `smallPrice`, `mediumPrice`, `largePrice` - Prices
- `category`, `subcategory` - Categories
- `imageSet`, `description`, `colorCode` - Display info

### Inventory (`LocalInventoryItem`)
- `inventoryItemID` - Inventory item identifier
- `itemName` - Item name
- `currentQuantity` - Current stock level
- `minimumQuantity`, `maximumQuantity` - Stock thresholds
- `unit` - Unit of measurement
- `supplierName`, `supplierContact` - Supplier info

### Transactions (`LocalTransaction`)
- `transactionID` - Transaction identifier
- `userId` - Cashier/employee ID
- `totalAmount` - Total sale amount
- `paymentMethod` - Payment type
- `transactionDate` - Date/time of sale
- `orderDetails` - JSON order details

### Pending Operations (`PendingOperation`)
- `Id` - Operation identifier
- `OperationType` - INSERT, UPDATE, DELETE
- `TableName` - Target table
- `DataJson` - Operation data as JSON
- `CreatedAt` - When operation was queued
- `IsSynced` - Whether operation has been synced
- `SyncedAt` - When operation was synced

## 📝 Usage Examples

### Check Connection Status

```csharp
// Check if online
bool isOnline = App.ConnectivityService.IsConnected;

// Listen for connectivity changes
App.ConnectivityService.ConnectivityChanged += (sender, isConnected) =>
{
    if (isConnected)
    {
        Debug.WriteLine("✅ Back online!");
    }
    else
    {
        Debug.WriteLine("⚠️ Went offline");
    }
};
```

### Manual Sync/Pull Trigger

**From UI (Settings Page):**
1. Open Settings
2. Click the blue "🔄 Sync Database" button
3. System will:
   - Show if you're offline (sync will happen automatically later)
   - **Pull latest data from online database** if no pending operations
   - Sync pending operations if any exist
   - Show success/failure message with details

**Use Cases:**
- ✅ **First time online**: Pull all data to start working
- ✅ **Refresh data**: Get latest changes from other devices/users
- ✅ **After being offline**: Push your changes and pull updates
- ✅ **Manual refresh**: When you want to ensure you have latest data

**From Code:**
```csharp
// Manually trigger sync
var result = await App.SyncService.SyncPendingOperationsAsync();

if (result.Success)
{
    Debug.WriteLine($"✅ Synced {result.OperationsSynced} operations");
}
else
{
    Debug.WriteLine($"❌ Sync failed: {result.Message}");
}
```

### Check Pending Operations

```csharp
// Get count of pending operations
int pendingCount = await App.LocalDb.GetPendingOperationsCountAsync();
Debug.WriteLine($"📝 {pendingCount} operations pending sync");
```

### Pull Latest Data

```csharp
// Pull latest data from online database
bool success = await App.SyncService.PullLatestDataAsync();

if (success)
{
    Debug.WriteLine("✅ Latest data pulled successfully");
}
```

## ⚙️ Configuration

### Adjust Sync Behavior

Edit `App.xaml.cs` to customize:

```csharp
// Timeout for database operations
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

// Initial data pull on startup
await SyncService.PullLatestDataAsync();
```

### Customize Sync Interval

Edit `Views/Controls/SyncStatusIndicator.xaml.cs`:

```csharp
// Update pending count every 5 seconds (default)
_updateTimer = new System.Threading.Timer(async _ =>
{
    await UpdatePendingCount();
}, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
```

## 🔧 Troubleshooting

### Problem: Offline mode not working

**Solution:**
1. Check that SQLite database is initialized: `App.LocalDb`
2. Verify local database file exists: `FileSystem.AppDataDirectory/coftea_local.db`
3. Check logs for initialization errors

### Problem: Sync not happening

**Solution:**
1. Verify internet connection: `App.ConnectivityService.IsConnected`
2. Test database connectivity: `await ConnectivityService.CanReachDatabaseAsync()`
3. Check pending operations: `await LocalDb.GetPendingOperationsAsync()`
4. Review sync logs in Debug output

### Problem: Data not appearing after sync

**Solution:**
1. Manually pull latest data: `await SyncService.PullLatestDataAsync()`
2. Clear app cache and restart
3. Check MySQL database for the data
4. Verify sync completed successfully in logs

## 📊 Database Location

### Local Database
- **Path**: `{FileSystem.AppDataDirectory}/coftea_local.db`
- **Type**: SQLite
- **Size**: Typically < 10 MB
- **Backup**: Stored in app's private storage

### Online Database
- **Type**: MySQL (PhpMyAdmin)
- **Location**: As configured in `Database.cs`
- **Sync**: Automatic when online

## 🎯 Best Practices

1. **Always check pending count** before critical operations
2. **Sync regularly** when internet is available
3. **Monitor sync status** indicator in UI
4. **Test offline mode** before relying on it
5. **Keep local database size manageable** (automatic cleanup of old synced operations after 7 days)

## 🔒 Data Persistence

- **Local data** persists across app restarts
- **Pending operations** are preserved until synced
- **Cache data** is refreshed when going online
- **Old synced operations** are auto-deleted after 7 days

## 🚨 Important Notes

### Sync Safety & Duplicate Prevention
- ✅ **Safe to press sync multiple times** - operations are NEVER synced twice
- ✅ **Multiple protection layers** prevent duplicate deductions
- ✅ **Automatic deduplication** at queue level
- ✅ **Operations marked as synced** after processing
- 📖 **See SYNC_PROTECTION_GUIDE.md** for detailed technical information

### First Time Offline Startup
- If you start the app offline with no cached data, you won't have any products or inventory to work with
- **Solution**: Once internet is available, the system will **automatically pull all data**
- **Or**: Manually click the "Sync Database" button in Settings once online

### Transaction Sync
- Transactions are saved locally but require manual review for sync
- This is due to complex order details structure
- Future update will add automatic transaction sync

### User Management
- Users are cached locally but managed from admin panel only
- Users are pulled automatically when going online

### Real-time Sync
- Inventory changes sync immediately when online
- Product updates sync immediately when online
- Activity logs sync immediately when online

### Data Pull on Connectivity Restore
- **Automatic**: System pulls data when internet is restored
- **Manual**: Use "Sync Database" button in Settings anytime
- **Smart**: Only pulls if you have internet connection

## 📱 UI Indicator

Add the sync status indicator to any page:

```xml
<views:SyncStatusIndicator 
    HorizontalOptions="End" 
    VerticalOptions="Start"
    Margin="10"/>
```

## 🚫 No More Blocking "No Internet" Popups!

The system **no longer shows blocking alerts** for database operations. Instead:

✅ **Silent Mode Switching** - Automatically switches to offline mode without interrupting you  
✅ **Status Indicator Only** - Small, non-intrusive indicator shows connection status  
✅ **No Retry Popups** - No need to manually retry database operations  
✅ **Seamless Experience** - Just keep working, system handles the rest  

**Exception:** Login and password reset features still require internet (they use email service), so those will show alerts if offline.

## 🎉 Benefits

✅ **No Internet Required** - Continue working even without connection  
✅ **Automatic Sync** - No manual intervention needed  
✅ **Data Safety** - All data preserved locally until synced  
✅ **Seamless Experience** - User doesn't notice the switch  
✅ **Reliable** - Works even in poor connectivity areas  
✅ **No Annoying Popups** - Silent offline mode activation  

---

## 🆘 Support

For issues or questions:
1. Check Debug logs for detailed error messages
2. Review pending operations queue
3. Test connectivity and database reachability
4. Contact system administrator if sync fails repeatedly

**System developed with offline-first architecture for maximum reliability! 🚀**

