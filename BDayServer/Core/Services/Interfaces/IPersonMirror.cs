using Entities.Models;

namespace Core.Services.Interfaces;

/// <summary>
/// Copies person changes from Azure SQL (the source of truth) to the PostgreSQL database that the daily birthday
/// e-mails are prepared from. The methods never throw: the change is already saved, so a failure is only logged.
/// </summary>
public interface IPersonMirror
{
    /// <summary>Adds or updates the copy of the person, and copies its owner first if needed.</summary>
    Task UpsertPersonAsync(Person person, CancellationToken cancellationToken = default);

    /// <summary>Removes the copy of the person, if there is one.</summary>
    Task DeletePersonAsync(Guid personId, CancellationToken cancellationToken = default);

    /// <summary>Removes the copy of the user together with their persons and events, if there is one.</summary>
    Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default);
}
