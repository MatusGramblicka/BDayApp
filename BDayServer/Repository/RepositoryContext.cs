using Entities.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Repository.Services;

namespace Repository;

public class RepositoryContext(DbContextOptions<RepositoryContext> options, IGetUserProvider userData)
    : IdentityDbContext<User>(options)
{
    private readonly string _userName = userData.UserName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Person>().HasQueryFilter(b => b.User.UserName == _userName);
        modelBuilder.Entity<Event>().HasQueryFilter(b => b.User.UserName == _userName);
    }

    public DbSet<Person> Persons { get; set; }
    public DbSet<Event> Events { get; set; }
}