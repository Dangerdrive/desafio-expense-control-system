using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

/// <summary>
/// Implementação genérica do Repository Pattern usando EF Core.
/// 
/// Por que herdar de IRepository&lt;T&gt; em vez de criar repositórios
/// específicos (PersonRepository, TransactionRepository)?
/// - Este projeto tem apenas 2 entidades. Se fossem 20, teríamos 20 classes
///   quase idênticas. A genérica resolve isso com uma só classe.
/// - Se uma entidade precisar de queries específicas (ex: "buscar pessoas
///   maiores de idade"), basta criar uma interface IPersonRepository : IRepository&lt;Person&gt;
///   e adicionar o método lá. É extensível sem quebrar o que já existe.
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    // O DbContext é protegido (protected) para que futuras subclasses
    // especializadas possam acessá-lo sem precisar de injeção extra.
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>(); // Obtém o DbSet<T> dinamicamente
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<T>> GetAllAsync(params Expression<Func<T, object?>>[] includes)
    {
        IQueryable<T> query = _dbSet;

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        // Não chamamos SaveChangesAsync aqui — o Service decide quando persistir.
        // Isso segue o princípio "Unit of Work": várias operações podem ser
        // agrupadas em uma única transação antes do commit.
    }

    public void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
