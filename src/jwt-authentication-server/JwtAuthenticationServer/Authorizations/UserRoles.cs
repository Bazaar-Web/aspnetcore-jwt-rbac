namespace JwtAuthenticationServer.Authorizations
{
    public static class UserRoles
    {
        public const string Manager = "Manager";
        public const string Sales = "Sales";
    }
}
namespace JwtAuthenticationServer.Authorizations
{
    public static class UserRoles
    {
        // Existing roles
        public const string Manager = "Manager";
        public const string Sales = "Sales";
        
        // Administrative roles
        public const string Admin = "Admin";
        public const string SuperAdmin = "SuperAdmin";
        
        // Material/Inventory related roles
        public const string InventoryManager = "InventoryManager";
        public const string WarehouseStaff = "WarehouseStaff";
        public const string PurchasingAgent = "PurchasingAgent";
        public const string QualityControl = "QualityControl";
        
        // Production roles
        public const string ProductionManager = "ProductionManager";
        public const string ProductionStaff = "ProductionStaff";
        public const string MaterialPlanner = "MaterialPlanner";
        
        // Financial roles
        public const string Accountant = "Accountant";
        public const string FinanceManager = "FinanceManager";
        
        // General user roles
        public const string Employee = "Employee";
        public const string Viewer = "Viewer";
        public const string Guest = "Guest";
        
        // Specialized roles
        public const string Auditor = "Auditor";
        public const string Supervisor = "Supervisor";
        public const string Technician = "Technician";
    }
}