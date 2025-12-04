using Microsoft.EntityFrameworkCore;
using Svipp.Domain.Assignments;
using Svipp.Domain.Users;

namespace Svipp.Infrastructure;

public class SvippDbContext : DbContext
{
    public SvippDbContext(DbContextOptions<SvippDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    // Core Svipp domain: assignments between customers and drivers moving cars
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Scooter> Scooters => Set<Scooter>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<HandoverConfirmation> HandoverConfirmations => Set<HandoverConfirmation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(u => u.Id);

            entity.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(320);

            entity.Property(u => u.PhoneNumber)
                .IsRequired()
                .HasMaxLength(32);

            entity.Property(u => u.PasswordHash)
                .IsRequired();

            entity.Property(u => u.CreatedAt)
                .IsRequired();

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.HasIndex(u => u.PhoneNumber)
                .IsUnique();
        });

        // Primary keys
        modelBuilder.Entity<Location>().HasKey(x => x.LocationId);
        modelBuilder.Entity<Vehicle>().HasKey(x => x.VehicleId);
        modelBuilder.Entity<Scooter>().HasKey(x => x.ScooterId);
        modelBuilder.Entity<Customer>().HasKey(x => x.CustomerId);
        modelBuilder.Entity<Driver>().HasKey(x => x.DriverId);
        modelBuilder.Entity<Booking>().HasKey(x => x.BookingId);
        modelBuilder.Entity<Payment>().HasKey(x => x.PaymentId);
        modelBuilder.Entity<Review>().HasKey(x => x.ReviewId);
        modelBuilder.Entity<HandoverConfirmation>().HasKey(x => x.HandoverConfirmationId);

        // Customer 1-* Booking
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Customer)
            .WithMany(c => c.Bookings)
            .HasForeignKey(b => b.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Customer 0-* Review
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Customer)
            .WithMany(c => c.Reviews)
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Driver 1-* Booking
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Driver)
            .WithMany(d => d.Bookings)
            .HasForeignKey(b => b.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        // Driver 1-* Scooter
        modelBuilder.Entity<Scooter>()
            .HasOne(s => s.Driver)
            .WithMany(d => d.Scooters)
            .HasForeignKey(s => s.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        // Scooter *-0..1 Location (current location)
        modelBuilder.Entity<Scooter>()
            .HasOne(s => s.CurrentLocation)
            .WithMany()
            .HasForeignKey(s => s.CurrentLocationId)
            .OnDelete(DeleteBehavior.SetNull);

        // Vehicle 1-* Booking
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Vehicle)
            .WithMany(v => v.Bookings)
            .HasForeignKey(b => b.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Location 1-* Booking (pickup)
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.PickupLocation)
            .WithMany(l => l.PickupBookings)
            .HasForeignKey(b => b.PickupLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Location 1-* Booking (dropoff)
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.DropoffLocation)
            .WithMany(l => l.DropoffBookings)
            .HasForeignKey(b => b.DropoffLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Booking 1-1 Payment
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Booking)
            .WithOne(b => b.Payment)
            .HasForeignKey<Payment>(p => p.BookingId);

        // Booking 0-1 Review
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Booking)
            .WithOne(b => b.Review)
            .HasForeignKey<Review>(r => r.BookingId);

        // Booking 0-1 HandoverConfirmation (digital ansvarsoverførings-sjekk)
        modelBuilder.Entity<HandoverConfirmation>()
            .HasOne(f => f.Booking)
            .WithOne(b => b.HandoverConfirmation)
            .HasForeignKey<HandoverConfirmation>(f => f.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

