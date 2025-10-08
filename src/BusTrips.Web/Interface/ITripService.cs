using BusTrips.Web.Models;

namespace BusTrips.Web.Interface
{
    public interface ITripService
    {
        Task<dynamic> GetAllTripsAsync();
        Task<ResponseVM<GroupDetailsResponseVM>> GetTripsAsync(Guid organizationId, Guid userId, string? bucket);
        Task<ResponseVM<GroupDetailsResponseVM>> GetTripsByGroupIdAsync(Guid groupId, Guid userId, string? bucket);
        Task<CreateTripVm?> GetTripAsync(Guid id);
        Task<CreateTripVm?> GetTripForCopyAsync(Guid id);
        Task<ResponseVM<string>> CreateTripAsync(CreateTripVm vm, Guid userId);
        Task<ResponseVM<string>> UpdateTripAsync(Guid id, CreateTripVm vm, Guid userId);
        Task<ResponseVM<string>> DeleteTripAsync(Guid id);
        Task<ResponseVM<string>> CancelTripAsync(Guid id);
        Task<ResponseVM<string>> CopyTripAsync(Guid id, Guid userId);

        Task<ResponseVM<string>> SubmitForQuoteAsync(Guid id);
        Task<ResponseVM<string>> ActivateTripAsync(Guid id);

        Task<List<TripListItemVm>> GetTripsForAdminAsync(string? bucket);
        Task<ResponseVM<TripDetailsResponseVm>> GetTripsDetailsAsync(Guid tripId);
        Task<List<object>> GetRequestsAsync();
        Task<ResponseVM<string>> ApproveOrRejectTripAsync(Guid tripId, string status);
        Task<ResponseVM<string>> AssignTripAsync(Guid id);
        Task<List<object>> GetTripsToAssignAsync();

        #region Home 

        Task<TripDashboardVM> GetFilteredTripsAsync(TripFilterVM filter, Guid userId);
        Task<CreateTripVm> GetTripByGroupIdAsync(Guid groupId);

        #endregion
    }
}
