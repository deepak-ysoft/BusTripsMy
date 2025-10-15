using BusTrips.Domain.Entities;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BusTrips.Web.Models;

public class LoginVm
{
    [Required, EmailAddress]
    public string Email { get; set; }
    [Required, DataType(DataType.Password)]
    public string Password { get; set; }
}

public class RegisterVm
{
    [Required]
    [RegularExpression("^[a-zA-Z0-9._%+-]+@[a-zA-Z.-]+\\.[a-zA-Z]{2,}$", ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; }
    [DisplayName("Secondary Email")]
    [RegularExpression("^[a-zA-Z0-9._%+-]+@[a-zA-Z.-]+\\.[a-zA-Z]{2,}$", ErrorMessage = "Please enter a valid email address.")]
    [EmailAddress] public string? SecondaryEmail { get; set; }
    [DisplayName("First Name")]
    [Required] public string FirstName { get; set; }
    [DisplayName("Last Name")]
    [Required] public string LastName { get; set; }
    [DisplayName("Phone Number")]
    [RegularExpression(@"^(?!\+?(\d)\1{5,14}$)\+?[0-9]{6,15}$",
    ErrorMessage = "Enter a valid mobile number (not all digits the same, with or without country code)")]
    [Required] public string PhoneNumber { get; set; }
    [DisplayName("Secondary Phone Number")]
    [RegularExpression(@"^(?!\+?(\d)\1{5,14}$)\+?[0-9]{6,15}$",
    ErrorMessage = "Enter a valid mobile number (not all digits the same, with or without country code)")]
    public string? PhoneNumber2 { get; set; }

    [Required, DataType(DataType.Password)]
    [RegularExpression("^(?=.*[a-zA-Z])(?=.*\\d)(?=.*[@$!%*?&])[A-Za-z\\d@$!%*#?&]{8,16}$", ErrorMessage = "Must Enter At Least 8 and Max 16 characters and must include Uppercase, Lowercase, digit and Special character")]
    public string Password { get; set; }
    [DisplayName("License Number")]
    public string? LicenseNumber { get; set; }
    [DisplayName("License Province")]
    public string? LicenseProvince { get; set; }
    public bool AcceptedUserTerms { get; set; }
}
public class AddUserVm
{
    public Guid OrgId { get; set; }
    [RegularExpression("^[a-zA-Z0-9._%+-]+@[a-zA-Z.-]+\\.[a-zA-Z]{2,}$", ErrorMessage = "Please enter a valid email address.")]
    [Required, EmailAddress] public string Email { get; set; }
    [DisplayName("First Name")]
    [Required] public string FirstName { get; set; }
    [DisplayName("Last Name")]
    [Required] public string LastName { get; set; }
    [DisplayName("Phone Number")]
    [RegularExpression(@"^(?!\+?(\d)\1{5,14}$)\+?[0-9]{6,15}$",
    ErrorMessage = "Enter a valid mobile number (not all digits the same, with or without country code)")]
    [Required] public string PhoneNumber { get; set; }
    [Required, DataType(DataType.Password)] public string Password { get; set; }
    [Required]
    public MemberTypeEnum? MemberType { get; set; }
    public string? returnUrl { get; set; }
}

public class UserRequestVm
{
    public Guid? UserId { get; set; }
    [Required, EmailAddress]
    [RegularExpression("^[a-zA-Z0-9._%+-]+@[a-zA-Z.-]+\\.[a-zA-Z]{2,}$", ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; }
    [DisplayName("Secondary Email")]
    [EmailAddress]
    [RegularExpression("^[a-zA-Z0-9._%+-]+@[a-zA-Z.-]+\\.[a-zA-Z]{2,}$", ErrorMessage = "Please enter a valid email address.")]
    public string? SecondaryEmail { get; set; }
    [DisplayName("First Name")]
    [Required]
    public string FirstName { get; set; }
    [DisplayName("Last Name")]
    [Required]
    public string LastName { get; set; }
    [DisplayName("Phone Number")]
    [Required]
    [RegularExpression(@"^(?!\+?(\d)\1{5,14}$)\+?[0-9]{6,15}$",
    ErrorMessage = "Enter a valid mobile number (not all digits the same, with or without country code)")]
    public string PhoneNumber { get; set; }
    [DisplayName("Secondary Phone Number")]
    [RegularExpression(@"^(?!\+?(\d)\1{5,14}$)\+?[0-9]{6,15}$",
    ErrorMessage = "Enter a valid mobile number (not all digits the same, with or without country code)")]
    public string? PhoneNumber2 { get; set; }
    [DisplayName("Driver Image")]
    public IFormFile? UserImg { get; set; }
    public string? PhotoUrl { get; set; }
    [DisplayName("License Number")]
    public string? LicenseNumber { get; set; }
    [DisplayName("License Province")]
    public string? LicenseProvince { get; set; }
    [DisplayName("Birth Date")]
    public DateOnly? BirthDate { get; set; }
    [DisplayName("Employment Type")]
    public String? EmploymentType { get; set; }
    public string? LicenseFrontUrl { get; set; }
    public string? LicenseBackUrl { get; set; }
    [DisplayName("License Front Image")]
    public IFormFile? LicenseFrontImg { get; set; }
    [DisplayName("License Back Image")]
    public IFormFile? LicenseBackImg { get; set; }

    public bool IsActive { get; set; }
    [DisplayName("Deactive Discription")]
    public string? DeActiveDiscription { get; set; }

    // 🔑 new property
    public string FullName => $"{FirstName} {LastName}";
}

public class UserResponseVm
{
    public Guid UserId { get; set; }
    public string Email { get; set; }
    public string? SecondaryEmail { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public string? PhoneNumber2 { get; set; }
    public string? LicenseNumber { get; set; }
    public string? LicenseProvince { get; set; }
    // convenience property for UI/API response
    public string FullName => $"{FirstName} {LastName}";
    public string? ReturnUrl { get; set; }
    public string? CreatedAt { get; set; }
    public DateOnly? BirthDate { get; set; }
    public String? EmploymentType { get; set; }
    public string? PhotoUrl { get; set; }
    public string? LicenseFrontUrl { get; set; }
    public string? LicenseBackUrl { get; set; }
    public string? env { get; set; }
    public string? Role { get; set; }
    public string? ApprovalStatus { get; set; }
    public List<OrgListItemVm>? Organizations { get; set; }

}
//public class OrgListWithUserId
//{
//    public Guid UserId { get; set; }
//    public List<OrgListItemVm> MyOrganizations { get; set; } = new();
//    public List<OrgListItemVm> InvitedOrganizations { get; set; } = new();
//}

public class UserVM
{
    public Guid? UserId { get; set; }
    public string? PhotoUrl { get; set; }
    public string OrganizarionName { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool isActive { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
}

public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Current password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; }

    [Required(ErrorMessage = "New password is required.")]
    [RegularExpression("^(?=.*[a-zA-Z])(?=.*\\d)(?=.*[@$!%*?&])[A-Za-z\\d@$!%*#?&]{8,16}$", ErrorMessage = "Must Enter At Least 8 and Max 16 characters and must include Uppercase, Lowercase, digit and Special character")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public string ConfirmNewPassword { get; set; }

}
public class EmailSettings
{
    public string SmtpServer { get; set; }
    public int SmtpPort { get; set; }
    public string SmtpUser { get; set; }
    public string SmtpPass { get; set; }
}
public class ResetPasswordViewModel
{
    [Required]
    public string Email { get; set; }

    [Required]
    public string Token { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [RegularExpression("^(?=.*[a-zA-Z])(?=.*\\d)(?=.*[@$!%*?&])[A-Za-z\\d@$!%*#?&]{8,16}$", ErrorMessage = "Must Enter At Least 8 and Max 16 characters and must include Uppercase, Lowercase, digit and Special character")]

    public string Password { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
public class RegisterDriverVm : RegisterVm
{
}
