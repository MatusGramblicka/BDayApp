namespace Core.Managers;

/// <summary>
/// Where uploaded files are stored on disk: the folder that is served under /StaticFiles.
/// Set at startup by <c>ConfigureUploads</c> in BDayServer.
/// </summary>
public class UploadOptions
{
    public string RootPath { get; set; } = string.Empty;
}
