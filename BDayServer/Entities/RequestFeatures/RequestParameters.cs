namespace Entities.RequestFeatures;

public abstract class RequestParameters
{
    public const int MaxPageSize = 50;

    private int _pageNumber = 1;
    private int _pageSize = 10;

    /// <summary>1-based; smaller values are treated as 1.</summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = Math.Max(1, value);
    }

    /// <summary>Between 1 and <see cref="MaxPageSize"/>; other values are clamped into that range.</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }

    /// <summary>Comma-separated "property [asc|desc]" list, e.g. "name desc".</summary>
    public string? OrderBy { get; set; }
}
