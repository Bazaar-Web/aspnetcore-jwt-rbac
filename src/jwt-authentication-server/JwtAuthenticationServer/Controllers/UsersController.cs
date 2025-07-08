using JwtAuthenticationServer.Attributes;
using JwtAuthenticationServer.Authorizations;
using JwtAuthenticationServer.Models;
using JwtAuthenticationServer.Services;
using JwtAuthenticationServer.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JwtAuthenticationServer.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/users")]
    public class UsersController : Controller
    {
        private readonly IHttpContextAccessor _authContext;
        private readonly IExternalIntegrationService _externalIntegrationService;

        public UsersController(
            IHttpContextAccessor authContext,
            IExternalIntegrationService externalIntegrationService)
        {
            _authContext = authContext;
            _externalIntegrationService = externalIntegrationService;
        }

        [HttpGet, Route("basic-info")]
        [Roles(UserRoles.Manager, UserRoles.Admin, UserRoles.SuperAdmin)]
        public IActionResult GetBasicUserInfo()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. Basic user info accessible by Manager, Admin, and SuperAdmin only.");
        }

        [HttpGet, Route("pii-data")]
        [Roles(UserRoles.Admin, UserRoles.SuperAdmin)]
        public IActionResult GetPIIData()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. PII data (SSN, addresses, etc.) accessible only by Admin and SuperAdmin.");
        }

        [HttpPost, Route("create")]
        [Roles(UserRoles.Admin, UserRoles.SuperAdmin)]
        public async Task<IActionResult> CreateUser([FromBody] UserModel user)
        {
            try
            {
                // Simulate user creation logic here...
                
                // Notify external services about user creation
                var notificationSuccess = await _externalIntegrationService.NotifyUserCreationAsync(user);
                
                if (notificationSuccess)
                {
                    return Ok($"User created successfully and external services notified. User: {user.Username}");
                }
                else
                {
                    return Ok($"User created successfully but some external notifications failed. User: {user.Username}");
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"User creation failed: {ex.Message}");
            }
        }

        [HttpPost, Route("process-payment")]
        [Roles(UserRoles.FinanceManager, UserRoles.SuperAdmin)]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
        {
            try
            {
                // Get user data (in real implementation, fetch from database)
                var user = new UserModel { Username = request.UserId, Email = request.Email };
                
                var result = await _externalIntegrationService.ProcessPaymentAsync(user, request.Amount, request.Currency);
                return Ok($"Payment processing result: {result}");
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Payment processing failed: {ex.Message}");
            }
        }

        [HttpPost, Route("upload-document")]
        [Roles(UserRoles.Admin, UserRoles.SuperAdmin)]
        public async Task<IActionResult> UploadDocument([FromForm] DocumentUploadRequest request)
        {
            try
            {
                // Convert IFormFile to byte array
                byte[] documentData;
                using (var memoryStream = new System.IO.MemoryStream())
                {
                    await request.File.CopyToAsync(memoryStream);
                    documentData = memoryStream.ToArray();
                }

                var success = await _externalIntegrationService.UploadUserDocumentAsync(
                    request.UserId, 
                    request.File.FileName, 
                    documentData);

                if (success)
                {
                    return Ok($"Document uploaded successfully to external storage: {request.File.FileName}");
                }
                else
                {
                    return BadRequest("Document upload to external storage failed");
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Document upload failed: {ex.Message}");
            }
        }

        [HttpGet, Route("analyze-behavior/{userId}")]
        [Roles(UserRoles.Auditor, UserRoles.SuperAdmin)]
        public async Task<IActionResult> AnalyzeUserBehavior(string userId)
        {
            try
            {
                var analysis = await _externalIntegrationService.AnalyzeUserBehaviorAsync(userId);
                return Ok($"AI Behavior Analysis for user {userId}: {analysis}");
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Behavior analysis failed: {ex.Message}");
            }
        }

        [HttpPost, Route("report-security-incident")]
        [Roles(UserRoles.SuperAdmin, UserRoles.Auditor)]
        public async Task<IActionResult> ReportSecurityIncident([FromBody] SecurityIncidentRequest request)
        {
            try
            {
                var success = await _externalIntegrationService.ReportSecurityIncidentAsync(
                    request.UserId, 
                    request.IncidentType, 
                    request.Details);

                if (success)
                {
                    return Ok($"Security incident reported successfully for user: {request.UserId}");
                }
                else
                {
                    return BadRequest("Failed to report security incident to external services");
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Security incident reporting failed: {ex.Message}");
            }
        }

        [HttpGet, Route("export-data/{userId}")]
        [Roles(UserRoles.SuperAdmin)]
        public async Task<IActionResult> ExportUserDataForCompliance(string userId)
        {
            try
            {
                var exportData = await _externalIntegrationService.ExportUserDataForComplianceAsync(userId);
                
                if (exportData != null && exportData.Length > 0)
                {
                    return File(exportData, "application/zip", $"user_export_{userId}.zip");
                }
                else
                {
                    return Ok($"Data export initiated for user: {userId}. Export will be available shortly.");
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Data export failed: {ex.Message}");
            }
        }

        [HttpGet, Route("financial-info")]
        [Roles(UserRoles.SuperAdmin, UserRoles.FinanceManager)]
        public IActionResult GetFinancialInfo()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. Financial information (salaries, bank details) accessible only by SuperAdmin and FinanceManager.");
        }

        [HttpGet, Route("medical-records")]
        [Roles(UserRoles.SuperAdmin)]
        public IActionResult GetMedicalRecords()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. Medical records accessible only by SuperAdmin. HIPAA protected data.");
        }

        [HttpGet, Route("contact-list")]
        [Roles(UserRoles.Manager, UserRoles.Sales, UserRoles.Employee)]
        public IActionResult GetContactList()
        {
            return Ok("Contact information accessible by Manager, Sales, and Employee roles.");
        }

        [HttpGet, Route("public-directory")]
        [AllowAnonymous]
        public IActionResult GetPublicDirectory()
        {
            return Ok("Public directory accessible by everyone, no JWT token required. Only basic non-sensitive info.");
        }

        [HttpGet, Route("hr-records")]
        [Authorize(Policy = KeyConstants.CustomAuthorizationPolicyName)]
        public IActionResult GetHRRecords()
        {
            return Ok("Policy-based access for HR records. Employment history, performance reviews, etc.");
        }

        [HttpGet, Route("audit-log")]
        [Roles(UserRoles.Auditor, UserRoles.SuperAdmin)]
        public IActionResult GetAuditLog()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. User activity audit logs accessible only by Auditor and SuperAdmin.");
        }

        [HttpGet, Route("security-info")]
        [Roles(UserRoles.SuperAdmin)]
        public IActionResult GetSecurityInfo()
        {
            return Ok("Security information (2FA secrets, security questions) accessible only by SuperAdmin.");
        }

        [HttpGet, Route("biometric-data")]
        [Roles(UserRoles.SuperAdmin, UserRoles.QualityControl)]
        public IActionResult GetBiometricData()
        {
            return Ok("Biometric data (fingerprints, facial recognition) accessible by SuperAdmin and QualityControl.");
        }

        [HttpPut, Route("update-pii")]
        [Roles(UserRoles.SuperAdmin)]
        public IActionResult UpdatePII([FromBody] UserModel user)
        {
            return Ok("PII updates allowed only by SuperAdmin role.");
        }

        [HttpDelete, Route("delete/{userId}")]
        [Roles(UserRoles.SuperAdmin)]
        public IActionResult DeleteUser(string userId)
        {
            return Ok($"User deletion (ID: {userId}) allowed only by SuperAdmin role.");
        }

        [HttpGet, Route("my-profile")]
        [Authorize]
        public IActionResult GetMyProfile()
        {
            var userId = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return Ok($"Own profile data for user: {userId}. Users can view their own basic profile.");
        }

        [HttpGet, Route("salary-info")]
        [Roles(UserRoles.FinanceManager, UserRoles.SuperAdmin)]
        public IActionResult GetSalaryInfo()
        {
            return Ok("Salary information accessible by FinanceManager and SuperAdmin only.");
        }

        [HttpGet, Route("emergency-contacts")]
        [Roles(UserRoles.Manager, UserRoles.Admin, UserRoles.SuperAdmin)]
        public IActionResult GetEmergencyContacts()
        {
            return Ok("Emergency contact information accessible by Manager, Admin, and SuperAdmin.");
        }

        [Authorize(Policy = ClaimTypes.DateOfBirth)]
        [HttpGet, Route("age-restricted")]
        public IActionResult GetAgeRestrictedData()
        {
            return Ok($"Request had {ClaimTypes.DateOfBirth} claim. Age-restricted data access granted.");
        }

        [HttpGet, Route("compliance-report")]
        [Roles(UserRoles.Auditor, UserRoles.SuperAdmin)]
        public IActionResult GetComplianceReport()
        {
            return Ok("GDPR/HIPAA compliance report accessible by Auditor and SuperAdmin.");
        }

        [HttpPost, Route("anonymize")]
        [Roles(UserRoles.SuperAdmin)]
        public IActionResult AnonymizeUser([FromBody] string userId)
        {
            return Ok($"User data anonymization for {userId} initiated. GDPR compliance action by SuperAdmin.");
        }

        // External integration endpoints that trigger external host connectivity
        [HttpPost, Route("sync-external")]
        [Roles(UserRoles.Admin, UserRoles.SuperAdmin)]
        public async Task<IActionResult> SyncWithExternalSystems([FromBody] UserModel user)
        {
            try
            {
                // This will trigger multiple external API calls
                var success = await _externalIntegrationService.NotifyUserCreationAsync(user);
                return Ok($"External synchronization {'succeeded' if success else 'failed'} for user: {user.Username}");
            }
            catch (System.Exception ex)
            {
                return BadRequest($"External sync failed: {ex.Message}");
            }
        }
    }

    // Request models for external integration endpoints
    public class PaymentRequest
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
    }

    public class DocumentUploadRequest
    {
        public string UserId { get; set; }
        public IFormFile File { get; set; }
    }

    public class SecurityIncidentRequest
    {
        public string UserId { get; set; }
        public string IncidentType { get; set; }
        public string Details { get; set; }
    }
}