using Microsoft.AspNetCore.Http;

namespace Interfaces.Managers;

public interface IUploadManager
{
    string? Upload(HttpRequest request);
}