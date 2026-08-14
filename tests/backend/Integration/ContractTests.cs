using System.Net.Http.Json;
using System.Text.Json;
using Backend.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Backend.Tests.Integration;

/// <summary>
/// Testes de CONTRATO da API.
///
/// Valida que as respostas JSON reais dos endpoints batem EXATAMENTE com o
/// contrato definido em contracts/api-contract.json (fonte única de verdade,
/// compartilhada com o teste de contrato do frontend).
///
/// Se alguém renomear/remover um campo em um DTO, o conjunto de propriedades
/// da resposta diverge do contrato e estes testes falham — capturando o drift
/// entre frontend e backend cedo, antes de chegar ao usuário.
/// </summary>
public class ContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ContractTests(TestWebApplicationFactory factory)
    {
        factory.EnsureDatabaseCreated();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PeopleResponse_ShouldMatchContract()
    {
        // Act — resposta real do POST /api/people
        var response = await _client.PostAsJsonAsync("/api/people", new { name = "Ana", age = 30 });
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        using var contract = LoadContract();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        AssertObjectMatchesContract(doc.RootElement, contract.RootElement.GetProperty("person"), "person");
    }

    [Fact]
    public async Task TransactionResponse_ShouldMatchContract()
    {
        // Arrange — pessoa adulta para a receita ser aceita
        var personId = await CreatePersonAsync();

        // Act — resposta real do POST /api/transactions
        var response = await _client.PostAsJsonAsync("/api/transactions", new
        {
            description = "Salário",
            amount = 5000,
            date = "2026-01-15",
            type = "receita",
            personId
        });
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        using var contract = LoadContract();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        AssertObjectMatchesContract(doc.RootElement, contract.RootElement.GetProperty("transaction"), "transaction");
    }

    [Fact]
    public async Task PeopleListResponse_ShouldMatchContract()
    {
        // Arrange — cria uma pessoa para a página não ser vazia
        await _client.PostAsJsonAsync("/api/people", new { name = "Ana", age = 30 });

        // Act — resposta real do GET /api/people (envelope paginado)
        var response = await _client.GetAsync("/api/people");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        using var contract = LoadContract();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Top-level: items, page, pageSize, totalItems, totalPages, hasNext, hasPrevious
        AssertObjectMatchesContract(doc.RootElement, contract.RootElement.GetProperty("personPage"), "personPage");

        // Cada item dentro de items: id, name, age
        var expectedItem = contract.RootElement.GetProperty("personPage").GetProperty("items")[0];
        var actualItem = doc.RootElement.GetProperty("items")[0];
        AssertObjectMatchesContract(actualItem, expectedItem, "personPage.items[]");
    }

    [Fact]
    public async Task TransactionsListResponse_ShouldMatchContract()
    {
        // Arrange — pessoa + transação para a página não ser vazia
        var personId = await CreatePersonAsync();
        await _client.PostAsJsonAsync("/api/transactions", new
        {
            description = "Salário",
            amount = 5000,
            date = "2026-01-15",
            type = "receita",
            personId
        });

        // Act — resposta real do GET /api/transactions (envelope paginado)
        var response = await _client.GetAsync("/api/transactions");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        using var contract = LoadContract();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Top-level: items, page, pageSize, totalItems, totalPages, hasNext, hasPrevious
        AssertObjectMatchesContract(doc.RootElement, contract.RootElement.GetProperty("transactionPage"), "transactionPage");

        // Cada item dentro de items: id, description, amount, date, type, personId, personName
        var expectedItem = contract.RootElement.GetProperty("transactionPage").GetProperty("items")[0];
        var actualItem = doc.RootElement.GetProperty("items")[0];
        AssertObjectMatchesContract(actualItem, expectedItem, "transactionPage.items[]");
    }

    [Fact]
    public async Task TotalsResponse_ShouldMatchContract()
    {
        // Arrange — pessoa + receita para os totais terem dados
        var personId = await CreatePersonAsync();
        await _client.PostAsJsonAsync("/api/transactions", new
        {
            description = "Salário",
            amount = 5000,
            date = "2026-01-15",
            type = "receita",
            personId
        });

        // Act — resposta real do GET /api/totals
        var response = await _client.GetAsync("/api/totals");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        using var contract = LoadContract();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Top-level: peopleTotals, grandTotalIncome, grandTotalExpense, grandBalance
        AssertObjectMatchesContract(doc.RootElement, contract.RootElement.GetProperty("totals"), "totals");

        // Cada item de peopleTotals: personId, personName, totalIncome, totalExpense, balance
        var expectedItem = contract.RootElement.GetProperty("totals").GetProperty("peopleTotals")[0];
        var actualItem = doc.RootElement.GetProperty("peopleTotals")[0];
        AssertObjectMatchesContract(actualItem, expectedItem, "totals.peopleTotals[]");
    }

    [Fact]
    public async Task ErrorResponse_ShouldMatchContract()
    {
        // Act — payload inválido gera 400 com o contrato de erro unificado
        var response = await _client.PostAsJsonAsync("/api/people", new { name = "", age = -1 });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // O corpo deve ter APENAS a propriedade "message" (contrato de erro)
        var names = doc.RootElement.EnumerateObject().Select(p => p.Name).ToArray();
        Assert.Equal(new[] { "message" }, names);
    }

    // ============================================================
    // Helpers
    // ============================================================

    private async Task<int> CreatePersonAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/people", new { name = "Ana", age = 30 });
        var person = await response.Content.ReadFromJsonAsync<PersonResponseDto>();
        return person!.Id;
    }

    /// <summary>Lê o contrato compartilhado copiado para o output dos testes.</summary>
    private static JsonDocument LoadContract()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "api-contract.json");
        return JsonDocument.Parse(File.ReadAllBytes(path));
    }

    /// <summary>
    /// Compara o conjunto de nomes de propriedades de um objeto JSON real com o
    /// contrato (independente de ordem). Se divergir — campo a mais ou a menos —
    /// a mensagem de erro lista os nomes esperados vs reais.
    /// </summary>
    private static void AssertObjectMatchesContract(JsonElement actual, JsonElement expected, string path)
    {
        Assert.Equal(JsonValueKind.Object, actual.ValueKind);
        Assert.Equal(JsonValueKind.Object, expected.ValueKind);

        var actualNames = actual.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray();
        var expectedNames = expected.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray();

        Assert.True(
            expectedNames.SequenceEqual(actualNames),
            $"Contrato divergente em '{path}'. Esperado: [{string.Join(", ", expectedNames)}]. Real: [{string.Join(", ", actualNames)}].");
    }
}
