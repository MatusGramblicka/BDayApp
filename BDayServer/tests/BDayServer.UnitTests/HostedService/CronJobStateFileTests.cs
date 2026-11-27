using BDayServer.HostedService;
using Microsoft.Extensions.Logging;

namespace BDayServer.UnitTests.HostedService;

public sealed class CronJobStateFileTests : IDisposable
{
    private static readonly DateTimeOffset Occurrence = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "BDayServer.UnitTests", nameof(CronJobStateFileTests), Guid.NewGuid().ToString("N"));

    private readonly TestLogger<CronJobStateFile> _logger = new();

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private CronJobStateFile CreateSut(params string[] relativePath) =>
        new(Path.Combine([_directory, .. relativePath]), _logger);

    [Fact]
    public async Task Read_NoFile_ReturnsNull()
    {
        (await CreateSut("last-run.txt").ReadLastCompletedOccurrenceAsync()).Should().BeNull();
    }

    [Fact]
    public async Task Write_CreatesTheFolderAndRoundTripsTheOccurrence_WithoutLeavingATemporaryFile()
    {
        var sut = CreateSut("Scheduler", "last-run.txt");
        var occurrenceInAnotherZone = Occurrence.ToOffset(TimeSpan.FromHours(1));

        await sut.WriteLastCompletedOccurrenceAsync(occurrenceInAnotherZone);

        (await sut.ReadLastCompletedOccurrenceAsync()).Should().Be(Occurrence);
        Directory.GetFiles(Path.Combine(_directory, "Scheduler")).Should().ContainSingle()
            .Which.Should().Be(sut.FilePath);
    }

    [Fact]
    public async Task Write_ReplacesThePreviousOccurrence()
    {
        var sut = CreateSut("last-run.txt");
        await sut.WriteLastCompletedOccurrenceAsync(Occurrence);

        await sut.WriteLastCompletedOccurrenceAsync(Occurrence.AddDays(1));

        (await sut.ReadLastCompletedOccurrenceAsync()).Should().Be(Occurrence.AddDays(1));
    }

    [Fact]
    public async Task Read_UnexpectedContent_ReturnsNullAndLogsAWarning()
    {
        var sut = CreateSut("last-run.txt");
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(sut.FilePath, "not a date");

        (await sut.ReadLastCompletedOccurrenceAsync()).Should().BeNull();
        _logger.Entries.Should().Contain(e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Read_AcceptsAHandEditedValue()
    {
        var sut = CreateSut("last-run.txt");
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(sut.FilePath, "2026-11-22T12:00:00Z" + Environment.NewLine);

        (await sut.ReadLastCompletedOccurrenceAsync()).Should().Be(Occurrence);
    }

    [Fact]
    public async Task Write_IoError_IsLoggedNotThrown()
    {
        // A file where the folder should be: the folder cannot be created.
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(Path.Combine(_directory, "blocked"), "");
        var sut = CreateSut("blocked", "last-run.txt");

        var act = () => sut.WriteLastCompletedOccurrenceAsync(Occurrence);

        await act.Should().NotThrowAsync();
        _logger.Entries.Should().Contain(e => e.Level == LogLevel.Error && e.Exception != null);
        (await sut.ReadLastCompletedOccurrenceAsync()).Should().BeNull();
    }

    [Fact]
    public void ResolvePath_ConfiguredPath_WinsEvenOnAppService()
    {
        var configured = Path.Combine(_directory, "custom.txt");

        CronJobStateFile.ResolvePath(configured, "bdayserver", Path.Combine(_directory, "home"), _directory)
            .Should().Be(configured);
    }

    [Fact]
    public void ResolvePath_OnAppService_UsesTheDataFolderOutsideTheDeployedApp()
    {
        var home = Path.Combine(_directory, "home");

        CronJobStateFile.ResolvePath(null, "bdayserver", home, _directory)
            .Should().Be(Path.Combine(home, "data", "Scheduler", "last-run.txt"));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "home")] // HOME alone is set locally by e.g. Git Bash
    [InlineData("bdayserver", null)]
    public void ResolvePath_Locally_UsesTheLocalApplicationDataFolder(string? siteName, string? home)
    {
        CronJobStateFile.ResolvePath(" ", siteName, home, _directory)
            .Should().Be(Path.Combine(_directory, "BDayServer", "Scheduler", "last-run.txt"));
    }

    [Fact]
    public void ResolvePath_WithoutLocalApplicationDataFolder_UsesTheTemporaryFolder()
    {
        CronJobStateFile.ResolvePath(null, null, null, "")
            .Should().Be(Path.Combine(Path.GetTempPath(), "BDayServer", "Scheduler", "last-run.txt"));
    }
}
