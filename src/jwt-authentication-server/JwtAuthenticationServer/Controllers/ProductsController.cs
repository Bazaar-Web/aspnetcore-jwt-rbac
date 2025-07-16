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
    [Route("api/products")]
    public class ProductsController : Controller
    {
        private readonly IHttpContextAccessor _authContext;
        private readonly IExternalIntegrationService _externalIntegrationService;
        private readonly IProductService _productService;

        public ProductsController(
            IHttpContextAccessor authContext,
            IExternalIntegrationService externalIntegrationService,
            IProductService productService)
        {
            _authContext = authContext;
            _externalIntegrationService = externalIntegrationService;
            _productService = productService;
        }

        [HttpGet, Route("catalog")]
        [Roles(UserRoles.Employee, UserRoles.Manager, UserRoles.Admin, UserRoles.SuperAdmin)]
        public IActionResult GetProductCatalog()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. Product catalog accessible by Employee, Manager, Admin, and SuperAdmin.");
        }

        [HttpGet, Route("pricing")]
        [Roles(UserRoles.Sales, UserRoles.Manager, UserRoles.Admin, UserRoles.SuperAdmin)]
        public IActionResult GetProductPricing()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. Product pricing information accessible by Sales, Manager, Admin, and SuperAdmin.");
        }

        [HttpGet, Route("cost-analysis")]
        [Roles(UserRoles.FinanceManager, UserRoles.Admin, UserRoles.SuperAdmin)]
        public IActionResult GetProductCostAnalysis()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. Product cost analysis accessible only by FinanceManager, Admin, and SuperAdmin.");
        }

        [HttpPost, Route("create")]
        [Roles(UserRoles.Admin, UserRoles.SuperAdmin)]
        public async Task<IActionResult> CreateProduct([FromBody] ProductModel product)
        {
            try
            {
                // Simulate product creation logic
                var createdProduct = await _productService.CreateProductAsync(product);
                
                // Notify external inventory management system
                var notificationSuccess = await _externalIntegrationService.NotifyProductCreationAsync(createdProduct);
                
                if (notificationSuccess)
                {
                    return Ok($"Product created successfully and external systems notified. Product: {product.Name}");
                }
                else
                {
                    return Ok($"Product created successfully but some external notifications failed. Product: {product.Name}");
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Product creation failed: {ex.Message}");
            }
        }

        [HttpPost, Route("process-order")]
        [Roles(UserRoles.Sales, UserRoles.Manager, UserRoles.SuperAdmin)]
        public async Task<IActionResult> ProcessOrder([FromBody] OrderRequest request)
        {
            try
            {
                var result = await _externalIntegrationService.ProcessOrderAsync(
                    request.CustomerId, 
                    request.ProductId, 
                    request.Quantity, 
                    request.TotalAmount);
                
                return Ok($"Order processing result: {result}");
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Order processing failed: {ex.Message}");
            }
        }

        [HttpPost, Route("upload-specifications")]
        [Roles(UserRoles.Admin, UserRoles.SuperAdmin)]
        public async Task<IActionResult> UploadProductSpecifications([FromForm] ProductSpecificationUploadRequest request)
        {
            try
            {
                // Convert IFormFile to byte array
                byte[] specificationData;
                using (var memoryStream = new System.IO.MemoryStream())
                {
                    await request.File.CopyToAsync(memoryStream);
                    specificationData = memoryStream.ToArray();
                }

                var success = await _externalIntegrationService.UploadProductSpecificationAsync(
                    request.ProductId, 
                    request.File.FileName, 
                    specificationData);

                if (success)
                {
                    return Ok($"Product specifications uploaded successfully: {request.File.FileName}");
                }
                else
                {
                    return BadRequest("Product specification upload to external storage failed");
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Specification upload failed: {ex.Message}");
            }
        }

        [HttpGet, Route("analyze-sales/{productId}")]
        [Roles(UserRoles.Auditor, UserRoles.FinanceManager, UserRoles.SuperAdmin)]
        public async Task<IActionResult> AnalyzeProductSales(string productId)
        {
            try
            {
                var analysis = await _externalIntegrationService.AnalyzeProductSalesAsync(productId);
                return Ok($"Sales Analysis for product {productId}: {analysis}");
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Sales analysis failed: {ex.Message}");
            }
        }

        [HttpPost, Route("report-quality-issue")]
        [Roles(UserRoles.QualityControl, UserRoles.Manager, UserRoles.SuperAdmin)]
        public async Task<IActionResult> ReportQualityIssue([FromBody] QualityIssueRequest request)
        {
            try
            {
                var success = await _externalIntegrationService.ReportQualityIssueAsync(
                    request.ProductId, 
                    request.IssueType, 
                    request.Description,
                    request.Severity);

                if (success)
                {
                    return Ok($"Quality issue reported successfully for product: {request.ProductId}");
                }
                else
                {
                    return BadRequest("Failed to report quality issue to external services");
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Quality issue reporting failed: {ex.Message}");
            }
        }

        [HttpGet, Route("export-inventory/{productId}")]
        [Roles(UserRoles.SuperAdmin, UserRoles.FinanceManager)]
        public async Task<IActionResult> ExportProductInventoryData(string productId)
        {
            try
            {
                var exportData = await _externalIntegrationService.ExportInventoryDataAsync(productId);
                
                if (exportData != null && exportData.Length > 0)
                {
                    return File(exportData, "application/json", $"inventory_export_{productId}.json");
                }
                else
                {
                    return Ok($"Inventory export initiated for product: {productId}. Export will be available shortly.");
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Inventory export failed: {ex.Message}");
            }
        }

        [HttpGet, Route("supplier-info")]
        [Roles(UserRoles.SuperAdmin, UserRoles.Manager, UserRoles.Admin)]
        public IActionResult GetSupplierInfo()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. Supplier information accessible by Manager, Admin, and SuperAdmin.");
        }

        [HttpGet, Route("manufacturing-details")]
        [Roles(UserRoles.SuperAdmin, UserRoles.QualityControl)]
        public IActionResult GetManufacturingDetails()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. Manufacturing details accessible only by SuperAdmin and QualityControl.");
        }

        [HttpGet, Route("public-catalog")]
        [AllowAnonymous]
        public IActionResult GetPublicCatalog()
        {
            return Ok("Public product catalog accessible by everyone, no JWT token required. Basic product information only.");
        }

        [HttpGet, Route("warranty-info")]
        [Authorize(Policy = KeyConstants.CustomAuthorizationPolicyName)]
        public IActionResult GetWarrantyInfo()
        {
            return Ok("Policy-based access for warranty information. Product warranties, service agreements, etc.");
        }

        [HttpGet, Route("sales-performance")]
        [Roles(UserRoles.Sales, UserRoles.Manager, UserRoles.SuperAdmin)]
        public IActionResult GetSalesPerformance()
        {
            var current = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok($"Current user: {current}. Sales performance data accessible by Sales, Manager, and SuperAdmin.");
        }

        [HttpGet, Route("confidential-specs")]
        [Roles(UserRoles.SuperAdmin)]
        public IActionResult GetConfidentialSpecifications()
        {
            return Ok("Confidential product specifications (trade secrets, formulas) accessible only by SuperAdmin.");
        }

        [HttpGet, Route("inventory-levels")]
        [Roles(UserRoles.Employee, UserRoles.Manager, UserRoles.Admin, UserRoles.SuperAdmin)]
        public IActionResult GetInventoryLevels()
        {
            return Ok("Current inventory levels accessible by Employee, Manager, Admin, and SuperAdmin.");
        }

        [HttpPut, Route("update-pricing")]
        [Roles(UserRoles.FinanceManager, UserRoles.SuperAdmin)]
        public IActionResult UpdateProductPricing([FromBody] ProductPricingModel pricing)
        {
            return Ok("Product pricing updates allowed only by FinanceManager and SuperAdmin.");
        }

        [HttpDelete, Route("discontinue/{productId}")]
        [Roles(UserRoles.SuperAdmin)]
        public IActionResult DiscontinueProduct(string productId)
        {
            return Ok($"Product discontinuation (ID: {productId}) allowed only by SuperAdmin role.");
        }

        [HttpGet, Route("my-orders")]
        [Authorize]
        public IActionResult GetMyOrders()
        {
            var userId = _authContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return Ok($"Order history for user: {userId}. Users can view their own orders.");
        }

        [HttpGet, Route("profit-margins")]
        [Roles(UserRoles.FinanceManager, UserRoles.SuperAdmin)]
        public IActionResult GetProfitMargins()
        {
            return Ok("Profit margin information accessible by FinanceManager and SuperAdmin only.");
        }

        [HttpGet, Route("regulatory-compliance")]
        [Roles(UserRoles.QualityControl, UserRoles.Admin, UserRoles.SuperAdmin)]
        public IActionResult GetRegulatoryCompliance()
        {
            return Ok("Regulatory compliance information accessible by QualityControl, Admin, and SuperAdmin.");
        }

        [Authorize(Policy = ClaimTypes.Country)]
        [HttpGet, Route("region-restricted")]
        public IActionResult GetRegionRestrictedProducts()
        {
            return Ok($"Request had {ClaimTypes.Country} claim. Region-restricted product access granted.");
        }

        [HttpGet, Route("audit-trail")]
        [Roles(UserRoles.Auditor, UserRoles.SuperAdmin)]
        public IActionResult GetProductAuditTrail()
        {
            return Ok("Product modification audit trail accessible by Auditor and SuperAdmin.");
        }

        [HttpPost, Route("recall-product")]
        [Roles(UserRoles.SuperAdmin)]
        public IActionResult RecallProduct([FromBody] ProductRecallRequest request)
        {
            return Ok($"Product recall for {request.ProductId} initiated. Critical safety action by SuperAdmin.");
        }

        // External integration endpoints
        [HttpPost, Route("sync-inventory")]
        [Roles(UserRoles.Admin, UserRoles.SuperAdmin)]
        public async Task<IActionResult> SyncInventoryWithExternalSystems([FromBody] ProductModel product)
        {
            try
            {
                var success = await _externalIntegrationService.SyncInventoryAsync(product);
                return Ok($"Inventory synchronization {'succeeded' if success else 'failed'} for product: {product.Name}");
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Inventory sync failed: {ex.Message}");
            }
        }

        [HttpPost, Route("update-marketplace")]
        [Roles(UserRoles.Sales, UserRoles.Admin, UserRoles.SuperAdmin)]
        public async Task<IActionResult> UpdateMarketplaceListing([FromBody] MarketplaceUpdateRequest request)
        {
            try
            {
                var success = await _externalIntegrationService.UpdateMarketplaceListingAsync(
                    request.ProductId, 
                    request.Platform, 
                    request.ListingData);

                if (success)
                {
                    return Ok($"Marketplace listing updated successfully for product: {request.ProductId}");
                }
                else
                {
                    return BadRequest("Failed to update marketplace listing");
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Marketplace update failed: {ex.Message}");
            }
        }
    }

    // Request models for external integration endpoints
    public class OrderRequest
    {
        public string CustomerId { get; set; }
        public string ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ProductSpecificationUploadRequest
    {
        public string ProductId { get; set; }
        public IFormFile File { get; set; }
    }

    public class QualityIssueRequest
    {
        public string ProductId { get; set; }
        public string IssueType { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; } = "Medium";
    }

    public class ProductRecallRequest
    {
        public string ProductId { get; set; }
        public string Reason { get; set; }
        public string NotificationText { get; set; }
    }

    public class MarketplaceUpdateRequest
    {
        public string ProductId { get; set; }
        public string Platform { get; set; }
        public object ListingData { get; set; }
    }

    // Supporting models
    public class ProductModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public string SKU { get; set; }
    }

    public class ProductPricingModel
    {
        public string ProductId { get; set; }
        public decimal NewPrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal MarginPercent { get; set; }
    }

    // Additional service interface for product-specific operations
    public interface IProductService
    {
        Task<ProductModel> CreateProductAsync(ProductModel product);
        Task<ProductModel> GetProductByIdAsync(string productId);
        Task<bool> UpdateProductAsync(ProductModel product);
        Task<bool> DeleteProductAsync(string productId);
    }
}