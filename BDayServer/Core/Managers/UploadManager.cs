using Contracts.Exceptions;
using Core.Managers.ManagerInterfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Core.Managers;

public class UploadManager(IOptions<UploadOptions> options) : IUploadManager
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;

    // Uploaded files are served from the site's own origin, so only plain image types are accepted
    // (an uploaded .html or .svg could run script in the app's origin and read the stored login token).
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };

    public async Task<string> UploadAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.HasFormContentType)
            throw new UploadException("Upload was not successful");

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.FirstOrDefault();

        if (file is null || file.Length == 0)
            throw new UploadException("Upload was not successful");

        if (file.Length > MaxFileSizeBytes)
            throw new UploadException("The image is too large (maximum is 10 MB).");

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
            throw new UploadException("Only image files (jpg, png, gif, webp, bmp) can be uploaded.");

        // Never use the client's file name on disk: it could contain "..\" or an absolute path
        // and overwrite files outside the images folder.
        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var folder = Path.Combine(options.Value.RootPath, "Images");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, fileName);

        try
        {
            await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 81920, useAsync: true);
            await file.CopyToAsync(stream, cancellationToken);
        }
        catch
        {
            // No half-written images (e.g. when the client disconnects).
            File.Delete(path);
            throw;
        }

        // Path of the image URL, served under /StaticFiles.
        return $"StaticFiles/Images/{fileName}";
    }
}
