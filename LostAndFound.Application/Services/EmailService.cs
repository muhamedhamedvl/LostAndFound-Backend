using LostAndFound.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MailKit.Net.Smtp;
using System.Text;
using System.Text.RegularExpressions;

namespace LostAndFound.Application.Services
{
    /// <summary>
    /// Production-ready email service with improved deliverability and Gmail compatibility.
    /// Implements best practices to avoid phishing warnings and maximize email deliverability.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        
        // Retry configuration for SMTP throttling
        private const int MaxRetryAttempts = 3;
        private const int RetryDelayMs = 2000; 

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Sends an email with improved headers and SMTP configuration for better deliverability.
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body (HTML or plain text)</param>
        /// <param name="isHtml">Whether the body is HTML format</param>
        /// <returns>True if email sent successfully, false otherwise</returns>
        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");
                var smtpServer = emailSettings["Host"] ?? emailSettings["SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(emailSettings["Port"] ?? emailSettings["SmtpPort"] ?? "587");
                var smtpUsername = emailSettings["From"] ?? emailSettings["SmtpUsername"];
                var smtpPassword = emailSettings["Password"] ?? emailSettings["SmtpPassword"];
                var fromEmail = emailSettings["From"] ?? emailSettings["FromEmail"];
                var fromName = emailSettings["FromName"] ?? "Lost & Found App";
                if (string.IsNullOrWhiteSpace(smtpServer) || string.IsNullOrWhiteSpace(smtpUsername) || 
                    string.IsNullOrWhiteSpace(smtpPassword) || string.IsNullOrWhiteSpace(fromEmail))
                {
                    Console.WriteLine("[ERROR] Email configuration is incomplete. Missing required SMTP settings.");
                    return false;
                }

                var message = new MimeMessage();
                
                message.From.Add(new MailboxAddress(Encoding.UTF8, fromName, fromEmail));
                
                message.To.Add(new MailboxAddress(Encoding.UTF8, "", to));
                
                message.ReplyTo.Add(new MailboxAddress(Encoding.UTF8, fromName, fromEmail));
                
                message.Subject = subject;
                message.Headers.Add("X-Mailer", "LostAndFoundMailer");
                message.Headers.Add("X-Entity-Ref-ID", Guid.NewGuid().ToString());
                message.Headers.Add("MIME-Version", "1.0");
                
                var unsubscribeEmail = $"mailto:{fromEmail}?subject=Unsubscribe";
                message.Headers.Add("List-Unsubscribe", unsubscribeEmail);
                message.Headers.Add("List-Unsubscribe-Post", "List-Unsubscribe=One-Click");

                var bodyBuilder = new BodyBuilder();
                
                if (isHtml)
                {
                    bodyBuilder.HtmlBody = body;
                    bodyBuilder.TextBody = ConvertHtmlToPlainText(body);
                }
                else
                {
                    bodyBuilder.TextBody = body;
                    bodyBuilder.HtmlBody = null;
                }
                
                message.Body = bodyBuilder.ToMessageBody();

                Console.WriteLine($"[EMAIL] Attempting to send email to: {to}");
                Console.WriteLine($"[EMAIL] SMTP Server: {smtpServer}:{smtpPort}");
                Console.WriteLine($"[EMAIL] From: {fromName} <{fromEmail}>");

                return await SendEmailWithRetryAsync(message, smtpServer, smtpPort, smtpUsername, smtpPassword, to);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Email sending failed: {ex.Message}");
                Console.WriteLine($"[ERROR] Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"[ERROR] Stack Trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[ERROR] Inner Exception: {ex.InnerException.Message}");
                }
                
                return false;
            }
        }
        /// <summary>
        /// Sends email with retry logic to handle SMTP server throttling.
        /// </summary>
        private async Task<bool> SendEmailWithRetryAsync(
            MimeMessage message, 
            string smtpServer, 
            int smtpPort, 
            string smtpUsername, 
            string smtpPassword, 
            string recipientEmail)
        {
            Exception? lastException = null;

            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                try
                {
                    Console.WriteLine($"[EMAIL] Attempt {attempt} of {MaxRetryAttempts}");

                    using var client = new SmtpClient();
                    var secureSocketOptions = smtpPort == 465 
                        ? MailKit.Security.SecureSocketOptions.SslOnConnect 
                        : MailKit.Security.SecureSocketOptions.StartTls;
                    await client.ConnectAsync(smtpServer, smtpPort, secureSocketOptions);
                    await client.AuthenticateAsync(smtpUsername, smtpPassword);
                    
                    // Send the message
                    await client.SendAsync(message);
                    
                    // Disconnect gracefully
                    await client.DisconnectAsync(true);

                    Console.WriteLine($"[EMAIL] Email sent successfully to: {recipientEmail}");
                    return true;
                }
                catch (SmtpCommandException ex) when (ex.StatusCode == SmtpStatusCode.ServiceNotAvailable || 
                                                      ex.Message.Contains("throttle", StringComparison.OrdinalIgnoreCase) ||
                                                      ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
                                                      ex.Message.Contains("temporarily unavailable", StringComparison.OrdinalIgnoreCase))
                {
                    // SMTP server is throttling or temporarily unavailable
                    lastException = ex;
                    Console.WriteLine($"[EMAIL] SMTP server throttling detected (attempt {attempt}/{MaxRetryAttempts}): {ex.Message}");
                    
                    if (attempt < MaxRetryAttempts)
                    {
                        var delay = RetryDelayMs * attempt;
                        Console.WriteLine($"[EMAIL] Waiting {delay}ms before retry...");
                        await Task.Delay(delay);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Non-retryable error: {ex.Message}");
                    throw;
                }
            }
            Console.WriteLine($"[ERROR] Failed to send email after {MaxRetryAttempts} attempts. Last error: {lastException?.Message}");
            return false;
        }

        /// <summary>
        /// Converts HTML to plain text for multipart/alternative email format.
        /// Simple conversion that removes HTML tags and preserves basic structure.
        /// </summary>
        private string ConvertHtmlToPlainText(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;
            html = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<h[1-6][^>]*>", "\n\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"</h[1-6]>", "\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<p[^>]*>", "\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"</p>", "\n\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<div[^>]*>", "\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"</div>", "\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<span[^>]*>", "", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"</span>", "", RegexOptions.IgnoreCase);

            html = Regex.Replace(html, @"<[^>]+>", "");

            html = System.Net.WebUtility.HtmlDecode(html);
            html = Regex.Replace(html, @"\n\s*\n\s*\n", "\n\n", RegexOptions.Multiline);
            html = html.Trim();

            return html;
        }

        /// <summary>
        /// Sends a verification code email with improved template and formatting.
        /// Uses a clean, simple design to avoid Gmail phishing warnings.
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="name">Recipient's full name</param>
        /// <param name="verificationCode">6-digit verification code</param>
        /// <returns>True if email sent successfully, false otherwise</returns>
        public async Task<bool> SendVerificationCodeEmailAsync(string to, string name, string verificationCode)
        {
            var subject = "Your Lost & Found Verification Code";
            
            Console.WriteLine("[EMAIL] === VERIFICATION EMAIL ===");
            Console.WriteLine($"[EMAIL] To: {to}");
            Console.WriteLine($"[EMAIL] Name: {name}");
            Console.WriteLine($"[EMAIL] Verification Code: {verificationCode}");
            Console.WriteLine("[EMAIL] ==========================");

            // Get sender email for footer
            var emailSettings = _configuration.GetSection("EmailSettings");
            var contactEmail = emailSettings["From"] ?? emailSettings["FromEmail"] ?? "lost.found2026@gmail.com";

            // Generate HTML email template
            var htmlBody = GenerateVerificationEmailHtml(name, verificationCode, contactEmail);
            
            // Generate plaintext version
            var plainTextBody = GenerateVerificationEmailPlainText(name, verificationCode, contactEmail);

            // Send email with both HTML and plaintext versions
            try
            {
                var emailSettingsSection = _configuration.GetSection("EmailSettings");
                var smtpServer = emailSettingsSection["Host"] ?? emailSettingsSection["SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(emailSettingsSection["Port"] ?? emailSettingsSection["SmtpPort"] ?? "587");
                var smtpUsername = emailSettingsSection["From"] ?? emailSettingsSection["SmtpUsername"];
                var smtpPassword = emailSettingsSection["Password"] ?? emailSettingsSection["SmtpPassword"];
                var fromEmail = emailSettingsSection["From"] ?? emailSettingsSection["FromEmail"];
                var fromName = "Lost & Found App"; // Consistent sender name

                // Validate required settings
                if (string.IsNullOrWhiteSpace(smtpServer) || string.IsNullOrWhiteSpace(smtpUsername) || 
                    string.IsNullOrWhiteSpace(smtpPassword) || string.IsNullOrWhiteSpace(fromEmail))
                {
                    Console.WriteLine("[ERROR] Email configuration is incomplete.");
                    return false;
                }

                // Create MIME message
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(Encoding.UTF8, fromName, fromEmail));
                message.To.Add(new MailboxAddress(Encoding.UTF8, "", to));
                message.ReplyTo.Add(new MailboxAddress(Encoding.UTF8, fromName, fromEmail));
                message.Subject = subject;

                // Add required headers for deliverability
                message.Headers.Add("X-Mailer", "LostAndFoundMailer");
                message.Headers.Add("X-Entity-Ref-ID", Guid.NewGuid().ToString());
                message.Headers.Add("MIME-Version", "1.0");
                
                var unsubscribeEmail = $"mailto:{fromEmail}?subject=Unsubscribe";
                message.Headers.Add("List-Unsubscribe", unsubscribeEmail);
                message.Headers.Add("List-Unsubscribe-Post", "List-Unsubscribe=One-Click");

                // Create multipart/alternative body
                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody,
                    TextBody = plainTextBody
                };
                
                message.Body = bodyBuilder.ToMessageBody();

                Console.WriteLine($"[EMAIL] Sending verification email to: {to}");

                // Send with retry logic
                return await SendEmailWithRetryAsync(message, smtpServer, smtpPort, smtpUsername, smtpPassword, to);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Verification email sending failed: {ex.Message}");
                Console.WriteLine($"[ERROR] Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"[ERROR] Stack Trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[ERROR] Inner Exception: {ex.InnerException.Message}");
                }
                
                return false;
            }
        }

        /// <summary>
        /// Generates the HTML template for verification email.
        /// Uses simple, clean styling to avoid Gmail phishing warnings.
        /// No external images, no URLs, minimal styling.
        /// </summary>
        private string GenerateVerificationEmailHtml(string name, string verificationCode, string contactEmail)
        {
            // Escape HTML to prevent XSS
            var safeName = System.Net.WebUtility.HtmlEncode(name);
            var safeCode = System.Net.WebUtility.HtmlEncode(verificationCode);
            var safeEmail = System.Net.WebUtility.HtmlEncode(contactEmail);

            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"">
    <title>Verification Code</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f5f5f5; color: #333333;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse; background-color: #f5f5f5;"">
        <tr>
            <td style=""padding: 40px 20px;"">
                <table role=""presentation"" style=""max-width: 600px; margin: 0 auto; background-color: #ffffff; border-collapse: collapse; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"">
                    <!-- Header -->
                    <tr>
                        <td style=""padding: 30px 30px 20px 30px; text-align: center; border-bottom: 2px solid #e0e0e0;"">
                            <h1 style=""margin: 0; font-size: 24px; font-weight: 600; color: #333333;"">Lost & Found</h1>
                        </td>
                    </tr>
                    
                    <!-- Main Content -->
                    <tr>
                        <td style=""padding: 30px;"">
                            <p style=""margin: 0 0 20px 0; font-size: 16px; line-height: 1.6; color: #333333;"">Hello {safeName},</p>
                            
                            <p style=""margin: 0 0 20px 0; font-size: 16px; line-height: 1.6; color: #333333;"">Thank you for creating an account with Lost & Found. Please use the verification code below to complete your registration:</p>
                            
                            <!-- Verification Code Box -->
                            <table role=""presentation"" style=""width: 100%; margin: 30px 0; border-collapse: collapse;"">
                                <tr>
                                    <td style=""text-align: center; padding: 20px;"">
                                        <div style=""display: inline-block; background-color: #f8f9fa; border: 2px solid #dee2e6; border-radius: 6px; padding: 20px 40px;"">
                                            <span style=""font-size: 36px; font-weight: 700; letter-spacing: 6px; color: #333333; font-family: 'Courier New', monospace;"">{safeCode}</span>
                                        </div>
                                    </td>
                                </tr>
                            </table>
                            
                            <p style=""margin: 20px 0; font-size: 14px; line-height: 1.6; color: #666666;"">This verification code will expire in 24 hours.</p>
                            
                            <p style=""margin: 20px 0 0 0; font-size: 16px; line-height: 1.6; color: #333333;"">If you did not create an account with Lost & Found, you can safely ignore this email.</p>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style=""padding: 30px; background-color: #f8f9fa; border-top: 1px solid #e0e0e0; border-radius: 0 0 8px 8px;"">
                            <p style=""margin: 0 0 15px 0; font-size: 14px; line-height: 1.6; color: #666666;"">You received this email because you created an account on Lost & Found.</p>
                            <p style=""margin: 0 0 15px 0; font-size: 14px; line-height: 1.6; color: #666666;"">If this was not you, please ignore this email.</p>
                            <p style=""margin: 15px 0 0 0; font-size: 12px; line-height: 1.6; color: #999999; border-top: 1px solid #e0e0e0; padding-top: 15px;"">
                                Lost & Found App<br>
                                Contact: {safeEmail}<br>
                                © {DateTime.UtcNow.Year} Lost & Found. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        /// <summary>
        /// Generates the plaintext version of the verification email.
        /// Required for multipart/alternative format and email clients that don't support HTML.
        /// </summary>
        private string GenerateVerificationEmailPlainText(string name, string verificationCode, string contactEmail)
        {
            return $@"Lost & Found

Hello {name},

Thank you for creating an account with Lost & Found. Please use the verification code below to complete your registration:

Verification Code: {verificationCode}

This verification code will expire in 24 hours.

If you did not create an account with Lost & Found, you can safely ignore this email.

---

You received this email because you created an account on Lost & Found.
If this was not you, please ignore this email.

Lost & Found App
Contact: {contactEmail}
© {DateTime.UtcNow.Year} Lost & Found. All rights reserved.";
        }
    }
}
