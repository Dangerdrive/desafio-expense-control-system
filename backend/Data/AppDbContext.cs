using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

/// <summary>
/// Contexto do banco de dados da aplicação, utilizando Entity Framework Core com SQLite.
/// Centraliza a configuração das entidades e o mapeamento objeto-relacional.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Tabela de pessoas cadastradas no sistema.
    /// </summary>
    public DbSet<Models.Person> People { get; set; } = null!;

    /// <summary>
    /// Tabela de transações financeiras.
    /// </summary>
    public DbSet<Models.Transaction> Transactions { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>
    /// Configura o relacionamento entre Person e Transaction:
    /// - Uma pessoa tem muitas transações.
    /// - Ao deletar uma pessoa, todas as suas transações são removidas (cascata).
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Relacionamento: Person (1) -> Transaction (N)
        modelBuilder.Entity<Models.Person>()
            .HasMany(p => p.Transactions)
            .WithOne(t => t.Person)
            .HasForeignKey(t => t.PersonId)
            .OnDelete(DeleteBehavior.Cascade); // Exclusão em cascata: deletar pessoa = deletar transações
    }
}
