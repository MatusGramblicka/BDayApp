using Microsoft.AspNetCore.Http;

namespace Core.Managers.ManagerInterfaces;

public interface IUploadManager
{
    /// <summary>Stores the first image of the multipart form and returns its URL path ("StaticFiles/Images/...").</summary>
    Task<string> UploadAsync(HttpRequest request, CancellationToken cancellationToken = default);
}
