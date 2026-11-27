using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace BDayServer.UnitTests.HostedService;

/// <summary>Collects log entries, with the message formatted as a logging provider would write it.</summary>
internal sealed class TestLogger<T> : ILogger<T>
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public IReadOnlyList<LogEntry> Entries => [.. _entries];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        _entries.Enqueue(new LogEntry(logLevel, formatter(state, exception), exception));
}

internal sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);
