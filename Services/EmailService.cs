using System.Net.Mail;
using System.Net;
using System.Text;

namespace Coftea_Capstone.Services
{
    public class EmailService
    {
        private readonly string _mailHogHost;
        private readonly int _mailHogPort;
        private string _manualMailHogHost;

        public EmailService(string mailHogHost = "192.168.1.6", int mailHogPort = 1025)
        {
            _mailHogHost = mailHogHost; // Will be resolved dynamically
            _mailHogPort = mailHogPort;
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string resetToken)
        {
            try
            {
                // Localhost-first behavior for MailHog
                // Priority: Manual MailHog host > Configured MailHog host > localhost
                var host = _manualMailHogHost
                           ?? _mailHogHost
                           ?? "localhost"; // Default to localhost for MailHog
                System.Diagnostics.Debug.WriteLine($"Attempting to send password reset email to: {email}");
                System.Diagnostics.Debug.WriteLine($"Using MailHog host: {host}:{_mailHogPort}");
                System.Diagnostics.Debug.WriteLine($"MailHog host source: {(_manualMailHogHost != null ? "Manual" : _mailHogHost != null ? "Configured" : "localhost")}");
                System.Diagnostics.Debug.WriteLine($"Reset token: {resetToken}");

                using var client = new SmtpClient(host, _mailHogPort);
                client.EnableSsl = false; // MailHog doesn't use SSL
                client.UseDefaultCredentials = false;

                // We now send a 6-digit code instead of a link for simulation
                var htmlBody = CreatePasswordResetEmailBodyWithCode(resetToken);
                System.Diagnostics.Debug.WriteLine($"Email body length: {htmlBody.Length} characters");

                var plainTextBody = CreatePasswordResetEmailPlainTextWithCode(resetToken);

                var message = new MailMessage
                {
                    From = new MailAddress("noreply@coftea.com", "Coftea System"),
                    Subject = "Password Reset Request",
                    Body = htmlBody,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                // Provide both MIME parts explicitly so MailHog shows the HTML tab
                var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, "text/html");
                var plainTextView = AlternateView.CreateAlternateViewFromString(plainTextBody, Encoding.UTF8, "text/plain");
                // Add HTML view first, then plain text as fallback
                message.AlternateViews.Add(htmlView);
                message.AlternateViews.Add(plainTextView);

                message.To.Add(email);

                System.Diagnostics.Debug.WriteLine($"Sending email to {email}...");
                await client.SendMailAsync(message);
                System.Diagnostics.Debug.WriteLine("Email sent successfully!");
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                System.Diagnostics.Debug.WriteLine($"Failed to send email: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public string GetEmailBodyForTesting(string code) => CreatePasswordResetEmailBodyWithCode(code);

        public void SetManualMailHogHost(string host)
        {
            _manualMailHogHost = host;
            System.Diagnostics.Debug.WriteLine($"Manual MailHog host set to: {host}");
        }

        public void ClearManualMailHogHost()
        {
            _manualMailHogHost = null;
            System.Diagnostics.Debug.WriteLine("Manual MailHog host cleared, using auto-detection");
        }

        public string GetCurrentMailHogHost()
        {
            return _manualMailHogHost ?? _mailHogHost ?? NetworkConfigurationService.GetEmailHost();
        }

        // Legacy method names for backward compatibility
        public void SetManualEmailHost(string host) => SetManualMailHogHost(host);
        public void ClearManualEmailHost() => ClearManualMailHogHost();
        public string GetCurrentEmailHost() => GetCurrentMailHogHost();

        private string CreatePasswordResetEmailPlainTextWithCode(string code)
        {
            return $@"
Coftea Password Reset Request

You have requested to reset your password for your Coftea account.

For Development Testing:
Use the verification code below in the app to reset your password:

Your Coftea reset code: {code}

This link will expire in 1 hour for security reasons.

If you did not request this password reset, please ignore this email.

This is a test email sent via MailHog for development purposes.
Coftea Management System
";
        }

        private string CreatePasswordResetEmailBodyWithCode(string code) // Updated to use code instead of link
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Password Reset - Coftea</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #5B4F45; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #5B4F45; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .note {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Coftea Password Reset</h1>
        </div>
        <div class='content'>
            <h2>Password Reset Code</h2>
            <p>You have requested to reset your password for your Coftea account.</p>
            <div class='note'>
                <strong>For Development Testing:</strong><br>
                Enter this verification code in the app to reset your password:
            </div>
            <p style='font-size:24px; font-weight:bold; letter-spacing:4px; background-color:#fff; display:inline-block; padding:10px 16px; border-radius:6px;'>
                {code}
            </p>
            
            <p>This link will expire in 1 hour for security reasons.</p>
            <p>If you did not request this password reset, please ignore this email.</p>
        </div>
        <div class='footer'>
            <p>This is a test email sent via MailHog for development purposes.</p>
            <p>Coftea Management System</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
