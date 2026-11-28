using Core.Services.Interfaces;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repository.Repositories;
using Identity = Microsoft.AspNetCore.Identity;

namespace Core.Services;

public class PostgresPersonMirror(
    PostgreDbRepositoryContext mirror,
    Identity.UserManager<User> userManager,
    ILogger<PostgresPersonMirror> logger) : IPersonMirror
{
    public async Task UpsertPersonAsync(Person person, CancellationToken cancellationToken = default)
    {
        try
        {
            // The copy references its owner, but users are not copied when they register.
            await AddUserIfMissingAsync(person.UserId, cancellationToken);

            var copy = await mirror.Persons.FirstOrDefaultAsync(p => p.Id == person.Id, cancellationToken);
            if (copy is null)
            {
                copy = new Person { Id = person.Id };
                mirror.Persons.Add(copy);
            }

            copy.Name = person.Name;
            copy.Surname = person.Surname;
            copy.DayOfBirth = person.DayOfBirth;
            copy.DayOfNameDay = person.DayOfNameDay;
            copy.ImageUrl = person.ImageUrl;
            copy.UserId = person.UserId;

            await mirror.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Person {PersonId} could not be copied to PostgreSQL; the birthday e-mails miss this change.",
                person.Id);
        }
        finally
        {
            mirror.ChangeTracker.Clear();
        }
    }

    public async Task DeletePersonAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        try
        {
            await mirror.Persons.Where(p => p.Id == personId).ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Person {PersonId} could not be deleted from PostgreSQL; the birthday e-mails still include it.",
                personId);
        }
    }

    public async Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await mirror.Persons.Where(p => p.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await mirror.Events.Where(e => e.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await mirror.Users.Where(u => u.Id == userId).ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "User {UserId} could not be deleted from PostgreSQL; the birthday e-mails still include their persons.",
                userId);
        }
    }

    private async Task AddUserIfMissingAsync(string userId, CancellationToken cancellationToken)
    {
        if (await mirror.Users.AnyAsync(u => u.Id == userId, cancellationToken))
            return;

        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new InvalidOperationException($"User {userId} does not exist.");

        // Only what the e-mail job needs: no password hash, security stamp or tokens.
        mirror.Users.Add(new User
        {
            Id = user.Id,
            UserName = user.UserName,
            NormalizedUserName = user.NormalizedUserName,
            Email = user.Email,
            NormalizedEmail = user.NormalizedEmail,
            EmailConfirmed = user.EmailConfirmed,
            IsAdmin = user.IsAdmin,
            RefreshToken = string.Empty,
            // "timestamp without time zone" column: Npgsql rejects DateTime values of kind UTC there.
            RefreshTokenExpiryTime = default
        });
    }
}
