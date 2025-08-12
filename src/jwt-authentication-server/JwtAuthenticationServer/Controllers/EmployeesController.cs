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
    [Route("api/employees")]
    public class EmployeesController : Controller
    {
        private readonly IHttpContextAccessor _authContext;
        private readonly IExternalIntegrationService _externalIntegrationService;
        private readonly IEmployeeService _employeeService;

        public EmployeesController(
            IHttpContextAccessor authContext,
            IExternalIntegrationService externalIntegrationService,
            IEmployeeService employeeService)
        {
            _authContext = authContext;
            _externalIntegrationService = externalIntegrationService;
            _employeeService = employeeService;
        }

        [HttpGet, Route("directory")]
        [Roles(UserRoles.Employee, UserRoles.Manager, UserRoles.Admin, UserRoles.SuperAdmin)]
        public IActionResult GetEmployeeDirectory()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. Employee directory accessible by Employee, Manager, Admin, and SuperAdmin.");
        }

        [HttpGet, Route("salary-info")]
        [Roles(UserRoles.FinanceManager, UserRoles.Manager, UserRoles.Admin, UserRoles.SuperAdmin)]
        public IActionResult GetSalaryInformation()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. Salary information accessible by FinanceManager, Manager, Admin, and SuperAdmin.");
        }

        [HttpGet, Route("performance-reports")]
        [Roles(UserRoles.Manager, UserRoles.Admin, UserRoles.SuperAdmin)]
        public IActionResult GetPerformanceReports()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. Performance reports accessible by Manager, Admin, and SuperAdmin.");
        }

        [HttpPost, Route("create")]
        [Roles(UserRoles.Admin, UserRoles.SuperAdmin)]
        public async Task<IActionResult> CreateEmployee([FromBody] EmployeeModel employee)
        {
            try
            {
                // Simulate employee creation logic
                var createdEmployee = await _employeeService.CreateEmployeeAsync(employee);
                
                // Notify external HR management system
                var notificationSuccess = await _externalIntegrationService.NotifyEmployeeCreationAsync(createdEmployee);
                
                if (notificationSuccess)
                {
                    return Ok($"Employee created successfully and external systems notified. Employee: {employee.FullName}");
                }
                else
                {
                    return Ok($"Employee created successfully but some external notifications failed. Employee: {employee.FullName}");
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Employee creation failed: {ex.Message}");
            }
        }

        [HttpPost, Route("process-timesheet")]
        [Roles(UserRoles.Employee, UserRoles.Manager, UserRoles.SuperAdmin)]
        public async Task<IActionResult> ProcessTimesheet([FromBody] TimesheetRequest request)
        {
            try
            {
                var result = await _externalIntegrationService.ProcessTimesheetAsync(
                    request.EmployeeId, 
                    request.Date, 
                    request.HoursWorked, 
                    request.ProjectCode);
                
                return Ok($"Timesheet processing result: {result}");
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Timesheet processing failed: {ex.Message}");
            }
        }

        [HttpPost, Route("upload-documents")]
        [Roles(UserRoles.Admin, UserRoles.SuperAdmin)]
        public async Task<IActionResult> UploadEmployeeDocuments([FromForm] EmployeeDocumentUploadRequest request)
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

                var success = await _externalIntegrationService.UploadEmployeeDocumentAsync(
                    request.EmployeeId, 
                    request.File.FileName, 
                    documentData,
                    request.DocumentType);

                if (success)
                {
                    return Ok($"Employee document uploaded successfully: {request.File.FileName}");
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

        [HttpGet, Route("analyze-attendance/{employeeId}")]
        [Roles(UserRoles.Manager, UserRoles.FinanceManager, UserRoles.SuperAdmin)]
        public async Task<IActionResult> AnalyzeEmployeeAttendance(string employeeId)
        {
            try
            {
                var analysis = await _externalIntegrationService.AnalyzeEmployeeAttendanceAsync(employeeId);
                return Ok($"Attendance Analysis for employee {employeeId}: {analysis}");
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Attendance analysis failed: {ex.Message}");
            }
        }

        [HttpPost, Route("report-hr-issue")]
        [Roles(UserRoles.Employee, UserRoles.Manager, UserRoles.SuperAdmin)]
        public async Task<IActionResult> ReportHrIssue([FromBody] HrIssueRequest request)
        {
            try
            {
                var success = await _externalIntegrationService.ReportHrIssueAsync(
                    request.EmployeeId, 
                    request.IssueType, 
                    request.Description,
                    request.Severity);

                if (success)
                {
                    return Ok($"HR issue reported successfully for employee: {request.EmployeeId}");
                }
                else
                {
                    return BadRequest("Failed to report HR issue to external services");
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"HR issue reporting failed: {ex.Message}");
            }
        }

        [HttpGet, Route("export-payroll/{employeeId}")]
        [Roles(UserRoles.SuperAdmin, UserRoles.FinanceManager)]
        public async Task<IActionResult> ExportEmployeePayrollData(string employeeId)
        {
            try
            {
                var exportData = await _externalIntegrationService.ExportPayrollDataAsync(employeeId);
                
                if (exportData != null && exportData.Length > 0)
                {
                    return File(exportData, "application/json", $"payroll_export_{employeeId}.json");
                }
                else
                {
                    return Ok($"Payroll export initiated for employee: {employeeId}. Export will be available shortly.");
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Payroll export failed: {ex.Message}");
            }
        }

        [HttpGet, Route("department-info")]
        [Roles(UserRoles.SuperAdmin, UserRoles.Manager, UserRoles.Admin)]
        public IActionResult GetDepartmentInfo()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. Department information accessible by Manager, Admin, and SuperAdmin.");
        }

        [HttpGet, Route("confidential-records")]
        [Roles(UserRoles.SuperAdmin)]
        public IActionResult GetConfidentialRecords()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. Confidential employee records accessible only by SuperAdmin.");
        }

        [HttpGet, Route("public-directory")]
        [AllowAnonymous]
        public IActionResult GetPublicDirectory()
        {
            return Ok("Public employee directory accessible by everyone, no JWT token required. Basic contact information only.");
        }

        [HttpGet, Route("benefits-info")]
        [Authorize(Policy = KeyConstants.CustomAuthorizationPolicyName)]
        public IActionResult GetBenefitsInfo()
        {
            return Ok("Policy-based access for benefits information. Health insurance, retirement plans, etc.");
        }

        [HttpGet, Route("team-performance")]
        [Roles(UserRoles.Manager, UserRoles.Admin, UserRoles.SuperAdmin)]
        public IActionResult GetTeamPerformance()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. Team performance data accessible by Manager, Admin, and SuperAdmin.");
        }

        [HttpGet, Route("executive-compensation")]
        [Roles(UserRoles.SuperAdmin)]
        public IActionResult GetExecutiveCompensation()
        {
            return Ok("Executive compensation data accessible only by SuperAdmin.");
        }

        [HttpGet, Route("leave-balances")]
        [Roles(UserRoles.Employee, UserRoles.Manager, UserRoles.Admin, UserRoles.SuperAdmin)]
        public IActionResult GetLeaveBalances()
        {
            return Ok("Employee leave balances accessible by Employee, Manager, Admin, and SuperAdmin.");
        }

        [HttpPut, Route("update-salary")]
        [Roles(UserRoles.FinanceManager, UserRoles.SuperAdmin)]
        public IActionResult UpdateEmployeeSalary([FromBody] EmployeeSalaryModel salary)
        {
            return Ok("Salary updates allowed only by FinanceManager and SuperAdmin.");
        }

        [HttpDelete, Route("terminate/{employeeId}")]
        [Roles(UserRoles.SuperAdmin)]
        public IActionResult TerminateEmployee(string employeeId)
        {
            return Ok($"Employee termination (ID: {employeeId}) allowed only by SuperAdmin role.");
        }

        [HttpGet, Route("my-profile")]
        [Authorize]
        public IActionResult GetMyProfile()
        {
            var userId = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return Ok($"Profile information for user: {userId}. Users can view their own profile.");
        }

        [HttpGet, Route("training-records")]
        [Roles(UserRoles.Manager, UserRoles.Admin, UserRoles.SuperAdmin)]
        public IActionResult GetTrainingRecords()
        {
            return Ok("Training records accessible by Manager, Admin, and SuperAdmin.");
        }

        [HttpGet, Route("compliance-status")]
        [Roles(UserRoles.Auditor, UserRoles.Admin, UserRoles.SuperAdmin)]
        public IActionResult GetComplianceStatus()
        {
            return Ok("Employee compliance status accessible by Auditor, Admin, and SuperAdmin.");
        }

        [Authorize(Policy = ClaimTypes.Country)]
        [HttpGet, Route("region-specific-benefits")]
        public IActionResult GetRegionSpecificBenefits()
        {
            return Ok($"Request had {ClaimTypes.Country} claim. Region-specific benefits access granted.");
        }

        [HttpGet, Route("audit-trail")]
        [Roles(UserRoles.Auditor, UserRoles.SuperAdmin)]
        public IActionResult GetEmployeeAuditTrail()
        {
            return Ok("Employee modification audit trail accessible by Auditor and SuperAdmin.");
        }

        [HttpPost, Route("disciplinary-action")]
        [Roles(UserRoles.Manager, UserRoles.SuperAdmin)]
        public IActionResult RecordDisciplinaryAction([FromBody] DisciplinaryActionRequest request)
        {
            return Ok($"Disciplinary action for employee {request.EmployeeId} recorded by Manager or SuperAdmin.");
        }

        // External integration endpoints
        [HttpPost, Route("sync-payroll")]
        [Roles(UserRoles.FinanceManager, UserRoles.SuperAdmin)]
        public async Task<IActionResult> SyncPayrollWithExternalSystems([FromBody] EmployeeModel employee)
        {
            try
            {
                var success = await _externalIntegrationService.SyncPayrollAsync(employee);
                return Ok($"Payroll synchronization {'succeeded' if success else 'failed'} for employee: {employee.FullName}");
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Payroll sync failed: {ex.Message}");
            }
        }

        [HttpPost, Route("update-benefits")]
        [Roles(UserRoles.Admin, UserRoles.SuperAdmin)]
        public async Task<IActionResult> UpdateBenefitsEnrollment([FromBody] BenefitsUpdateRequest request)
        {
            try
            {
                var success = await _externalIntegrationService.UpdateBenefitsEnrollmentAsync(
                    request.EmployeeId, 
                    request.BenefitType, 
                    request.EnrollmentData);

                if (success)
                {
                    return Ok($"Benefits enrollment updated successfully for employee: {request.EmployeeId}");
                }
                else
                {
                    return BadRequest("Failed to update benefits enrollment");
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Benefits update failed: {ex.Message}");
            }
        }

        [HttpPost, Route("schedule-review")]
        [Roles(UserRoles.Manager, UserRoles.Admin, UserRoles.SuperAdmin)]
        public async Task<IActionResult> SchedulePerformanceReview([FromBody] PerformanceReviewRequest request)
        {
            try
            {
                var success = await _externalIntegrationService.SchedulePerformanceReviewAsync(
                    request.EmployeeId, 
                    request.ReviewDate, 
                    request.ReviewType,
                    request.ManagerId);

                if (success)
                {
                    return Ok($"Performance review scheduled successfully for employee: {request.EmployeeId}");
                }
                else
                {
                    return BadRequest("Failed to schedule performance review");
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Performance review scheduling failed: {ex.Message}");
            }
        }
    }

    // Request models for external integration endpoints
    public class TimesheetRequest
    {
        public string EmployeeId { get; set; }
        public System.DateTime Date { get; set; }
        public double HoursWorked { get; set; }
        public string ProjectCode { get; set; }
    }

    public class EmployeeDocumentUploadRequest
    {
        public string EmployeeId { get; set; }
        public IFormFile File { get; set; }
        public string DocumentType { get; set; } = "General";
    }

    public class HrIssueRequest
    {
        public string EmployeeId { get; set; }
        public string IssueType { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; } = "Medium";
    }

    public class DisciplinaryActionRequest
    {
        public string EmployeeId { get; set; }
        public string ActionType { get; set; }
        public string Reason { get; set; }
        public string Notes { get; set; }
    }

    public class BenefitsUpdateRequest
    {
        public string EmployeeId { get; set; }
        public string BenefitType { get; set; }
        public object EnrollmentData { get; set; }
    }

    public class PerformanceReviewRequest
    {
        public string EmployeeId { get; set; }
        public System.DateTime ReviewDate { get; set; }
        public string ReviewType { get; set; } = "Annual";
        public string ManagerId { get; set; }
    }

    // Supporting models
    public class EmployeeModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public System.DateTime HireDate { get; set; }
        public decimal Salary { get; set; }
        public string EmployeeNumber { get; set; }
    }

    public class EmployeeSalaryModel
    {
        public string EmployeeId { get; set; }
        public decimal NewSalary { get; set; }
        public decimal PreviousSalary { get; set; }
        public System.DateTime EffectiveDate { get; set; }
        public string Reason { get; set; }
    }

    // Additional service interface for employee-specific operations
    public interface IEmployeeService
    {
        Task<EmployeeModel> CreateEmployeeAsync(EmployeeModel employee);
        Task<EmployeeModel> GetEmployeeByIdAsync(string employeeId);
        Task<bool> UpdateEmployeeAsync(EmployeeModel employee);
        Task<bool> DeleteEmployeeAsync(string employeeId);
    }

    // Extended external integration service interface
    public interface IExternalIntegrationService
    {
        // Employee-specific methods
        Task<bool> NotifyEmployeeCreationAsync(EmployeeModel employee);
        Task<string> ProcessTimesheetAsync(string employeeId, System.DateTime date, double hours, string projectCode);
        Task<bool> UploadEmployeeDocumentAsync(string employeeId, string fileName, byte[] data, string documentType);
        Task<string> AnalyzeEmployeeAttendanceAsync(string employeeId);
        Task<bool> ReportHrIssueAsync(string employeeId, string issueType, string description, string severity);
        Task<byte[]> ExportPayrollDataAsync(string employeeId);
        Task<bool> SyncPayrollAsync(EmployeeModel employee);
        Task<bool> UpdateBenefitsEnrollmentAsync(string employeeId, string benefitType, object enrollmentData);
        Task<bool> SchedulePerformanceReviewAsync(string employeeId, System.DateTime reviewDate, string reviewType, string managerId);
    }
}