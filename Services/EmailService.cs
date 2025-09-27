using System.Net.Mail;
using System.Net;
using System.Text;

namespace Coftea_Capstone.Services
{
    public class EmailService
    {
        private readonly string _mailHogHost;
        private readonly int _mailHogPort;

        public EmailService(string mailHogHost = "192.168.254.104", int mailHogPort = 1025)
        {
            _mailHogHost = mailHogHost;
            _mailHogPort = mailHogPort;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var client = new SmtpClient(_mailHogHost, _mailHogPort);
                client.EnableSsl = false;
                client.UseDefaultCredentials = false;
                
                // Try to connect without sending
                await client.SendMailAsync(new MailMessage("test@test.com", "test@test.com", "Test", "Test"));
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MailHog connection test failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string resetToken)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Attempting to send password reset email to: {email}");
                System.Diagnostics.Debug.WriteLine($"Using MailHog host: {_mailHogHost}:{_mailHogPort}");
                System.Diagnostics.Debug.WriteLine($"Reset token: {resetToken}");

                using var client = new SmtpClient(_mailHogHost, _mailHogPort);
                client.EnableSsl = false; // MailHog doesn't use SSL
                client.UseDefaultCredentials = false;

                var resetLink = $"http://localhost:3000/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(resetToken)}";
                System.Diagnostics.Debug.WriteLine($"Reset link: {resetLink}");
                
                var htmlBody = CreatePasswordResetEmailBody(resetLink);
                System.Diagnostics.Debug.WriteLine($"Email body length: {htmlBody.Length} characters");

                var plainTextBody = CreatePasswordResetEmailPlainText(resetLink);

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

        public string GetEmailBodyForTesting(string resetLink)
        {
            return CreatePasswordResetEmailBody(resetLink);
        }

        private string CreatePasswordResetEmailPlainText(string resetLink)
        {
            return $@"
Coftea Password Reset Request

You have requested to reset your password for your Coftea account.

For Development Testing:
Click the link below to open the password reset popup directly in the app, or copy the reset link for testing.

Reset your password by clicking this link:
{resetLink}

This link will expire in 1 hour for security reasons.

If you did not request this password reset, please ignore this email.

This is a test email sent via MailHog for development purposes.
Coftea Management System
";
        }

        private string CreatePasswordResetEmailBody(string resetLink)
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
            <h2>Password Reset Request</h2>
            <p>You have requested to reset your password for your Coftea account.</p>
            
            <div class='note'>
                <strong>For Development Testing:</strong><br>
                Click the button below to open the password reset popup directly in the app, or copy the reset link for testing.
            </div>
            
            <p>Click the button below to reset your password:</p>
            <a href='{resetLink}' class='button'>Reset Password</a>
            
            <p><strong>Reset Link:</strong><br>
            <code style='background-color: #f0f0f0; padding: 5px; border-radius: 3px; word-break: break-all;'>{resetLink}</code></p>
            
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
