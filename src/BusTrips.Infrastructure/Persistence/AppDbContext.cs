using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BusTrips.Domain.Entities;

namespace BusTrips.Infrastructure.Persistence
{
    public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Users
        public DbSet<AppUser> AppUsers { get; set; }

        // Drivers
        public DbSet<BusDriver> BusDrivers { get; set; }
        public DbSet<DriverDocument> DriverDocuments { get; set; }

        // Organizations
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<OrganizationMembership> OrganizationMemberships { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<OrganizationPermissions> OrganizationPermissions { get; set; }
        public DbSet<OrgCreatorLog> OrgCreatorLogs { get; set; }

        // Trips
        public DbSet<Trip> Trips { get; set; }
        public DbSet<TripBusAssignment> TripBusAssignments { get; set; }
        public DbSet<TripChangeLog> TripChangeLogs { get; set; }

        // Equipment
        public DbSet<Equipment> Equipment { get; set; }
        public DbSet<EquipmentDocument> EquipmentDocuments { get; set; }

        // Notifications
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }

        // Misc
        public DbSet<TermsAndConditions> TermsAndConditions { get; set; }
        public DbSet<ContactUs> ContactUsEntries { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // -----------------------
            // BusDriver -> AppUser
            // -----------------------
            builder.Entity<BusDriver>()
                .HasOne(d => d.AppUser)
                .WithMany()
                .HasForeignKey(d => d.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DriverDocument>()
                .HasOne(dd => dd.BusDriver)
                .WithMany(d => d.AdditionalDocuments)
                .HasForeignKey(dd => dd.BusDriverId)
                .OnDelete(DeleteBehavior.Cascade);

            // -----------------------
            // Organization
            // -----------------------
            builder.Entity<OrganizationMembership>()
                .HasOne(m => m.AppUser)
                .WithMany(u => u.OrganizationMemberships)
                .HasForeignKey(m => m.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrganizationMembership>()
                .HasOne(m => m.Organization)
                .WithMany(o => o.Members)
                .HasForeignKey(m => m.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Group>()
                .HasOne(g => g.Org)
                .WithMany(o => o.Groups)
                .HasForeignKey(g => g.OrgId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Group>()
                .HasOne(g => g.CreatedForUser)
                .WithMany()
                .HasForeignKey(g => g.CreatedForId)
                .OnDelete(DeleteBehavior.Restrict);

            // -----------------------
            // Trips
            // -----------------------
            builder.Entity<Trip>()
                .HasOne(t => t.Organization)
                .WithMany(o => o.Trips)
                .HasForeignKey(t => t.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Trip>()
                .HasOne(t => t.Group)
                .WithMany()
                .HasForeignKey(t => t.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Trip>()
                .HasOne(t => t.CreatedForUser)
                .WithMany()
                .HasForeignKey(t => t.CreatedForId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TripBusAssignment>()
                .HasOne(a => a.Trip)
                .WithMany(t => t.BusAssignments)
                .HasForeignKey(a => a.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TripBusAssignment>()
                .HasOne(a => a.Equipment)
                .WithMany()
                .HasForeignKey(a => a.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TripBusAssignment>()
                .HasOne(a => a.Driver)
                .WithMany()
                .HasForeignKey(a => a.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TripChangeLog>()
                .HasOne(c => c.Trip)
                .WithMany(t => t.ChangeLogs)
                .HasForeignKey(c => c.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TripChangeLog>()
                .HasOne(c => c.ChangedByUser)
                .WithMany()
                .HasForeignKey(c => c.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // -----------------------
            // Equipment Documents
            // -----------------------
            builder.Entity<EquipmentDocument>()
                .HasOne(d => d.Equipment)
                .WithMany(e => e.Documents)
                .HasForeignKey(d => d.EquipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // -----------------------
            // Notifications
            // -----------------------
            builder.Entity<UserNotification>()
                .HasOne(un => un.Notification)
                .WithMany(n => n.UserNotifications)
                .HasForeignKey(un => un.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserNotification>()
                .HasOne(un => un.User)
                .WithMany()
                .HasForeignKey(un => un.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // -----------------------
            // Organization Logs & Permissions
            // -----------------------
            builder.Entity<OrgCreatorLog>()
                .HasOne(l => l.Org)
                .WithMany()
                .HasForeignKey(l => l.OrgId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrgCreatorLog>()
                .HasOne(l => l.AppUser)
                .WithMany()
                .HasForeignKey(l => l.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrganizationPermissions>()
                .HasOne(p => p.Org)
                .WithMany()
                .HasForeignKey(p => p.OrgId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
