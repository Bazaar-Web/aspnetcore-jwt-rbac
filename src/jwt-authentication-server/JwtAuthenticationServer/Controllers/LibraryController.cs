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
    [Route("api/library")]
    public class LibraryController : Controller
    {
        private readonly IHttpContextAccessor _authContext;
        private readonly IExternalIntegrationService _externalIntegrationService;
        private readonly ILibraryService _libraryService;

        public LibraryController(
            IHttpContextAccessor authContext,
            IExternalIntegrationService externalIntegrationService,
            ILibraryService libraryService)
        {
            _authContext = authContext;
            _externalIntegrationService = externalIntegrationService;
            _libraryService = libraryService;
        }

        [HttpGet, Route("catalog")]
        [Roles(UserRoles.Member, UserRoles.Librarian, UserRoles.Admin, UserRoles.SuperAdmin)]
        public IActionResult GetBookCatalog()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. Book catalog accessible by Member, Librarian, Admin, and SuperAdmin.");
        }

        [HttpGet, Route("restricted-collection")]
        [Roles(UserRoles.Researcher, UserRoles.Librarian, UserRoles.Admin, UserRoles.SuperAdmin)]
        public IActionResult GetRestrictedCollection()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. Restricted collection accessible by Researcher, Librarian, Admin, and SuperAdmin.");
        }

        [HttpGet, Route("acquisition-reports")]
        [Roles(UserRoles.Librarian, UserRoles.Admin, UserRoles.SuperAdmin)]
        public IActionResult GetAcquisitionReports()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. Acquisition reports accessible by Librarian, Admin, and SuperAdmin.");
        }

        [HttpPost, Route("add-book")]
        [Roles(UserRoles.Librarian, UserRoles.SuperAdmin)]
        public async Task<IActionResult> AddBook([FromBody] BookModel book)
        {
            try
            {
                // Simulate book addition logic
                var addedBook = await _libraryService.AddBookAsync(book);
                
                // Notify external catalog management system
                var notificationSuccess = await _externalIntegrationService.NotifyBookAdditionAsync(addedBook);
                
                if (notificationSuccess)
                {
                    return Ok($"Book added successfully and external systems notified. Book: {book.Title} by {book.Author}");
                }
                else
                {
                    return Ok($"Book added successfully but some external notifications failed. Book: {book.Title}");
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Book addition failed: {ex.Message}");
            }
        }

        [HttpPost, Route("checkout-book")]
        [Roles(UserRoles.Member, UserRoles.Librarian, UserRoles.SuperAdmin)]
        public async Task<IActionResult> CheckoutBook([FromBody] CheckoutRequest request)
        {
            try
            {
                var result = await _externalIntegrationService.ProcessBookCheckoutAsync(
                    request.MemberId, 
                    request.BookId, 
                    request.DueDateOverride);
                
                return Ok($"Book checkout result: {result}");
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Book checkout failed: {ex.Message}");
            }
        }

        [HttpPost, Route("upload-digital-content")]
        [Roles(UserRoles.Librarian, UserRoles.SuperAdmin)]
        public async Task<IActionResult> UploadDigitalContent([FromForm] DigitalContentUploadRequest request)
        {
            try
            {
                // Convert IFormFile to byte array
                byte[] contentData;
                using (var memoryStream = new System.IO.MemoryStream())
                {
                    await request.File.CopyToAsync(memoryStream);
                    contentData = memoryStream.ToArray();
                }

                var success = await _externalIntegrationService.UploadDigitalContentAsync(
                    request.BookId, 
                    request.File.FileName, 
                    contentData,
                    request.ContentType);

                if (success)
                {
                    return Ok($"Digital content uploaded successfully: {request.File.FileName}");
                }
                else
                {
                    return BadRequest("Digital content upload to external storage failed");
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Digital content upload failed: {ex.Message}");
            }
        }

        [HttpGet, Route("analyze-circulation/{bookId}")]
        [Roles(UserRoles.Librarian, UserRoles.Admin, UserRoles.SuperAdmin)]
        public async Task<IActionResult> AnalyzeBookCirculation(string bookId)
        {
            try
            {
                var analysis = await _externalIntegrationService.AnalyzeBookCirculationAsync(bookId);
                return Ok($"Circulation Analysis for book {bookId}: {analysis}");
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Circulation analysis failed: {ex.Message}");
            }
        }

        [HttpPost, Route("report-damage")]
        [Roles(UserRoles.Member, UserRoles.Librarian, UserRoles.SuperAdmin)]
        public async Task<IActionResult> ReportBookDamage([FromBody] DamageReportRequest request)
        {
            try
            {
                var success = await _externalIntegrationService.ReportBookDamageAsync(
                    request.BookId, 
                    request.DamageType, 
                    request.Description,
                    request.Severity);

                if (success)
                {
                    return Ok($"Book damage reported successfully for book: {request.BookId}");
                }
                else
                {
                    return BadRequest("Failed to report book damage to external services");
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Damage reporting failed: {ex.Message}");
            }
        }

        [HttpGet, Route("export-member-history/{memberId}")]
        [Roles(UserRoles.Librarian, UserRoles.SuperAdmin)]
        public async Task<IActionResult> ExportMemberHistory(string memberId)
        {
            try
            {
                var exportData = await _externalIntegrationService.ExportMemberHistoryAsync(memberId);
                
                if (exportData != null && exportData.Length > 0)
                {
                    return File(exportData, "application/json", $"member_history_{memberId}.json");
                }
                else
                {
                    return Ok($"Member history export initiated for member: {memberId}. Export will be available shortly.");
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Member history export failed: {ex.Message}");
            }
        }

        [HttpGet, Route("publisher-info")]
        [Roles(UserRoles.Librarian, UserRoles.Admin, UserRoles.SuperAdmin)]
        public IActionResult GetPublisherInfo()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. Publisher information accessible by Librarian, Admin, and SuperAdmin.");
        }

        [HttpGet, Route("rare-books")]
        [Roles(UserRoles.Researcher, UserRoles.SuperAdmin)]
        public IActionResult GetRareBooksCollection()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. Rare books collection accessible by Researcher and SuperAdmin only.");
        }

        [HttpGet, Route("public-catalog")]
        [AllowAnonymous]
        public IActionResult GetPublicCatalog()
        {
            return Ok("Public library catalog accessible by everyone, no JWT token required. Basic book information only.");
        }

        [HttpGet, Route("digital-access")]
        [Authorize(Policy = KeyConstants.CustomAuthorizationPolicyName)]
        public IActionResult GetDigitalAccess()
        {
            return Ok("Policy-based access for digital resources. E-books, audiobooks, digital journals, etc.");
        }

        [HttpGet, Route("reading-statistics")]
        [Roles(UserRoles.Member, UserRoles.Librarian, UserRoles.Admin, UserRoles.SuperAdmin)]
        public IActionResult GetReadingStatistics()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. Reading statistics accessible by Member, Librarian, Admin, and SuperAdmin.");
        }

        [HttpGet, Route("budget-reports")]
        [Roles(UserRoles.SuperAdmin)]
        public IActionResult GetBudgetReports()
        {
            return Ok("Library budget and financial reports accessible only by SuperAdmin.");
        }

        [HttpGet, Route("overdue-books")]
        [Roles(UserRoles.Member, UserRoles.Librarian, UserRoles.Admin, UserRoles.SuperAdmin)]
        public IActionResult GetOverdueBooks()
        {
            return Ok("Overdue book information accessible by Member, Librarian, Admin, and SuperAdmin.");
        }

        [HttpPut, Route("update-book-info")]
        [Roles(UserRoles.Librarian, UserRoles.SuperAdmin)]
        public IActionResult UpdateBookInformation([FromBody] BookUpdateModel bookUpdate)
        {
            return Ok("Book information updates allowed only by Librarian and SuperAdmin.");
        }

        [HttpDelete, Route("remove-book/{bookId}")]
        [Roles(UserRoles.SuperAdmin)]
        public IActionResult RemoveBook(string bookId)
        {
            return Ok($"Book removal (ID: {bookId}) allowed only by SuperAdmin role.");
        }

        [HttpGet, Route("my-loans")]
        [Authorize]
        public IActionResult GetMyLoans()
        {
            var userId = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return Ok($"Current loans for user: {userId}. Users can view their own borrowed books.");
        }

        [HttpGet, Route("fine-details")]
        [Roles(UserRoles.Member, UserRoles.Librarian, UserRoles.SuperAdmin)]
        public IActionResult GetFineDetails()
        {
            return Ok("Library fine information accessible by Member, Librarian, and SuperAdmin.");
        }

        [HttpGet, Route("research-access")]
        [Roles(UserRoles.Researcher, UserRoles.Librarian, UserRoles.SuperAdmin)]
        public IActionResult GetResearchAccess()
        {
            return Ok("Research materials and archives accessible by Researcher, Librarian, and SuperAdmin.");
        }

        [Authorize(Policy = ClaimTypes.Country)]
        [HttpGet, Route("local-collection")]
        public IActionResult GetLocalCollection()
        {
            return Ok($"Request had {ClaimTypes.Country} claim. Local/regional collection access granted.");
        }

        [HttpGet, Route("audit-trail")]
        [Roles(UserRoles.Auditor, UserRoles.SuperAdmin)]
        public IActionResult GetLibraryAuditTrail()
        {
            return Ok("Library system audit trail accessible by Auditor and SuperAdmin.");
        }

        [HttpPost, Route("place-hold")]
        [Roles(UserRoles.Member, UserRoles.Librarian, UserRoles.SuperAdmin)]
        public IActionResult PlaceBookHold([FromBody] HoldRequest request)
        {
            return Ok($"Book hold placed for book {request.BookId} by member {request.MemberId}.");
        }

        [HttpPost, Route("renew-book")]
        [Roles(UserRoles.Member, UserRoles.Librarian, UserRoles.SuperAdmin)]
        public async Task<IActionResult> RenewBook([FromBody] RenewalRequest request)
        {
            try
            {
                var success = await _externalIntegrationService.RenewBookAsync(
                    request.MemberId,
                    request.BookId,
                    request.ExtensionDays);

                if (success)
                {
                    return Ok($"Book renewed successfully for member: {request.MemberId}");
                }
                else
                {
                    return BadRequest("Book renewal failed - may have reached maximum renewals");
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Book renewal failed: {ex.Message}");
            }
        }

        // External integration endpoints
        [HttpPost, Route("sync-catalog")]
        [Roles(UserRoles.Librarian, UserRoles.SuperAdmin)]
        public async Task<IActionResult> SyncCatalogWithExternalSystems([FromBody] BookModel book)
        {
            try
            {
                var success = await _externalIntegrationService.SyncCatalogAsync(book);
                return Ok($"Catalog synchronization {'succeeded' if success else 'failed'} for book: {book.Title}");
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Catalog sync failed: {ex.Message}");
            }
        }

        [HttpPost, Route("interlibrary-loan")]
        [Roles(UserRoles.Member, UserRoles.Librarian, UserRoles.SuperAdmin)]
        public async Task<IActionResult> RequestInterlibraryLoan([FromBody] InterlibraryLoanRequest request)
        {
            try
            {
                var success = await _externalIntegrationService.RequestInterlibraryLoanAsync(
                    request.MemberId, 
                    request.RequestedTitle, 
                    request.Author,
                    request.ISBN,
                    request.RequestingLibrary);

                if (success)
                {
                    return Ok($"Interlibrary loan request submitted successfully for: {request.RequestedTitle}");
                }
                else
                {
                    return BadRequest("Failed to submit interlibrary loan request");
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Interlibrary loan request failed: {ex.Message}");
            }
        }

        [HttpPost, Route("schedule-event")]
        [Roles(UserRoles.Librarian, UserRoles.Admin, UserRoles.SuperAdmin)]
        public async Task<IActionResult> ScheduleLibraryEvent([FromBody] LibraryEventRequest request)
        {
            try
            {
                var success = await _externalIntegrationService.ScheduleLibraryEventAsync(
                    request.EventName,
                    request.EventDate,
                    request.EventType,
                    request.MaxAttendees,
                    request.Description);

                if (success)
                {
                    return Ok($"Library event '{request.EventName}' scheduled successfully");
                }
                else
                {
                    return BadRequest("Failed to schedule library event");
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Event scheduling failed: {ex.Message}");
            }
        }
    }

    // Request models for external integration endpoints
    public class CheckoutRequest
    {
        public string MemberId { get; set; }
        public string BookId { get; set; }
        public System.DateTime? DueDateOverride { get; set; }
    }

    public class DigitalContentUploadRequest
    {
        public string BookId { get; set; }
        public IFormFile File { get; set; }
        public string ContentType { get; set; } = "PDF";
    }

    public class DamageReportRequest
    {
        public string BookId { get; set; }
        public string DamageType { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; } = "Minor";
    }

    public class HoldRequest
    {
        public string MemberId { get; set; }
        public string BookId { get; set; }
        public System.DateTime? ExpirationDate { get; set; }
    }

    public class RenewalRequest
    {
        public string MemberId { get; set; }
        public string BookId { get; set; }
        public int ExtensionDays { get; set; } = 14;
    }

    public class InterlibraryLoanRequest
    {
        public string MemberId { get; set; }
        public string RequestedTitle { get; set; }
        public string Author { get; set; }
        public string ISBN { get; set; }
        public string RequestingLibrary { get; set; }
    }

    public class LibraryEventRequest
    {
        public string EventName { get; set; }
        public System.DateTime EventDate { get; set; }
        public string EventType { get; set; } = "Workshop";
        public int MaxAttendees { get; set; } = 20;
        public string Description { get; set; }
    }

    // Supporting models
    public class BookModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string ISBN { get; set; }
        public string Publisher { get; set; }
        public int PublicationYear { get; set; }
        public string Genre { get; set; }
        public string Location { get; set; }
        public bool IsAvailable { get; set; } = true;
    }

    public class BookUpdateModel
    {
        public string BookId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Location { get; set; }
        public bool IsAvailable { get; set; }
        public string Condition { get; set; }
    }

    public class LibraryMemberModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public System.DateTime MembershipDate { get; set; }
        public string MembershipType { get; set; }
        public decimal OutstandingFines { get; set; }
    }

    // Additional service interface for library-specific operations
    public interface ILibraryService
    {
        Task<BookModel> AddBookAsync(BookModel book);
        Task<BookModel> GetBookByIdAsync(string bookId);
        Task<bool> UpdateBookAsync(BookModel book);
        Task<bool> RemoveBookAsync(string bookId);
        Task<bool> CheckoutBookAsync(string memberId, string bookId);
        Task<bool> ReturnBookAsync(string memberId, string bookId);
    }

    // Extended external integration service interface for library operations
    public interface ILibraryExternalIntegrationService
    {
        // Library-specific methods
        Task<bool> NotifyBookAdditionAsync(BookModel book);
        Task<string> ProcessBookCheckoutAsync(string memberId, string bookId, System.DateTime? dueDateOverride);
        Task<bool> UploadDigitalContentAsync(string bookId, string fileName, byte[] data, string contentType);
        Task<string> AnalyzeBookCirculationAsync(string bookId);
        Task<bool> ReportBookDamageAsync(string bookId, string damageType, string description, string severity);
        Task<byte[]> ExportMemberHistoryAsync(string memberId);
        Task<bool> RenewBookAsync(string memberId, string bookId, int extensionDays);
        Task<bool> SyncCatalogAsync(BookModel book);
        Task<bool> RequestInterlibraryLoanAsync(string memberId, string title, string author, string isbn, string requestingLibrary);
        Task<bool> ScheduleLibraryEventAsync(string eventName, System.DateTime eventDate, string eventType, int maxAttendees, string description);
    }
}