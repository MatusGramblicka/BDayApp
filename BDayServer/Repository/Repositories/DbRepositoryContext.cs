using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Repository.Services;

namespace Repository.Repositories;

public class DbRepositoryContext(DbContextOptions<DbRepositoryContext> options, IGetUserProvider userData)
    : IdentityDbContext<User>(options)
{
    private readonly string? _userName = userData.UserName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Person>().HasQueryFilter(b => b.User.UserName == _userName);
        modelBuilder.Entity<Event>().HasQueryFilter(b => b.User.UserName == _userName);

        // One row per login (device or browser); only the hash of the token is stored.
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");

            entity.Property(t => t.UserId)
                .HasMaxLength(100);

            entity.Property(t => t.TokenHash)
                .HasMaxLength(64);

            entity.HasIndex(t => t.TokenHash)
                .IsUnique();

            entity.HasIndex(t => t.UserId);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AspNetUsers
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(r => r.Id)
                .HasMaxLength(100);

            // The e-mail address is also the user name. 256 is ASP.NET Core Identity's default length.
            entity.Property(u => u.UserName)
                .HasMaxLength(256);

            entity.Property(u => u.NormalizedUserName)
                .HasMaxLength(256);

            entity.Property(u => u.Email)
                .HasMaxLength(256);

            entity.Property(u => u.NormalizedEmail)
                .HasMaxLength(256);

            entity.Property(u => u.PasswordHash)
                .HasMaxLength(500);

            entity.Property(u => u.SecurityStamp)
                .HasMaxLength(100);

            entity.Property(u => u.ConcurrencyStamp)
                .HasMaxLength(100);

            entity.Property(u => u.PhoneNumber)
                .HasMaxLength(30);
        });

        // AspNetRoles
        modelBuilder.Entity<IdentityRole>(entity =>
        {
            entity.Property(r => r.Id)
                .HasMaxLength(100);

            entity.Property(r => r.Name)
                .HasMaxLength(25);

            entity.Property(r => r.NormalizedName)
                .HasMaxLength(25);

            entity.Property(r => r.ConcurrencyStamp)
                .HasMaxLength(250);
        });

        // AspNetRoleClaims
        modelBuilder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.Property(rc => rc.RoleId)
                .HasMaxLength(100);

            entity.Property(rc => rc.ClaimType)
                .HasMaxLength(200);

            entity.Property(rc => rc.ClaimValue)
                .HasMaxLength(500);
        });

        // AspNetUserClaims
        modelBuilder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.Property(uc => uc.UserId)
                .HasMaxLength(100);

            entity.Property(uc => uc.ClaimType)
                .HasMaxLength(200);

            entity.Property(uc => uc.ClaimValue)
                .HasMaxLength(500);
        });

        // AspNetUserLogins
        modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.Property(ul => ul.UserId)
                .HasMaxLength(100);

            entity.Property(ul => ul.LoginProvider)
                .HasMaxLength(100);

            entity.Property(ul => ul.ProviderKey)
                .HasMaxLength(100);

            entity.Property(ul => ul.ProviderDisplayName)
                .HasMaxLength(200);
        });

        // AspNetUserTokens
        modelBuilder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.Property(t => t.UserId)
                .HasMaxLength(100);

            entity.Property(t => t.LoginProvider)
                .HasMaxLength(100);

            entity.Property(t => t.Name)
                .HasMaxLength(100);

            entity.Property(t => t.Value)
                .HasMaxLength(500);
        });

        // AspNetUserRoles
        modelBuilder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.Property(ur => ur.UserId)
                .HasMaxLength(100); 

            entity.Property(ur => ur.RoleId)
                .HasMaxLength(100); 
        });
    }

    public DbSet<Person> Persons { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
}