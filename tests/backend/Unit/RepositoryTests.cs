using Backend.Data;
using Backend.Models;

namespace Backend.Tests.Unit;

/// <summary>
/// Testes unitários do Repository&lt;T&gt; (camada de dados).
/// Cobrem o CRUD básico e o suporte a Include (carregamento de navegações),
/// que é o que garante o personName correto nas transações (bug #1).
/// </summary>
public class RepositoryTests
{
    // ============================================================
    // GET BY ID
    // ============================================================

    [Fact]
    public async Task GetByIdAsync_WithExistingEntity_ShouldReturnEntity()
    {
        // Arrange
        using var context = TestDatabase.CreateContext();
        var repo = new Repository<Person>(context);
        var person = new Person { Name = "Ana", Age = 30 };
        context.People.Add(person);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetByIdAsync(person.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Ana", result!.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        using var context = TestDatabase.CreateContext();
        var repo = new Repository<Person>(context);

        // Act
        var result = await repo.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    // ============================================================
    // GET ALL
    // ============================================================

    [Fact]
    public async Task GetAllAsync_WithNoEntities_ShouldReturnEmptyList()
    {
        // Arrange
        using var context = TestDatabase.CreateContext();
        var repo = new Repository<Person>(context);

        // Act
        var result = await repo.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WithEntities_ShouldReturnAll()
    {
        // Arrange
        using var context = TestDatabase.CreateContext();
        var repo = new Repository<Person>(context);
        context.People.AddRange(
            new Person { Name = "Ana", Age = 30 },
            new Person { Name = "Bruno", Age = 25 });
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_WithInclude_ShouldLoadNavigation()
    {
        // Arrange — pessoa com 2 transações. O Include na navegação Transactions
        // é o que garante o personName populado nas listagens (bug #1).
        using var context = TestDatabase.CreateContext();
        var repo = new Repository<Person>(context);

        var person = new Person { Name = "Ana", Age = 30 };
        context.People.Add(person);
        await context.SaveChangesAsync();

        context.Transactions.AddRange(
            new Transaction { Description = "Salário", Amount = 5000, Type = TransactionType.Receita, PersonId = person.Id },
            new Transaction { Description = "Aluguel", Amount = 1500, Type = TransactionType.Despesa, PersonId = person.Id });
        await context.SaveChangesAsync();

        // Act — GetAllAsync(p => p.Transactions)
        var result = await repo.GetAllAsync(p => p.Transactions);
        var personWithTxs = Assert.Single(result);

        // Assert — a navegação deve estar populada com as 2 transações
        Assert.Equal(2, personWithTxs.Transactions.Count);
    }

    // ============================================================
    // ADD / SAVE (Unit of Work)
    // ============================================================

    [Fact]
    public async Task AddAsync_AndSaveChangesAsync_ShouldPersist()
    {
        // Arrange
        using var context = TestDatabase.CreateContext();
        var repo = new Repository<Person>(context);
        var person = new Person { Name = "Ana", Age = 30 };

        // Act — AddAsync NÃO persiste sozinho (Unit of Work);
        // SaveChangesAsync é quem faz o commit.
        await repo.AddAsync(person);
        await repo.SaveChangesAsync();

        // Assert
        var saved = await repo.GetByIdAsync(person.Id);
        Assert.NotNull(saved);
        Assert.Equal("Ana", saved!.Name);
    }

    // ============================================================
    // DELETE
    // ============================================================

    [Fact]
    public async Task Delete_AndSaveChangesAsync_ShouldRemoveEntity()
    {
        // Arrange
        using var context = TestDatabase.CreateContext();
        var repo = new Repository<Person>(context);
        var person = new Person { Name = "Ana", Age = 30 };
        await repo.AddAsync(person);
        await repo.SaveChangesAsync();

        // Act
        repo.Delete(person);
        await repo.SaveChangesAsync();

        // Assert
        Assert.Null(await repo.GetByIdAsync(person.Id));
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutChanges_ShouldNotThrow()
    {
        // Arrange
        using var context = TestDatabase.CreateContext();
        var repo = new Repository<Person>(context);

        // Act & Assert — commit vazio não deve lançar exceção
        await repo.SaveChangesAsync();
    }
}
