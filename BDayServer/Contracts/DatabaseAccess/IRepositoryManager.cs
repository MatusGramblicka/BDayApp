namespace Contracts.DatabaseAccess;

public interface IRepositoryManager
{
    IPersonRepository Person { get; }

    IEventRepository Event { get; }

    Task SaveAsync();
}