namespace Contracts.Exceptions;

public class UserNotExistException(string message) : Exception(message);