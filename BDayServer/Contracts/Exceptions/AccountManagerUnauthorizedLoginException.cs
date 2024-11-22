namespace Contracts.Exceptions;

public class AccountManagerUnauthorizedLoginException(string message) : Exception(message);