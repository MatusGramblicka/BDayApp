namespace Contracts.Exceptions;

/// <summary>An administrator's request that breaks a rule, e.g. removing the last administrator.</summary>
public class UserManagementException(string message) : Exception(message);
