using BusTrips.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BusTrips.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public DbSet<BusDriver> BusDrivers => Set<BusDriver>();
    public DbSet<DriverDocument> DriverDocuments => Set<DriverDocument>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripMembership> TripMemberships => Set<TripMembership>();
    public DbSet<TripBusAssignment> TripBusAssignments => Set<TripBusAssignment>();
    public DbSet<TripChangeLog> TripChangeLogs => Set<TripChangeLog>();
    public DbSet<TermsAndConditions> TermsAndConditions => Set<TermsAndConditions>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<OrganizationPermissions> OrganizationPermissions => Set<OrganizationPermissions>();
    public DbSet<EquipmentDocument> EquipmentDocuments => Set<EquipmentDocument>();
    public DbSet<OrgCreatorLog> OrgCreatorLogs => Set<OrgCreatorLog>();


    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ----------------------------
        // Indexes
        // ----------------------------
        b.Entity<Organization>().HasIndex(x => x.OrgName).IsUnique();
        b.Entity<OrganizationMembership>().HasIndex(x => new { x.OrganizationId, x.AppUserId }).IsUnique();
        b.Entity<TripMembership>().HasIndex(x => new { x.TripId, x.AppUserId }).IsUnique();

        // ----------------------------
        // Enum conversions
        // ----------------------------
        b.Entity<Trip>().Property(x => x.Status).HasConversion<string>();
        b.Entity<TripBusAssignment>().Property(x => x.Status).HasConversion<string>();
        b.Entity<BusDriver>().Property(x => x.ApprovalStatus).HasConversion<string>();

        // ----------------------------
        // AppUser relationships
        // ----------------------------
        b.Entity<AppUser>()
            .HasMany(u => u.OrganizationMemberships)
            .WithOne(m => m.AppUser)
            .HasForeignKey(m => m.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<AppUser>()
            .HasMany(u => u.TripMemberships)
            .WithOne(m => m.AppUser)
            .HasForeignKey(m => m.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ----------------------------
        // BusDriver ↔ AppUser (1:1)
        // ----------------------------
        b.Entity<BusDriver>()
            .HasOne(d => d.AppUser)
            .WithOne()
            .HasForeignKey<BusDriver>(d => d.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // BusDriver ↔ DriverDocuments (1:many)
        b.Entity<DriverDocument>()
            .HasOne(d => d.BusDriver)
            .WithMany(bd => bd.AdditionalDocuments)
            .HasForeignKey(d => d.BusDriverId)
            .OnDelete(DeleteBehavior.Cascade);

        // ----------------------------
        // Organization ↔ Memberships (1:many)
        // ----------------------------
        b.Entity<OrganizationMembership>()
            .HasOne(m => m.Organization)
            .WithMany(o => o.Managers)
            .HasForeignKey(m => m.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        // ----------------------------
        // Organization ↔ Trips (1:many)
        // ----------------------------
        b.Entity<Trip>()
            .HasOne(t => t.Organization)
            .WithMany(o => o.Trips)
            .HasForeignKey(t => t.OrganizationId)
            .OnDelete(DeleteBehavior.SetNull);

        // ----------------------------
        // Organization ↔ Groups (1:many)
        // ----------------------------
        b.Entity<Group>()
            .HasOne(g => g.Org)
            .WithMany(o => o.Groups)
            .HasForeignKey(g => g.OrgId)
            .OnDelete(DeleteBehavior.Cascade);

        // ----------------------------
        // Trip ↔ Memberships (1:many)
        // ----------------------------
        b.Entity<TripMembership>()
            .HasOne(m => m.Trip)
            .WithMany(t => t.Managers)
            .HasForeignKey(m => m.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        // ----------------------------
        // Trip ↔ BusAssignments (1:many)
        // ----------------------------
        b.Entity<TripBusAssignment>()
            .HasOne(a => a.Trip)
            .WithMany(t => t.BusAssignments)
            .HasForeignKey(a => a.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<TripBusAssignment>()
            .HasOne(a => a.Equipment)
            .WithMany()
            .HasForeignKey(a => a.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<TripBusAssignment>()
            .HasOne(a => a.Driver)
            .WithMany()
            .HasForeignKey(a => a.DriverId)
            .OnDelete(DeleteBehavior.SetNull);

        // ----------------------------
        // Trip ↔ ChangeLogs (1:many)
        // ----------------------------
        b.Entity<TripChangeLog>()
            .HasOne(l => l.Trip)
            .WithMany(t => t.ChangeLogs)
            .HasForeignKey(l => l.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        // ----------------------------
        // Equipment ↔ EquipmentDocuments (1:many)
        // ----------------------------
        b.Entity<EquipmentDocument>()
            .HasOne(d => d.Equipment)
            .WithMany(e => e.Documents)
            .HasForeignKey(d => d.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // ----------------------------
        // OrgCreatorLog ↔ AppUser & Organization (many-to-1)
        // ----------------------------
        b.Entity<OrgCreatorLog>()
            .HasOne(l => l.AppUser)
            .WithMany() // if you want: add ICollection<OrgCreatorLog> OrgCreations {get;set;} in AppUser
            .HasForeignKey(l => l.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<OrgCreatorLog>()
            .HasOne(l => l.Org)
            .WithMany() // if you want: add ICollection<OrgCreatorLog> CreatorLogs {get;set;} in Organization
            .HasForeignKey(l => l.OrgId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
