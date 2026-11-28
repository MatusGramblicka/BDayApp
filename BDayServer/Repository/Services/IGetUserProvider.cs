namespace Repository.Services;

public interface IGetUserProvider
{
    /// <summary>The signed-in user's name (their e-mail address); null outside an authenticated request.</summary>
    string? UserName { get; }
}
