using BusTrips.Domain.Entities;
using BusTrips.Web.Models;
using static BusTrips.Web.Services.OrganizationService;

namespace BusTrips.Web.Interface
{
    public interface IOrganizationService
    {
        Task<CreateOrganizationVm> GetDefaultOrganizationByUserAsync(Guid CurrentUserId);
        Task<ResponseVM<string>> CreatorDefaultOrgAcync(Guid userId);
        Task<ResponseVM<string>> RevertOrganizationAsync(Guid? id);
        Task<OrganizationLists> GetUserOrganizationsAsync(Guid CurrentUserId);
        Task<dynamic> GetUserOrganizationDetailsAsync(Guid id, Guid CurrentUserId);
        //Task<OrgMemberDetailsResponseVM> GetMemberDetailsAsync(Guid id, Guid orgId, Guid CurrentUserId);
        Task<CreateOrganizationVm> GetOrganizationAsync(Guid id);
        Task<ResponseVM<string>> AddEditOrganizationAsync(CreateOrganizationVm vm, Guid userId);
        Task<ResponseVM<Organization>> DeleteOrganizationAsync(Guid id);
        Task<ResponseVM<string>> InviteAsync(InviteManagerVm vm, Guid userId);
        //Task<ResponseVM<string>> RemoveMemberAsync(Guid id, Guid orgId, Guid currentUserId);
        //Task<ResponseVM<string>> MakeRemoverAdminAcync(Guid id, Guid orgId);
        //Task<ResponseVM<string>> MakeCreatorAcync(Guid id, Guid orgId, Guid CurrentUserId);

        Task<ResponseVM<string>> ChangeOrgMemberRoleAsync(Guid targetUserId, Guid orgId, Guid currentUserId, MemberTypeEnum action);
        Task<ResponseVM<string>> SelfRemoveFromOrgAsync(Guid currentUserId, Guid orgId, MemberTypeEnum memberType);

        //Task<ResponseVM<string>> SelfRemoveFromCreatorAcync(Guid currentUserId, Guid orgId);
        //Task<ResponseVM<string>> SelfRemoveFromAdminAcync(Guid currentUserId, Guid orgId);
        //Task<ResponseVM<string>> SelfRemoveAsMemberFromOrgAsync(Guid currentUserId, Guid orgId);

        #region Admin

        Task<List<OrgListItemVm>> GetOrganizationsAsync(); 
        Task<ResponseVM<List<OrgListItemVm>>> getUserOrgGropTripShortDetails(Guid userId);
        Task<ResponseVM<string>> AddEditOrganizationByAdminAsync(CreateOrganizationVm vm, Guid userId);
        Task<ResponseVM<OrgDetailsVm>> GetOrganizationDetailsAsync(Guid orgId, Guid? userId = null);
        Task<ResponseVM<List<GroupListItemVm>>> GetOrganizationGroupsAsync(Guid orgId);
        Task<ResponseVM<List<OrgMembersListVM>>> GetOrganizationMembersAsync(Guid orgId,Guid? CurrentUserId = null);
        Task<OrgMemberDetailsResponseVM> GetMemberDetailsByAdminAsync(Guid id, Guid orgId);
        Task<ResponseVM<string>> DeleteOrgMemberAsync(Guid id);
        Task<ResponseVM<string>> ChangeMemberRoleAsync(Guid memberShipId, MemberTypeEnum newRole);

        #endregion

        #region groups

        Task<ResponseVM<List<GroupResponseVM>>> GetUserGroupsAsync(Guid userId, Guid orgId);
        Task<ResponseVM<GroupResponseVM>> GetGroupAsync(Guid groupId);
        Task<ResponseVM<GroupRequestVM>> GetGroupByOrgIdAsync(Guid orgId);
        Task<ResponseVM<GroupDetailsResponseVM>> GetGroupDetailsAsync(Guid groupId);
        Task<ResponseVM<Group>> AddEditGroupAsync(GroupRequestVM vm, Guid userId);
        Task<ResponseVM<string>> RevertGroupAsync(Guid gId);
        Task<ResponseVM<dynamic>> DeleteGroupAsync(Guid groupId);

        #endregion
    }
}
