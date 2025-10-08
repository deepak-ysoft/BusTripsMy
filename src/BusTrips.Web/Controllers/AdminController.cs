using Azure;
using BusTrips.Domain.Entities;
using BusTrips.Infrastructure.Identity;
using BusTrips.Web.Interface;
using BusTrips.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Plugins;
using System.Security.Claims;

namespace BusTrips.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class AdminController : Controller
{
    private readonly IUserService _userService;
    private readonly UserManager<AppUser> _users;
    private readonly IDriverService _driverService;
    private readonly IOrganizationService _organizationService;
    private readonly ITripService _tripService;
    private readonly IEquipmentService _equipmentService;
    private readonly IOrganizationPermissionService _orgPermissionService;
    private readonly IAccountService _account;
    private readonly IEmailSender _emailSender;

    public AdminController(
        IUserService userService,
        IDriverService driverService,
        IOrganizationService organizationService,
        ITripService tripService,
        IEquipmentService equipmentService,
        IOrganizationPermissionService orgPermissionService, UserManager<AppUser> users, IAccountService account, IEmailSender emailSender)
    {
        _userService = userService;
        _driverService = driverService;
        _organizationService = organizationService;
        _tripService = tripService;
        _equipmentService = equipmentService;
        _orgPermissionService = orgPermissionService;
        _users = users;
        _account = account;
        _emailSender = emailSender;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    #region Users

    // Users page
    public IActionResult Users() => View(); // just load the view, data comes via AJAX

    // JSON endpoint for DataTable
    [HttpGet]
    public async Task<IActionResult> GetUsersJson()  
    {
        var users = await _userService.GetAllUsersAsync();
        users ??= new List<UserVM>();

        return Json(users); // must be a JSON array
    }
    [HttpGet]
    public async Task<IActionResult> UserDetails(Guid userId) // Details page
    {
        var user = await _userService.GetUserDetailsForAdminByIdAsync(userId);
        if (user.Role == "User")
        {
            ViewData["ActiveMenu"] = "Users";        // Parent menu
            ViewData["ActiveChild"] = "Users";     // Child menu you want active
        }
        else
        {
            ViewData["ActiveMenu"] = "Users";        // Parent menu
            ViewData["ActiveChild"] = "Drivers";      // Child menu you want active
        }

        return View(user ?? new UserResponseVm());
    }

    // Edit GET (inline redirect)
    [HttpGet]
    public async Task<IActionResult> EditUser(Guid userId)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user == null) return RedirectToAction("Users");

        var driver = await _driverService.GetDriverByIdAsync(userId);

        var vm = new UserRequestVm
        {
            UserId = user.Id,
            Email = user.Email,
            SecondaryEmail = user.SecondaryEmail,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            PhoneNumber2 = user.PhoneNumber2,
            IsActive = user.IsActive,
            DeActiveDiscription = user.DeActiveDiscription,
            LicenseNumber = driver?.LicenseNumber,
            LicenseProvince = driver?.LicenseProvince,
            BirthDate = driver?.BirthDate,
            EmploymentType = driver?.EmploymentType,
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> EditUser(UserRequestVm model) // Edit POST
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                                   .ToDictionary(
                                       kvp => kvp.Key,
                                       kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                                   );
            return Json(new { isSuccess = false, errors = errors });
        }
        var user = await _users.FindByIdAsync(model.UserId.ToString());
        if (user is null)
            return Json(new { isSuccess = false, message = "User not found." });

        // ==== CUSTOM VALIDATIONS ====

        // Email uniqueness
        if (!string.IsNullOrEmpty(model.Email))
        {
            var emailExists = await _users.Users
                .AnyAsync(u => u.Email.ToLower() == model.Email.ToLower() && u.Id != user.Id);
            if (emailExists)
                ModelState.AddModelError("Email", "The email you entered is already registered by another user.");
        }

        // Secondary email
        if (!string.IsNullOrEmpty(model.SecondaryEmail))
        {
            var secondaryEmailExists = await _users.Users
                .AnyAsync(u => u.SecondaryEmail.ToLower() == model.SecondaryEmail.ToLower() && u.Id != user.Id);
            if (secondaryEmailExists)
                ModelState.AddModelError("SecondaryEmail", "The secondary email you entered is already registered by another user.");
            if (model.SecondaryEmail.ToLower() == model.Email.ToLower())
                ModelState.AddModelError("SecondaryEmail", "The secondary email must be different from the primary email.");
        }

        // Phone number uniqueness
        if (!string.IsNullOrEmpty(model.PhoneNumber))
        {
            var phoneExists = await _users.Users
                .AnyAsync(u => u.PhoneNumber == model.PhoneNumber && u.Id != user.Id);
            if (phoneExists)
                ModelState.AddModelError("PhoneNumber", "The phone number you entered is already registered by another user.");
        }

        if (!string.IsNullOrEmpty(model.PhoneNumber2))
        {
            var phone2Exists = await _users.Users
                .AnyAsync(u => u.PhoneNumber2 == model.PhoneNumber2 && u.Id != user.Id);
            if (phone2Exists)
                ModelState.AddModelError("PhoneNumber2", "The alternate phone number you entered is already registered by another user.");
            if (model.PhoneNumber2 == model.PhoneNumber)
                ModelState.AddModelError("PhoneNumber2", "The alternate phone number must be different from the primary phone number.");
        }

        // Deactivation reason
        if (!model.IsActive && string.IsNullOrEmpty(model.DeActiveDiscription))
            ModelState.AddModelError("DeActiveDiscription", "Please provide valid reason for deactive account.");

        // ==== RETURN VALIDATION ERRORS IF ANY ====
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                                   .ToDictionary(
                                       kvp => kvp.Key,
                                       kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                                   );
            return Json(new { isSuccess = false, errors = errors });
        }

        // ==== UPDATE USER ====
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.SecondaryEmail = model.SecondaryEmail;
        user.PhoneNumber = model.PhoneNumber;
        user.PhoneNumber2 = model.PhoneNumber2;
        user.IsActive = model.IsActive;
        if (!string.IsNullOrEmpty(model.DeActiveDiscription))
            user.DeActiveDiscription = model.DeActiveDiscription;
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

        return Json(new { isSuccess = true });
    }

    // Delete user
    [HttpDelete]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        var result = await _userService.DeleteUserAsync(userId);
        return Json(new { isSuccess = result.IsSuccess, message = result.Message });
    }
    #endregion

    #region Drivers

    public async Task<IActionResult> Drivers(string? status = null) // Drivers page
    {
        var list = await _driverService.GetDriversAsync(status);
        return View(list);
    }

    [HttpPost]
    public async Task<IActionResult> Approve(Guid userId) 
    {
        var response = await _driverService.ApproveDriverAsync(userId); //  approve driver by userId 
        return RedirectToAction(nameof(UserDetails), new { userId = userId, requestFrom = "Drivers" });
    }

    [HttpPost]
    public async Task<IActionResult> Reject(Guid userId)
    {
        var response = await _driverService.RejectDriverAsync(userId); // reject driver by userId
        return RedirectToAction(nameof(UserDetails), new { userId = userId, requestFrom = "Drivers" });
    }

    #endregion

    #region Organizations

    [HttpGet]
    public IActionResult Organizations() // Organizations page
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetOrganizationsJson() // JSON endpoint for DataTable
    {
        var items = await _organizationService.GetOrganizationsAsync();
        return Json(items ?? new List<OrgListItemVm>());
    }

    [HttpGet]
    public IActionResult GetOrgDetails(Guid orgId) // Details page for Razor view
    {
        ViewData["ActiveMenu"] = "Organizations";
        return View(new OrgDetailsVm { Id = orgId }); // only Id filled
    }

    [HttpGet("/Admin/OrgDetailsJson")]
    public async Task<IActionResult> OrgDetails(Guid orgId) // AJAX endpoint for details tab data
    {
        ViewData["ActiveMenu"] = "Organizations";
        var items = await _organizationService.GetOrganizationDetailsAsync(orgId);
        if (!items.IsSuccess)
            return Json(new { isSuccess = false, message = items.Message });

        return Json(new { isSuccess = true, data = items.Data });
    }

    // Groups
    [HttpGet("/Admin/OrgGroupsJson")]
    public async Task<IActionResult> OrgGroupsJson(Guid orgId) // AJAX endpoint for groups tab data
    {
        ViewData["ActiveMenu"] = "Organizations";
        var result = await _organizationService.GetOrganizationGroupsAsync(orgId); // CurrentUserId is optional here
        if (!result.IsSuccess)
            return Json(new List<object>()); // return empty array if failed

        return Json(result.Data); // must be pure array for DataTables
    }

    // Members
    [HttpGet("/Admin/OrgMembersJson")] 
    public async Task<IActionResult> OrgMembersJson(Guid orgId) // AJAX endpoint for members tab data
    {
        var result = await _organizationService.GetOrganizationMembersAsync(orgId); // CurrentUserId is optional here
        if (!result.IsSuccess)
            return Json(new List<object>());

        return Json(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> MemberDetails(Guid userId, Guid orgId, bool isAjax = false) // AJAX endpoint for member details modal
    {
        var response = await _organizationService.GetMemberDetailsByAdminAsync(userId, orgId); // userId is member's userId here

        if (response == null)
            return Json(new { isSuccess = false, message = "Member not found" });

        if (isAjax)
            return Json(new { isSuccess = true, member = response.Member, org = response.Org });

        return Json(response); // fallback if accessed directly
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteOrgMember(Guid id) // id is membershipId here
    {
        var response = await _organizationService.DeleteOrgMemberAsync(id); // membershipId
        return Json(new { isSuccess = response.IsSuccess, message = response.Message });
    }

    [HttpPost]
    public async Task<IActionResult> ChangeMemberRole(Guid memberShipId, MemberTypeEnum newRole) 
    {
        var response = await _organizationService.ChangeMemberRoleAsync(memberShipId, newRole); 
        return Json(new { isSuccess = response.IsSuccess, message = response.Message });
    }

    [HttpGet]
    public IActionResult AddUser(Guid orgId) // GET for Add User modal
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

        var redirectUrl = Url.Action("GetOrgDetails", "Admin", new { orgId = orgId, tab = "members" });
        return View(new AddUserVm { OrgId = orgId, returnUrl = redirectUrl });
    }

    [HttpPost]
    public async Task<IActionResult> AddUser(AddUserVm vm) // POST for Add User modal
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

        if (!ModelState.IsValid)
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

        var response = await _account.AddUserAsync(vm, CurrentUserId); // create user first 
        if (!response.IsSuccess)
        {
            return Json(new { isSuccess = false, message = response.Message });
        }

        var token = await _users.GenerateEmailConfirmationTokenAsync(response.Data); // generate email confirmation token
        var confirmationLink = Url.Action("ConfirmEmail", "Account",
            new { userId = response.Data.Id, token = token, redirectTo = "LoginUser" }, protocol: Request.Scheme); // link to AccountController ConfirmEmail action 

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
        var redirectUrl = Url.Action("GetOrgDetails", "Admin", new { orgId = vm.OrgId, tab = "members" });
        return Json(new { isSuccess = true, redirectTo = redirectUrl, message = "Member Added successfully" });
    }

    [HttpPost]
    public async Task<IActionResult> Invite(InviteManagerVm vm) // POST for Invite User modal
    {
        var response = await _organizationService.InviteAsync(vm, CurrentUserId);

        if (!response.IsSuccess)
        {
            return Json(new
            {
                isSuccess = false,
                message = response.Message,
                errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                                   .ToDictionary(
                                       kvp => kvp.Key,
                                       kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                                   )
            });
        }

        return Json(new
        {
            isSuccess = true,
            message = response.Message,
            redirectTo = Url.Action("GetOrgDetails", "Admin", new { orgId = vm.OrganizationId, tab = "members" })
        });
    }

    [HttpGet]
    public async Task<IActionResult> OrganizationDetails(Guid id, Guid userId) // Details page for Razor view
    {
        ViewData["ActiveMenu"] = "Users";
        ViewData["ActiveChild"] = "Users";

        var items = await _organizationService.GetUserOrganizationDetailsAsync(id, CurrentUserId);
        ViewBag.DetailsUserId = userId;

        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> GetUserOrganizations(Guid userId) // AJAX endpoint for user's organizations partial
    {
        var userOrgs = await _organizationService.getUserOrgGropTripShortDetails(userId); // userId is the target user's ID here
        return PartialView("_UserOrganizationsPartial", userOrgs.Data);
    }

    [HttpGet]
    public async Task<IActionResult> GetOrganization(Guid id) // AJAX endpoint for Add/Edit modal
    {
        var org = await _organizationService.GetOrganizationAsync(id); // id is orgId here 
        if (org == null) RedirectToAction(nameof(Organizations));
        return Json(org);
    }

    [HttpPost]
    public async Task<IActionResult> AddEditOrg(CreateOrganizationVm vm) // POST for Add/Edit modal
    {
        if (!ModelState.IsValid)
            return Json(new { isSuccess = false, message = "Invalid input" });
        var response = await _organizationService.AddEditOrganizationByAdminAsync(vm, vm.userId.Value); // vm.userId is the admin's userId here
        return Json(new { isSuccess = response.IsSuccess, message = response.Message });
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteOrganization(Guid id) // id is orgId here
    {
        var response = await _organizationService.DeleteOrganizationAsync(id); 
        return Json(new { isSuccess = response.IsSuccess, message = response.Message });
    }

    #region Org Permissions

    [HttpGet]
    public async Task<IActionResult> GetPermission(Guid orgId) // Permissions page for Razor view
    {
        ViewData["ActiveMenu"] = "Users";
        ViewData["ActiveChild"] = "Users";

        var permissions = await _orgPermissionService.GetPermissionsAsync(orgId); // get all permissions for this org

        // fetch user orgs for dropdown
        //var userOrgs = await _organizationService.GetUserOrganizationsAsync(CurrentUserId);

        var vm = new PermissionPageVM
        {
            OrgId = orgId,
            OrgName = permissions.FirstOrDefault()?.OrgName ?? string.Empty, // take name from first record
            Permissions = permissions,
            //Organizations = userOrgs
        };

        ViewData["ActiveMenu"] = "Organizations";
        return View(vm);
    }

    // AJAX endpoint to load permissions JSON
    [HttpGet]
    public async Task<IActionResult> GetPermissionData(Guid orgId) // AJAX endpoint for permissions data
    {
        var result = await _orgPermissionService.GetPermissionsAsync(orgId); // get all permissions for this org
        return Ok(result); // JSON for AJAX
    }

    [HttpPut("/Admin/UpdatePermission/{pid}")]
    public async Task<IActionResult> UpdatePermission(Guid pid, [FromBody] PermissionRequestVM request) // Edit permission AJAX endpoint 
    {
        var result = await _orgPermissionService.UpdatePermissionAsync(pid, request); // pid is permissionId here
        if (result == null) return NotFound();
        return Ok(result);
    }

    #endregion

    #endregion

    #region Groups 

    [HttpGet]
    public async Task<IActionResult> GroupDetails(Guid groupId, Guid userId, string? bucket = null) // Details page for Razor view
    {
        ViewData["ActiveMenu"] = "Organizations";

        var list = await _tripService.GetTripsByGroupIdAsync(groupId, userId, bucket); // userId is the target user's ID here
        if (list?.Data != null)
        {
            list.Data.userId = userId;
        }

        return View(list.Data); // 👈 This loads your Razor view with model
    }

    [HttpGet]
    public async Task<IActionResult> GetTripsJson(Guid groupId, Guid userId, string? bucket = null) // AJAX endpoint for trips data
    {
        var list = await _tripService.GetTripsByGroupIdAsync(groupId, userId, bucket); // userId is the target user's ID here

        if (!list.IsSuccess || list.Data == null)
            return Json(new { success = false, message = list.Message });

        list.Data.userId = userId;
        return Json(list.Data.Trips); // 👈 Only send the Trips for DataTables
    }

    [HttpGet]
    public async Task<IActionResult> GetGroup(Guid id) // AJAX endpoint for Add/Edit modal
    {
        ViewData["ActiveMenu"] = "Users";
        ViewData["ActiveChild"] = "Users";

        var group = await _organizationService.GetGroupAsync(id); // id is groupId here 

        // return JSON object that matches what your JS expects
        return Json(new
        {
            Id = group.Data.Id,
            orgId = group.Data.OrgId,
            groupName = group.Data.GroupName,
            shortName = group.Data.ShortName,
            description = group.Data.Description,
            IsActive = group.Data.IsActive,
            DeActiveDiscription = group.Data.DeActiveDiscription,
        });
    }

    [HttpPost]
    public async Task<IActionResult> AddEditGroup(GroupRequestVM vm) // POST for Add/Edit modal
    {
        if (!ModelState.IsValid)
        {
            return Json(new { isSuccess = false, message = "Invalid input" });
        }

        var response = await _organizationService.AddEditGroupAsync(vm, CurrentUserId); // CurrentUserId is admin's userId here

        return Json(new
        {
            isSuccess = response.IsSuccess,
            message = response.Message
        });
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteGroup(Guid id) // id is groupId here
    {
        var response = await _organizationService.DeleteGroupAsync(id); 
        return Json(new { isSuccess = response.IsSuccess, message = response.Message });
    }

    #endregion

    #region Trips

    [HttpGet]
    public async Task<IActionResult> Trips(string? bucket = null) // Trips page for Razor view
    {
        var list = await _tripService.GetTripsForAdminAsync(bucket); // fetch all trips for admin

        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> TripDetails(Guid tripId) // Details page for Razor view
    {
        ViewData["ActiveMenu"] = "Trips";
        ViewData["ActiveChild"] = "Trips";
        var response = await _tripService.GetTripsDetailsAsync(tripId); // fetch trip details by tripId
        return View(response.Data);
    }

    [HttpGet]
    public async Task<IActionResult> CreateTrip(Guid groupId, Guid? userId, string returnUrl, string? controller) // GET for Create Trip page
    {
        ViewData["ActiveMenu"] = "Organizations";
        ViewData["Title"] = "Create Trip";

        userId ??= Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)); // default to current admin if not provided

        var vm = new CreateTripVm
        {
            groupId = groupId,
            userId = userId,
            returnUrl = returnUrl,
            controller = controller
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTrip(CreateTripVm vm, string returnUrl) // POST for Create Trip page
    {
        if (vm.NumberOfPassengers == null)
            ModelState.AddModelError("NumberOfPassengers", "Number of passengers is required.");
        if (vm.NumberOfPassengers == 0)
            ModelState.AddModelError("NumberOfPassengers", "Number of passengers must be greater than 0.");

        // Validate that destination arrival is after departure
        DateTime departureDateTime = vm.DepartureDate.ToDateTime(vm.DepartureTime);
        DateTime arrivalDateTime = vm.DestinationArrivalDate.ToDateTime(vm.DestinationArrivalTime);

        if (departureDateTime < DateTime.Now)
            ModelState.AddModelError(nameof(vm.DepartureDate), "Departure date/time must be future date/time.");

        if (arrivalDateTime <= departureDateTime)
            ModelState.AddModelError(nameof(vm.DestinationArrivalDate), "Arrival date/time must be later than departure date/time.");

        if (!ModelState.IsValid) 
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState
                    .Where(ms => ms.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return Json(new { success = false, errors });
            }
            return PartialView("~/Views/Trips/_TripFormPartial.cshtml", vm);
        }

        var response = await _tripService.CreateTripAsync(vm, CurrentUserId); // CurrentUserId is admin's userId here 

        if (!response.IsSuccess)
        {
            return Json(new
            {
                success = response.IsSuccess,
                message = response.Message,
            });
        }

        return Json(new
        {
            success = response.IsSuccess,
            message = response.Message,
            redirectUrl = returnUrl
        });
    }

    [HttpGet]
    public async Task<IActionResult> EditTrip(Guid id, Guid userId, string returnUrl,string? controller) // GET for Edit Trip page
    {
        if (returnUrl == "/Admin/Trips")
        {
            ViewData["ActiveMenu"] = "Trips";        // Parent menu
            ViewData["ActiveChild"] = "Trips";     // Child menu you want active
        }
        else
        {
            ViewData["ActiveMenu"] = "Organizations";
        }
        ViewData["Title"] = "Edit Trip";

        var vm = await _tripService.GetTripAsync(id); // id is tripId here
        if (vm == null)
        {
            TempData["IsSuccess"] = false;
            TempData["Message"] = "Trip Not Found";
            return Redirect(returnUrl);
        }

        ViewBag.IsEdit = true;
        vm.userId = userId;
        vm.returnUrl = returnUrl;
        vm.controller = controller;

        // Use same view as Create
        return View("CreateTrip", vm);
    }

    [HttpPost]
    public async Task<IActionResult> EditTrip(Guid id, CreateTripVm vm) // POST for Edit Trip page
    { 
        ViewBag.IsEdit = true;
        if (vm.NumberOfPassengers == null || vm.NumberOfPassengers == 0)
            ModelState.AddModelError("NumberOfPassengers", "Number of passengers must be greater than 0.");

        if (vm.TripDays == null || vm.TripDays == 0)
            ModelState.AddModelError("TripDays", "Trip days must be greater than 0.");

        // Validate that destination arrival is after departure
        DateTime departureDateTime = vm.DepartureDate.ToDateTime(vm.DepartureTime);
        DateTime arrivalDateTime = vm.DestinationArrivalDate.ToDateTime(vm.DestinationArrivalTime);

        if (departureDateTime < DateTime.Now)
            ModelState.AddModelError(nameof(vm.DepartureDate), "Departure date/time must be future date/time.");

        if (arrivalDateTime <= departureDateTime)
            ModelState.AddModelError(nameof(vm.DestinationArrivalDate), "Arrival date/time must be later than departure date/time.");

        if (!ModelState.IsValid)
        {
            // Return partial view for AJAX validation
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("~/Views/Trips/_TripFormPartial.cshtml", vm);

            return View("CreateTrip", vm);
        }

        var response = await _tripService.UpdateTripAsync(id, vm, vm.userId ?? CurrentUserId); // vm.userId is the target user's ID here

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new
            {
                success = response.IsSuccess,
                message = response.Message,
                redirectUrl = vm.returnUrl // Keep redirect consistent
            });
        }

        return Redirect(vm.returnUrl);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteTrip(Guid id) // id is tripId here
    {
        var response = await _tripService.DeleteTripAsync(id); 

        return Json(new
        {
            isSuccess = response.IsSuccess,
            message = response.Message
        });
    }

    [HttpGet]
    public async Task<IActionResult> Requests() // Trip Requests page for Razor view
    {
        var items = await _tripService.GetRequestsAsync(); // fetch all trip requests for admin
        // Pass TempData to ViewBag if you want
        ViewBag.IsSuccess = TempData["IsSuccess"];
        ViewBag.Message = TempData["Message"];
        return View(items);
    }

    [HttpPost]
    public async Task<IActionResult> AssignTrip(Guid id) // Assign Trip action
    {
        var response = await _tripService.AssignTripAsync(id); // id is tripId here
        TempData["IsSuccess"] = response.IsSuccess;
        TempData["Message"] = response.Message;
        return RedirectToAction(nameof(Requests));
    }

    [HttpGet]
    public async Task<IActionResult> ToAssign() // Trips to Assign page for Razor view
    {
        var items = await _tripService.GetTripsToAssignAsync(); // fetch all trips to assign for admin
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> ApproveOrRejectTrip(Guid tripId, string status, string reqFrom) // Approve or Reject Trip action
    {
        var response = await _tripService.ApproveOrRejectTripAsync(tripId, status); // status is "Approved" or "Rejected" here

        // Use TempData to persist data after redirect
        TempData["IsSuccess"] = response.IsSuccess;
        TempData["Message"] = response.Message;
        return RedirectToAction(nameof(TripDetails), new { tripId, reqFrom });

    }

    #endregion

    #region Equipment

    [HttpGet]
    public IActionResult Equipments() // Equipments page for Razor view
    {
        ViewData["isActive"] = "Equipments";
        //ViewBag.IsSuccess = TempData["IsSuccess"];
        //ViewBag.Message = TempData["Message"];
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetEquipmentsJson() // AJAX endpoint for equipments data
    {
        var items = await _equipmentService.GetEquipmentsAsync(); // fetch all equipments for admin
        return Json(items);
    }

    [HttpGet]
    public async Task<IActionResult> EquipmentDetails(Guid id) // Details page for Razor view
    {
        ViewData["ActiveMenu"] = "Equipments";
        var vm = await _equipmentService.GetEquipmentByIdAsync(id); // fetch equipment details by id
        if (vm is null)
        {
            //TempData["IsSuccess"] = false;
            //TempData["Message"] = "Details not Found";
            return RedirectToAction(nameof(Equipments));
        }
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> GetEquipmentDetailsPartial(Guid id) // AJAX endpoint for equipment details partial
    {
        var vm = await _equipmentService.GetEquipmentByIdAsync(id); // fetch equipment details by id
        if (vm == null) return NotFound();

        return PartialView("_EquipmentDetails", vm);
    }

    [HttpGet]
    public IActionResult AddEquipment()     // GET for Add Equipment page
    {
        ModelState.Clear();
        ViewData["ActiveMenu"] = "Equipments";
        return View(new EquipmentVM());
    }

    [HttpPost]
    public async Task<IActionResult> AddEquipment(EquipmentVM vm) // POST for Add Equipment page
    {
        if (!ModelState.IsValid)
        {
            return Json(new
            {
                isSuccess = false,
                errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                                   .ToDictionary(
                                       kvp => kvp.Key,
                                       kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                                   )
            });
        }

        // Business validation
        if (!vm.IsActive && string.IsNullOrWhiteSpace(vm.DeactivationReason))
        {
            ModelState.AddModelError(nameof(vm.DeactivationReason), "Please provide a reason for deactivating the equipment.");
            return Json(new
            {
                isSuccess = false,
                errors = new Dictionary<string, string[]> {
                { nameof(vm.DeactivationReason), new [] { "Please provide a reason for deactivating the equipment." } }
            }
            });
        }

        // Vehicle Images validation
        int maxImageFiles = 8;
        if (vm.VehicleImages != null)
        {
            if (vm.VehicleImages.Count > maxImageFiles)
            {
                return Json(new { isSuccess = false, errors = new { VehicleImages = new[] { $"You can upload a maximum of {maxImageFiles} images." } } });
            }

            foreach (var file in vm.VehicleImages)
            {
                if (!ValidateImage(file, out var error))
                {
                    return Json(new { isSuccess = false, errors = new { VehicleImages = new[] { error } } });
                }
            }
        }

        // Documents validation
        int maxDocFiles = 5;
        if (vm.Documents != null)
        {
            if (vm.Documents.Count > maxDocFiles)
            {
                return Json(new { isSuccess = false, errors = new { Documents = new[] { $"You can upload a maximum of {maxDocFiles} documents." } } });
            }

            foreach (var doc in vm.Documents)
            {
                if (doc.File != null && !ValidateDocument(doc.File, out var error))
                {
                    return Json(new { isSuccess = false, errors = new { Documents = new[] { error } } });
                }
            }
        }

        var response = await _equipmentService.AddEquipmentAsync(vm, CurrentUserId); // CurrentUserId is admin's userId here
        return Json(new { isSuccess = response.IsSuccess, message = response.Message });
    }


    [HttpGet]
    public async Task<IActionResult> EditEquipment(Guid id) // GET for Edit Equipment page
    {
        var vm = await _equipmentService.GetEquipmentByIdAsync(id); // fetch equipment details by id
        if (vm is null) return RedirectToAction(nameof(Equipments)); 
        ViewBag.IsEdit = true;
        ViewData["ActiveMenu"] = "Equipments";
        return View("AddEquipment", vm);
    }

    [HttpPost]
    public async Task<IActionResult> EditEquipment(EquipmentVM vm) // POST for Edit Equipment page
    {
        if (!ModelState.IsValid)
        {
            return Json(new
            {
                isSuccess = false,
                errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                                   .ToDictionary(
                                       kvp => kvp.Key,
                                       kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                                   )
            });
        }

        if (!vm.IsActive && string.IsNullOrWhiteSpace(vm.DeactivationReason))
        {
            return Json(new { isSuccess = false, errors = new { DeactivationReason = new[] { "Please provide a reason for deactivating the equipment." } } });
        }
        var imgDocCount = await _equipmentService.GetEquipmentImgAndDocByIdAsync(vm.Id); // get current counts from DB
        // Same validations for images and docs as above
        int maxImageFiles = 8;
        if (vm.VehicleImages != null)
        {
            if (vm.VehicleImages.Count + imgDocCount.ImageCount > maxImageFiles)
            {
                return Json(new { isSuccess = false, errors = new { VehicleImages = new[] { $"You can upload a maximum of {maxImageFiles} images." } } });
            }

            foreach (var file in vm.VehicleImages)
            {
                if (!ValidateImage(file, out var error))
                {
                    return Json(new { isSuccess = false, errors = new { VehicleImages = new[] { error } } });
                }
            }
        }

        int maxDocFiles = 5;
        if (vm.Documents != null)
        {
            if (vm.Documents.Count + imgDocCount.DocumentCount > maxDocFiles)
            {
                return Json(new { isSuccess = false, errors = new { Documents = new[] { $"You can upload a maximum of {maxDocFiles} documents." } } });
            }

            foreach (var doc in vm.Documents)
            {
                if (doc.File != null && !ValidateDocument(doc.File, out var error))
                {
                    return Json(new { isSuccess = false, errors = new { Documents = new[] { error } } });
                }
            }
        }

        var response = await _equipmentService.UpdateEquipmentAsync(vm, CurrentUserId); // CurrentUserId is admin's userId here
        return Json(new { isSuccess = response.IsSuccess, message = response.Message });
    }
    [HttpDelete]
    public async Task<IActionResult> DeleteEquipment(Guid id) // id is equipmentId here
    {
        var response = await _equipmentService.DeleteEquipmentAsync(id, CurrentUserId); // CurrentUserId is admin's userId here
        return Json(response);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteEquipmentImage(Guid id, string imgUrl) // id is equipmentId here
    {
        var response = await _equipmentService.DeleteEquipmentImageAsync(id, imgUrl); 
        return Json(response);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteEquipmentDocument(Guid id) // id is documentId here
    {
        var response = await _equipmentService.DeleteEquipmentDocumentAsync(id); 
        return Json(response);
    }

    private bool ValidateImage(IFormFile file, out string errorMessage)  // for images like jpg, png, gif, webp etc.
    {
        errorMessage = string.Empty;
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var maxFileSizeMB = 5;

        if (file == null)
        {
            errorMessage = "No file uploaded.";
            return false;
        }

        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(ext))
        {
            errorMessage = "Only JPG, PNG, GIF, or WebP files are allowed.";
            return false;
        }

        if (file.Length > maxFileSizeMB * 1024 * 1024)
        {
            errorMessage = $"File size must be less than {maxFileSizeMB} MB.";
            return false;
        }

        return true;
    }

    private bool ValidateDocument(IFormFile file, out string error) // for docs like pdf, docx, xlsx etc.
    {
        error = "";
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx", ".xls", ".xlsx" };
        var maxFileSizeMB = 5;

        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(ext))
        {
            error = "Invalid file type.";
            return false;
        }

        if (file.Length > maxFileSizeMB * 1024 * 1024)
        {
            error = $"File too large. Max {maxFileSizeMB} MB allowed.";
            return false;
        }

        return true;
    }

    #endregion
}
