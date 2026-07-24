using System.Net.Http.Json;
using Backend.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Backend.Tests.Integration;

/// <summary>
/// Testes de integração do TransactionsController.
/// Valida os endpoints HTTP e a regra de negócio (menor = só despesa) na camada de API.
/// </summary>
public class TransactionsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TransactionsControllerTests(TestWebApplicationFactory factory)
    {
        factory.EnsureDatabaseCreated();
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Helper: cria uma pessoa e retorna o ID.
    /// </summary>
    private async Task<int> CreatePersonAsync(string name, int age)
    {
        var response = await _client.PostAsJsonAsync("/api/people", new { name, age });
        var person = await response.Content.ReadFromJsonAsync<PersonResponseDto>();
        return person!.Id;
    }

    [Fact]
    public async Task Post_AdultWithIncome_ShouldReturn201()
    {
        // Arrange
        var personId = await CreatePersonAsync("Adulto", 30);
        var dto = new { description = "Salário", amount = 5000, type = "receita", personId };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_MinorWithIncome_ShouldReturn400()
    {
        // Arrange — menor de idade tentando cadastrar receita
        var personId = await CreatePersonAsync("Menor", 15);
        var dto = new { description = "Mesada", amount = 100, type = "receita", personId };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        // Assert — deve retornar 400 Bad Request
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(error);
        Assert.Contains("Menores de 18 anos", error!["message"]);
    }

    [Fact]
    public async Task Post_MinorWithExpense_ShouldReturn201()
    {
        // Arrange — menor cadastrando despesa (permitido)
        var personId = await CreatePersonAsync("Menor", 15);
        var dto = new { description = "Lanche", amount = 25.50, type = "despesa", personId };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithNonExistingPerson_ShouldReturn400()
    {
        // Arrange
        var dto = new { description = "Teste", amount = 100, type = "despesa", personId = 99999 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_ShouldReturn200()
    {
        // Arrange — cria transação para garantir que há dados
        var personId = await CreatePersonAsync("Teste", 30);
        await _client.PostAsJsonAsync("/api/transactions", new
        {
            description = "T1", amount = 100, type = "receita", personId
        });

        // Act
        var response = await _client.GetAsync("/api/transactions");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionResponseDto>>();
        Assert.NotNull(transactions);
        Assert.NotEmpty(transactions!);
    }

    [Fact]
    public async Task Post_AdultWithExpense_ShouldReturn201()
    {
        // Arrange
        var personId = await CreatePersonAsync("Adulto", 25);
        var dto = new { description = "Conta de luz", amount = 200, type = "despesa", personId };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    }

    // ============================================================
    // VALIDAÇÃO DE ENTRADA (model validation)
    // ============================================================

    [Fact]
    public async Task Post_WithZeroAmount_ShouldReturn400()
    {
        // Arrange
        var personId = await CreatePersonAsync("Adulto", 30);
        var dto = new { description = "Teste", amount = 0, type = "despesa", personId };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithInvalidType_ShouldReturn400()
    {
        // Arrange
        var personId = await CreatePersonAsync("Adulto", 30);
        var dto = new { description = "Teste", amount = 100, type = "investimento", personId };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithEmptyDescription_ShouldReturn400()
    {
        // Arrange
        var personId = await CreatePersonAsync("Adulto", 30);
        var dto = new { description = "", amount = 100, type = "despesa", personId };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithDescriptionTooLong_ShouldReturn400()
    {
        // Arrange — descrição com 201 caracteres (max = 200)
        var personId = await CreatePersonAsync("Adulto", 30);
        var dto = new { description = new string('A', 201), amount = 100, type = "despesa", personId };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }
}
