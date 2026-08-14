using Backend.Data;
using Backend.Models;
using Backend.Services;

namespace Backend.Tests.Unit;

/// <summary>
/// Testes unitários do TotalsService.
/// Valida o cálculo correto de receitas, despesas e saldo por pessoa e geral.
/// </summary>
public class TotalsServiceTests
{
    [Fact]
    public async Task GetTotalsAsync_WithNoPeople_ShouldReturnEmptyList()
    {
        // Arrange
        using var context = TestDatabase.CreateContext();
        var service = new TotalsService(new Repository<Person>(context));

        // Act
        var result = await service.GetTotalsAsync();

        // Assert
        Assert.Empty(result.PeopleTotals);
        Assert.Equal(0, result.GrandTotalIncome);
        Assert.Equal(0, result.GrandTotalExpense);
        Assert.Equal(0, result.GrandBalance);
    }

    [Fact]
    public async Task GetTotalsAsync_WithPeopleButNoTransactions_ShouldReturnZeros()
    {
        // Arrange — pessoas cadastradas mas sem transações
        using var context = TestDatabase.CreateContext();
        context.People.Add(new Person { Name = "Ana", Age = 30 });
        context.People.Add(new Person { Name = "Bruno", Age = 25 });
        await context.SaveChangesAsync();
        var service = new TotalsService(new Repository<Person>(context));

        // Act
        var result = await service.GetTotalsAsync();

        // Assert — totais zerados para cada pessoa
        Assert.Equal(2, result.PeopleTotals.Count);
        foreach (var pt in result.PeopleTotals)
        {
            Assert.Equal(0, pt.TotalIncome);
            Assert.Equal(0, pt.TotalExpense);
            Assert.Equal(0, pt.Balance);
        }
        Assert.Equal(0, result.GrandBalance);
    }

    [Fact]
    public async Task GetTotalsAsync_ShouldCalculateCorrectTotals()
    {
        // Arrange — cenário completo com receitas e despesas
        using var context = TestDatabase.CreateContext();

        var joao = new Person { Name = "João", Age = 30 };
        var maria = new Person { Name = "Maria", Age = 25 };
        context.People.AddRange(joao, maria);
        await context.SaveChangesAsync();

        // João: R$5000 receita + R$2000 despesa = R$3000 saldo
        context.Transactions.Add(new Transaction { Description = "Salário", Amount = 5000, Type = "receita", PersonId = joao.Id });
        context.Transactions.Add(new Transaction { Description = "Aluguel", Amount = 2000, Type = "despesa", PersonId = joao.Id });

        // Maria: R$3000 receita + R$1000 despesa = R$2000 saldo
        context.Transactions.Add(new Transaction { Description = "Salário", Amount = 3000, Type = "receita", PersonId = maria.Id });
        context.Transactions.Add(new Transaction { Description = "Contas", Amount = 1000, Type = "despesa", PersonId = maria.Id });

        await context.SaveChangesAsync();
        var service = new TotalsService(new Repository<Person>(context));

        // Act
        var result = await service.GetTotalsAsync();

        // Assert — totais por pessoa
        var joaoTotals = result.PeopleTotals.First(p => p.PersonId == joao.Id);
        Assert.Equal(5000, joaoTotals.TotalIncome);
        Assert.Equal(2000, joaoTotals.TotalExpense);
        Assert.Equal(3000, joaoTotals.Balance);

        var mariaTotals = result.PeopleTotals.First(p => p.PersonId == maria.Id);
        Assert.Equal(3000, mariaTotals.TotalIncome);
        Assert.Equal(1000, mariaTotals.TotalExpense);
        Assert.Equal(2000, mariaTotals.Balance);

        // Assert — total geral
        Assert.Equal(8000, result.GrandTotalIncome);   // 5000 + 3000
        Assert.Equal(3000, result.GrandTotalExpense);   // 2000 + 1000
        Assert.Equal(5000, result.GrandBalance);        // 8000 - 3000
    }

    [Fact]
    public async Task GetTotalsAsync_WithNegativeBalance_ShouldCalculateCorrectly()
    {
        // Arrange — pessoa com mais despesas que receitas (saldo negativo)
        using var context = TestDatabase.CreateContext();
        var person = new Person { Name = "Endividado", Age = 40 };
        context.People.Add(person);
        await context.SaveChangesAsync();

        context.Transactions.Add(new Transaction { Description = "Freela", Amount = 1000, Type = "receita", PersonId = person.Id });
        context.Transactions.Add(new Transaction { Description = "Cartão", Amount = 3000, Type = "despesa", PersonId = person.Id });
        await context.SaveChangesAsync();
        var service = new TotalsService(new Repository<Person>(context));

        // Act
        var result = await service.GetTotalsAsync();

        // Assert — saldo negativo
        var pt = result.PeopleTotals.Single();
        Assert.Equal(1000, pt.TotalIncome);
        Assert.Equal(3000, pt.TotalExpense);
        Assert.Equal(-2000, pt.Balance);
        Assert.Equal(-2000, result.GrandBalance);
    }

    [Fact]
    public async Task GetTotalsAsync_OnlyIncome_ShouldHavePositiveBalance()
    {
        // Arrange — pessoa só com receitas
        using var context = TestDatabase.CreateContext();
        var person = new Person { Name = "Poupador", Age = 35 };
        context.People.Add(person);
        await context.SaveChangesAsync();

        context.Transactions.Add(new Transaction { Description = "Investimento", Amount = 10000, Type = "receita", PersonId = person.Id });
        await context.SaveChangesAsync();
        var service = new TotalsService(new Repository<Person>(context));

        // Act
        var result = await service.GetTotalsAsync();

        // Assert
        var pt = result.PeopleTotals.Single();
        Assert.Equal(10000, pt.TotalIncome);
        Assert.Equal(0, pt.TotalExpense);
        Assert.Equal(10000, pt.Balance);
    }

    [Fact]
    public async Task GetTotalsAsync_OnlyExpense_ShouldHaveNegativeBalance()
    {
        // Arrange — pessoa só com despesas
        using var context = TestDatabase.CreateContext();
        var person = new Person { Name = "Gastador", Age = 22 };
        context.People.Add(person);
        await context.SaveChangesAsync();

        context.Transactions.Add(new Transaction { Description = "Shopping", Amount = 500, Type = "despesa", PersonId = person.Id });
        await context.SaveChangesAsync();
        var service = new TotalsService(new Repository<Person>(context));

        // Act
        var result = await service.GetTotalsAsync();

        // Assert
        var pt = result.PeopleTotals.Single();
        Assert.Equal(0, pt.TotalIncome);
        Assert.Equal(500, pt.TotalExpense);
        Assert.Equal(-500, pt.Balance);
    }

    [Fact]
    public async Task GetTotalsAsync_ShouldOrderByName()
    {
        // Arrange
        using var context = TestDatabase.CreateContext();
        context.People.Add(new Person { Name = "Zebra", Age = 30 });
        context.People.Add(new Person { Name = "Alpha", Age = 25 });
        await context.SaveChangesAsync();
        var service = new TotalsService(new Repository<Person>(context));

        // Act
        var result = await service.GetTotalsAsync();

        // Assert — ordenado alfabeticamente
        Assert.Equal("Alpha", result.PeopleTotals[0].PersonName);
        Assert.Equal("Zebra", result.PeopleTotals[1].PersonName);
    }
}
