using BusTrips.Domain.Entities;
using BusTrips.Infrastructure.Persistence;
using BusTrips.Web.Hubs;
using BusTrips.Web.Interface;
using BusTrips.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BusTrips.Web.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly UserManager<AppUser> _users;
        public NotificationService(AppDbContext appDb, IHubContext<NotificationHub> hubContext, UserManager<AppUser> users)
        {
            _db = appDb;
            _hubContext = hubContext;
            _users = users;
        }
        public async Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var roles = await _users.GetRolesAsync(user);

            return await _db.Notifications
                .Where(n =>
                    n.UserId == userId ||
                    (n.UserId == null && n.Role == null) ||
                    (n.UserId == null && roles.Contains(n.Role))
                )
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    FullMessage = n.FullMessage,
                    Role = n.Role,
                    IsRead = n.IsRead,
                    Date = n.CreatedAt.ToString("dd MMM yyyy hh:mm tt")
                })
                .ToListAsync();
        }

        public async Task<NotificationDto?> NotificationDetailsAsync(Guid id)
        {
            var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id);
            if (notification == null)
                return null;

            // Mark as read if not already
            if (!notification.IsRead)
            {
                notification.IsRead = true;
                _db.Notifications.Update(notification);
                await _db.SaveChangesAsync();
            }

            // Return a DTO instead of the EF entity
            return new NotificationDto
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                FullMessage = notification.FullMessage,
                Role = notification.Role,
                IsRead = notification.IsRead,
                Date = notification.CreatedAt.ToString("dd MMM yyyy hh:mm tt")
            };
        }

        public async Task SaveAndSendNotification(string title, string message, string? fullMessage, Guid? userId, string? role)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                FullMessage = fullMessage,
                UserId = userId,
                Role = role,
                CreatedAt = DateTime.Now
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            var notifDto = new
            {
                id = notification.Id,
                title = notification.Title,
                message = notification.Message,
                date = notification.CreatedAt.ToString("dd MMM yyyy hh:mm tt"),
                isRead = false
            };

            if (userId != null)
            {
                // Send to specific user’s SignalR group (userId = group name)
                await _hubContext.Clients.Group(userId.ToString()).SendAsync("ReceiveNotification", notifDto);
                Console.WriteLine($"📨 Sent notification '{title}' to user {userId}");
            }
            else if (!string.IsNullOrEmpty(role))
            {
                // Send to role group
                await _hubContext.Clients.Group(role).SendAsync("ReceiveNotification", notifDto);
                Console.WriteLine($"📨 Sent notification '{title}' to role group {role}");
            }
            else
            {
                // Send to everyone
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", notifDto);
                Console.WriteLine($"📨 Sent notification '{title}' to all users");
            }
        }
    }
}
