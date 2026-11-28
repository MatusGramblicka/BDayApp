using System.Globalization;

namespace BDayServer.HostedService;

/// <summary>
/// Records the last completed occurrence of the scheduled job in a small text file (no database change needed), so that
/// an occurrence missed while the app was asleep (App Service Free tier has no Always On) can be caught up, and a
/// completed occurrence is not run again after a restart. Reading and writing never throw: IO problems are logged, and
/// a file that cannot be read counts as "nothing recorded".
/// </summary>
public sealed class CronJobStateFile(string path, ILogger<CronJobStateFile> logger)
{
    public const string FileName = "last-run.txt";

    public string FilePath { get; } = path;

    /// <summary>
    /// Where the file lives: the setting <c>Scheduler:StateFile</c> if set; on Azure App Service
    /// <c>%HOME%\data\Scheduler\last-run.txt</c> (persistent storage outside site\wwwroot, so no deployment can delete
    /// it - not even Web Deploy with "Remove additional files at destination"); otherwise a folder in the user's local
    /// application data, which is never published or committed.
    /// </summary>
    public static string ResolvePath(string? configuredPath, string? siteName, string? home, string? localDataFolder)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
            return Path.GetFullPath(configuredPath);

        if (!string.IsNullOrEmpty(siteName) && !string.IsNullOrEmpty(home))
            return Path.Combine(home, "data", "Scheduler", FileName);

        var localRoot = string.IsNullOrEmpty(localDataFolder) ? Path.GetTempPath() : localDataFolder;
        return Path.Combine(localRoot, "BDayServer", "Scheduler", FileName);
    }

    /// <summary>The last completed occurrence, or null when none is recorded or the file cannot be read.</summary>
    public async Task<DateTimeOffset?> ReadLastCompletedOccurrenceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(FilePath))
                return null;

            var text = await File.ReadAllTextAsync(FilePath, cancellationToken);
            if (DateTimeOffset.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
                    out var occurrence))
                return occurrence;

            logger.LogWarning("Ignoring the scheduler state file {StateFile}: it does not contain a date and time", FilePath);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Reading the scheduler state file {StateFile} failed", FilePath);
        }

        return null;
    }

    /// <summary>
    /// Records <paramref name="occurrence"/> as completed. Written to a temporary file that then replaces the state file,
    /// so a crash or a concurrent reader never sees a half-written file.
    /// </summary>
    public async Task WriteLastCompletedOccurrenceAsync(DateTimeOffset occurrence)
    {
        var temporaryPath = FilePath + ".tmp";
        try
        {
            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(occurrence.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
                await writer.FlushAsync();
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, FilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Writing the scheduler state file {StateFile} failed; a restart within 24 hours may " +
                                "run the occurrence {Occurrence:u} again", FilePath, occurrence);
            TryDelete(temporaryPath);
        }
    }

    private static void TryDelete(string file)
    {
        try
        {
            File.Delete(file);
        }
        catch (Exception)
        {
            // Best effort: a leftover temporary file is overwritten by the next write.
        }
    }
}
