using BusTrips.Domain.Entities;
using BusTrips.Web.Models;
using System.Security.Claims;

namespace BusTrips.Web.Interface
{
    public interface INotificationService
    {
        Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId);
        Task SaveAndSendNotification(string title, string message, string? FullMessage, Guid? userId, string? role);
        Task<NotificationDto?> NotificationDetailsAsync(Guid id);
    }
}
