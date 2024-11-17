using Contracts.Exceptions;
using Entities.DataTransferObjects.Auth;
using Entities.Models;
using Interfaces;
using Interfaces.Managers;
using Identity = Microsoft.AspNetCore.Identity;

namespace Core.Managers;

public class SwaggerLoginManager(Identity.UserManager<User> userManager, IAuthenticationService authenticationService)
    : ISwaggerLoginManager
{
    public async Task<string?> Login(SwaggerLoginDto swaggerLoginDto)
    {
        if (string.IsNullOrEmpty(swaggerLoginDto.Email) ||
            string.IsNullOrEmpty(swaggerLoginDto.Password))
            throw new SwaggerLoginAuthenticationException("Username and/or Password not specified");

        var managedUser = await userManager.FindByEmailAsync(swaggerLoginDto.Email);
        if (managedUser is null)
            throw new SwaggerLoginAuthenticationException("Bad credentials");

        var isPasswordValid = await userManager.CheckPasswordAsync(managedUser, swaggerLoginDto.Password);
        if (!isPasswordValid)
            throw new SwaggerLoginAuthenticationException("Bad credentials");

        var jwtSecurityToken = await authenticationService.GetToken(managedUser);
        return jwtSecurityToken;
    }
}