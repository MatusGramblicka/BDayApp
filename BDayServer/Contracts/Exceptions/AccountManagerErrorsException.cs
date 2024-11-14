namespace Contracts.Exceptions;

public class AccountManagerErrorsException(IEnumerable<string> errors) : Exception
{
    public IEnumerable<string> Errors { get; set; } = errors;
}