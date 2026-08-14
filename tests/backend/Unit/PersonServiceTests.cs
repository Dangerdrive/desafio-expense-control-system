using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;

namespace Backend.Tests.Unit;

/// <summary>
/// Testes unitários do PersonService.
/// Cobre criação, listagem, exclusão e regras de negócio de pessoas.
/// </summary>
public class PersonServiceTests
{
    // ============================================================
    // CREATE
    // ============================================================

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreatePerson()
    {
        // Arrange
        using var context = TestDatabase.CreateContext();
        var service = new PersonService(new Repository<Person>(context));
        var dto = new CreatePersonDto { Name = "João Silva", Age = 30 };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("João Silva", result.Name);
        Assert.Equal(30, result.Age);
    }

    [Fact]
    public async Task CreateAsync_WithMinimumAge_ShouldCreatePerson()
    {
        // Arrange — idade 0 é válida (recém-nascido)
        using var context = TestDatabase.CreateContext();
        var service = new PersonService(new Repository<Person>(context));
        var dto = new CreatePersonDto { Name = "Bebê", Age = 0 };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.Equal(0, result.Age);
    }

    [Fact]
    public async Task CreateAsync_WithMaximumAge_ShouldCreatePerson()
    {
        // Arrange — idade 150 é o limite máximo
        using var context = TestDatabase.CreateContext();
        var service = new PersonService(new Repository<Person>(context));
        var dto = new CreatePersonDto { Name = "Centenário", Age = 150 };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.Equal(150, result.Age);
    }

    // ============================================================
    // GET ALL
    // ============================================================

    [Fact]
    public async Task GetAllAsync_WithNoPeople_ShouldReturnEmptyList()
    {
        // Arrange
        using var context = TestDatabase.CreateContext();
        var service = new PersonService(new Repository<Person>(context));

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WithMultiplePeople_ShouldReturnAllSortedByName()
    {
        // Arrange
        using var context = TestDatabase.CreateContext();
        var service = new PersonService(new Repository<Person>(context));

        // Insere em ordem não-alfabética para validar ordenação
        context.People.Add(new Person { Name = "Carlos", Age = 40 });
        context.People.Add(new Person { Name = "Ana", Age = 25 });
        context.People.Add(new Person { Name = "Bruno", Age = 35 });
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllAsync();

        // Assert — deve retornar ordenado por nome
        Assert.Equal(3, result.Count);
        Assert.Equal("Ana", result[0].Name);
        Assert.Equal("Bruno", result[1].Name);
        Assert.Equal("Carlos", result[2].Name);
    }

    // ============================================================
    // DELETE
    // ============================================================

    [Fact]
    public async Task DeleteAsync_WithExistingPerson_ShouldReturnTrue()
    {
        // Arrange
        using var context = TestDatabase.CreateContext();
        var service = new PersonService(new Repository<Person>(context));
        var person = new Person { Name = "José", Age = 50 };
        context.People.Add(person);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteAsync(person.Id);

        // Assert
        Assert.True(result);
        Assert.Null(await context.People.FindAsync(person.Id));
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingPerson_ShouldReturnFalse()
    {
        // Arrange
        using var context = TestDatabase.CreateContext();
        var service = new PersonService(new Repository<Person>(context));

        // Act
        var result = await service.DeleteAsync(999); // ID inexistente

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldCascadeDeleteTransactions()
    {
        // Arrange — cria pessoa com transações
        using var context = TestDatabase.CreateContext();
        var service = new PersonService(new Repository<Person>(context));

        var person = new Person { Name = "Maria", Age = 28 };
        context.People.Add(person);
        await context.SaveChangesAsync();

        context.Transactions.Add(new Transaction
        {
            Description = "Salário", Amount = 5000, Type = TransactionType.Receita, PersonId = person.Id
        });
        context.Transactions.Add(new Transaction
        {
            Description = "Aluguel", Amount = 1500, Type = TransactionType.Despesa, PersonId = person.Id
        });
        await context.SaveChangesAsync();

        // Act — deleta a pessoa
        await service.DeleteAsync(person.Id);

        // Assert — transações devem ser removidas em cascata
        var remainingTransactions = context.Transactions.ToList();
        Assert.Empty(remainingTransactions);
    }
}
