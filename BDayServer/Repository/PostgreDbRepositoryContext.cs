using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Repository
{
    public class PostgreDbRepositoryContext : IdentityDbContext<User>
    {
        public DbSet<Person> Persons { get; set; }
        public DbSet<Event> Events { get; set; }

        public PostgreDbRepositoryContext(DbContextOptions<PostgreDbRepositoryContext> options)
            : base(options) {}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
          
            // AspNetUsers
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(r => r.Id)
                      .HasMaxLength(100);

                entity.Property(u => u.UserName)
                      .HasMaxLength(50);

                entity.Property(u => u.NormalizedUserName)
                      .HasMaxLength(50);

                entity.Property(u => u.Email)
                      .HasMaxLength(50);

                entity.Property(u => u.NormalizedEmail)
                      .HasMaxLength(50);

                entity.Property(u => u.PasswordHash)
                      .HasMaxLength(500);

                entity.Property(u => u.SecurityStamp)
                      .HasMaxLength(100);

                entity.Property(u => u.ConcurrencyStamp)
                      .HasMaxLength(100);

                entity.Property(u => u.PhoneNumber)
                      .HasMaxLength(30);

                // for posgresql
                entity.Property(u => u.RefreshTokenExpiryTime)
                      .HasColumnType("timestamp without time zone");
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
    }
}
