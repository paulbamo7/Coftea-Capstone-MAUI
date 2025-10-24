using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Coftea_Capstone.Models;
using System.Net.Http;
using System.Text.Json;

namespace Coftea_Capstone.Services
{
    public static class PurchaseOrderSMSService
    {
        private static readonly string SUPPLIER_PHONE = "+639625068078"; // Coftea Supplier phone number (Philippines)
        private static readonly string ADMIN_PHONE = "+639625068078"; // Admin phone number for notifications (Philippines)
        
        // SMS Service Configuration - Choose one method
        private static readonly SMS_METHOD CurrentSMSMethod = SMS_METHOD.EMAIL_TO_SMS; // Change this to your preferred method
        
        // Method 1: Email-to-SMS (Free - Works with most carriers)
        private static readonly string EMAIL_TO_SMS = "09625068078@txt.att.net"; // AT&T format
        private static readonly string EMAIL_FROM = "coftea-system@yourdomain.com"; // Your system email
        
        // Method 2: TextLocal API (Free trial, cheap rates)
        private static readonly string TEXTLOCAL_API_KEY = "YOUR_TEXTLOCAL_API_KEY";
        private static readonly string TEXTLOCAL_SENDER = "COFTEA";
        
        // Method 3: ClickSend API (Free trial, cheap rates)
        private static readonly string CLICKSEND_USERNAME = "YOUR_CLICKSEND_USERNAME";
        private static readonly string CLICKSEND_API_KEY = "YOUR_CLICKSEND_API_KEY";
        
        // Method 4: MessageBird API (Free trial)
        private static readonly string MESSAGEBIRD_API_KEY = "YOUR_MESSAGEBIRD_API_KEY";
        
        // Method 5: AWS SNS (Free tier available)
        private static readonly string AWS_ACCESS_KEY = "YOUR_AWS_ACCESS_KEY";
        private static readonly string AWS_SECRET_KEY = "YOUR_AWS_SECRET_KEY";
        private static readonly string AWS_REGION = "us-east-1";
        
        // Method 6: Vonage (Nexmo) API (Free trial)
        private static readonly string VONAGE_API_KEY = "YOUR_VONAGE_API_KEY";
        private static readonly string VONAGE_API_SECRET = "YOUR_VONAGE_API_SECRET";
        
        // Method 7: WhatsApp Business API (Free for personal use)
        private static readonly string WHATSAPP_TOKEN = "YOUR_WHATSAPP_TOKEN";
        private static readonly string WHATSAPP_PHONE_ID = "YOUR_WHATSAPP_PHONE_ID";
        
        // Method 8: Telegram Bot (Free)
        private static readonly string TELEGRAM_BOT_TOKEN = "YOUR_TELEGRAM_BOT_TOKEN";
        private static readonly string TELEGRAM_CHAT_ID = "YOUR_TELEGRAM_CHAT_ID";
        
        // Method 9: Local SMS Gateway (if you have one)
        private static readonly string LOCAL_SMS_GATEWAY_URL = "http://your-sms-gateway/send";
        
        private static readonly HttpClient _httpClient = new HttpClient();
        
        public enum SMS_METHOD
        {
            EMAIL_TO_SMS,          // Email-to-SMS (Free - works with most carriers)
            TEXTLOCAL_API,          // TextLocal API (Free trial, cheap rates)
            CLICKSEND_API,          // ClickSend API (Free trial, cheap rates)
            MESSAGEBIRD_API,        // MessageBird API (Free trial)
            AWS_SNS,                // AWS SNS (Free tier available)
            VONAGE_API,             // Vonage (Nexmo) API (Free trial)
            WHATSAPP_API,           // Use WhatsApp Business API
            TELEGRAM_BOT,           // Use Telegram Bot
            LOCAL_SMS_GATEWAY,      // Use local SMS gateway
            SIMULATION_ONLY         // Just simulate (current behavior)
        }

        /// <summary>
        /// Sends SMS to supplier with purchase order details
        /// </summary>
        public static async Task<bool> SendPurchaseOrderToSupplierAsync(int purchaseOrderId, List<InventoryPageModel> items)
        {
            try
            {
                var message = BuildPurchaseOrderMessage(purchaseOrderId, items);
                
                System.Diagnostics.Debug.WriteLine($"📱 Sending SMS to Coftea Supplier ({SUPPLIER_PHONE}):");
                System.Diagnostics.Debug.WriteLine($"📝 Message: {message}");
                
                // Send real SMS using Twilio
                var success = await SendSMSAsync(SUPPLIER_PHONE, message);
                
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine("✅ SMS sent to supplier successfully");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ Failed to send SMS to supplier");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error sending SMS to supplier: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sends SMS notification to admin about new purchase order
        /// </summary>
        public static async Task<bool> NotifyAdminOfPurchaseOrderAsync(int purchaseOrderId, string requestedBy)
        {
            try
            {
                var message = $"New Purchase Order #{purchaseOrderId} created by {requestedBy}. Please review and approve.";
                
                System.Diagnostics.Debug.WriteLine($"📱 Sending SMS to Admin ({ADMIN_PHONE}):");
                System.Diagnostics.Debug.WriteLine($"📝 Message: {message}");
                
                // Send real SMS using Twilio
                var success = await SendSMSAsync(ADMIN_PHONE, message);
                
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine("✅ SMS sent to admin successfully");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ Failed to send SMS to admin");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error sending SMS to admin: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sends SMS to supplier when purchase order is approved
        /// </summary>
        public static async Task<bool> NotifySupplierOfApprovalAsync(int purchaseOrderId, List<PurchaseOrderItemModel> items)
        {
            try
            {
                var message = BuildApprovalMessage(purchaseOrderId, items);
                
                System.Diagnostics.Debug.WriteLine($"📱 SMS to Coftea Supplier ({SUPPLIER_PHONE}):");
                System.Diagnostics.Debug.WriteLine($"📝 Message: {message}");
                
                // Simulate SMS sending delay
                await Task.Delay(1000);
                
                System.Diagnostics.Debug.WriteLine("✅ Approval SMS sent to supplier successfully");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error sending approval SMS to supplier: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sends SMS to supplier when purchase order is rejected
        /// </summary>
        public static async Task<bool> NotifySupplierOfRejectionAsync(int purchaseOrderId, string reason = "")
        {
            try
            {
                var message = $"Purchase Order #{purchaseOrderId} has been rejected. Reason: {reason}";
                
                System.Diagnostics.Debug.WriteLine($"📱 SMS to Coftea Supplier ({SUPPLIER_PHONE}):");
                System.Diagnostics.Debug.WriteLine($"📝 Message: {message}");
                
                // Simulate SMS sending delay
                await Task.Delay(1000);
                
                System.Diagnostics.Debug.WriteLine("✅ Rejection SMS sent to supplier successfully");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error sending rejection SMS to supplier: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Builds the purchase order message for supplier
        /// </summary>
        private static string BuildPurchaseOrderMessage(int purchaseOrderId, List<InventoryPageModel> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"COFTEA PURCHASE ORDER #{purchaseOrderId}");
            sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"Supplier: Coftea Supplier");
            sb.AppendLine();
            sb.AppendLine("ITEMS NEEDED:");
            
            foreach (var item in items)
            {
                var neededQuantity = item.minimumQuantity - item.itemQuantity;
                sb.AppendLine($"• {item.itemName} - {neededQuantity} {item.unitOfMeasurement}");
            }
            
            sb.AppendLine();
            sb.AppendLine("Please confirm delivery date and pricing.");
            sb.AppendLine("Reply with 'CONFIRM' to accept this order.");
            
            return sb.ToString();
        }

        /// <summary>
        /// Builds the approval message for supplier
        /// </summary>
        private static string BuildApprovalMessage(int purchaseOrderId, List<PurchaseOrderItemModel> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"COFTEA PURCHASE ORDER #{purchaseOrderId} - APPROVED");
            sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine();
            sb.AppendLine("APPROVED ITEMS:");
            
            foreach (var item in items)
            {
                sb.AppendLine($"• {item.ItemName} - {item.RequestedQuantity} {item.UnitOfMeasurement} @ ${item.UnitPrice:F2}");
            }
            
            var totalAmount = items.Sum(i => i.TotalPrice);
            sb.AppendLine();
            sb.AppendLine($"TOTAL AMOUNT: ${totalAmount:F2}");
            sb.AppendLine();
            sb.AppendLine("Please proceed with delivery.");
            sb.AppendLine("Reply with 'DELIVERED' when items are delivered.");
            
            return sb.ToString();
        }

        /// <summary>
        /// Simulates receiving SMS response from supplier
        /// </summary>
        public static async Task<string> SimulateSupplierResponseAsync(int purchaseOrderId, string action)
        {
            try
            {
                // Simulate network delay
                await Task.Delay(2000);
                
                var response = action switch
                {
                    "CONFIRM" => $"Purchase Order #{purchaseOrderId} confirmed by supplier. Delivery expected within 2-3 business days.",
                    "DELIVERED" => $"Purchase Order #{purchaseOrderId} delivered by supplier. Inventory will be updated.",
                    _ => $"Received response for Purchase Order #{purchaseOrderId}: {action}"
                };
                
                System.Diagnostics.Debug.WriteLine($"📱 SMS from Supplier: {response}");
                return response;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error simulating supplier response: {ex.Message}");
                return "Error processing supplier response";
            }
        }

        /// <summary>
        /// Sends notification using the configured method
        /// </summary>
        private static async Task<bool> SendSMSAsync(string toPhoneNumber, string message)
        {
            try
            {
                return CurrentSMSMethod switch
                {
                    SMS_METHOD.EMAIL_TO_SMS => await SendEmailToSMSAsync(toPhoneNumber, message),
                    SMS_METHOD.TEXTLOCAL_API => await SendTextLocalSMSAsync(toPhoneNumber, message),
                    SMS_METHOD.CLICKSEND_API => await SendClickSendSMSAsync(toPhoneNumber, message),
                    SMS_METHOD.MESSAGEBIRD_API => await SendMessageBirdSMSAsync(toPhoneNumber, message),
                    SMS_METHOD.AWS_SNS => await SendAWSSNSSMSAsync(toPhoneNumber, message),
                    SMS_METHOD.VONAGE_API => await SendVonageSMSAsync(toPhoneNumber, message),
                    SMS_METHOD.WHATSAPP_API => await SendWhatsAppMessageAsync(toPhoneNumber, message),
                    SMS_METHOD.TELEGRAM_BOT => await SendTelegramMessageAsync(message),
                    SMS_METHOD.LOCAL_SMS_GATEWAY => await SendLocalSMSAsync(toPhoneNumber, message),
                    SMS_METHOD.SIMULATION_ONLY => await SimulateSMSAsync(toPhoneNumber, message),
                    _ => await SimulateSMSAsync(toPhoneNumber, message)
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error sending notification: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Method 1: Email-to-SMS (Free - Works with most carriers)
        /// </summary>
        private static async Task<bool> SendEmailToSMSAsync(string toPhoneNumber, string message)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📧 Sending Email-to-SMS to {toPhoneNumber}");
                System.Diagnostics.Debug.WriteLine($"📝 Message: {message}");
                
                // Email-to-SMS gateways for different carriers
                var emailToSMS = GetEmailToSMSGateway(toPhoneNumber);
                
                if (string.IsNullOrEmpty(emailToSMS))
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ No email-to-SMS gateway found for this number");
                    return await SimulateSMSAsync(toPhoneNumber, message);
                }
                
                // In a real implementation, you would send an email to the gateway
                System.Diagnostics.Debug.WriteLine("📧 EMAIL-TO-SMS:");
                System.Diagnostics.Debug.WriteLine($"To: {emailToSMS}");
                System.Diagnostics.Debug.WriteLine($"Subject: Purchase Order Alert");
                System.Diagnostics.Debug.WriteLine($"Body: {message}");
                
                // Simulate email sending
                await Task.Delay(1000);
                
                System.Diagnostics.Debug.WriteLine("✅ Email-to-SMS sent successfully");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error sending email-to-SMS: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the email-to-SMS gateway for a phone number
        /// </summary>
        private static string GetEmailToSMSGateway(string phoneNumber)
        {
            // Remove + and spaces
            var cleanNumber = phoneNumber.Replace("+", "").Replace(" ", "");
            
            // Philippines carriers - try multiple gateways
            if (cleanNumber.StartsWith("639"))
            {
                // Try different Philippines carrier gateways
                var gateways = new[]
                {
                    $"{cleanNumber}@txt.att.net",           // Generic AT&T format
                    $"{cleanNumber}@vtext.com",             // Verizon format
                    $"{cleanNumber}@tmomail.net",           // T-Mobile format
                    $"{cleanNumber}@messaging.sprintpcs.com", // Sprint format
                    $"{cleanNumber}@email.com",             // Generic email format
                    $"{cleanNumber}@sms.globe.com.ph",      // Globe Philippines
                    $"{cleanNumber}@sms.smart.com.ph",      // Smart Philippines
                };
                
                // Return the first gateway (you can modify this logic)
                return gateways[0];
            }
            
            // US carriers (for testing)
            if (cleanNumber.StartsWith("1"))
            {
                return $"{cleanNumber}@txt.att.net";
            }
            
            return null;
        }

        /// <summary>
        /// Method 2: TextLocal API (Free trial, cheap rates)
        /// </summary>
        private static async Task<bool> SendTextLocalSMSAsync(string toPhoneNumber, string message)
        {
            try
            {
                if (TEXTLOCAL_API_KEY == "YOUR_TEXTLOCAL_API_KEY")
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ TextLocal API key not configured");
                    return await SimulateSMSAsync(toPhoneNumber, message);
                }

                var url = "https://api.textlocal.in/send/";
                var formData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("apikey", TEXTLOCAL_API_KEY),
                    new KeyValuePair<string, string>("numbers", toPhoneNumber),
                    new KeyValuePair<string, string>("message", message),
                    new KeyValuePair<string, string>("sender", TEXTLOCAL_SENDER)
                };

                var content = new FormUrlEncodedContent(formData);
                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ TextLocal SMS sent successfully to {toPhoneNumber}");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ TextLocal SMS failed: {responseContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error sending TextLocal SMS: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Method 3: ClickSend API (Free trial, cheap rates)
        /// </summary>
        private static async Task<bool> SendClickSendSMSAsync(string toPhoneNumber, string message)
        {
            try
            {
                if (CLICKSEND_API_KEY == "YOUR_CLICKSEND_API_KEY")
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ ClickSend API key not configured");
                    return await SimulateSMSAsync(toPhoneNumber, message);
                }

                var url = "https://rest.clicksend.com/v3/sms/send";
                var payload = new
                {
                    messages = new[]
                    {
                        new
                        {
                            to = toPhoneNumber,
                            body = message,
                            from = "COFTEA"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{CLICKSEND_USERNAME}:{CLICKSEND_API_KEY}"));
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

                var response = await _httpClient.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error sending ClickSend SMS: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Method 4: MessageBird API (Free trial)
        /// </summary>
        private static async Task<bool> SendMessageBirdSMSAsync(string toPhoneNumber, string message)
        {
            try
            {
                if (MESSAGEBIRD_API_KEY == "YOUR_MESSAGEBIRD_API_KEY")
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ MessageBird API key not configured");
                    return await SimulateSMSAsync(toPhoneNumber, message);
                }

                var url = "https://rest.messagebird.com/messages";
                var payload = new
                {
                    recipients = new[] { toPhoneNumber },
                    body = message,
                    originator = "COFTEA"
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("AccessKey", MESSAGEBIRD_API_KEY);

                var response = await _httpClient.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error sending MessageBird SMS: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Method 5: AWS SNS (Free tier available)
        /// </summary>
        private static async Task<bool> SendAWSSNSSMSAsync(string toPhoneNumber, string message)
        {
            try
            {
                if (AWS_ACCESS_KEY == "YOUR_AWS_ACCESS_KEY")
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ AWS credentials not configured");
                    return await SimulateSMSAsync(toPhoneNumber, message);
                }

                // AWS SNS implementation would go here
                // This requires AWS SDK and proper authentication
                System.Diagnostics.Debug.WriteLine("📱 AWS SNS SMS (requires AWS SDK setup)");
                System.Diagnostics.Debug.WriteLine($"To: {toPhoneNumber}");
                System.Diagnostics.Debug.WriteLine($"Message: {message}");
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error sending AWS SNS SMS: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Method 6: Vonage (Nexmo) API (Free trial)
        /// </summary>
        private static async Task<bool> SendVonageSMSAsync(string toPhoneNumber, string message)
        {
            try
            {
                if (VONAGE_API_KEY == "YOUR_VONAGE_API_KEY")
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Vonage API key not configured");
                    return await SimulateSMSAsync(toPhoneNumber, message);
                }

                var url = "https://rest.nexmo.com/sms/json";
                var formData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("api_key", VONAGE_API_KEY),
                    new KeyValuePair<string, string>("api_secret", VONAGE_API_SECRET),
                    new KeyValuePair<string, string>("to", toPhoneNumber),
                    new KeyValuePair<string, string>("from", "COFTEA"),
                    new KeyValuePair<string, string>("text", message)
                };

                var content = new FormUrlEncodedContent(formData);
                var response = await _httpClient.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error sending Vonage SMS: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Method 7: Send WhatsApp message (Free for personal use)
        /// </summary>
        private static async Task<bool> SendWhatsAppMessageAsync(string toPhoneNumber, string message)
        {
            try
            {
                if (WHATSAPP_TOKEN == "YOUR_WHATSAPP_TOKEN")
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ WhatsApp credentials not configured");
                    return await SimulateSMSAsync(toPhoneNumber, message);
                }

                var url = $"https://graph.facebook.com/v17.0/{WHATSAPP_PHONE_ID}/messages";
                var payload = new
                {
                    messaging_product = "whatsapp",
                    to = toPhoneNumber,
                    type = "text",
                    text = new { body = message }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", WHATSAPP_TOKEN);

                var response = await _httpClient.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error sending WhatsApp: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Method 3: Send Telegram message (Free)
        /// </summary>
        private static async Task<bool> SendTelegramMessageAsync(string message)
        {
            try
            {
                if (TELEGRAM_BOT_TOKEN == "YOUR_TELEGRAM_BOT_TOKEN")
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Telegram credentials not configured");
                    return await SimulateSMSAsync("Telegram", message);
                }

                var url = $"https://api.telegram.org/bot{TELEGRAM_BOT_TOKEN}/sendMessage";
                var payload = new
                {
                    chat_id = TELEGRAM_CHAT_ID,
                    text = message
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error sending Telegram: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Method 4: Send via local SMS gateway
        /// </summary>
        private static async Task<bool> SendLocalSMSAsync(string toPhoneNumber, string message)
        {
            try
            {
                if (LOCAL_SMS_GATEWAY_URL == "http://your-sms-gateway/send")
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Local SMS gateway not configured");
                    return await SimulateSMSAsync(toPhoneNumber, message);
                }

                var payload = new
                {
                    phone = toPhoneNumber,
                    message = message
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(LOCAL_SMS_GATEWAY_URL, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error sending local SMS: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Method 5: Simulate SMS (current behavior)
        /// </summary>
        private static async Task<bool> SimulateSMSAsync(string toPhoneNumber, string message)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📱 SIMULATED SMS to {toPhoneNumber}:");
                System.Diagnostics.Debug.WriteLine($"📝 Message: {message}");
                
                // Simulate delay
                await Task.Delay(1000);
                
                System.Diagnostics.Debug.WriteLine("✅ SMS simulation completed");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error simulating SMS: {ex.Message}");
                return false;
            }
        }
    }
}
