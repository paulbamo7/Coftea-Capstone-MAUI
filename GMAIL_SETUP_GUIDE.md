# Gmail SMTP Setup Guide

This guide will help you configure Gmail SMTP for sending emails (registration, password reset, email verification) in the Coftea Management System.

## Prerequisites

- A Gmail account (create a dedicated one for your app is recommended)
- 2-Step Verification enabled on your Gmail account

## Setup Steps

### Step 1: Enable 2-Step Verification

1. Go to [Google Account Security](https://myaccount.google.com/security)
2. Under "Signing in to Google", click **2-Step Verification**
3. Follow the prompts to enable 2-Step Verification

### Step 2: Generate App Password

1. Go back to [Google Account Security](https://myaccount.google.com/security)
2. Under "Signing in to Google", click **App passwords**
3. Select app: **Mail**
4. Select device: **Other (Custom name)**
5. Enter name: **Coftea App**
6. Click **Generate**
7. **Copy the 16-character password** (you'll need this!)

### Step 3: Configure in Your App

Add this code to your app initialization (e.g., in `App.xaml.cs` or when the app starts):

```csharp
// Configure Gmail SMTP
EmailService.ConfigureGmail(
    gmailAddress: "your-email@gmail.com",  // Your Gmail address
    appPassword: "xxxx xxxx xxxx xxxx"     // The 16-character app password (with or without spaces)
);
```

**Example:**
```csharp
EmailService.ConfigureGmail("coftea.app@gmail.com", "abcd efgh ijkl mnop");
```

### Step 4: Test Email Sending

After configuration, test by:
1. Registering a new user
2. Requesting a password reset
3. Check if emails are received

## Switching Back to MailHog (Development)

If you want to use MailHog for local testing instead:

```csharp
EmailService.UseMailHog();
```

## Security Notes

- **Never commit your Gmail credentials to version control**
- Store credentials in app settings or environment variables
- Use a dedicated Gmail account for the app (not your personal account)
- The app password is different from your Gmail password

## Troubleshooting

### "Authentication failed" error
- Make sure 2-Step Verification is enabled
- Verify you're using the App Password (not your regular password)
- Check that the App Password was copied correctly (no extra spaces)

### "Connection timeout" error
- Check your internet connection
- Verify Gmail SMTP settings: `smtp.gmail.com:587`
- Check firewall settings

### Emails not received
- Check spam/junk folder
- Verify recipient email address is correct
- Check Gmail account for any security alerts

## Email Types Supported

1. **Registration Success Email** - Sent when user successfully registers
2. **Password Reset Email** - Sent with verification code for password reset
3. **Email Verification** - Sent with verification code (if implemented)

## Default Behavior

- If Gmail is not configured, the app will use MailHog (for development)
- If Gmail is configured, all emails will be sent via Gmail SMTP
- Email sending failures won't block user operations (graceful degradation)

