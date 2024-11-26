using Entities.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Repository;

public class RepositoryContextScheduleJob : IdentityDbContext<User>
{
    public RepositoryContextScheduleJob(DbContextOptions<RepositoryContextScheduleJob> options)
        : base(options) { }

    public DbSet<Person> Persons { get; set; }
    public DbSet<Event> Events { get; set; }
}