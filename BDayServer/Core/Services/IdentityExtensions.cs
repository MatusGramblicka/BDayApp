using Microsoft.AspNetCore.Identity;

namespace Core.Services;

public static class IdentityExtensions
{
    public static void ThrowIfFailed(this IdentityResult result, string operation)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException($"{operation} failed: {result.Describe()}");
    }

    public static string Describe(this IdentityResult result) =>
        string.Join(" ", result.Errors.Select(e => e.Description));

    /// <summary>
    /// Creates the role if the database doesn't have it yet. Nothing seeds the roles, so they are created on first use.
    /// </summary>
    public static async Task EnsureRoleExistsAsync(this RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
            return;

        var result = await roleManager.CreateAsync(new IdentityRole(roleName));

        // Fine if a concurrent request created it in the meantime.
        if (!result.Succeeded && !await roleManager.RoleExistsAsync(roleName))
            throw new InvalidOperationException($"The role '{roleName}' could not be created: {result.Describe()}");
    }
}
