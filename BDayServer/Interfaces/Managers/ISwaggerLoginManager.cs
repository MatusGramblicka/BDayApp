using Contracts.DataTransferObjects.Auth;

namespace Interfaces.Managers;

public interface ISwaggerLoginManager
{
    Task<string?> Login(SwaggerLoginDto swaggerLoginDto);
}