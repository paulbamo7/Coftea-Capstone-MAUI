# Coftea POS - Disaster Recovery & Business Continuity Plan

## Overview
This document outlines the disaster recovery strategies for the Coftea POS system to ensure business continuity during power outages, internet failures, or hardware issues.

## ⚡ Current Risks

### 1. Power Outage
- **Risk**: System shuts down immediately
- **Impact**: 
  - Loss of unsaved transactions
  - Database corruption if power loss during write
  - Unable to process sales

### 2. Internet Outage  
- **Risk**: May affect system depending on configuration
- **Impact**: Varies based on setup

### 3. Hardware Failure
- **Risk**: Computer/hard drive failure
- **Impact**: Complete data loss without backups

## ✅ Solutions Implemented

### 1. Automatic Database Backup System ⭐

**Location**: `Services/DatabaseBackupService.cs`

**Features**:
- ✅ **Automatic backups every 4 hours**
- ✅ **Backup on app startup**
- ✅ **Keeps last 30 backups** (auto-cleanup old ones)
- ✅ **Stored in**: `Documents/CofteaBackups/`
- ✅ **File format**: SQL dump files
- ✅ **One-click restore capability**

**Usage**:
```csharp
// Manual backup
await App.BackupService.CreateBackupAsync();

// Manual restore
await App.BackupService.RestoreFromBackupAsync("path/to/backup.sql");

// Get list of available backups
var backups = App.BackupService.GetAvailableBackups();

// Export to specific location
await App.BackupService.ExportToFileAsync("D:/manual_backup.sql");
```

### 2. Transaction Logging
- ✅ All transactions saved to database immediately
- ✅ Inventory activity log tracks all changes
- ✅ Can recover transaction history from backups

## 📋 Required Setup

### Step 1: Install MySqlBackup NuGet Package

You need to install the MySqlBackup.NET package for backup functionality:

```bash
# Run this in Package Manager Console
Install-Package MySqlBackup.NET
```

Or using .NET CLI:
```bash
dotnet add package MySqlBackup.NET
```

### Step 2: Configure Backup Location (Optional)

By default, backups are stored in `Documents/CofteaBackups/`. To change:

```csharp
// In App.xaml.cs, modify the BackupService initialization:
BackupService = new Services.DatabaseBackupService(
    connectionString, 
    backupDirectory: "D:/CofteaBackups" // Custom location
);
```

### Step 3: Test Backup System

1. Run the app
2. Check Debug output for: `✅ Database backup service initialized`
3. Check `Documents/CofteaBackups/` folder for backup files
4. First backup should be created immediately on startup

## 🚨 Emergency Procedures

### Scenario 1: Power Outage During Operation

**What Happens**:
- Last backup (within last 4 hours) is safe in `Documents/CofteaBackups/`
- MySQL has transaction logs that may allow recovery

**Recovery Steps**:
1. Restart computer and MySQL
2. Start Coftea POS app
3. If database is corrupted, restore from last backup (see Scenario 3)

### Scenario 2: Hardware Failure

**What Happens**:
- If hard drive is still accessible, backups can be retrieved
- If hard drive is dead, data is lost unless you followed "Additional Recommendations"

**Recovery Steps**:
1. Install MySQL on new computer
2. Install Coftea POS on new computer
3. Copy backups from old hard drive or cloud storage
4. Restore latest backup

### Scenario 3: Database Corruption

**Recovery Steps**:
1. Open Coftea POS app
2. Go to Settings
3. Look for "Restore Database" option (you may need to add this UI)
4. Select the latest backup file
5. Click Restore

**Manual Restore** (if no UI):
```csharp
var backupService = App.BackupService;
var backups = backupService.GetAvailableBackups();
var latestBackup = backups.FirstOrDefault();

if (latestBackup != null)
{
    await backupService.RestoreFromBackupAsync(latestBackup.FilePath);
}
```

### Scenario 4: Internet Outage

**Good News**: Your app runs on LOCALHOST, so internet is NOT required for operation!

**However, be aware**:
- If MySQL is configured to require internet authentication: Change to localhost-only
- If using cloud MySQL: Won't work during outage (but your'e using localhost)
- External reporting/syncing won't work

## 📱 Additional Recommendations

### 1. **USB Backup Drive** (Highly Recommended!)
Set up automatic backup copying to USB drive:

```csharp
// Add this to backup service
public async Task CopyBackupsToUSB(string usbDrivePath)
{
    var backups = GetAvailableBackups();
    var usbBackupPath = Path.Combine(usbDrivePath, "CofteaBackups");
    
    Directory.CreateDirectory(usbBackupPath);
    
    foreach (var backup in backups.Take(5)) // Copy last 5 backups
    {
        var destPath = Path.Combine(usbBackupPath, backup.FileName);
        File.Copy(backup.FilePath, destPath, overwrite: true);
    }
}
```

**Setup**:
1. Keep USB drive plugged into POS computer
2. Backups auto-copy to USB every 4 hours
3. If computer dies, USB drive has recent backups

### 2. **Cloud Backup** (Optional)
Use OneDrive/Google Drive to auto-sync backup folder:

1. Move backup folder to cloud-synced location:
   ```
   C:/Users/[User]/OneDrive/CofteaBackups
   ```
2. Configure backup service to use that location

### 3. **UPS (Uninterruptible Power Supply)** 💡
**Hardware Solution**: Buy a UPS battery backup ($50-200)

Benefits:
- Computer stays on during brief power outages
- Time to save work and shut down properly
- Protects against power surges

Recommended capacity:
- 600-1000 VA for desktop PC + monitor
- 30 minutes of backup power

### 4. **Offline Mode Operation**
Your system already works offline since it's localhost-based!

Just ensure:
- MySQL starts automatically on Windows startup
- No external API dependencies for core POS functions

### 5. **Daily End-of-Day Backup**
Add a manual backup button that staff triggers at end of day:

```csharp
[RelayCommand]
private async Task CreateEndOfDayBackup()
{
    var success = await App.BackupService.CreateBackupAsync();
    
    if (success)
    {
        await App.NotificationPopup.ShowNotificationAsync(
            "✅ End-of-day backup created successfully!", 
            "success"
        );
    }
}
```

## 📊 Backup File Management

### Backup File Naming Convention
```
coftea_backup_2025-01-27_14-30-00.sql
                └─ Date      └─ Time
```

### Storage Requirements
- Each backup: ~1-10 MB (depends on data size)
- 30 backups: ~30-300 MB total
- Recommend: Reserve 1 GB for backup folder

### Retention Policy
- **Automatic**: Last 30 backups kept (1 week of 4-hour intervals)
- **Manual backups**: Kept indefinitely (not auto-deleted)
- **Recommendation**: Keep monthly backups separately for long-term records

## 🔧 Maintenance Tasks

### Weekly
- [ ] Verify backups are being created (check folder)
- [ ] Test restore from backup (on test database)

### Monthly
- [ ] Copy important backups to external drive
- [ ] Verify USB backup drive is working
- [ ] Clean up very old backups (>3 months) manually if needed

### Quarterly
- [ ] Full disaster recovery drill (simulate failure and restore)
- [ ] Update this documentation if procedures change

## 🆘 Support Contact
If recovery fails or you need help:
1. Check Debug output for error messages
2. Verify MySQL service is running
3. Try restoring from multiple recent backups
4. Check if backup files are corrupted (should be readable SQL text)

## 📝 Changelog
- **2025-01-27**: Initial disaster recovery system implemented
  - Added DatabaseBackupService
  - Automatic 4-hour backups
  - 30-backup retention
  - Manual backup/restore capability

