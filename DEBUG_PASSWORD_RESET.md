# Password Reset Debug Guide

## Issue
The password reset code sent via MailHog email doesn't work when entered in the ResetPasswordPage.

## Debug Logging Added

I've added comprehensive debug logging to help diagnose the issue:

### 1. RequestPasswordResetAsync (Database.cs)
- Logs the generated token and its length
- Logs the expiry time
- Verifies what was actually stored in the database after the UPDATE

### 2. ResetPasswordAsync (Database.cs)
- Logs the email and provided token
- Queries the database to show what token is stored
- Compares the provided token with the stored token
- Shows expiry time and whether the token has expired
- Logs whether tokens match

### 3. ResetPasswordPageViewModel.cs
- Logs the email being used
- Logs the code entered by the user (with length)
- Logs the trimmed code (with length)

## How to Test

1. **Run the application** and navigate to the Forgot Password page
2. **Enter an email** and request a password reset
3. **Check the Debug Output** in Visual Studio (View > Output > Show output from: Debug)
4. **Look for these log entries:**
   - `[RequestPasswordReset] Generated token: 'XXXXXX' (Length: 6)`
   - `[RequestPasswordReset] Verified stored token: 'XXXXXX' (Length: X)`
5. **Open MailHog** (http://localhost:8025) and copy the 6-digit code from the email
6. **Enter the code** in the Reset Password page
7. **Check the Debug Output again** for:
   - `[ResetPasswordVM] Code entered: 'XXXXXX' (Length: X)`
   - `[ResetPassword] Provided reset token: 'XXXXXX' (Length: X)`
   - `[ResetPassword] Stored token in DB: 'XXXXXX' (Length: X)`
   - `[ResetPassword] Tokens match: True/False`
   - `[ResetPassword] Token expired: True/False`

## Potential Issues to Look For

1. **Token Length Mismatch**: Check if the stored token length differs from the provided token length
2. **Whitespace Issues**: Check if there are extra spaces in the stored or provided token
3. **Token Expiry**: Check if the token has expired (should be valid for 1 hour)
4. **Timezone Issues**: Check if there's a mismatch between `DateTime.Now` and MySQL's `NOW()`
5. **Character Encoding**: Check if the token characters are being stored/retrieved correctly

## Fix Applied

### Timezone Issue Fixed
The most likely cause was a timezone mismatch between the application and MySQL server. The fix includes:

1. **Changed `DateTime.Now` to `DateTime.UtcNow`** in `RequestPasswordResetAsync()`
   - Token expiry is now set using UTC time: `DateTime.UtcNow.AddHours(1)`

2. **Changed `NOW()` to `UTC_TIMESTAMP()`** in the SQL query in `ResetPasswordAsync()`
   - Query now uses: `WHERE email = @Email AND reset_token = @ResetToken AND reset_expiry > UTC_TIMESTAMP()`

This ensures that both the application and database use UTC time consistently, eliminating any timezone-related issues.

## Testing the Fix

1. **Run the application** and test the password reset flow
2. **Request a password reset** from the Forgot Password page
3. **Check MailHog** for the 6-digit code
4. **Enter the code** in the Reset Password page
5. **Verify it works** - you should be able to reset your password successfully

## Debug Output

If the issue persists, check the debug output for:
- Token length mismatches
- Whitespace in tokens
- Token expiry times
- Whether tokens match

## Additional Notes

The debug logging remains in place to help diagnose any other issues that might occur. You can remove it later once everything is working correctly.
