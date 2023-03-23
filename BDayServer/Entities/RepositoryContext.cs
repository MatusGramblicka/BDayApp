using Entities.Configuration;
using Entities.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Entities
{
    public class RepositoryContext : IdentityDbContext<User>
    {
        private readonly string _userId;

        public RepositoryContext(DbContextOptions options, IGetUserProvider userData)
            : base(options)
        {
            _userId = userData.UserId;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new PersonConfiguration());
            modelBuilder.ApplyConfiguration(new RoleConfiguration());

            modelBuilder.Entity<Person>().HasQueryFilter(b => b.PersonCreator == _userId);
        }

        public DbSet<Person> Persons { get; set; }
    }
}
