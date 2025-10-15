using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace BusTrips.Web.Hubs
{
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var user = Context.User;

            if (user != null && user.Identity != null && user.Identity.IsAuthenticated)
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrEmpty(userId))
                {
                    // Add connection to a private group (user-specific)
                    await Groups.AddToGroupAsync(Context.ConnectionId, userId);
                    Console.WriteLine($"User connected: {user.Identity.Name} ({userId}) added to personal group.");
                }

                // Add to role groups
                if (user.IsInRole("Admin"))
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Admin");

                if (user.IsInRole("User"))
                    await Groups.AddToGroupAsync(Context.ConnectionId, "User");

                if (user.IsInRole("Driver"))
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Driver");
            }
            else
            {
                Console.WriteLine("⚠️ Unauthenticated user tried to connect to NotificationHub.");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                // Remove from user-specific group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
                Console.WriteLine($"👋 User disconnected: {userId} removed from group.");
            }

            // Remove from role groups
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admin");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "User");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Driver");

            await base.OnDisconnectedAsync(exception);
        }
    }
}
