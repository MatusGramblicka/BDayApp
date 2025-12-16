using Microsoft.AspNetCore.Http;

namespace Core.Managers.ManagerInterfaces;

public interface IUploadManager
{
    string? Upload(HttpRequest request);
}