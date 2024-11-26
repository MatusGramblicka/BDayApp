using Contracts.Exceptions;
using Interfaces.Managers;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace Core.Managers;

public class UploadManager : IUploadManager
{
    public string? Upload(HttpRequest request)
    {
        var file = request.Form.Files[0];

        var folderName = Path.Combine("StaticFiles", "Images");

        var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);

        if (file.Length > 0)
        {
            var fileName = ContentDispositionHeaderValue
                .Parse(file.ContentDisposition).FileName?.Trim('"');

            var fullPath = Path.Combine(pathToSave, fileName);
            var dbPath = Path.Combine(folderName, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            file.CopyTo(stream);

            return dbPath;
        }

        throw new UploadException("Upload was not successful");
    }
}