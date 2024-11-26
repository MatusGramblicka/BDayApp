using Entities.Models;
using Interfaces.UserProvider;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Repository;

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
        //modelBuilder.ApplyConfiguration(new PersonConfiguration());
        //modelBuilder.ApplyConfiguration(new RoleConfiguration());

        modelBuilder.Entity<Person>().HasQueryFilter(b => b.User.UserName == _userName);
        modelBuilder.Entity<Event>().HasQueryFilter(b => b.User.UserName == _userName);
    }

    public DbSet<Person> Persons { get; set; }
    public DbSet<Event> Events { get; set; }
}