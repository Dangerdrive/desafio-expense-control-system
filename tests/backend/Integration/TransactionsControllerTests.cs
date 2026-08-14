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
    public async Task Get_WithExistingTransaction_ShouldReturn200()
    {
        // Arrange — cria pessoa + transação e captura o ID
        var personId = await CreatePersonAsync("Busca Tx", 30);
        var createResponse = await _client.PostAsJsonAsync("/api/transactions", new
        {
            description = "Busca", amount = 100, type = "receita", personId
        });
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponseDto>();

        // Act
        var response = await _client.GetAsync($"/api/transactions/{created!.Id}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var transaction = await response.Content.ReadFromJsonAsync<TransactionResponseDto>();
        Assert.Equal("Busca", transaction!.Description);
        Assert.Equal("Busca Tx", transaction.PersonName);
    }

    [Fact]
    public async Task Get_WithNonExistingTransaction_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync("/api/transactions/99999");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
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
    // REGRESSÃO: personName populado corretamente
    // ============================================================

    [Fact]
    public async Task Post_ShouldReturnPersonName()
    {
        // Arrange
        var personId = await CreatePersonAsync("João Teste", 30);
        var dto = new { description = "Salário", amount = 5000, type = "receita", personId };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        var transaction = await response.Content.ReadFromJsonAsync<TransactionResponseDto>();
        Assert.Equal("João Teste", transaction!.PersonName);
    }

    [Fact]
    public async Task Get_ShouldPopulatePersonName()
    {
        // Arrange — descrição única para isolar a transação deste teste
        var personId = await CreatePersonAsync("Maria Silva", 30);
        var description = $"Tx_{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/transactions", new
        {
            description,
            amount = 150,
            type = "receita",
            personId
        });

        // Act
        var response = await _client.GetAsync("/api/transactions");
        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionResponseDto>>();

        // Assert — a transação deve exibir o nome da pessoa (não "Desconhecida")
        var transaction = Assert.Single(transactions!.Where(t => t.Description == description));
        Assert.Equal("Maria Silva", transaction.PersonName);
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
        // O TransactionTypeJsonConverter produz uma mensagem clara em PT-BR
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(body);
        Assert.Contains("O tipo deve ser 'receita' ou 'despesa'.", body!["message"]);
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
