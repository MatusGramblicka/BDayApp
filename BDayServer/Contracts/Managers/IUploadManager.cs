using Microsoft.AspNetCore.Http;

namespace Contracts.Managers;

public interface IUploadManager
{
    string? Upload(HttpRequest request);
}