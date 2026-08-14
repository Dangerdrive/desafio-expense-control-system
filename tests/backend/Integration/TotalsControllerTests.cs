using System.Net.Http.Json;
using Backend.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Backend.Tests.Integration;

/// <summary>
/// Testes de integração do TotalsController.
/// Valida o endpoint de consulta de totais com dados reais.
/// </summary>
public class TotalsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TotalsControllerTests(TestWebApplicationFactory factory)
    {
        factory.EnsureDatabaseCreated();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ShouldReturn200WithValidStructure()
    {
        // Act — mesmo com dados de outros testes, a estrutura deve estar correta
        var response = await _client.GetAsync("/api/totals");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var totals = await response.Content.ReadFromJsonAsync<TotalsResponseDto>();
        Assert.NotNull(totals);
        Assert.NotNull(totals!.PeopleTotals);
        // Totais gerais devem ser consistentes: saldo = receitas - despesas
        Assert.Equal(totals.GrandTotalIncome - totals.GrandTotalExpense, totals.GrandBalance);
    }

    [Fact]
    public async Task Get_WithFullData_ShouldReturnCorrectTotals()
    {
        // Arrange — monta cenário completo via API
        // Cria pessoas
        var joaoResponse = await _client.PostAsJsonAsync("/api/people", new { name = "João", age = 30 });
        var mariaResponse = await _client.PostAsJsonAsync("/api/people", new { name = "Maria", age = 25 });
        var joao = await joaoResponse.Content.ReadFromJsonAsync<PersonResponseDto>();
        var maria = await mariaResponse.Content.ReadFromJsonAsync<PersonResponseDto>();

        // Transações do João: +5000 receita, -2000 despesa = +3000
        await _client.PostAsJsonAsync("/api/transactions", new { description = "Salário", amount = 5000, date = "2026-01-15", type = "receita", personId = joao!.Id });
        await _client.PostAsJsonAsync("/api/transactions", new { description = "Aluguel", amount = 2000, date = "2026-01-16", type = "despesa", personId = joao.Id });

        // Transações da Maria: +3000 receita, -1000 despesa = +2000
        await _client.PostAsJsonAsync("/api/transactions", new { description = "Salário", amount = 3000, date = "2026-01-15", type = "receita", personId = maria!.Id });
        await _client.PostAsJsonAsync("/api/transactions", new { description = "Conta", amount = 1000, date = "2026-01-16", type = "despesa", personId = maria.Id });

        // Act
        var response = await _client.GetAsync("/api/totals");
        var totals = await response.Content.ReadFromJsonAsync<TotalsResponseDto>();

        // Assert
        Assert.NotNull(totals);
        Assert.Equal(2, totals!.PeopleTotals.Count);

        // Totais do João
        var joaoTotals = totals.PeopleTotals.First(p => p.PersonId == joao.Id);
        Assert.Equal(5000, joaoTotals.TotalIncome);
        Assert.Equal(2000, joaoTotals.TotalExpense);
        Assert.Equal(3000, joaoTotals.Balance);

        // Totais da Maria
        var mariaTotals = totals.PeopleTotals.First(p => p.PersonId == maria.Id);
        Assert.Equal(3000, mariaTotals.TotalIncome);
        Assert.Equal(1000, mariaTotals.TotalExpense);
        Assert.Equal(2000, mariaTotals.Balance);

        // Totais gerais
        Assert.Equal(8000, totals.GrandTotalIncome);
        Assert.Equal(3000, totals.GrandTotalExpense);
        Assert.Equal(5000, totals.GrandBalance);
    }

    [Fact]
    public async Task Get_ResponseStructure_ShouldContainAllRequiredFields()
    {
        // Arrange — cria dados mínimos
        await _client.PostAsJsonAsync("/api/people", new { name = "Teste", age = 30 });

        // Act
        var response = await _client.GetAsync("/api/totals");
        var json = await response.Content.ReadAsStringAsync();

        // Assert — verifica que todos os campos esperados estão presentes no JSON
        Assert.Contains("peopleTotals", json);
        Assert.Contains("grandTotalIncome", json);
        Assert.Contains("grandTotalExpense", json);
        Assert.Contains("grandBalance", json);
    }
}
