using Entities.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Repository.Repositories;

public class RepositoryContextScheduleJob(DbContextOptions<RepositoryContextScheduleJob> options)
    : IdentityDbContext<User>(options)
{
    public DbSet<Person> Persons { get; set; }
    public DbSet<Event> Events { get; set; }
}