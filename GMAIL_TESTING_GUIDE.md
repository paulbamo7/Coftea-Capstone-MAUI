# Gmail SMTP Testing & Configuration Guide

## Quick Setup (5 minutes)

### Step 1: Get Gmail App Password

1. **Go to Google Account**: https://myaccount.google.com/security
2. **Enable 2-Step Verification** (if not already enabled)
   - Click "2-Step Verification"
   - Follow the setup process
3. **Generate App Password**:
   - Go back to Security page
   - Click "App passwords" (under "Signing in to Google")
   - Select app: **Mail**
   - Select device: **Other (Custom name)**
   - Enter: **Coftea App**
   - Click **Generate**
   - **Copy the 16-character password** (format: `xxxx xxxx xxxx xxxx`)

### Step 2: Configure in Your App

Open `App.xaml.cs` and find this section (around line 85):

```csharp
// Configure Gmail SMTP (uncomment and fill in your Gmail credentials)
// EmailService.ConfigureGmail(
//     gmailAddress: "your-email@gmail.com",  // Replace with your Gmail address
//     appPassword: "xxxx xxxx xxxx xxxx"       // Replace with your 16-character App Password
// );
```

**Uncomment and fill in your details:**

```csharp
// Configure Gmail SMTP
EmailService.ConfigureGmail(
    gmailAddress: "coftea.app@gmail.com",  // Your Gmail address
    appPassword: "abcd efgh ijkl mnop"     // Your 16-character App Password
);
```

### Step 3: Test Email Sending

#### Test 1: Registration Email
1. Run your app
2. Go to Register page
3. Register a new user with a real email address
4. Check the email inbox (and spam folder)
5. You should receive a "Welcome to Coftea" email

#### Test 2: Password Reset Email
1. Go to Login page
2. Click "Forgot Password"
3. Enter an email address
4. Check the email inbox
5. You should receive a password reset code email

#### Test 3: Check Debug Output
- Open Debug/Output window in Visual Studio
- Look for messages like:
  - `📧 Using Gmail SMTP for email sending`
  - `✅ Email sent successfully!`
  - Or error messages if something fails

## Verification Checklist

✅ **Configuration:**
- [ ] Gmail credentials added to `App.xaml.cs`
- [ ] App Password copied correctly (16 characters)
- [ ] 2-Step Verification enabled on Gmail account

✅ **Testing:**
- [ ] Registration email received
- [ ] Password reset email received
- [ ] Emails appear in inbox (not just spam)
- [ ] Email content looks correct

## Troubleshooting

### ❌ "Authentication failed" error
**Solution:**
- Make sure 2-Step Verification is enabled
- Verify you're using App Password (not regular password)
- Check for extra spaces in App Password
- Try regenerating the App Password

### ❌ "Connection timeout" error
**Solution:**
- Check internet connection
- Verify Gmail SMTP: `smtp.gmail.com:587`
- Check firewall/antivirus settings
- Try again (Gmail may be temporarily blocking)

### ❌ Emails not received
**Solution:**
- Check spam/junk folder
- Verify recipient email is correct
- Check Gmail account for security alerts
- Wait a few minutes (can take 1-2 minutes)
- Check Debug output for errors

### ❌ Still using MailHog
**Solution:**
- Make sure you uncommented the `EmailService.ConfigureGmail()` line
- Check Debug output - should say "Using Gmail SMTP"
- Verify credentials are saved: Check Preferences in debugger

## Switch Back to MailHog

If you want to test locally with MailHog instead:

```csharp
// In App.xaml.cs, comment out Gmail config and use:
EmailService.UseMailHog();
```

## Example Configuration

```csharp
// In App.xaml.cs, around line 85-90:

// Initialize view models
InitializeViewModels();

// Configure Gmail SMTP
EmailService.ConfigureGmail(
    gmailAddress: "coftea.management@gmail.com",
    appPassword: "abcd efgh ijkl mnop"  // Your actual App Password
);
```

## Security Reminder

⚠️ **Never commit your Gmail credentials to Git!**

- Use a dedicated Gmail account for the app
- Don't share your App Password
- Consider using environment variables for production

## Testing Tips

1. **Use a real email** you can access
2. **Check spam folder** - Gmail might filter first emails
3. **Wait 1-2 minutes** - emails aren't instant
4. **Check Debug output** - shows what's happening
5. **Test with different email providers** - Gmail, Yahoo, Outlook, etc.

