using BusTrips.Web.Models;
using BusTrips.Web.Interface;
using BusTrips.Infrastructure.Persistence;
using BusTrips.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

public class FirstTimeController : Controller
{
    private readonly AppDbContext _db;
    private readonly IOrganizationService _orgService;
    private readonly ITripService _tripService;
    private readonly UserManager<AppUser> _users;

    public FirstTimeController(AppDbContext db, IOrganizationService orgService, ITripService tripService, UserManager<AppUser> users)
    {
        _db = db;
        _orgService = orgService;
        _tripService = tripService;
        _users = users;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public IActionResult Wizard()
    {
        TempData["ActiveMenu"] = "Wizard";
        ViewData["Title"] = "Wizard";
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> WizardStep(
        string step,
        CreateOrganizationVm orgVm = null,
        GroupRequestVM groupVm = null,
        string choice = null,
        bool clearValidation = false,
        bool isSubmit = false) // Handles each step of the wizard process 
    {
        if (clearValidation) ModelState.Clear(); // Clear old validations
        var existingOrg = await _orgService.GetDefaultOrganizationByUserAsync(CurrentUserId);
        var groupExist = new ResponseVM<GroupRequestVM>();
        if (existingOrg != null)
        {
            groupExist = await _orgService.GetGroupByOrgIdAsync(existingOrg.Id.Value);
        }


        ViewBag.UserChoice = choice; // Track user choice

        switch (step) // Determine which step to process
        {
            case "choice":
                ModelState.Clear();
                return PartialView("_FirstTimeChoice");

            case "organization":
                if (choice == "trip")
                {
                    // Skip organization if user chose trip
                    var existingGroup = new GroupRequestVM();
                    return PartialView("_WizardStepGroup", existingGroup);
                }

                // Pre-fill orgVm if null and organization exists
                if (orgVm == null || string.IsNullOrWhiteSpace(orgVm.OrgName))
                {
                    if (existingOrg != null)
                    {
                        orgVm = new CreateOrganizationVm
                        {
                            Id = existingOrg.Id,
                            OrgName = existingOrg.OrgName,
                            ShortName = existingOrg.ShortName,
                            IsActive = existingOrg.IsActive,
                            IsPrimary = existingOrg.IsPrimary
                        };
                    }
                    else
                    {
                        orgVm = new CreateOrganizationVm();
                    }
                }

                if (isSubmit)
                {
                    var allowedFields = new[] { "OrgName", "ShortName" };
                    foreach (var key in ModelState.Keys.ToList())
                        if (!allowedFields.Contains(key)) ModelState.Remove(key);

                    TryValidateModel(orgVm);
                    if (!ModelState.IsValid)
                        return PartialView("_WizardStepOrg", orgVm);
                    if (existingOrg != null)
                        orgVm.Id = existingOrg.Id;
                    orgVm.IsPrimary = true;
                    orgVm.IsActive = true;
                    var orgResponse = await _orgService.AddEditOrganizationAsync(orgVm, CurrentUserId);

                    if (!orgResponse.IsSuccess)
                    {
                        ViewBag.Message = orgResponse.Message;
                        return PartialView("_WizardStepOrg", orgVm);
                    }

                    ModelState.Clear();

                    GroupRequestVM existingGroupVm = new GroupRequestVM();
                    existingGroupVm.OrgId = Guid.Parse(orgResponse.Data);

                    if (groupExist.IsSuccess)
                    {
                        existingGroupVm = new GroupRequestVM
                        {
                            Id = groupExist.Data.Id,
                            OrgId = groupExist.Data.OrgId,
                            GroupName = groupExist.Data.GroupName,
                            ShortName = groupExist.Data.ShortName,
                            Description = groupExist.Data.Description,
                            IsActive = groupExist.Data.IsActive
                        };
                    }

                    return PartialView("_WizardStepGroup", existingGroupVm);
                }

                return PartialView("_WizardStepOrg", orgVm);

            case "group":
                // Pre-fill groupVm if null
                if (groupVm == null || string.IsNullOrWhiteSpace(groupVm.GroupName))
                {
                    GroupRequestVM existingGroupVm = null;

                    if (existingOrg != null)
                    {
                        if (groupExist.IsSuccess)
                        {
                            existingGroupVm = new GroupRequestVM
                            {
                                Id = groupExist.Data.Id,
                                OrgId = groupExist.Data.OrgId,
                                GroupName = groupExist.Data.GroupName,
                                ShortName = groupExist.Data.ShortName,
                                Description = groupExist.Data.Description,
                                IsActive = groupExist.Data.IsActive
                            };
                        }
                    }

                    groupVm = existingGroupVm ?? new GroupRequestVM();
                }

                if (isSubmit)
                {
                    var allowedFields2 = new[] { "GroupName", "ShortName", "Description" };
                    foreach (var key in ModelState.Keys.ToList())
                        if (!allowedFields2.Contains(key)) ModelState.Remove(key);

                    TryValidateModel(groupVm);
                    if (!ModelState.IsValid)
                        return PartialView("_WizardStepGroup", groupVm);

                    if (existingOrg == null)
                    {
                        var defaultOrg = await _orgService.CreatorDefaultOrgAcync(CurrentUserId);
                        groupVm.OrgId = Guid.Parse(defaultOrg.Data);
                    }

                    if (groupExist.IsSuccess)
                        groupVm.Id = groupExist.Data.Id;
                    groupVm.IsActive = true;

                    var groupResponse = await _orgService.AddEditGroupAsync(groupVm, CurrentUserId);
                    if (!groupResponse.IsSuccess)
                    {
                        ViewBag.Message = groupResponse.Message;
                        return PartialView("_WizardStepGroup", groupVm);
                    }

                    var user = await _users.FindByIdAsync(CurrentUserId.ToString());
                    if (user != null)
                    {
                        user.IsFirstLogin = false;
                        user.WizardCompleted = true;
                        _db.Users.Update(user);
                        await _db.SaveChangesAsync();
                    }

                    return PartialView("_CongratsPopup");
                }

                return PartialView("_WizardStepGroup", groupVm);
        }

        return BadRequest();
    }

}
