using Kindergarten.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Child>        Children      { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Payment>      Payments      { get; set; }
    public DbSet<Trip>         Trips         { get; set; }
    public DbSet<TripChild>    TripChildren  { get; set; }
    public DbSet<TripLocation> TripLocations { get; set; }
    public DbSet<Employee>     Employees     { get; set; }
    public DbSet<Attendance>   Attendance    { get; set; }
    public DbSet<UserDevice>   UserDevices   { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // TripChild — composite primary key
        builder.Entity<TripChild>()
            .HasKey(tc => new { tc.TripId, tc.ChildId });

        // Child → Parent (ApplicationUser)
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
