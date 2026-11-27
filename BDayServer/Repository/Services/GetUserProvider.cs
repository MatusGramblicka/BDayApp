using Microsoft.AspNetCore.Http;

namespace Repository.Services;

public class GetUserProvider(IHttpContextAccessor accessor) : IGetUserProvider
{
    public string? UserName { get; } = accessor.HttpContext?.User.Identity?.Name;
}
