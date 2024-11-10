namespace Contracts.Exceptions;

public class PersonNotExistException(string message) : Exception(message);