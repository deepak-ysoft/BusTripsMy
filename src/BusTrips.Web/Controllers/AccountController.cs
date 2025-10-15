using Azure;
using BusTrips.Domain.Entities;
using BusTrips.Infrastructure.Persistence;
using BusTrips.Web.Interface;
using BusTrips.Web.Models;
using BusTrips.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NuGet.Common;
using System.Data;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace BusTrips.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAccountService _account;
    private readonly IUserService _usersService;
    private readonly IDriverService _driverService;
    private readonly IOrganizationService _orgService;
    private readonly UserManager<AppUser> _users;
    private readonly IEmailSender _emailSender;
    private readonly SignInManager<AppUser> _signIn;
    private readonly INotificationService _notificationService;
    private readonly AppDbContext _dbContext;
    public AccountController(IAccountService account, UserManager<AppUser> users, IEmailSender emailSender, IUserService usersService, IDriverService driverService, SignInManager<AppUser> signIn, IOrganizationService orgService, INotificationService notificationService,AppDbContext dbContext)
    {
        _account = account;
        _users = users;
        _emailSender = emailSender;
        _usersService = usersService;
        _driverService = driverService;
        _signIn = signIn;
        _orgService = orgService;
        _notificationService = notificationService;
        _dbContext = dbContext;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ===== User Register=====

    [HttpGet]
    public IActionResult RegisterUser()
    {
        ViewBag.Role = "User";
        return View("Register", new RegisterVm());
    }

    [HttpPost]
    public async Task<IActionResult> RegisterUser(RegisterVm vm)
    {
        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return Json(new { success = false, errors });
            }

            ViewBag.Role = "User";
            return View("Register", vm);
        }


        // Begin a database transaction
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // Register the admin
            var response = await _account.RegisterAsync(vm, "User");

            if (!response.IsSuccess)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = response.Message });

                ModelState.AddModelError("", response.Message);
                ViewBag.Role = "User";
                return View("Register", vm);
            }

            // Send confirmation email
            var emailResult = await SendConfirmationEmailAsync(response.Data);

            // If email fails, throw exception to rollback
            if (emailResult.StartsWith("Error"))
                throw new Exception(emailResult);

            // Commit transaction if everything is fine
            await transaction.CommitAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, message = response.Message, redirectUrl = Url.Action("LoginUser", "Account") });

            return RedirectToAction(nameof(LoginUser));
        }
        catch (Exception ex)
        {
            // Rollback transaction
            await transaction.RollbackAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = false, message = "Registration failed: " + ex.Message });

            ModelState.AddModelError("", "Registration failed: " + ex.Message);
            ViewBag.Role = "User";
            return View("Register", vm);
        }
    }

    // ===== DRIVER Register=====
    [HttpGet]
    public IActionResult RegisterDriver()
    {
        ViewBag.Role = "Driver";
        return View("Register", new RegisterVm());
    }

    [HttpPost]
    public async Task<IActionResult> RegisterDriver(RegisterVm vm)
    {
        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return Json(new { success = false, errors });
            }

            ViewBag.Role = "Driver";
            return View("Register", vm);
        }

        // Begin a database transaction
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // Register the admin
            var response = await _account.RegisterAsync(vm, "Driver");

            if (!response.IsSuccess)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = response.Message });

                ModelState.AddModelError("", response.Message);
                ViewBag.Role = "Driver";
                return View("Register", vm);
            }

            // Send confirmation email
            var emailResult = await SendConfirmationEmailAsync(response.Data);

            // If email fails, throw exception to rollback
            if (emailResult.StartsWith("Error"))
                throw new Exception(emailResult);

            // Commit transaction if everything is fine
            await transaction.CommitAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, message = response.Message, redirectUrl = Url.Action("LoginDriver", "Account") });

            return RedirectToAction(nameof(LoginDriver));
        }
        catch (Exception ex)
        {
            // Rollback transaction
            await transaction.RollbackAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = false, message = "Registration failed: " + ex.Message });

            ModelState.AddModelError("", "Registration failed: " + ex.Message);
            ViewBag.Role = "Driver";
            return View("Register", vm);
        }
    }

    // ===== ADMIN Register=====
    [HttpGet]
    public IActionResult RegisterAdmin()
    {
        ViewBag.Role = "Admin";
        return View("Register", new RegisterVm());
    }

    [HttpPost]
    public async Task<IActionResult> RegisterAdmin(RegisterVm vm)
    {
        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return Json(new { success = false, errors });
            }

            ViewBag.Role = "Admin";
            return View("Register", vm);
        }

        // Begin a database transaction
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // Register the admin
            var response = await _account.RegisterAsync(vm, "Admin");

            if (!response.IsSuccess)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = response.Message });

                ModelState.AddModelError("", response.Message);
                ViewBag.Role = "Admin";
                return View("Register", vm);
            }

            // Send confirmation email
            var emailResult = await SendConfirmationEmailAsync(response.Data);

            // If email fails, throw exception to rollback
            if (emailResult.StartsWith("Error"))
                throw new Exception(emailResult);

            // Commit transaction if everything is fine
            await transaction.CommitAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, message = response.Message, redirectUrl = Url.Action("LoginAdmin", "Account") });

            return RedirectToAction(nameof(LoginAdmin));
        }
        catch (Exception ex)
        {
            // Rollback transaction
            await transaction.RollbackAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = false, message = "Registration failed: " + ex.Message });

            ModelState.AddModelError("", "Registration failed: " + ex.Message);
            ViewBag.Role = "Admin";
            return View("Register", vm);
        }
    }

    // ===== USER LOGIN =====
    [HttpGet]
    public IActionResult LoginUser()
    {
        ModelState.Clear();
        ViewBag.Role = "User";
        return View("Login", new LoginVm());
    }

    [HttpPost]
    public async Task<IActionResult> LoginUser(LoginVm vm)
    {
        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,               // property name
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return Json(new { success = false, errors });
            }

            ViewBag.Role = "User";
            return View("Login", vm);
        }

        // service to login user
        var response = await _account.LoginAsync(vm, "User");
        if (!response.IsSuccess)
        {
            if (response.Message == "Confirm Email")
            {
                var user = await _users.FindByEmailAsync(vm.Email);
                var message = await SendConfirmationEmailAsync(user);
                return Json(new { success = false, message = message });
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = false, message = response.Message });

            ModelState.AddModelError("", response.Message);
            ViewBag.Role = "User";
            return View("Login", vm);
        }

        // ✅ Success → AJAX vs Normal
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
        }

        return RedirectToAction("Index", "Home");
    }

    // ===== DRIVER LOGIN =====
    [HttpGet]
    public IActionResult LoginDriver()
    {
        ModelState.Clear();
        ViewBag.Role = "Driver";
        return View("Login", new LoginVm());
    }

    [HttpPost]
    public async Task<IActionResult> LoginDriver(LoginVm vm)
    {
        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,               // property name
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return Json(new { success = false, errors });
            }

            ViewBag.Role = "Driver";
            return View("Login", vm);
        }

        var response = await _account.LoginAsync(vm, "Driver");
        if (!response.IsSuccess)
        {
            if (response.Message == "Confirm Email")
            {
                var user = await _users.FindByEmailAsync(vm.Email);
                var message = await SendConfirmationEmailAsync(user);
                return Json(new { success = false, message = message });
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = false, message = response.Message });

            ModelState.AddModelError("", response.Message);
            ViewBag.Role = "Driver";
            return View("Login", vm);
        }

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });

        return RedirectToAction("Index", "Home");
    }

    // ===== ADMIN LOGIN =====
    [HttpGet]
    public IActionResult LoginAdmin()
    {
        ModelState.Clear();
        ViewBag.Role = "Admin";
        return View("Login", new LoginVm());
    }

    [HttpPost]
    public async Task<IActionResult> LoginAdmin(LoginVm vm)
    {
        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,               // property name
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return Json(new { success = false, errors });
            }

            ViewBag.Role = "Admin";
            return View("Login", vm);
        }

        var response = await _account.LoginAsync(vm, "Admin");
        if (!response.IsSuccess)
        {
            if (response.Message == "Confirm Email")
            {
                var user = await _users.FindByEmailAsync(vm.Email);
                var message = await SendConfirmationEmailAsync(user);
                return Json(new { success = false, message = message });
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = false, message = response.Message });

            ModelState.AddModelError("", response.Message);
            ViewBag.Role = "Admin";
            return View("Login", vm);
        }

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { success = true, redirectUrl = Url.Action("IndexAdmin", "Home") });

        return RedirectToAction("IndexAdmin", "Home");
    }

    // Consfirm Email address
    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(Guid userId, string token, string redirectTo)
    {
        // Find user by user id
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            TempData["IsSuccess"] = false;
            TempData["Message"] = "User not found.";
            return RedirectToAction(redirectTo);
        }

        // Already confirmed
        if (user.EmailConfirmed)
        {
            TempData["IsSuccess"] = false;
            TempData["Message"] = "Your email is already confirmed. Please log in.";
            return RedirectToAction(redirectTo);
        }

        // Try confirm
        var result = await _users.ConfirmEmailAsync(user, token);

        if (result.Succeeded)
        {
            //await _orgService.CreatorDefaultOrgAcync(user.Id);
            TempData["IsSuccess"] = true;
            TempData["Message"] = "Your email has been confirmed successfully.";
            return RedirectToAction(redirectTo);
        }

        // Expired or invalid token
        TempData["IsSuccess"] = false;
        TempData["Message"] = "The confirmation link is invalid or has expired. Please request a new one.";
        return RedirectToAction(nameof(ResendConfirmationEmail), new { userId = user.Id });
    }

    [HttpGet] // Resend confirmation email
    public async Task<IActionResult> ResendConfirmationEmail(Guid userId)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            TempData["Message"] = "User not found.";
            return RedirectToAction("LoginUser");
        }

        var message = await SendConfirmationEmailAsync(user);

        // Determine redirect based on role
        var roles = await _users.GetRolesAsync(user);
        var redirectTo = roles.FirstOrDefault() switch
        {
            "Admin" => "LoginAdmin",
            "Driver" => "LoginDriver",
            _ => "LoginUser"
        };

        TempData["Message"] = message;
        return RedirectToAction(redirectTo);
    }

    // Send confirmation email method
    private async Task<string> SendConfirmationEmailAsync(AppUser user)
    {
        if (user.EmailConfirmed)
            return "Email already confirmed.";

        var token = await _users.GenerateEmailConfirmationTokenAsync(user);

        // Find user role
        var roles = await _users.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "User";

        var redirectTo = role switch
        {
            "Admin" => "LoginAdmin",
            "Driver" => "LoginDriver",
            _ => "LoginUser"
        };

        var confirmationLink = Url.Action("ConfirmEmail", "Account",
            new { userId = user.Id, token = token, redirectTo }, protocol: Request.Scheme);
        try
        {
            // Build the path to the shared view
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "Shared", "BusLogo.html");
            var logoHtml = System.IO.File.ReadAllText(logoPath);

            // Insert it into the email body
            var htmlBody = $@"
            <html>
            <body style='font-family: Arial, sans-serif; color: #333; margin:0; padding:0; width:100%;'>
                {logoHtml} <!-- Bus Logo here -->
                <h2 style='color:#2E86C1;'>Welcome, {user.FirstName ?? "User"}!</h2>
                <p>Please confirm your account by clicking the button below:</p>
                <p style='text-align: center;'>
                    <a href='{confirmationLink}' style='background-color:#2E86C1; color:#fff; padding:10px 20px; text-decoration:none; border-radius:5px;'>Confirm Email</a>
                </p>
                <hr/>
                <p style='font-size:14px; color:#888;'>If you did not register, please ignore this email.</p>
                <p style='font-size:12px; color:#888;'>This is an automated message, please do not reply.</p>
            </body>
            </html>";

            await _emailSender.SendEmailAsync(user.Email, "Confirm your email", htmlBody);

            return "Your email is not confirmed. A new confirmation email has been sent to your email address.";
        }
        catch (Exception ex)
        {
            return $"Error sending email: {ex.Message}";
        }
    }

    [HttpGet]
    public async Task<IActionResult> Logout() // Logout for all roles
    {
        var currentUser = await _users.GetUserAsync(User);
        var userRoles = currentUser != null ? await _users.GetRolesAsync(currentUser) : new List<string>();
        var role = userRoles.FirstOrDefault() ?? "User";

        await _account.LogoutAsync();

        // Redirect back to correct login page by role
        return role switch
        {
            "Admin" => RedirectToAction("LoginAdmin"),
            "Driver" => RedirectToAction("LoginDriver"),
            _ => RedirectToAction("LoginUser")
        };
    }

    [HttpGet]
    public IActionResult AccessDenied() // 403 page 
    {
        return View();
    }

    [HttpGet]
    public IActionResult AddUser(Guid orgId) // Add user to organization
    {
        ViewBag.OrganizationId = orgId;
        ModelState.Clear();
        ViewData["Title"] = "Add New Member";
        var memberTypes = Enum.GetValues(typeof(MemberTypeEnum))
         .Cast<MemberTypeEnum>()
         .Where(m => m != MemberTypeEnum.Creator) // 👈 Exclude Creator
         .Select(m => new SelectListItem
         {
             Value = m.ToString(),
             Text = m.ToString()
         }).ToList();
        ViewData["ActiveMenu"] = "Organizations";

        ViewBag.MemberTypes = memberTypes;
        var redirectUrl = Url.Action("OrganizationDetails", "Organizations", new { id = orgId });
        return View(new AddUserVm { OrgId = orgId, returnUrl = redirectUrl });
    }

    [HttpPost]
    public async Task<IActionResult> AddUser(AddUserVm vm) // Post add user to organization
    {
        var memberTypes = Enum.GetValues(typeof(MemberTypeEnum))
            .Cast<MemberTypeEnum>()
            .Where(m => m != MemberTypeEnum.Creator)
            .Select(m => new SelectListItem
            {
                Value = m.ToString(),
                Text = m.ToString()
            }).ToList();

        ViewBag.MemberTypes = memberTypes;

        if (!ModelState.IsValid) // Validation errors
        {
            // Return JSON with validation errors
            var errors = ModelState
                .Where(ms => ms.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            return Json(new { isSuccess = false, errors });
        }

        var response = await _account.AddUserAsync(vm, CurrentUserId); // Service to add user
        if (!response.IsSuccess)
        {
            return Json(new { isSuccess = false, message = response.Message });
        }

        var token = await _users.GenerateEmailConfirmationTokenAsync(response.Data); // Generate email confirmation token
        var confirmationLink = Url.Action("ConfirmEmail", "Account",
            new { userId = response.Data.Id, token = token, redirectTo = "LoginUser" }, protocol: Request.Scheme); // Build confirmation link

        // Build the path to the shared view
        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "Shared", "BusLogo.html");
        var logoHtml = System.IO.File.ReadAllText(logoPath);

        // Insert it into the email body
        var htmlBody = $@"
            <html>
            <body style='font-family: Arial, sans-serif; color: #333; margin:0; padding:0; width:100%;'>
                {logoHtml} <!-- Bus Logo here -->
                <h2 style='color:#2E86C1;'>Welcome, {response.Data.FirstName ?? "User"}!</h2>
                <p>Please confirm your account by clicking the button below:</p>
                <p style='text-align: center;'>
                    <a href='{confirmationLink}' style='background-color:#2E86C1; color:#fff; padding:10px 20px; text-decoration:none; border-radius:5px;'>Confirm Email</a>
                </p>
                <hr/>
                <p style='font-size:12px; color:#888;'>If you did not register, please ignore this email.</p>
            </body>
            </html>";

        await _emailSender.SendEmailAsync(
            response.Data.Email,
            "Confirm your email",
            htmlBody
        );

        // Return response right away
        var redirectUrl = Url.Action("OrganizationDetails", "Organizations", new { id = vm.OrgId });
        return Json(new { isSuccess = true, redirectTo = redirectUrl, message = "Member Added successfully" });

    }

    [HttpGet]
    public async Task<IActionResult> UserProfileSetting() // User profile details
    {
        var user = await _usersService.GetUserDetailsForAdminByIdAsync(CurrentUserId);
        return View(user ?? new UserResponseVm());
    }

    [HttpGet]
    public async Task<IActionResult> EditUser() // Edit user profile details
    {
        AppUser user = await _users.FindByIdAsync(CurrentUserId.ToString());
        if (user is null) return RedirectToAction("LoginUser");

        var driver = await _driverService.GetDriverByIdAsync(CurrentUserId); // Get driver details if user is a driver

        var vm = new UserRequestVm
        {
            UserId = user.Id,
            Email = user.Email,
            SecondaryEmail = user.SecondaryEmail,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhotoUrl = user.PhotoUrl,
            PhoneNumber = user.PhoneNumber,
            PhoneNumber2 = user.PhoneNumber2,
            LicenseNumber = driver?.LicenseNumber,
            LicenseProvince = driver?.LicenseProvince,
            BirthDate = driver?.BirthDate,
            EmploymentType = driver?.EmploymentType,
        };

        ViewBag.ActiveTab = "personalDetails";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(UserRequestVm model) // Post edit user profile details
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            return Json(new { success = false, errors });
        }

        var user = await _users.FindByIdAsync(model.UserId.ToString());
        if (user is null)
            return Json(new { success = false, message = "User not found." });

        // ------------------------------
        // Server-side uniqueness checks
        // ------------------------------

        // Check Email
        if (!string.IsNullOrEmpty(model.Email))
        {
            var emailExists = await _users.Users
                .AnyAsync(u => u.Email.ToLower() == model.Email.ToLower() && u.Id != user.Id);
            if (emailExists)
                ModelState.AddModelError("Email", "The email is already used by another user.");
        }

        // Check SecondaryEmail
        if (!string.IsNullOrEmpty(model.SecondaryEmail))
        {
            var secondaryEmailExists = await _users.Users
                .AnyAsync(u => u.SecondaryEmail.ToLower() == model.SecondaryEmail.ToLower() && u.Id != user.Id);
            if (secondaryEmailExists)
                ModelState.AddModelError("SecondaryEmail", "The secondary email is already used by another user.");

            if (model.SecondaryEmail.ToLower() == model.Email.ToLower())
                ModelState.AddModelError("SecondaryEmail", "Secondary email must be different from primary email.");
        }

        // Check PhoneNumber
        if (!string.IsNullOrEmpty(model.PhoneNumber))
        {
            var phoneExists = await _users.Users
                .AnyAsync(u => u.PhoneNumber == model.PhoneNumber && u.Id != user.Id);
            if (phoneExists)
                ModelState.AddModelError("PhoneNumber", "Phone number is already used by another user.");
        }

        // Check PhoneNumber2
        if (!string.IsNullOrEmpty(model.PhoneNumber2))
        {
            var phone2Exists = await _users.Users
                .AnyAsync(u => u.PhoneNumber2 == model.PhoneNumber2 && u.Id != user.Id);
            if (phone2Exists)
                ModelState.AddModelError("PhoneNumber2", "Alternate phone number is already used by another user.");

            if (model.PhoneNumber2 == model.PhoneNumber)
                ModelState.AddModelError("PhoneNumber2", "Alternate phone number must be different from primary phone number.");
        }

        // ------------------------------
        // Return all validation errors
        // ------------------------------
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            return Json(new { success = false, errors });
        }

        // ------------------------------
        // Update user
        // ------------------------------
        user.Email = model.Email;
        user.SecondaryEmail = model.SecondaryEmail;
        user.UserName = model.Email;
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.PhoneNumber = model.PhoneNumber;
        user.PhoneNumber2 = model.PhoneNumber2;
        if (model.UserImg != null)
        {
            var photoUrl = await _driverService.SaveFileAsync(model.UserImg, "uploads/userImg");

            if (!string.IsNullOrEmpty(photoUrl))
                user.PhotoUrl = photoUrl;
        }

        await _users.UpdateAsync(user);

        var roles = await _users.GetRolesAsync(user);
        if (roles.FirstOrDefault() == "Driver")
            await _driverService.UpdateDriverAsync(model, user);

        return Json(new { success = true, message = "Profile updated successfully!" });
    }

    [HttpGet]
    public IActionResult ForgotPassword(string? message = null) // Forgot password page
    {
        ViewBag.Message = message;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model) // Post forgot password
    {
        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                return Json(new { isSuccess = false, errors });
            }

            return View(model);
        }

        var user = await _users.FindByEmailAsync(model.Email); // Find user by email
        if (user == null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { isSuccess = false, message = "Your email is not registered." });

            ViewBag.IsSuccess = false;
            ViewBag.Message = "Your email is not registered.";
            return View(model);
        }

        var token = await _users.GeneratePasswordResetTokenAsync(user); // Generate password reset token
        var callbackUrl = Url.Action("ResetPassword", "Account",
            new { token = token, email = model.Email }, protocol: Request.Scheme); // Build reset link

        // Build the path to the shared view
        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "Shared", "BusLogo.html");
        var logoHtml = System.IO.File.ReadAllText(logoPath);

        // Insert it into the email body
        var htmlBody = $@"
            <html>
            <body style='font-family: Arial, sans-serif; color: #333; margin:0; padding:0; width:100%;'>
                {logoHtml} <!-- Bus Logo here -->
                <h2 style='color:#2E86C1; text-align:center;'>Password Reset Request</h2>
                <p style='text-align:center;'>Hello {user.FirstName ?? "User"},</p>
                <p style='text-align:center;'>We received a request to reset your password. Click the button below to choose a new password:</p>
                <p style='text-align:center;'>
                    <a href='{callbackUrl}' style='background-color:#2E86C1; color:#fff; padding:10px 20px; text-decoration:none; border-radius:5px; display:inline-block;'>Reset Password</a>
                </p>
                <p style='text-align:center;'>If you did not request a password reset, you can safely ignore this email.</p>
                <hr style='margin:20px 0;'/>
                <p style='font-size:12px; color:#888; text-align:center;'>This is an automated message, please do not reply.</p>
            </body>
            </html>";

        await _emailSender.SendEmailAsync(model.Email, "Reset Password", htmlBody);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { isSuccess = true, message = $"Please check your email: {model.Email}" });

        ViewBag.IsSuccess = true;
        ViewBag.Message = $"Please check your email: {model.Email}";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model) // Change password for logged in user
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            return Json(new { success = false, errors });
        }

        var user = await _users.GetUserAsync(User); // Get current user
        if (user == null)
            return Json(new { success = false, message = "User not found." });

        var result = await _users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword); // Change password

        if (!result.Succeeded)
        {
            var errorList = result.Errors.Select(e => e.Description).ToArray();
            return Json(new { success = false, errors = new { CurrentPassword = errorList } });
        }

        await _signIn.RefreshSignInAsync(user);
        return Json(new { success = true, message = "Password changed successfully!" });
    }

    [HttpGet]
    public IActionResult ResetPassword(string token, string email) // Reset password page
    {
        var model = new ResetPasswordViewModel { Token = token, Email = email };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model) // Post reset password
    {
        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                return Json(new { isSuccess = false, errors });
            }
            return View(model);
        }

        var user = await _users.FindByEmailAsync(model.Email); // Find user by email
        if (user == null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { isSuccess = false, message = "User not found." });

            ViewBag.IsSuccess = false;
            ViewBag.Message = "User not found.";
            return View(model);
        }

        var result = await _users.ResetPasswordAsync(user, model.Token, model.Password); // Reset password with token
        if (result.Succeeded)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { isSuccess = true, redirectUrl = Url.Action("LoginUser", "Account") });

            return RedirectToAction("LoginUser", "Account");
        }

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var errors = result.Errors.Select(e => e.Description).ToArray();
            return Json(new { isSuccess = false, errors });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }

    #region Notifications

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        var notifications = await _notificationService.GetUserNotificationsAsync(CurrentUserId);
        return View(notifications);
    }

    [HttpGet]
    public async Task<IActionResult> Notifications()
    {
        var notifications = await _notificationService.GetUserNotificationsAsync(CurrentUserId);
        var unreadCount = notifications.Count(n => !n.IsRead);

        return Json(new
        {
            count = unreadCount,
            data = notifications
        });
    }

    [HttpGet]
    public async Task<IActionResult> NotificationDetails(Guid id)
    {
        var notification = await _notificationService.NotificationDetailsAsync(id);
        if (notification == null)
            return Content("<p>Notification not found.</p>", "text/html");

        // Return partial view or formatted HTML content
        return PartialView("_NotificationDetails", notification);
    }

    #endregion
}
