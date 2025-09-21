# MailHog Troubleshooting Guide

## Issue: Forgot Password Not Sending Emails to MailHog

### Step 1: Check if MailHog is Running

1. **Check MailHog Status:**
   - Open a web browser and go to: http://localhost:8025
   - If MailHog is running, you should see the MailHog web interface
   - If you get a "connection refused" error, MailHog is not running

2. **Start MailHog:**
   - Download MailHog from: https://github.com/mailhog/MailHog/releases
   - Run MailHog: `./MailHog` (or `MailHog.exe` on Windows)
   - MailHog will start on:
     - SMTP Server: localhost:1025
     - Web Interface: localhost:8025

### Step 2: Check Debug Output

The app now includes enhanced debugging. Check the debug output in Visual Studio or your IDE for these messages:
- "Requesting password reset for email: [email]"
- "Reset token received: True/False"
- "Attempting to send email to: [email]"
- "Email sent successfully: True/False"

### Step 3: Common Issues and Solutions

#### Issue 1: MailHog Not Running
**Symptoms:** "Failed to send password reset email" error
**Solution:** Start MailHog before testing

#### Issue 2: Database Connection Issues
**Symptoms:** "No account found with this email address" (even with valid email)
**Solution:** Check database connection and ensure user exists

#### Issue 3: SMTP Connection Refused
**Symptoms:** Debug shows "Email sent successfully: False"
**Solution:** 
- Verify MailHog is running on port 1025
- Check firewall settings
- Try restarting MailHog

#### Issue 4: User Not Found
**Symptoms:** "No account found with this email address"
**Solution:** 
- Ensure the email exists in the database
- Check if the email is properly stored (case-sensitive)

### Step 4: Manual Testing

1. **Test MailHog Connection:**
   ```bash
   telnet localhost 1025
   ```
   If connection is successful, you should see MailHog's SMTP banner

2. **Test Database Connection:**
   - Check if the user exists in the database
   - Verify the `reset_token` and `reset_expiry` columns exist in the `users` table

### Step 5: Verify Email Content

When emails are sent successfully, check MailHog at http://localhost:8025 to see:
- Email subject: "Password Reset Request"
- Email content: HTML formatted with reset link
- Reset link format: `http://localhost:3000/reset-password?email=[email]&token=[token]`

### Debug Information

The enhanced debugging will show:
- Database query results
- Email service connection status
- Detailed error messages
- SMTP server response

### Quick Fix Commands

```bash
# Start MailHog (if downloaded)
./MailHog

# Or using Docker
docker run -d -p 1025:1025 -p 8025:8025 mailhog/mailhog

# Check if port 1025 is in use
netstat -an | findstr :1025
```
