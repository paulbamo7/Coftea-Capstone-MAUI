# SMS Setup Instructions for Purchase Order System

## 📱 Setting Up Real SMS with Twilio

### Step 1: Create Twilio Account
1. Go to [https://www.twilio.com](https://www.twilio.com)
2. Sign up for a free account
3. Verify your phone number
4. Get a free trial phone number

### Step 2: Get Your Twilio Credentials
1. Go to your Twilio Console Dashboard
2. Find your **Account SID** and **Auth Token**
3. Get your **Twilio Phone Number** (starts with +1 for US numbers)

### Step 3: Update SMS Service Configuration
In `Services/PurchaseOrderSMSService.cs`, replace these values:

```csharp
// Replace these with your actual Twilio credentials
private static readonly string TWILIO_ACCOUNT_SID = "YOUR_ACTUAL_ACCOUNT_SID";
private static readonly string TWILIO_AUTH_TOKEN = "YOUR_ACTUAL_AUTH_TOKEN";
private static readonly string TWILIO_PHONE_NUMBER = "YOUR_ACTUAL_TWILIO_PHONE_NUMBER";
```

### Step 4: Test SMS Functionality
1. Run your application
2. Create a purchase order
3. Check your phone for SMS messages

## 🔧 Alternative SMS Services

If you prefer other SMS services:

### AWS SNS (Simple Notification Service)
- More complex setup but very reliable
- Good for enterprise applications

### TextLocal
- Popular in Asia
- Good rates for international SMS

### MessageBird
- European-based service
- Good global coverage

## 💰 Cost Considerations

- **Twilio Trial**: Free SMS to verified numbers
- **Twilio Paid**: ~$0.0075 per SMS to Philippines
- **Volume Discounts**: Available for high-volume usage

## 🚨 Important Notes

1. **Trial Limitations**: Twilio trial accounts have limitations
2. **Phone Number Format**: Always use international format (+639625068078)
3. **Rate Limits**: Be mindful of SMS rate limits
4. **Security**: Never commit real credentials to version control

## 📞 Current Configuration

- **Your Phone**: +639625068078 (Philippines)
- **SMS Service**: Twilio (configurable)
- **Message Types**: Purchase orders, approvals, notifications

## 🔍 Troubleshooting

### If SMS doesn't arrive:
1. Check Twilio credentials are correct
2. Verify phone number format (+639625068078)
3. Check Twilio console for delivery status
4. Ensure sufficient account balance

### If you get errors:
1. Check debug output for specific error messages
2. Verify internet connection
3. Check Twilio account status
4. Ensure phone number is verified (for trial accounts)
