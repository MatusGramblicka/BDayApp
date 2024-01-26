using Entities.Configuration;
using Entities.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Entities
{
    public class RepositoryContext : IdentityDbContext<User>
    {
        private readonly string _userName;

        public RepositoryContext(DbContextOptions<RepositoryContext> options, IGetUserProvider userData)
            : base(options)
        {
            _userName = userData.UserName;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new PersonConfiguration());
            modelBuilder.ApplyConfiguration(new RoleConfiguration());

            //modelBuilder.Entity<Person>().HasQueryFilter(b => b.PersonCreator == _userName);
            modelBuilder.Entity<Person>().HasQueryFilter(b => b.User.UserName == _userName);
            modelBuilder.Entity<Event>().HasQueryFilter(b => b.EventCreator == _userName);
        }

        public DbSet<Person> Persons { get; set; }
        public DbSet<Event> Events { get; set; }
    }    
}
