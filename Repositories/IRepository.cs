using System.Linq.Expressions;

namespace APICatalogo.Repositories;

// cuidado para não violar o principio ISP

public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetAsync(Expression<Func<T, bool>> predicate);

    // Não acessa o banco de dados, realizam as operações na memória, não precisa ser assincrono
    T Create(T entity);
    T Update(T entity);
    T Delete(T entity);
}
