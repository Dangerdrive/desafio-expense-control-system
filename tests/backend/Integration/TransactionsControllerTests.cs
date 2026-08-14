using System.Net.Http.Json;
using Backend.DTOs;
using Backend.Models;
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

    /// <summary>
    /// Helper: cria uma transação (despesa/receita) e retorna a resposta criada.
    /// </summary>
    private async Task<TransactionResponseDto> CreateTransactionAsync(int personId, string description, decimal amount, string type)
    {
        var response = await _client.PostAsJsonAsync("/api/transactions", new
        {
            description, amount, date = "2026-01-15", type, personId
        });
        return (await response.Content.ReadFromJsonAsync<TransactionResponseDto>())!;
    }

    [Fact]
    public async Task Post_AdultWithIncome_ShouldReturn201()
    {
        // Arrange
        var personId = await CreatePersonAsync("Adulto", 30);
        var dto = new { description = "Salário", amount = 5000, date = "2026-01-15", type = "receita", personId };

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
        var dto = new { description = "Mesada", amount = 100, date = "2026-01-15", type = "receita", personId };

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
        var dto = new { description = "Lanche", amount = 25.50, date = "2026-01-15", type = "despesa", personId };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithNonExistingPerson_ShouldReturn400()
    {
        // Arrange
        var dto = new { description = "Teste", amount = 100, date = "2026-01-15", type = "despesa", personId = 99999 };

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
            description = "T1", amount = 100, date = "2026-01-15", type = "receita", personId
        });

        // Act
        var response = await _client.GetAsync("/api/transactions");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var transactions = await response.Content.ReadFromJsonAsync<PagedResult<TransactionResponseDto>>();
        Assert.NotNull(transactions);
        Assert.NotEmpty(transactions!.Items);
    }

    [Fact]
    public async Task Get_WithExistingTransaction_ShouldReturn200()
    {
        // Arrange — cria pessoa + transação e captura o ID
        var personId = await CreatePersonAsync("Busca Tx", 30);
        var createResponse = await _client.PostAsJsonAsync("/api/transactions", new
        {
            description = "Busca", amount = 100, date = "2026-01-15", type = "receita", personId
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
        var dto = new { description = "Conta de luz", amount = 200, date = "2026-01-15", type = "despesa", personId };

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
        var dto = new { description = "Salário", amount = 5000, date = "2026-01-15", type = "receita", personId };

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
            date = "2026-01-15",
            type = "receita",
            personId
        });

        // Act
        var response = await _client.GetAsync("/api/transactions?pageSize=100");
        var transactions = await response.Content.ReadFromJsonAsync<PagedResult<TransactionResponseDto>>();

        // Assert — a transação deve exibir o nome da pessoa (não "Desconhecida")
        var transaction = Assert.Single(transactions!.Items.Where(t => t.Description == description));
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
        var dto = new { description = "Teste", amount = 0, date = "2026-01-15", type = "despesa", personId };

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
        var dto = new { description = "Teste", amount = 100, date = "2026-01-15", type = "investimento", personId };

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
        var dto = new { description = "", amount = 100, date = "2026-01-15", type = "despesa", personId };

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
        var dto = new { description = new string('A', 201), amount = 100, date = "2026-01-15", type = "despesa", personId };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ============================================================
    // DATA — filtro por período + ordenação + validação de campo
    // ============================================================

    [Fact]
    public async Task Post_WithMissingDate_ShouldReturn400()
    {
        // Arrange — payload SEM o campo date (agora obrigatório)
        var personId = await CreatePersonAsync("Adulto", 30);
        var dto = new { description = "Teste", amount = 100, type = "despesa", personId };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", dto);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithDateFilterAndSort_ShouldFilterAndSort()
    {
        // Arrange — três transações em datas diferentes
        var personId = await CreatePersonAsync("Adulto", 30);
        await _client.PostAsJsonAsync("/api/transactions", new { description = "Jan", amount = 100, date = "2026-01-10", type = "despesa", personId });
        await _client.PostAsJsonAsync("/api/transactions", new { description = "Jun", amount = 200, date = "2026-06-15", type = "despesa", personId });
        await _client.PostAsJsonAsync("/api/transactions", new { description = "Dez", amount = 300, date = "2026-12-20", type = "despesa", personId });

        // Act — filtro de março a novembro (inclusivo) + ordenação crescente
        var response = await _client.GetAsync("/api/transactions?pageSize=100&from=2026-03-01&to=2026-11-30&sort=date_asc");
        var transactions = await response.Content.ReadFromJsonAsync<PagedResult<TransactionResponseDto>>();

        // Assert — apenas a transação de junho, com a data correta
        var tx = Assert.Single(transactions!.Items);
        Assert.Equal("Jun", tx.Description);
        Assert.Equal(new DateOnly(2026, 6, 15), tx.Date);
    }

    [Fact]
    public async Task Get_DefaultOrder_ShouldBeMostRecentDateFirst()
    {
        // Arrange — descrições únicas para isolar das demais transações da classe
        // (a classe compartilha o mesmo banco InMemory via IClassFixture)
        var personId = await CreatePersonAsync("Adulto", 30);
        var older = $"Older_{Guid.NewGuid():N}";
        var recent = $"Recent_{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/transactions", new { description = older, amount = 100, date = "2026-01-10", type = "despesa", personId });
        await _client.PostAsJsonAsync("/api/transactions", new { description = recent, amount = 300, date = "2026-12-20", type = "despesa", personId });

        // Act — sem parâmetros (padrão: mais recente primeiro); pageSize alto
        // para que todos os itens do banco compartilhado caibam na página 1
        var response = await _client.GetAsync("/api/transactions?pageSize=100");
        var transactions = await response.Content.ReadFromJsonAsync<PagedResult<TransactionResponseDto>>();

        // Assert — a mais recente (dezembro) deve vir antes da mais antiga (janeiro)
        var recentIndex = transactions!.Items.FindIndex(t => t.Description == recent);
        var olderIndex = transactions.Items.FindIndex(t => t.Description == older);
        Assert.True(recentIndex >= 0, "A transação mais recente deve existir na resposta.");
        Assert.True(olderIndex >= 0, "A transação mais antiga deve existir na resposta.");
        Assert.True(recentIndex < olderIndex, "A transação mais recente deve vir antes da mais antiga.");
    }

    // ============================================================
    // ATUALIZAÇÃO (PUT) e EXCLUSÃO (DELETE)
    // ============================================================

    [Fact]
    public async Task Put_ShouldReturnUpdatedTransaction()
    {
        // Arrange
        var personId = await CreatePersonAsync("Adulto", 30);
        var created = await CreateTransactionAsync(personId, "Antes", 100, "despesa");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/transactions/{created.Id}", new
        {
            description = "Depois", amount = 250, date = "2026-05-20", type = "receita", personId
        });

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<TransactionResponseDto>();
        Assert.Equal("Depois", updated!.Description);
        Assert.Equal(250m, updated.Amount);
        Assert.Equal(new DateOnly(2026, 5, 20), updated.Date);
        Assert.Equal(TransactionType.Receita, updated.Type);
    }

    [Fact]
    public async Task Put_WithNonExistingId_ShouldReturn404()
    {
        // Arrange
        var personId = await CreatePersonAsync("Adulto", 30);

        // Act
        var response = await _client.PutAsJsonAsync("/api/transactions/99999", new
        {
            description = "X", amount = 100, date = "2026-01-15", type = "despesa", personId
        });

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_MinorWithIncome_ShouldReturn400()
    {
        // Arrange — transação de despesa de um menor
        var personId = await CreatePersonAsync("Menor", 15);
        var created = await CreateTransactionAsync(personId, "Lanche", 10, "despesa");

        // Act — tenta mudar para receita
        var response = await _client.PutAsJsonAsync($"/api/transactions/{created.Id}", new
        {
            description = "Mesada", amount = 100, date = "2026-01-15", type = "receita", personId
        });

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturn204()
    {
        // Arrange
        var personId = await CreatePersonAsync("Adulto", 30);
        var created = await CreateTransactionAsync(personId, "Remover", 100, "despesa");

        // Act
        var response = await _client.DeleteAsync($"/api/transactions/{created.Id}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        var get = await _client.GetAsync($"/api/transactions/{created.Id}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Delete_WithNonExistingId_ShouldReturn404()
    {
        // Act
        var response = await _client.DeleteAsync("/api/transactions/99999");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    // ============================================================
    // PAGINAÇÃO
    // ============================================================

    [Fact]
    public async Task Get_WithPagination_ShouldReturnPageMetadata()
    {
        // Arrange — descrições únicas para isolar das demais transações da classe
        var personId = await CreatePersonAsync("Adulto", 30);
        for (var i = 1; i <= 3; i++)
        {
            await CreateTransactionAsync(personId, $"Pag_{i}_{Guid.NewGuid():N}", i * 100, "despesa");
        }

        // Act — página 2, 2 itens por página
        var response = await _client.GetAsync("/api/transactions?page=2&pageSize=2");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TransactionResponseDto>>();

        // Assert — metadados da página (total de itens depende do banco compartilhado,
        // então validamos apenas a estrutura e a quantidade da página)
        Assert.Equal(2, result!.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.Items.Count);
        Assert.True(result.TotalItems >= 3);
        Assert.True(result.TotalPages >= 2);
    }
}
