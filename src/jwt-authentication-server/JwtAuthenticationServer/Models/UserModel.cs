using System;
using System.ComponentModel.DataAnnotations;

namespace JwtAuthenticationServer.Models
{
    public class UserModel
    {
        // Existing properties
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        
        // Basic Personal Information (PII)
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        
        // Contact Information (PII)
        [EmailAddress]
        public string Email { get; set; }
        [Phone]
        public string PhoneNumber { get; set; }
        public string MobileNumber { get; set; }
        
        // Address Information (PII)
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string Country { get; set; }
        
        // Government Identifiers (Highly Sensitive)
        public string SocialSecurityNumber { get; set; }
        public string DriversLicenseNumber { get; set; }
        public string PassportNumber { get; set; }
        public string TaxId { get; set; }
        
        // Financial Information (Highly Sensitive)
        public string BankAccountNumber { get; set; }
        public string BankRoutingNumber { get; set; }
        public string CreditCardNumber { get; set; }
        public string PayrollNumber { get; set; }
        
        // Employment Information (Sensitive)
        public string EmployeeId { get; set; }
        public string Department { get; set; }
        public string JobTitle { get; set; }
        public DateTime? HireDate { get; set; }
        public decimal? Salary { get; set; }
        public string ManagerId { get; set; }
        
        // Emergency Contact (PII)
        public string EmergencyContactName { get; set; }
        public string EmergencyContactPhone { get; set; }
        public string EmergencyContactRelationship { get; set; }
        
        // Medical Information (Highly Sensitive)
        public string MedicalConditions { get; set; }
        public string Medications { get; set; }
        public string BloodType { get; set; }
        public string Allergies { get; set; }
        
        // Security Information (Sensitive)
        public string SecurityQuestion1 { get; set; }
        public string SecurityAnswer1 { get; set; }
        public string SecurityQuestion2 { get; set; }
        public string SecurityAnswer2 { get; set; }
        public string TwoFactorSecret { get; set; }
        
        // Biometric Data (Highly Sensitive)
        public byte[] FingerprintData { get; set; }
        public byte[] FaceRecognitionData { get; set; }
        
        // System Metadata
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string CreatedBy { get; set; }
        public string LastModifiedBy { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginDate { get; set; }
        
        // Additional Sensitive Fields
        public string Nationality { get; set; }
        public string MaritalStatus { get; set; }
        public int? NumberOfDependents { get; set; }
        public string VeteranStatus { get; set; }
        public string DisabilityStatus { get; set; }
        public string EthnicBackground { get; set; }
        
        // IT/Security Related
        public string ActiveDirectoryGuid { get; set; }
        public string[] AssignedCertificates { get; set; }
        public string[] SecurityClearances { get; set; }
        public DateTime? PasswordLastChanged { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? AccountLockedUntil { get; set; }
    }
}