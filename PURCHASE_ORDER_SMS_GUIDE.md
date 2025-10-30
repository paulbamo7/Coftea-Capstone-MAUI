# 📱 Purchase Order SMS Guide

## Overview

The Coftea app automatically sends SMS notifications to suppliers when creating purchase orders. The system uses your **phone's built-in SMS app** (100% FREE - no API keys or setup required).

## 🎯 How It Works

### Step-by-Step Flow

1. **Low Stock Detection**
   - Go to Inventory page
   - Items below minimum stock will show in red with alerts
   
2. **Create Purchase Order**
   - Click "Create Purchase Order" button
   - System finds all items below minimum stock
   - Shows confirmation dialog with:
     - List of items needed
     - Quantities required
     - What will happen next

3. **SMS Notification**
   - After confirmation, phone's SMS app opens automatically
   - **Pre-filled with:**
     - Supplier phone number: `+639625068078` (change in code)
     - Purchase order message with all details
   - **You just need to press "Send"**

4. **Message Content**
   ```
   COFTEA PURCHASE ORDER #123
   Date: 2025-10-29 14:30
   Supplier: Coftea Supplier
   
   ITEMS NEEDED:
   • Milk - 5 L
   • Sugar - 2 kg
   • Coffee Beans - 3 kg
   
   Please confirm delivery date and pricing.
   Reply with 'CONFIRM' to accept this order.
   ```

## 🔧 Configuration

### Change Supplier Phone Number

Open `Services/PurchaseOrderSMSService.cs` and update:

```csharp
private static readonly string SUPPLIER_PHONE = "+639625068078"; // Your supplier's number
private static readonly string ADMIN_PHONE = "+639625068078";     // Admin's number
```

**Format:** Use international format with `+` prefix
- Philippines: `+639XXXXXXXXX`
- US: `+1XXXXXXXXXX`
- UK: `+44XXXXXXXXXX`

### SMS Methods Available

The system supports multiple SMS methods. Current default is `PHONE_SMS_APP`:

```csharp
private static readonly SMS_METHOD CurrentSMSMethod = SMS_METHOD.PHONE_SMS_APP;
```

**Available Options:**

| Method | Cost | Setup Required | Auto-Send |
|--------|------|----------------|-----------|
| `PHONE_SMS_APP` | FREE (uses your phone plan) | None | No (manual send) |
| `EMAIL_TO_SMS` | FREE | Email config | Yes |
| `TEXTLOCAL_API` | Paid (cheap) | API key | Yes |
| `CLICKSEND_API` | Paid (cheap) | API key + username | Yes |
| `MESSAGEBIRD_API` | Paid (free trial) | API key | Yes |
| `AWS_SNS` | Paid (free tier) | AWS credentials | Yes |
| `VONAGE_API` | Paid (free trial) | API key + secret | Yes |
| `WHATSAPP_API` | FREE (personal) | WhatsApp token | Yes |
| `TELEGRAM_BOT` | FREE | Bot token | Yes |
| `LOCAL_SMS_GATEWAY` | Varies | Gateway URL | Yes |
| `SIMULATION_ONLY` | FREE | None | No (just logs) |

## 📲 Testing

### Test Scenario 1: Admin User

1. **Login as Admin**
2. **Go to Inventory page**
3. **Ensure some items are below minimum:**
   - Edit an inventory item
   - Set current quantity below minimum
   - Save
4. **Click "Create Purchase Order"**
5. **Confirmation shows:**
   ```
   Found 2 items below minimum stock levels:
   
   • Milk - 5 L
   • Sugar - 2 kg
   
   This will:
   • Create a purchase order
   • Auto-approve (you're admin)
   • Send SMS to Coftea Supplier
   • Update inventory immediately
   
   Proceed?
   ```
6. **Click "Yes"**
7. **SMS app opens** with pre-filled message
8. **Press "Send"** in SMS app
9. **Success message appears:**
   ```
   Purchase Order Created & Approved
   Purchase order #123 has been created, auto-approved by admin,
   and sent to Coftea Supplier via SMS. Inventory has been updated.
   ```

### Test Scenario 2: Regular Employee

1. **Login as Employee** (non-admin)
2. **Go to Inventory page**
3. **Click "Create Purchase Order"**
4. **Confirmation shows:**
   ```
   Found 2 items below minimum stock levels:
   
   • Milk - 5 L
   • Sugar - 2 kg
   
   This will:
   • Create a purchase order
   • Send SMS to Coftea Supplier
   • Notify admin for approval
   
   Proceed?
   ```
5. **Click "Yes"**
6. **SMS app opens TWICE:**
   - First: Message to supplier
   - Second: Notification to admin
7. **Press "Send"** for both
8. **Success message:**
   ```
   Purchase Order Created
   Purchase order #123 has been created and sent to Coftea Supplier
   via SMS. Admin has been notified for approval.
   ```

## 🚨 Troubleshooting

### Issue: SMS app doesn't open

**Possible causes:**
- Device doesn't support SMS (like tablets without SIM)
- SMS permission not granted

**Solution:**
- Check if device has SIM card
- Ensure SMS app is installed
- Grant SMS permissions to the app

**Fallback:** System shows alert:
```
SMS Not Available
Your device doesn't support SMS. Please contact the supplier manually.
```

### Issue: Wrong phone number

**Solution:**
1. Open `Services/PurchaseOrderSMSService.cs`
2. Update `SUPPLIER_PHONE` constant
3. Rebuild app

### Issue: Message not sending automatically

**This is normal!** The `PHONE_SMS_APP` method opens your SMS app but **requires manual send**. This is the FREE option.

**If you want automatic sending:**
1. Choose a different SMS method (see table above)
2. Set up API credentials
3. Change `CurrentSMSMethod` in code
4. Rebuild app

### Issue: "Failed to send SMS to supplier"

**Debug steps:**
1. Check debug output in Visual Studio
2. Look for error messages
3. Verify phone number format
4. Ensure SMS app is available

## 🔐 Privacy & Security

### What data is sent?

- Purchase order number
- Date and time
- List of items and quantities
- Your company name ("COFTEA")

### What is NOT sent?

- ❌ User passwords
- ❌ Customer data
- ❌ Transaction history
- ❌ Personal information
- ❌ Pricing (optional in message)

### Who receives SMS?

1. **Supplier** - Gets purchase order details
2. **Admin** - Gets notification (employee orders only)

### Can I disable SMS?

Yes! Change to `SIMULATION_ONLY` mode:

```csharp
private static readonly SMS_METHOD CurrentSMSMethod = SMS_METHOD.SIMULATION_ONLY;
```

This will only log to debug output, no actual SMS sent.

## 💰 Cost Comparison

### FREE Options

1. **PHONE_SMS_APP** (Current)
   - ✅ Uses your phone's SMS plan
   - ✅ No setup required
   - ❌ Manual send required
   - **Best for:** Small businesses, occasional use

2. **TELEGRAM_BOT**
   - ✅ Completely free
   - ✅ Automatic sending
   - ⚠️ Supplier needs Telegram account
   - **Best for:** Tech-savvy suppliers

3. **EMAIL_TO_SMS**
   - ✅ Free (if you have email)
   - ⚠️ Not reliable (carrier-dependent)
   - **Best for:** Testing only

### Paid Options (Cheap)

1. **TextLocal** - ~$0.01-0.05 per SMS
2. **ClickSend** - ~$0.02-0.06 per SMS
3. **MessageBird** - ~$0.01-0.04 per SMS
4. **Vonage** - ~$0.02-0.05 per SMS

**For 100 purchase orders per month:**
- Cost: $1-5/month
- Benefit: Automatic sending, delivery reports

## 🎓 Advanced: Using Paid SMS Service

### Example: Setting up ClickSend

1. **Sign up at clicksend.com**
2. **Get API credentials:**
   - Username
   - API Key
3. **Update code:**
   ```csharp
   private static readonly SMS_METHOD CurrentSMSMethod = SMS_METHOD.CLICKSEND_API;
   private static readonly string CLICKSEND_USERNAME = "your_username";
   private static readonly string CLICKSEND_API_KEY = "your_api_key";
   ```
4. **Rebuild app**
5. **Test:** SMS will be sent automatically (no manual send needed)

### Example: Setting up Telegram Bot

1. **Create Telegram bot:**
   - Talk to @BotFather on Telegram
   - Get bot token
2. **Get chat ID:**
   - Send message to your bot
   - Visit: `https://api.telegram.org/bot<YOUR_TOKEN>/getUpdates`
   - Find chat ID in response
3. **Update code:**
   ```csharp
   private static readonly SMS_METHOD CurrentSMSMethod = SMS_METHOD.TELEGRAM_BOT;
   private static readonly string TELEGRAM_BOT_TOKEN = "your_bot_token";
   private static readonly string TELEGRAM_CHAT_ID = "your_chat_id";
   ```
4. **Rebuild app**

## 📊 Debug Output

When creating a purchase order, check debug console for:

```
🛒 Creating purchase order...
📱 Sending SMS to Coftea Supplier (+639625068078):
📝 Message: COFTEA PURCHASE ORDER #123...
📱 Opening phone SMS app for +639625068078
✅ SMS app opened successfully
✅ SMS sent to supplier successfully
✅ Purchase order 123 created and processed
```

## 🎉 Summary

- ✅ **SMS feature is WORKING** - uses phone's built-in SMS app
- ✅ **100% FREE** - no API keys or setup needed
- ✅ **Easy to use** - just press "Send" when SMS app opens
- ✅ **Customizable** - change phone number in code
- ✅ **Upgradeable** - can switch to paid auto-sending later
- ✅ **Secure** - only sends purchase order data
- ✅ **Reliable** - uses your phone's SMS capability

**Your purchase order SMS is ready to use! Just test it and press send when the SMS app opens.** 📱✨

