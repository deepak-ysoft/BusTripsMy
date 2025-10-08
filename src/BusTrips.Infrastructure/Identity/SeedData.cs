using BusTrips.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BusTrips.Infrastructure.Identity;

public static class SeedData
{
    public static async Task SeedAsync(UserManager<AppUser> users, RoleManager<IdentityRole<Guid>> roles)
    {
        foreach (var role in new[] { AppRoles.Admin, AppRoles.User, AppRoles.Driver })
        {
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole<Guid>(role));
        }
        var adminEmail = "admin@demo.local";
        var admin = await users.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (admin is null)
        {
            admin = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "User",
            };
            await users.CreateAsync(admin, "Pass123$");
            await users.AddToRoleAsync(admin, AppRoles.Admin);
        }
    }
}
