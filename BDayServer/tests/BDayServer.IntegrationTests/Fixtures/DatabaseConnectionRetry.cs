using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;
using Polly.Retry;

namespace BDayServer.IntegrationTests.Fixtures;

internal static class DatabaseConnectionRetry
{
    public static async Task<TConnection> OpenConnectionWithRetryAsync<TConnection, TException>(
        string connectionString, string pipelineName, Func<string, TConnection> createConnection)
        where TConnection : DbConnection
        where TException : Exception
    {
        var services = new ServiceCollection();
        services.AddResiliencePipeline<string, TConnection>(pipelineName, builder =>
        {
            builder.AddRetry(new RetryStrategyOptions<TConnection>
            {
                Delay = TimeSpan.FromSeconds(5),
                MaxRetryAttempts = 5,
                ShouldHandle = new PredicateBuilder<TConnection>().Handle<TException>()
            });
        });

        await using var serviceProvider = services.BuildServiceProvider();
        var pipeline = serviceProvider
            .GetRequiredService<ResiliencePipelineProvider<string>>()
            .GetPipeline<TConnection>(pipelineName);

        return await pipeline.ExecuteAsync(async cancellationToken =>
        {
            var connection = createConnection(connectionString);

            try
            {
                await connection.OpenAsync(cancellationToken);
                return connection;
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }, CancellationToken.None);
    }
}
