namespace BDayClient.HttpRepository;

/// <summary>The outcome of an API call that returns nothing on success.</summary>
public sealed record ApiResult
{
    private ApiResult(bool succeeded, IReadOnlyList<string> errors)
    {
        Succeeded = succeeded;
        Errors = errors;
    }

    public bool Succeeded { get; }

    /// <summary>The server's messages when the call failed (at least one); empty on success.</summary>
    public IReadOnlyList<string> Errors { get; }

    public static ApiResult Success() => new(true, []);

    public static ApiResult Failure(IReadOnlyList<string> errors) => new(false, errors);
}
