using System.Threading.Tasks;

namespace Contracts
{
    public interface IRepositoryManager
    {
        IPersonRepository Person { get; }       
        Task SaveAsync();
    }
}
