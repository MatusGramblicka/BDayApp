using Entities.DataTransferObjects.Auth;

namespace Contracts.Managers;

public interface ISwaggerLoginManager
{
    Task<string?> Login(SwaggerLoginDto swaggerLoginDto);
}