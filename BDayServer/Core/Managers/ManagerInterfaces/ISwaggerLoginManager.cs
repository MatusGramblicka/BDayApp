using Contracts.DataTransferObjects.Auth;

namespace Core.Managers.ManagerInterfaces;

public interface ISwaggerLoginManager
{
    Task<string?> Login(SwaggerLoginDto swaggerLoginDto);
}