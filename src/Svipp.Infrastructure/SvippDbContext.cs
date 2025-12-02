using Microsoft.EntityFrameworkCore;
using Svipp.Domain.Users;

namespace Svipp.Infrastructure;

public class SvippDbContext : DbContext
{
    public SvippDbContext(DbContextOptions<SvippDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(u => u.Id);

            entity.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(200);

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
        });
    }
}



