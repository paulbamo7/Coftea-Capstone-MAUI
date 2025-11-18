using System.Net.Mail;
using System.Net;
using System.Text;
using Microsoft.Maui.Storage;

namespace Coftea_Capstone.Services
{
    public class EmailService
    {
        private readonly string _mailHogHost;
        private readonly int _mailHogPort;
        private string _manualMailHogHost;
        
        // Gmail SMTP Configuration (not readonly so we can re-read from Preferences)
        private bool _useGmail;
        private string _gmailAddress;
        private string _gmailAppPassword;

        public EmailService(string mailHogHost = null, int mailHogPort = 1025)
        {
            _mailHogHost = mailHogHost ?? NetworkConfigurationService.GetEmailHost();
            _mailHogPort = mailHogPort;
            
            // Check if Gmail is configured (from Preferences or environment)
            _useGmail = Preferences.Get("UseGmailSMTP", false);
            _gmailAddress = Preferences.Get("GmailAddress", string.Empty);
            _gmailAppPassword = Preferences.Get("GmailAppPassword", string.Empty);
            
            // Debug logging
            System.Diagnostics.Debug.WriteLine($"📧 EmailService initialized:");
            System.Diagnostics.Debug.WriteLine($"   UseGmail: {_useGmail}");
            System.Diagnostics.Debug.WriteLine($"   GmailAddress: {(_useGmail && !string.IsNullOrEmpty(_gmailAddress) ? _gmailAddress : "Not set")}");
            System.Diagnostics.Debug.WriteLine($"   AppPassword: {(_useGmail && !string.IsNullOrEmpty(_gmailAppPassword) ? "***SET***" : "Not set")}");
        }

        /// <summary>
        /// Configure Gmail SMTP settings
        /// </summary>
        public static void ConfigureGmail(string gmailAddress, string appPassword)
        {
            Preferences.Set("UseGmailSMTP", true);
            Preferences.Set("GmailAddress", gmailAddress);
            Preferences.Set("GmailAppPassword", appPassword);
        }

        /// <summary>
        /// Disable Gmail and use MailHog instead
        /// </summary>
        public static void UseMailHog()
        {
            Preferences.Set("UseGmailSMTP", false);
        }

        private SmtpClient CreateSmtpClient()
        {
            if (_useGmail && !string.IsNullOrWhiteSpace(_gmailAddress) && !string.IsNullOrWhiteSpace(_gmailAppPassword))
            {
                // Remove spaces from App Password if present (Gmail App Passwords sometimes have spaces)
                var cleanPassword = _gmailAppPassword.Replace(" ", "");
                
                // Use Gmail SMTP
                var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_gmailAddress, cleanPassword),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 30000 // Increased timeout for Gmail
                };
                System.Diagnostics.Debug.WriteLine($"📧 Using Gmail SMTP for email sending to: {_gmailAddress}");
                return client;
            }
            else
            {
                // Use MailHog for development
                var host = _manualMailHogHost ?? _mailHogHost ?? "localhost";
                var client = new SmtpClient(host, _mailHogPort)
                {
                    EnableSsl = false,
                    UseDefaultCredentials = false
                };
                System.Diagnostics.Debug.WriteLine($"📧 Using MailHog for email sending: {host}:{_mailHogPort}");
                if (!_useGmail)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Gmail not configured - UseGmail: {_useGmail}, Address: {(_gmailAddress ?? "null")}, Password: {(!string.IsNullOrWhiteSpace(_gmailAppPassword) ? "SET" : "NOT SET")}");
                }
                return client;
            }
        }

        private MailAddress GetFromAddress()
        {
            if (_useGmail && !string.IsNullOrWhiteSpace(_gmailAddress))
            {
                return new MailAddress(_gmailAddress, "Coftea System");
            }
            return new MailAddress("noreply@coftea.com", "Coftea System");
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string resetToken)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Attempting to send password reset email to: {email}");
                
                // Check internet connectivity
                if (!NetworkService.HasInternetConnection())
                {
                    System.Diagnostics.Debug.WriteLine($"❌ No internet connection - cannot send email");
                    return false;
                }
                
                // Re-read Preferences to ensure we have latest Gmail config
                _useGmail = Preferences.Get("UseGmailSMTP", false);
                _gmailAddress = Preferences.Get("GmailAddress", string.Empty);
                _gmailAppPassword = Preferences.Get("GmailAppPassword", string.Empty);
                
                // Validate Gmail configuration if using Gmail
                if (_useGmail && (string.IsNullOrWhiteSpace(_gmailAddress) || string.IsNullOrWhiteSpace(_gmailAppPassword)))
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Gmail is enabled but credentials are missing!");
                    return false;
                }
                
                using var client = CreateSmtpClient();
                
                var htmlBody = CreatePasswordResetEmailBodyWithCode(resetToken);
                var plainTextBody = CreatePasswordResetEmailPlainTextWithCode(resetToken);

                var message = new MailMessage
                {
                    From = GetFromAddress(),
                    Subject = "Password Reset Request - Coftea",
                    Body = htmlBody,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, "text/html");
                var plainTextView = AlternateView.CreateAlternateViewFromString(plainTextBody, Encoding.UTF8, "text/plain");
                message.AlternateViews.Add(htmlView);
                message.AlternateViews.Add(plainTextView);

                message.To.Add(email);

                System.Diagnostics.Debug.WriteLine($"Sending email to {email}...");
                await client.SendMailAsync(message);
                System.Diagnostics.Debug.WriteLine("✅ Email sent successfully!");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to send email: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> SendRegistrationSuccessEmailAsync(string email, string firstName, string lastName, bool isAdmin = false)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📧 ===== Starting email send process =====");
                System.Diagnostics.Debug.WriteLine($"   Recipient: {email}");
                System.Diagnostics.Debug.WriteLine($"   Name: {firstName} {lastName}");
                System.Diagnostics.Debug.WriteLine($"   IsAdmin: {isAdmin}");
                
                // Check internet connectivity
                if (!NetworkService.HasInternetConnection())
                {
                    System.Diagnostics.Debug.WriteLine($"❌ No internet connection - cannot send email");
                    return false;
                }
                
                // Re-read Preferences to ensure we have latest Gmail config
                _useGmail = Preferences.Get("UseGmailSMTP", false);
                _gmailAddress = Preferences.Get("GmailAddress", string.Empty);
                _gmailAppPassword = Preferences.Get("GmailAppPassword", string.Empty);
                
                System.Diagnostics.Debug.WriteLine($"   Gmail Config - UseGmail: {_useGmail}, Address: {(_gmailAddress ?? "null")}, Password: {(!string.IsNullOrWhiteSpace(_gmailAppPassword) ? "SET" : "NOT SET")}");
                
                // Validate Gmail configuration if using Gmail
                if (_useGmail && (string.IsNullOrWhiteSpace(_gmailAddress) || string.IsNullOrWhiteSpace(_gmailAppPassword)))
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Gmail is enabled but credentials are missing!");
                    return false;
                }
                
                // Create SMTP client after updating instance variables
                using var client = CreateSmtpClient();
                
                var fullName = $"{firstName} {lastName}".Trim();
                var htmlBody = CreateRegistrationSuccessEmailBody(fullName, isAdmin);
                var plainTextBody = CreateRegistrationSuccessEmailPlainText(fullName, isAdmin);

                var fromAddress = GetFromAddress();
                System.Diagnostics.Debug.WriteLine($"   From Address: {fromAddress.Address} ({fromAddress.DisplayName})");

                var message = new MailMessage
                {
                    From = fromAddress,
                    Subject = "Welcome to Coftea Management System",
                    Body = htmlBody,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, "text/html");
                var plainTextView = AlternateView.CreateAlternateViewFromString(plainTextBody, Encoding.UTF8, "text/plain");
                message.AlternateViews.Add(htmlView);
                message.AlternateViews.Add(plainTextView);

                message.To.Add(email);
                System.Diagnostics.Debug.WriteLine($"   To Address: {email}");

                System.Diagnostics.Debug.WriteLine($"📤 Sending email via SMTP...");
                await client.SendMailAsync(message);
                System.Diagnostics.Debug.WriteLine($"✅ Registration email sent successfully to {email}!");
                System.Diagnostics.Debug.WriteLine($"📧 ===== Email send process completed =====");
                return true;
            }
            catch (SmtpException smtpEx)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SMTP Error sending registration email:");
                System.Diagnostics.Debug.WriteLine($"   Status Code: {smtpEx.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"   Message: {smtpEx.Message}");
                if (smtpEx.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"   Inner: {smtpEx.InnerException.Message}");
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to send registration email:");
                System.Diagnostics.Debug.WriteLine($"   Exception type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"   Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"   Inner exception: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
                }
                return false;
            }
        }

        public async Task<bool> SendEmailVerificationAsync(string email, string firstName, string verificationCode)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Attempting to send email verification to: {email}");
                
                // Check internet connectivity
                if (!NetworkService.HasInternetConnection())
                {
                    System.Diagnostics.Debug.WriteLine($"❌ No internet connection - cannot send email");
                    return false;
                }
                
                // Re-read Preferences to ensure we have latest Gmail config
                _useGmail = Preferences.Get("UseGmailSMTP", false);
                _gmailAddress = Preferences.Get("GmailAddress", string.Empty);
                _gmailAppPassword = Preferences.Get("GmailAppPassword", string.Empty);
                
                // Validate Gmail configuration if using Gmail
                if (_useGmail && (string.IsNullOrWhiteSpace(_gmailAddress) || string.IsNullOrWhiteSpace(_gmailAppPassword)))
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Gmail is enabled but credentials are missing!");
                    return false;
                }
                
                using var client = CreateSmtpClient();
                
                var htmlBody = CreateEmailVerificationBody(firstName, verificationCode);
                var plainTextBody = CreateEmailVerificationPlainText(firstName, verificationCode);

                var message = new MailMessage
                {
                    From = GetFromAddress(),
                    Subject = "Verify Your Email - Coftea",
                    Body = htmlBody,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, "text/html");
                var plainTextView = AlternateView.CreateAlternateViewFromString(plainTextBody, Encoding.UTF8, "text/plain");
                message.AlternateViews.Add(htmlView);
                message.AlternateViews.Add(plainTextView);

                message.To.Add(email);

                System.Diagnostics.Debug.WriteLine($"Sending verification email to {email}...");
                await client.SendMailAsync(message);
                System.Diagnostics.Debug.WriteLine("✅ Verification email sent successfully!");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to send verification email: {ex.Message}");
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
            return _manualMailHogHost ?? NetworkConfigurationService.GetEmailHost() ?? _mailHogHost;
        }

        // Legacy method names for backward compatibility
        public void SetManualEmailHost(string host) => SetManualMailHogHost(host);
        public void ClearManualEmailHost() => ClearManualMailHogHost();
        public string GetCurrentEmailHost() => GetCurrentMailHogHost();

        // ===================== Email Templates =====================

        private string CreatePasswordResetEmailPlainTextWithCode(string code)
        {
            return $@"
Coftea Password Reset Request

You have requested to reset your password for your Coftea account.

Your verification code: {code}

This code will expire in 1 hour for security reasons.

If you did not request this password reset, please ignore this email.

Coftea Management System
";
        }

        private string CreatePasswordResetEmailBodyWithCode(string code)
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
        .code-box {{ font-size: 28px; font-weight: bold; letter-spacing: 6px; background-color: #fff; display: inline-block; padding: 15px 20px; border-radius: 8px; border: 2px solid #5B4F45; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
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
            <p>Enter this verification code in the app to reset your password:</p>
            <div style='text-align: center;'>
                <div class='code-box'>{code}</div>
            </div>
            <p><strong>This code will expire in 1 hour for security reasons.</strong></p>
            <p>If you did not request this password reset, please ignore this email.</p>
        </div>
        <div class='footer'>
            <p>Coftea Management System</p>
        </div>
    </div>
</body>
</html>";
        }

        private string CreateRegistrationSuccessEmailPlainText(string fullName, bool isAdmin)
        {
            var adminNote = isAdmin ? "\n\nYou have been granted administrator privileges as the first user." : "\n\nYour account is pending admin approval. You will be notified once your account is approved.";
            
            return $@"
Welcome to Coftea Management System!

Dear {fullName},

Thank you for registering with Coftea Management System!{adminNote}

You can now log in using your registered email and password.

If you have any questions, please contact your system administrator.

Best regards,
Coftea Management System
";
        }

        private string CreateRegistrationSuccessEmailBody(string fullName, bool isAdmin)
        {
            var adminNote = isAdmin 
                ? "<p style='background-color: #d4edda; padding: 15px; border-radius: 5px; border-left: 4px solid #28a745;'><strong>Administrator Access:</strong> You have been granted administrator privileges as the first user.</p>"
                : "<p style='background-color: #fff3cd; padding: 15px; border-radius: 5px; border-left: 4px solid #ffc107;'><strong>Pending Approval:</strong> Your account is pending admin approval. You will be notified once your account is approved.</p>";
            
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Welcome to Coftea</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #5B4F45; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to Coftea!</h1>
        </div>
        <div class='content'>
            <h2>Registration Successful</h2>
            <p>Dear {fullName},</p>
            <p>Thank you for registering with Coftea Management System!</p>
            {adminNote}
            <p>You can now log in using your registered email and password.</p>
            <p>If you have any questions, please contact your system administrator.</p>
        </div>
        <div class='footer'>
            <p>Best regards,<br>Coftea Management System</p>
        </div>
    </div>
</body>
</html>";
        }

        private string CreateEmailVerificationPlainText(string firstName, string code)
        {
            return $@"
Email Verification - Coftea

Dear {firstName},

Please verify your email address by entering the verification code below:

Your verification code: {code}

This code will expire in 1 hour for security reasons.

If you did not request this verification, please ignore this email.

Coftea Management System
";
        }

        private string CreateEmailVerificationBody(string firstName, string code)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Verify Your Email - Coftea</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #5B4F45; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .code-box {{ font-size: 28px; font-weight: bold; letter-spacing: 6px; background-color: #fff; display: inline-block; padding: 15px 20px; border-radius: 8px; border: 2px solid #5B4F45; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Verify Your Email</h1>
        </div>
        <div class='content'>
            <h2>Email Verification</h2>
            <p>Dear {firstName},</p>
            <p>Please verify your email address by entering the verification code below:</p>
            <div style='text-align: center;'>
                <div class='code-box'>{code}</div>
            </div>
            <p><strong>This code will expire in 1 hour for security reasons.</strong></p>
            <p>If you did not request this verification, please ignore this email.</p>
        </div>
        <div class='footer'>
            <p>Coftea Management System</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
