using System.Net.Http.Json;
using Backend.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Backend.Tests.Integration;

/// <summary>
/// Testes de integração do PeopleController.
/// Testa os endpoints HTTP reais com WebApplicationFactory (API em memória).
/// </summary>
public class PeopleControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PeopleControllerTests(TestWebApplicationFactory factory)
    {
        factory.EnsureDatabaseCreated();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithValidData_ShouldReturn201()
    {
        // Arrange
        var dto = new { name = "João Silva", age = 30 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/people", dto);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        var person = await response.Content.ReadFromJsonAsync<PersonResponseDto>();
        Assert.NotNull(person);
        Assert.Equal("João Silva", person!.Name);
        Assert.True(person.Id > 0);
    }

    [Fact]
    public async Task Get_WithPeople_ShouldReturn200()
    {
        // Arrange — cria duas pessoas primeiro
        await _client.PostAsJsonAsync("/api/people", new { name = "Ana", age = 25 });
        await _client.PostAsJsonAsync("/api/people", new { name = "Bruno", age = 30 });

        // Act
        var response = await _client.GetAsync("/api/people");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var people = await response.Content.ReadFromJsonAsync<PagedResult<PersonResponseDto>>();
        Assert.NotNull(people);
        Assert.True(people!.Items.Count >= 2);
    }

    [Fact]
    public async Task Get_WithPagination_ShouldReturnPageMetadata()
    {
        // Arrange — cria 3 pessoas
        await _client.PostAsJsonAsync("/api/people", new { name = "C", age = 30 });
        await _client.PostAsJsonAsync("/api/people", new { name = "A", age = 30 });
        await _client.PostAsJsonAsync("/api/people", new { name = "B", age = 30 });

        // Act — página 2, 2 itens por página
        // (o banco é compartilhado entre os testes da classe via IClassFixture,
        // então validamos apenas a estrutura e limites, não valores absolutos)
        var response = await _client.GetAsync("/api/people?page=2&pageSize=2");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<PersonResponseDto>>();

        // Assert
        Assert.Equal(2, result!.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.Items.Count);
        Assert.True(result.TotalItems >= 3, "Deve haver pelo menos as 3 pessoas criadas.");
        Assert.True(result.TotalPages >= 2, "Deve haver pelo menos 2 páginas.");
        Assert.True(result.HasPrevious);
    }

    [Fact]
    public async Task Get_WithExistingPerson_ShouldReturn200()
    {
        // Arrange — cria uma pessoa e captura o ID
        var createResponse = await _client.PostAsJsonAsync("/api/people", new { name = "Busca", age = 35 });
        var created = await createResponse.Content.ReadFromJsonAsync<PersonResponseDto>();

        // Act
        var response = await _client.GetAsync($"/api/people/{created!.Id}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var person = await response.Content.ReadFromJsonAsync<PersonResponseDto>();
        Assert.Equal("Busca", person!.Name);
    }

    [Fact]
    public async Task Get_WithNonExistingPerson_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync("/api/people/99999");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]

    public async Task Delete_WithExistingPerson_ShouldReturn204()
    {
        // Arrange — cria uma pessoa para deletar
        var createResponse = await _client.PostAsJsonAsync("/api/people", new { name = "Deletar", age = 50 });
        var created = await createResponse.Content.ReadFromJsonAsync<PersonResponseDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/people/{created!.Id}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithNonExistingPerson_ShouldReturn404()
    {
        // Act
        var response = await _client.DeleteAsync("/api/people/99999");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldRemoveAssociatedTransactions()
    {
        // Arrange — cria pessoa com transações
        var personResponse = await _client.PostAsJsonAsync("/api/people", new { name = "Com Transações", age = 40 });
        var person = await personResponse.Content.ReadFromJsonAsync<PersonResponseDto>();

        await _client.PostAsJsonAsync("/api/transactions", new
        {
            description = "Salário", amount = 5000, date = "2026-01-15", type = "receita", personId = person!.Id
        });

        // Act — deleta a pessoa
        await _client.DeleteAsync($"/api/people/{person.Id}");

        // Assert — transações devem sumir (cascata)
        var txResponse = await _client.GetAsync("/api/transactions");
        var transactions = await txResponse.Content.ReadFromJsonAsync<PagedResult<TransactionResponseDto>>();
        Assert.Empty(transactions!.Items);
    }

    // ============================================================
    // VALIDAÇÃO DE ENTRADA (model validation)
    // ============================================================

    [Fact]
    public async Task Post_WithEmptyName_ShouldReturn400()
    {
        // Arrange
        var dto = new { name = "", age = 30 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/people", dto);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithNegativeAge_ShouldReturn400()
    {
        // Arrange
        var dto = new { name = "Teste", age = -1 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/people", dto);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithAgeAbove150_ShouldReturn400()
    {
        // Arrange
        var dto = new { name = "Teste", age = 151 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/people", dto);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithNameTooLong_ShouldReturn400()
    {
        // Arrange — nome com 101 caracteres (max = 100)
        var dto = new { name = new string('A', 101), age = 30 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/people", dto);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidationError_ShouldReturnUnifiedMessageShape()
    {
        // Arrange — nome vazio viola [Required]
        var dto = new { name = "", age = 30 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/people", dto);

        // Assert — corpo padronizado { message }, sem o formato antigo { errors }
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(error);
        Assert.True(error!.ContainsKey("message"));
        Assert.False(error.ContainsKey("errors"));
    }
}
