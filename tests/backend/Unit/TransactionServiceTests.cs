using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;

namespace Backend.Tests.Unit;

/// <summary>
/// Testes unitários do TransactionService.
/// Foco na regra de negócio: menores de 18 anos só podem ter despesas.
/// </summary>
public class TransactionServiceTests
{
    /// <summary>
    /// Helper: cria um TransactionService com uma pessoa adulta já no banco.
    /// Agora usa o Repository Pattern em vez do DbContext diretamente.
    /// </summary>
    private async Task<(TransactionService Service, int PersonId)> SetupAdultAsync()
    {
        var context = TestDatabase.CreateContext();
        var personRepo = new Repository<Person>(context);
        var transactionRepo = new Repository<Transaction>(context);
        var personService = new PersonService(personRepo);

        var person = new Person { Name = "Adulto", Age = 30 };
        await personRepo.AddAsync(person);
        await personRepo.SaveChangesAsync();

        var service = new TransactionService(transactionRepo, personService);
        return (service, person.Id);
    }

    /// <summary>
    /// Helper: cria um TransactionService com uma pessoa menor de idade no banco.
    /// </summary>
    private async Task<(TransactionService Service, int PersonId)> SetupMinorAsync()
    {
        var context = TestDatabase.CreateContext();
        var personRepo = new Repository<Person>(context);
        var transactionRepo = new Repository<Transaction>(context);
        var personService = new PersonService(personRepo);

        var person = new Person { Name = "Menor", Age = 15 };
        await personRepo.AddAsync(person);
        await personRepo.SaveChangesAsync();

        var service = new TransactionService(transactionRepo, personService);
        return (service, person.Id);
    }

    // ============================================================
    // ADULTO (>= 18 anos) — pode receita E despesa
    // ============================================================

    [Fact]
    public async Task CreateAsync_AdultWithIncome_ShouldSucceed()
    {
        // Arrange — adulto de 30 anos cadastrando receita
        var (service, personId) = await SetupAdultAsync();
        var dto = new CreateTransactionDto
        {
            Description = "Salário",
            Amount = 5000m,
            Type = TransactionType.Receita,
            PersonId = personId
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Salário", result.Description);
        Assert.Equal(5000m, result.Amount);
        Assert.Equal(TransactionType.Receita, result.Type);
    }

    [Fact]
    public async Task CreateAsync_AdultWithExpense_ShouldSucceed()
    {
        // Arrange — adulto cadastrando despesa
        var (service, personId) = await SetupAdultAsync();
        var dto = new CreateTransactionDto
        {
            Description = "Aluguel",
            Amount = 1500m,
            Type = TransactionType.Despesa,
            PersonId = personId
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.Equal(TransactionType.Despesa, result.Type);
    }

    // ============================================================
    // MENOR DE IDADE (< 18 anos) — regra de negócio crítica
    // ============================================================

    [Fact]
    public async Task CreateAsync_MinorWithIncome_ShouldThrowException()
    {
        // Arrange — menor de 15 anos tentando cadastrar receita
        var (service, personId) = await SetupMinorAsync();
        var dto = new CreateTransactionDto
        {
            Description = "Mesada",
            Amount = 100m,
            Type = TransactionType.Receita,
            PersonId = personId
        };

        // Act & Assert — deve lançar exceção com mensagem específica
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(dto));
        Assert.Contains("Menores de 18 anos", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_MinorWithExpense_ShouldSucceed()
    {
        // Arrange — menor de 15 anos cadastrando despesa (PERMITIDO)
        var (service, personId) = await SetupMinorAsync();
        var dto = new CreateTransactionDto
        {
            Description = "Lanche",
            Amount = 25.50m,
            Type = TransactionType.Despesa,
            PersonId = personId
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert — deve permitir despesa para menor
        Assert.NotNull(result);
        Assert.Equal(TransactionType.Despesa, result.Type);
    }

    [Fact]
    public async Task CreateAsync_MinorExactly17_WithIncome_ShouldThrowException()
    {
        // Arrange — pessoa com exatamente 17 anos (ainda é menor)
        var context = TestDatabase.CreateContext();
        var personRepo = new Repository<Person>(context);
        var personService = new PersonService(personRepo);
        var transactionRepo = new Repository<Transaction>(context);

        var person = new Person { Name = "Quase 18", Age = 17 };
        await personRepo.AddAsync(person);
        await personRepo.SaveChangesAsync();

        var service = new TransactionService(transactionRepo, personService);
        var dto = new CreateTransactionDto
        {
            Description = "Freela", Amount = 200m, Type = TransactionType.Receita, PersonId = person.Id
        };

        // Act & Assert — 17 anos ainda é barrado
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(dto));
        Assert.Contains("Menores de 18 anos", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_Exactly18_WithIncome_ShouldSucceed()
    {
        // Arrange — pessoa com exatamente 18 anos (boundary: maior de idade)
        var context = TestDatabase.CreateContext();
        var personRepo = new Repository<Person>(context);
        var personService = new PersonService(personRepo);
        var transactionRepo = new Repository<Transaction>(context);

        var person = new Person { Name = "Recém 18", Age = 18 };
        await personRepo.AddAsync(person);
        await personRepo.SaveChangesAsync();

        var service = new TransactionService(transactionRepo, personService);
        var dto = new CreateTransactionDto
        {
            Description = "Salário", Amount = 2000m, Type = TransactionType.Receita, PersonId = person.Id
        };

        // Act — 18 anos deve ser permitido
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TransactionType.Receita, result.Type);
    }

    // ============================================================
    // PESSOA INEXISTENTE
    // ============================================================

    [Fact]
    public async Task CreateAsync_WithNonExistingPerson_ShouldThrowException()
    {
        // Arrange
        using var context = TestDatabase.CreateContext();
        var personService = new PersonService(new Repository<Person>(context));
        var service = new TransactionService(new Repository<Transaction>(context), personService);
        var dto = new CreateTransactionDto
        {
            Description = "Teste", Amount = 100m, Type = TransactionType.Despesa, PersonId = 999
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(dto));
        Assert.Contains("não existe", ex.Message);
    }

    // ============================================================
    // LISTAGEM
    // ============================================================

    [Fact]
    public async Task GetAllAsync_WithNoTransactions_ShouldReturnEmptyList()
    {
        // Arrange
        using var context = TestDatabase.CreateContext();
        var personService = new PersonService(new Repository<Person>(context));
        var service = new TransactionService(new Repository<Transaction>(context), personService);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleTransactions_ShouldReturnAll()
    {
        // Arrange
        var context = TestDatabase.CreateContext();
        var personRepo = new Repository<Person>(context);
        var transactionRepo = new Repository<Transaction>(context);
        var personService = new PersonService(personRepo);

        var person = new Person { Name = "Adulto", Age = 30 };
        await personRepo.AddAsync(person);
        await personRepo.SaveChangesAsync();

        await transactionRepo.AddAsync(new Transaction { Description = "T1", Amount = 100, Type = TransactionType.Receita, PersonId = person.Id });
        await transactionRepo.AddAsync(new Transaction { Description = "T2", Amount = 50, Type = TransactionType.Despesa, PersonId = person.Id });
        await transactionRepo.SaveChangesAsync();

        var service = new TransactionService(transactionRepo, personService);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    // ============================================================
    // VALORES LIMITE (edge cases)
    // ============================================================

    [Fact]
    public async Task CreateAsync_WithVeryLargeAmount_ShouldSucceed()
    {
        // Arrange — valor grande (ex: compra de imóvel)
        var (service, personId) = await SetupAdultAsync();
        var dto = new CreateTransactionDto
        {
            Description = "Apartamento", Amount = 999_999_999.99m, Type = TransactionType.Despesa, PersonId = personId
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.Equal(999_999_999.99m, result.Amount);
    }

    [Fact]
    public async Task CreateAsync_WithDecimalPrecision_ShouldPreserveCents()
    {
        // Arrange — valor com centavos
        var (service, personId) = await SetupAdultAsync();
        var dto = new CreateTransactionDto
        {
            Description = "Café", Amount = 4.75m, Type = TransactionType.Despesa, PersonId = personId
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert — centavos devem ser preservados
        Assert.Equal(4.75m, result.Amount);
    }
}
