namespace Contracts.Exceptions;

public class UnauthorizedUserException(string message) : Exception(message);