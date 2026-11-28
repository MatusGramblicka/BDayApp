using Entities.RequestFeatures;
using Microsoft.EntityFrameworkCore;

namespace Repository.Extensions;

public static class PagingExtensions
{
    /// <summary>Reads one page and the total count asynchronously (two queries).</summary>
    public static async Task<PagedList<T>> ToPagedListAsync<T>(this IQueryable<T> source, int pageNumber,
        int pageSize, CancellationToken cancellationToken = default)
    {
        var count = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<T>(items, count, pageNumber, pageSize);
    }
}
