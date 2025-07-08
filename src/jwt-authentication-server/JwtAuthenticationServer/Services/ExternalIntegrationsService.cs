using System;
using System.Threading.Tasks;
using JwtAuthenticationServer.Models;
using JwtAuthenticationServer.Integrations;
using Microsoft.Extensions.Logging;

namespace JwtAuthenticationServer.Services
{
    public class ExternalIntegrationService : IExternalIntegrationService
    {
        private readonly ISlackApiProvider _slackProvider;
        private readonly IAnthropicApiProvider _anthropicProvider;
        private readonly IPayPalApiProvider _paypalProvider;
        private readonly IAwsS3Provider _s3Provider;
        private readonly IStripeApiProvider _stripeProvider;
        private readonly ISendGridApiProvider _sendGridProvider;
        private readonly ITwilioApiProvider _twilioProvider;
        private readonly IExternalDatabaseProvider _externalDbProvider;
        private readonly ILogger<ExternalIntegrationService> _logger;

        public ExternalIntegrationService(
            ISlackApiProvider slackProvider,
            IAnthropicApiProvider anthropicProvider,
            IPayPalApiProvider paypalProvider,
            IAwsS3Provider s3Provider,
            IStripeApiProvider stripeProvider,
            ISendGridApiProvider sendGridProvider,
            ITwilioApiProvider twilioProvider,
            IExternalDatabaseProvider externalDbProvider,
            ILogger<ExternalIntegrationService> logger)
        {
            _slackProvider = slackProvider;
            _anthropicProvider = anthropicProvider;
            _paypalProvider = paypalProvider;
            _s3Provider = s3Provider;
            _stripeProvider = stripeProvider;
            _sendGridProvider = sendGridProvider;
            _twilioProvider = twilioProvider;
            _externalDbProvider = externalDbProvider;
            _logger = logger;
        }

        // User lifecycle notifications via external services
        public async Task<bool> NotifyUserCreationAsync(UserModel user)
        {
            try
            {
                // Send Slack notification
                var slackMessage = $"New user created: {user.FirstName} {user.LastName} ({user.Email}) with role {user.Role}";
                await _slackProvider.SendNotificationAsync(slackMessage);

                // Send welcome email via SendGrid
                var emailSubject = "Welcome to our platform!";
                var emailContent = $"<h1>Welcome {user.FirstName}!</h1><p>Your account has been created successfully.</p>";
                await _sendGridProvider.SendEmailAsync(user.Email, emailSubject, emailContent);

                // Send SMS notification via Twilio (if phone number provided)
                if (!string.IsNullOrEmpty(user.PhoneNumber))
                {
                    var smsMessage = $"Welcome {user.FirstName}! Your account is ready.";
                    await _twilioProvider.SendSmsAsync(user.PhoneNumber, smsMessage);
                }

                // Sync user data to external database
                await _externalDbProvider.SyncUserDataAsync(user);

                _logger.LogInformation($"Successfully notified external services about user creation: {user.Username}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to notify external services about user creation: {user.Username}");
                return false;
            }
        }

        // AI-powered user analysis
        public async Task<string> AnalyzeUserBehaviorAsync(string userId)
        {
            try
            {
                // Get user data from external database
                var userData = await _externalDbProvider.QueryUserDataAsync(userId);
                
                // Use Anthropic AI to analyze behavior patterns
                var prompt = $"Analyze this user behavior data and provide insights: {userData}";
                var analysis = await _anthropicProvider.ChatAsync(prompt);

                _logger.LogInformation($"AI analysis completed for user: {userId}");
                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to analyze user behavior for: {userId}");
                return "Analysis failed";
            }
        }

        // Payment processing integration
        public async Task<string> ProcessPaymentAsync(UserModel user, decimal amount, string currency = "USD")
        {
            try
            {
                // Create Stripe customer
                var stripeCustomer = await _stripeProvider.CreateCustomerAsync(user);
                
                // Process payment via PayPal (alternative)
                var paypalPayment = await _paypalProvider.CreatePaymentAsync(amount, currency);

                // Send payment confirmation email
                var emailSubject = "Payment Confirmation";
                var emailContent = $"<h1>Payment Processed</h1><p>Amount: {amount} {currency}</p>";
                await _sendGridProvider.SendEmailAsync(user.Email, emailSubject, emailContent);

                // Notify via Slack
                var slackMessage = $"Payment processed: {amount} {currency} for user {user.Username}";
                await _slackProvider.SendNotificationAsync(slackMessage);

                _logger.LogInformation($"Payment processed successfully for user: {user.Username}");
                return "Payment processed successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Payment processing failed for user: {user.Username}");
                return "Payment failed";
            }
        }

        // Document management via cloud storage
        public async Task<bool> UploadUserDocumentAsync(string userId, string fileName, byte[] documentData)
        {
            try
            {
                // Upload to AWS S3
                var uploadSuccess = await _s3Provider.UploadFileAsync($"users/{userId}/{fileName}", documentData);

                if (uploadSuccess)
                {
                    // Notify via Slack
                    var message = $"Document uploaded for user {userId}: {fileName}";
                    await _slackProvider.SendNotificationAsync(message);

                    _logger.LogInformation($"Document uploaded successfully: {fileName} for user: {userId}");
                }

                return uploadSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Document upload failed: {fileName} for user: {userId}");
                return false;
            }
        }

        // Security incident reporting
        public async Task<bool> ReportSecurityIncidentAsync(string userId, string incidentType, string details)
        {
            try
            {
                // Send immediate Slack alert
                var urgentMessage = $"🚨 SECURITY INCIDENT 🚨\nUser: {userId}\nType: {incidentType}\nDetails: {details}";
                await _slackProvider.SendNotificationAsync(urgentMessage);

                // Use AI to analyze incident severity
                var prompt = $"Analyze this security incident and rate its severity: Type: {incidentType}, Details: {details}";
                var aiAnalysis = await _anthropicProvider.ChatAsync(prompt);

                // Send detailed email to security team
                var emailSubject = $"Security Incident Report - {incidentType}";
                var emailContent = $"<h1>Security Incident</h1><p>User: {userId}</p><p>Type: {incidentType}</p><p>Details: {details}</p><p>AI Analysis: {aiAnalysis}</p>";
                await _sendGridProvider.SendEmailAsync("security@company.com", emailSubject, emailContent);

                _logger.LogWarning($"Security incident reported for user: {userId}, Type: {incidentType}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to report security incident for user: {userId}");
                return false;
            }
        }

        // Compliance data export
        public async Task<byte[]> ExportUserDataForComplianceAsync(string userId)
        {
            try
            {
                // Get user data from external database
                var userData = await _externalDbProvider.QueryUserDataAsync(userId);

                // Download any stored documents from S3
                var documents = await _s3Provider.DownloadFileAsync($"users/{userId}/profile.pdf");

                // Notify compliance team
                var message = $"Data export requested for user: {userId} (GDPR/Compliance)";
                await _slackProvider.SendNotificationAsync(message);

                _logger.LogInformation($"Compliance data export completed for user: {userId}");
                
                // In a real implementation, you'd combine all data into a single export file
                return documents ?? new byte[0];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Compliance data export failed for user: {userId}");
                return new byte[0];
            }
        }
    }

    public interface IExternalIntegrationService
    {
        Task<bool> NotifyUserCreationAsync(UserModel user);
        Task<string> AnalyzeUserBehaviorAsync(string userId);
        Task<string> ProcessPaymentAsync(UserModel user, decimal amount, string currency = "USD");
        Task<bool> UploadUserDocumentAsync(string userId, string fileName, byte[] documentData);
        Task<bool> ReportSecurityIncidentAsync(string userId, string incidentType, string details);
        Task<byte[]> ExportUserDataForComplianceAsync(string userId);
    }
}