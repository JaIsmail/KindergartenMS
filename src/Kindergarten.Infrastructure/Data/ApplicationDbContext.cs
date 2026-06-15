using Kindergarten.Core.Entities;
using Kindergarten.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private int CurrentTenantId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?
                .FindFirst("TenantId")?.Value;
            return int.TryParse(claim, out var id) ? id : 1;
        }
    }
    public DbSet<Kindergarten.Core.Entities.AuditLog> AuditLogs { get; set; }
public DbSet<ApplicationUser> Users { get; set; }
public DbSet<Child>        Children      { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Payment>      Payments      { get; set; }
    public DbSet<Trip>         Trips         { get; set; }
    public DbSet<TripChild>    TripChildren  { get; set; }
    public DbSet<TripLocation> TripLocations { get; set; }
    public DbSet<Kindergarten.Core.Entities.LeaveRequest>   LeaveRequests   { get; set; }
    public DbSet<Kindergarten.Core.Entities.Tenant>         Tenants         { get; set; }
    public DbSet<Kindergarten.Core.Entities.Permission>     Permissions     { get; set; }
    public DbSet<Kindergarten.Core.Entities.UserPermission> UserPermissions { get; set; }
    public DbSet<Employee>     Employees     { get; set; }
    public DbSet<Attendance>   Attendance    { get; set; }
    public DbSet<UserDevice>   UserDevices   { get; set; }
    public DbSet<DynamicList>        DynamicLists       { get; set; }
    public DbSet<AttendancePeriod>   AttendancePeriods  { get; set; }
    public DbSet<PermissionGroup>           PermissionGroups           { get; set; }
    public DbSet<PermissionGroupPermission> PermissionGroupPermissions { get; set; }
    public DbSet<UserPermissionGroup>       UserPermissionGroups       { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);



        // Global Query Filters — تصفية تلقائية بـ TenantId
        builder.Entity<Child>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);
        builder.Entity<Employee>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);
        builder.Entity<Trip>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);
        builder.Entity<Attendance>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);
        builder.Entity<LeaveRequest>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);
        builder.Entity<Subscription>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);
        builder.Entity<Payment>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);
        builder.Entity<Kindergarten.Core.Entities.UserPermission>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);
        builder.Entity<UserDevice>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);
        builder.Entity<Attendance>()
            .HasMany(a => a.Periods)
            .WithOne(p => p.Attendance)
            .HasForeignKey(p => p.AttendanceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<DynamicList>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);
        builder.Entity<PermissionGroup>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);
        builder.Entity<UserPermissionGroup>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

  // ApplicationUser configuration (was Identity-managed table AspNetUsers)
        builder.Entity<ApplicationUser>().ToTable("AspNetUsers");
        builder.Entity<ApplicationUser>().HasKey(u => u.Id);
        builder.Entity<ApplicationUser>().HasIndex(u => u.Email).IsUnique(false);
        builder.Entity<ApplicationUser>().HasIndex(u => u.UserName).IsUnique(false);

        // Tenant navigation — no FK
        builder.Entity<Kindergarten.Core.Entities.ApplicationUser>().Ignore(e => e.Tenant);
        builder.Entity<Kindergarten.Core.Entities.Child>().Ignore(e => e.Tenant);
        builder.Entity<Kindergarten.Core.Entities.Trip>().Ignore(e => e.Tenant);
        builder.Entity<Kindergarten.Core.Entities.Employee>().Ignore(e => e.Tenant);
        builder.Entity<Kindergarten.Core.Entities.Subscription>().Ignore(e => e.Tenant);

        builder.Entity<TripChild>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);
        builder.Entity<TripLocation>()
            .HasQueryFilter(x => x.TenantId == CurrentTenantId);

        // PermissionGroupPermission — composite primary key
        builder.Entity<PermissionGroupPermission>()
            .HasKey(x => new { x.GroupId, x.PermissionId });
        builder.Entity<PermissionGroupPermission>()
            .HasOne(x => x.Group)
            .WithMany(g => g.GroupPermissions)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<PermissionGroupPermission>()
            .HasOne(x => x.Permission)
            .WithMany()
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<UserPermissionGroup>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<UserPermissionGroup>()
            .HasOne(x => x.Group)
            .WithMany(g => g.UserGroups)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // TripChild — composite primary key
        builder.Entity<TripChild>()
            .HasKey(tc => new { tc.TripId, tc.ChildId });

        // Child → Parent
        builder.Entity<Child>()
            .HasOne(c => c.Parent)
            .WithMany()
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Subscription → Parent
        builder.Entity<Subscription>()
            .HasOne(s => s.Parent)
            .WithMany()
            .HasForeignKey(s => s.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Subscription → Child
        builder.Entity<Subscription>()
            .HasOne(s => s.Child)
            .WithMany()
            .HasForeignKey(s => s.ChildId)
            .OnDelete(DeleteBehavior.Restrict);

// Trip → Driver
        builder.Entity<Trip>()
            .HasOne(t => t.Driver)
            .WithMany()
            .HasForeignKey(t => t.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        // TripChild → Trip
        builder.Entity<TripChild>()
            .HasOne(tc => tc.Trip)
            .WithMany(t => t.TripChildren)
            .HasForeignKey(tc => tc.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        // TripChild → Child
        builder.Entity<TripChild>()
            .HasOne(tc => tc.Child)
            .WithMany(c => c.TripChildren)
            .HasForeignKey(tc => tc.ChildId)
            .OnDelete(DeleteBehavior.Restrict);

        // Employee → User
        builder.Entity<Employee>()
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Decimal precision
        builder.Entity<Subscription>()
            .Property(s => s.Price)
            .HasPrecision(10, 2);
        builder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasPrecision(10, 2);
    }
}
