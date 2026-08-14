using System.Linq.Expressions;

namespace Backend.Data;

/// <summary>
/// Interface genérica para o padrão Repository.
/// 
/// Por que usar uma interface genérica em vez de repositórios específicos?
/// - DRY (Don't Repeat Yourself): as operações CRUD básicas são idênticas
///   para Person e Transaction. Uma interface genérica evita duplicação.
/// - Testabilidade: nos testes unitários, podemos mockar IRepository&lt;T&gt;
///   em vez de depender do DbContext real.
/// - Flexibilidade: se no futuro trocarmos SQLite por PostgreSQL, apenas
///   a implementação do Repository muda — os Services permanecem iguais.
/// </summary>
/// <typeparam name="T">Tipo da entidade (Person ou Transaction).</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Busca uma entidade pelo ID.
    /// Retorna null se não encontrada — o chamador decide como tratar.
    /// </summary>
    Task<T?> GetByIdAsync(int id);

    /// <summary>
    /// Lista todas as entidades.
    /// O parâmetro opcional permite incluir navegações (ex: Transaction.Person).
    /// Usa object? porque navegações são anuláveis (ex: Transaction.Person).
    /// </summary>
    Task<List<T>> GetAllAsync(params Expression<Func<T, object?>>[] includes);

    /// <summary>
    /// Adiciona uma nova entidade ao contexto.
    /// O SaveChanges NÃO é chamado aqui — segue o padrão Unit of Work:
    /// quem orquestra a operação decide quando persistir.
    /// </summary>
    Task AddAsync(T entity);

    /// <summary>
    /// Remove uma entidade do contexto.
    /// Para Person, a exclusão em cascata das Transactions é garantida
    /// pelo DeleteBehavior.Cascade configurado no AppDbContext.
    /// </summary>
    void Delete(T entity);

    /// <summary>
    /// Persiste todas as mudanças pendentes no banco de dados.
    /// Centralizado aqui para que o Service não precise acessar o DbContext diretamente.
    /// </summary>
    Task SaveChangesAsync();
}
