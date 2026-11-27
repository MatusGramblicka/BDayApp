using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Repository.DatabaseAccessInterfaces;
using Repository.Repositories;

namespace Repository;

public abstract class RepositoryBase<T>(DbRepositoryContext repositoryContext) : IRepositoryBase<T> where T : class
{
    protected readonly DbRepositoryContext RepositoryContext = repositoryContext;

    public IQueryable<T> FindAll(bool trackChanges) =>
        trackChanges
            ? RepositoryContext.Set<T>()
            : RepositoryContext.Set<T>().AsNoTracking();

    public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges) =>
        FindAll(trackChanges).Where(expression);

    public void Create(T entity) => RepositoryContext.Set<T>().Add(entity);

    public void Delete(T entity) => RepositoryContext.Set<T>().Remove(entity);
}
