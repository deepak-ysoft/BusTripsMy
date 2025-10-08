using Azure;
using BusTrips.Domain.Entities;
using BusTrips.Infrastructure.Identity;
using BusTrips.Infrastructure.Persistence;
using BusTrips.Web.Interface;
using BusTrips.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using static BusTrips.Web.Services.OrganizationService;

namespace BusTrips.Web.Controllers;

[Authorize(Roles = AppRoles.User)]
public class OrganizationsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly IOrganizationService _organizationService;
    private readonly IOrganizationPermissionService _orgPermissionService;
    public OrganizationsController(AppDbContext db, UserManager<AppUser> users, IOrganizationService organizationService, IOrganizationPermissionService orgPermissionService)
    {
        _db = db; _users = users;
        _organizationService = organizationService;
        _orgPermissionService = orgPermissionService;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    #region Org
    public async Task<IActionResult> Index() // List of organizations for the user
    {
        var items = await _organizationService.GetUserOrganizationsAsync(CurrentUserId); // Fetch user's organizations

        // Pass TempData to ViewBag if you want
        ViewBag.IsSuccess = TempData["IsSuccess"];
        ViewBag.Message = TempData["Message"];
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyOrganizationsPartial() // Partial view for user's organizations
    {
        var data = await _organizationService.GetUserOrganizationsAsync(CurrentUserId); // Fetch user's organizations
        return PartialView("_OrganizationsList", data.MyOrg);
    }

    [HttpGet]
    public async Task<IActionResult> GetInvitedOrganizationsPartial() // Partial view for organizations user is invited to
    {
        var data = await _organizationService.GetUserOrganizationsAsync(CurrentUserId); // Fetch user's organizations
        return PartialView("_OrganizationsList", data.InvitedOrg);
    }

    //[HttpGet]
    //public async Task<IActionResult> OrganizationDetails(Guid id)
    //{
    //    var items = await _organizationService.GetUserOrganizationDetailsAsync(id, CurrentUserId);

    //    // Pass TempData to ViewBag if you want
    //    ViewBag.IsSuccess = TempData["IsSuccess"];
    //    ViewBag.Message = TempData["Message"];

    //    ViewData["ActiveMenu"] = "Organizations";
    //    return View(items);
    //}

    [HttpGet]
    public async Task<IActionResult> OrganizationDetails(Guid id) // Basic org details view
    {
        ViewData["ActiveMenu"] = "Organizations";
        var items = await _organizationService.GetOrganizationDetailsAsync(id, CurrentUserId); // Fetch org details
        return View(new OrgDetailsVm { Id = id, MemberType = items.Data.MemberType });
    }

    [HttpGet("/Organizations/OrgDetailsJson")] 
    public async Task<IActionResult> OrgDetails(Guid orgId) // AJAX endpoint for org details JSON
    {
        ViewData["ActiveMenu"] = "Organizations";
        var items = await _organizationService.GetOrganizationDetailsAsync(orgId, CurrentUserId); // Fetch org details
        if (!items.IsSuccess)
            return Json(new { isSuccess = false, message = items.Message });

        return Json(new { isSuccess = true, data = items.Data });
    }

    // Groups
    [HttpGet("/Organizations/OrgGroupsJson")]
    public async Task<IActionResult> OrgGroupsJson(Guid orgId) // AJAX endpoint for org groups JSON
    {
        ViewData["ActiveMenu"] = "Organizations";
        var result = await _organizationService.GetOrganizationGroupsAsync(orgId); // Fetch org groups
        if (!result.IsSuccess)
            return Json(new List<object>()); // return empty array if failed

        return Json(result.Data); // must be pure array for DataTables
    }

    // Members
    [HttpGet("/Organizations/OrgMembersJson")]
    public async Task<IActionResult> OrgMembersJson(Guid orgId) // AJAX endpoint for org members JSON
    {
        ViewData["ActiveMenu"] = "Organizations";
        var result = await _organizationService.GetOrganizationMembersAsync(orgId, CurrentUserId); // Fetch org members
        if (!result.IsSuccess)
            return Json(new List<object>());

        return Json(result.Data);
    }

    [HttpGet("Organizations/MemberDetails")]
    public async Task<IActionResult> MemberDetails(Guid userId, Guid orgId, bool isAjax = false) // Member details view or AJAX
    {
        ViewData["ActiveMenu"] = "Organizations";
        var response = await _organizationService.GetMemberDetailsByAdminAsync(userId, orgId); // Fetch member details

        if (response == null)
            return Json(new { isSuccess = false, message = "Member not found" });

        if (isAjax)
            return Json(new { isSuccess = true, member = response.Member, org = response.Org });

        return Json(response); // fallback if accessed directly
    }

    [HttpPost]
    public async Task<IActionResult> Invite(InviteManagerVm vm)
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
            redirectTo = Url.Action("OrganizationDetails", "Organizations", new { id = vm.OrganizationId })
        });
    }


    [HttpGet]
    public async Task<IActionResult> Delete(Guid id) // Delete organization action
    {
        var response = await _organizationService.DeleteOrganizationAsync(id); // Attempt to delete org
        // Use TempData to persist data after redirect
        TempData["IsSuccess"] = response.IsSuccess;
        TempData["Message"] = response.Message;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetOrganization(Guid id) // AJAX endpoint for org details JSON
    {
        var org = await _organizationService.GetOrganizationAsync(id); // Fetch org details
        if (org == null) RedirectToAction(nameof(Index));
        return Json(org);
    }


    [HttpPost]
    public async Task<IActionResult> AddEditOrg(CreateOrganizationVm vm) // Add or edit organization action
    {
        if (!ModelState.IsValid)
            return Json(new { isSuccess = false, message = "Invalid input" });

        var response = await _organizationService.AddEditOrganizationAsync(vm, CurrentUserId);
        return Json(new { isSuccess = response.IsSuccess, message = response.Message });
    }

    //[HttpGet]
    //public async Task<IActionResult> MemberDetails(Guid userId, Guid orgId)
    //{
    //    ViewData["ActiveMenu"] = "Organizations";
    //    var response = await _organizationService.GetMemberDetailsAsync(userId, orgId,
    //        CurrentUserId);

    //    if (response == null)
    //    {

    //        ViewBag.IsSuccess = TempData["IsSuccess"];
    //        ViewBag.Message = TempData["Message"];
    //        return RedirectToAction(nameof(OrganizationDetails), new { id = orgId });
    //    }

    //    // Pass TempData to ViewBag if you want
    //    ViewBag.IsSuccess = TempData["IsSuccess"];
    //    ViewBag.Message = TempData["Message"];

    //    return View(response);
    //}

    [HttpPost]
    public async Task<IActionResult> ChangeOrgMemberRole(Guid targetUserId, Guid orgId, MemberTypeEnum action) // Change member role action
    {
        var response = await _organizationService.ChangeOrgMemberRoleAsync(targetUserId, orgId, CurrentUserId, action); // Attempt to change role
        return Json(new { isSuccess = response.IsSuccess, message = response.Message });
    }

    [HttpGet]
    public async Task<IActionResult> SelfRemoveFromOrg(Guid orgId, string memberType) // Self-remove from org action
    {
        if (!Enum.TryParse<MemberTypeEnum>(memberType, true, out var type))
        {
            return Json(new { isSuccess = false, message = "Invalid member type." });
        }

        var response = await _organizationService.SelfRemoveFromOrgAsync(CurrentUserId, orgId, type); // Attempt to self-remove

        return Json(new { isSuccess = response.IsSuccess, message = response.Message });
    }



    //[HttpGet]
    //public async Task<IActionResult> RemoveMember(Guid userId, Guid orgId)
    //{
    //    var response = await _organizationService.RemoveMemberAsync(userId, orgId, CurrentUserId);
    //    if (!response.IsSuccess)
    //    {
    //        // Use TempData to persist data after redirect
    //        TempData["IsSuccess"] = response.IsSuccess;
    //        TempData["Message"] = response.Message;
    //        return RedirectToAction(nameof(MemberDetails), new { userId = userId, orgId = orgId });
    //    }

    //    // Use TempData to persist data after redirect
    //    TempData["IsSuccess"] = response.IsSuccess;
    //    TempData["Message"] = response.Message;
    //    return RedirectToAction(nameof(OrganizationDetails), new { id = orgId });
    //}

    //[HttpGet]
    //public async Task<IActionResult> MakeRemoverAdmin(Guid userId, Guid orgId)
    //{
    //    var response = await _organizationService.MakeRemoverAdminAcync(userId, orgId);
    //    if (!response.IsSuccess)
    //    {
    //        // Use TempData to persist data after redirect
    //        TempData["IsSuccess"] = response.IsSuccess;
    //        TempData["Message"] = response.Message;
    //        return RedirectToAction(nameof(MemberDetails), new { userId = userId, orgId = orgId });
    //    }

    //    // Use TempData to persist data after redirect
    //    TempData["IsSuccess"] = response.IsSuccess;
    //    TempData["Message"] = response.Message;
    //    return RedirectToAction(nameof(OrganizationDetails), new { id = orgId });
    //}

    //[HttpGet]
    //public async Task<IActionResult> MakeCreator(Guid userId, Guid orgId)
    //{
    //    var response = await _organizationService.MakeCreatorAcync(userId, orgId, CurrentUserId);
    //    if (!response.IsSuccess)
    //    {
    //        // Use TempData to persist data after redirect
    //        TempData["IsSuccess"] = response.IsSuccess;
    //        TempData["Message"] = response.Message;
    //        return RedirectToAction(nameof(MemberDetails), new { userId = userId, orgId = orgId });
    //    }

    //    // Use TempData to persist data after redirect
    //    TempData["IsSuccess"] = response.IsSuccess;
    //    TempData["Message"] = response.Message;
    //    return RedirectToAction(nameof(OrganizationDetails), new { id = orgId });
    //}

    //[HttpGet]
    //public async Task<IActionResult> SelfRemoveFromCreator(Guid orgId)
    //{
    //    var response = await _organizationService.SelfRemoveFromCreatorAcync(CurrentUserId, orgId);
    //    // Use TempData to persist data after redirect
    //    TempData["IsSuccess"] = response.IsSuccess;
    //    TempData["Message"] = response.Message;
    //    return RedirectToAction(nameof(OrganizationDetails), new { id = orgId });
    //}

    //[HttpGet]
    //public async Task<IActionResult> SelfRemoveFromAdmin(Guid orgId)
    //{
    //    var response = await _organizationService.SelfRemoveFromAdminAcync(CurrentUserId, orgId);
    //    // Use TempData to persist data after redirect
    //    TempData["IsSuccess"] = response.IsSuccess;
    //    TempData["Message"] = response.Message;
    //    return RedirectToAction(nameof(OrganizationDetails), new { id = orgId });
    //}

    //[HttpGet]
    //public async Task<IActionResult> SelfRemoveAsMemberFromOrg(Guid orgId)
    //{
    //    var response = await _organizationService.SelfRemoveAsMemberFromOrgAsync(CurrentUserId, orgId);
    //    // Use TempData to persist data after redirect
    //    TempData["IsSuccess"] = response.IsSuccess;
    //    TempData["Message"] = response.Message;
    //    return RedirectToAction(nameof(OrganizationDetails), new { id = orgId });
    //}
    #endregion

    #region Group

    [HttpGet]
    public async Task<IActionResult> GetUserGroups(Guid orgId) // List of groups in an organization for the user
    {
        var groups = await _organizationService.GetUserGroupsAsync(CurrentUserId, orgId); // Fetch user's groups in the org

        // Pass TempData to ViewBag if you want
        ViewBag.IsSuccess = TempData["IsSuccess"];
        ViewBag.Message = TempData["Message"];

        ViewData["ActiveMenu"] = "Organizations";
        ViewBag.OrganizationId = orgId;
        return View(groups.Data);
    }

    [HttpGet]
    public async Task<IActionResult> GetGroup(Guid id) // AJAX endpoint for group details JSON
    {
        var group = await _organizationService.GetGroupAsync(id); // Fetch group details

        // Pass TempData to ViewBag if you want
        ViewBag.IsSuccess = TempData["IsSuccess"];
        ViewBag.Message = TempData["Message"];
        // return JSON object that matches what your JS expects

        ViewData["ActiveMenu"] = "Organizations";
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

    [HttpGet]
    public async Task<IActionResult> GetGroupDetails(Guid? id) // Group details view
    {
        var group = id.HasValue ? await _organizationService.GetGroupDetailsAsync(id.Value) : null; // Fetch group details if id provided
        // Use TempData to persist data after redirect
        TempData["IsSuccess"] = group.IsSuccess;
        TempData["Message"] = group.Message;

        ViewData["ActiveMenu"] = "Organizations";

        return View("GroupDetails", group.Data);
    }

    [HttpPost]
    public async Task<IActionResult> AddEditGroup(GroupRequestVM vm) // Add or edit group action
    {
        if (!ModelState.IsValid)
        {
            return Json(new { isSuccess = false, message = "Invalid input" });
        }

        var response = await _organizationService.AddEditGroupAsync(vm, CurrentUserId); // Attempt to add/edit group

        return Json(new
        {
            isSuccess = response.IsSuccess,
            message = response.Message
        });
    }

    #endregion

    #region OrganizationPermission

    [HttpGet]
    public async Task<IActionResult> GetPermission(Guid orgId) // Permission management view for an organization
    {
        var permissions = await _orgPermissionService.GetPermissionsAsync(orgId); // Fetch org permissions

        // fetch user orgs for dropdown 
        var userOrgs = await _organizationService.GetUserOrganizationsAsync(CurrentUserId); 

        var vm = new PermissionPageVM
        {
            OrgId = orgId,
            OrgName = permissions.FirstOrDefault()?.OrgName ?? string.Empty, // take name from first record
            Permissions = permissions,
            Organizations = userOrgs
        };

        ViewData["ActiveMenu"] = "Organizations";
        return View(vm);
    }

    // AJAX endpoint to load permissions JSON
    [HttpGet]
    public async Task<IActionResult> GetPermissionData(Guid orgId)
    {
        var result = await _orgPermissionService.GetPermissionsAsync(orgId); // Fetch org permissions
        return Ok(result); // JSON for AJAX
    }

    [HttpPost]
    public async Task<IActionResult> CreatePermission([FromBody] PermissionRequestVM request) // Create new permission action
    {
        var result = await _orgPermissionService.CreatePermissionAsync(request);
        return Ok(result);
    }

    [HttpPut("/Organizations/UpdatePermission/{pid}")]
    public async Task<IActionResult> UpdatePermission(Guid pid, [FromBody] PermissionRequestVM request) // Update permission action
    {
        var result = await _orgPermissionService.UpdatePermissionAsync(pid, request); // Attempt to update permission
        if (result == null) return NotFound();
        return Ok(result);
    }

    #endregion
}
