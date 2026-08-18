namespace Backend.Tests.Integration;

/// <summary>
/// Testes dos cabeçalhos de segurança aplicados a todas as respostas da API
/// pelo SecurityHeadersMiddleware.
/// </summary>
public class SecurityHeadersTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityHeadersTests(TestWebApplicationFactory factory)
    {
        factory.EnsureDatabaseCreated();
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("X-Content-Type-Options", "nosniff")]
    [InlineData("X-Frame-Options", "DENY")]
    [InlineData("Referrer-Policy", "no-referrer")]
    [InlineData("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'")]
    public async Task Get_ShouldIncludeSecurityHeaders(string header, string expected)
    {
        var response = await _client.GetAsync("/api/people");

        Assert.True(response.Headers.TryGetValues(header, out var values), $"Cabeçalho ausente: {header}");
        Assert.Equal(expected, string.Join(", ", values!));
    }

    [Fact]
    public async Task NotFound_ShouldIncludeSecurityHeaders()
    {
        var response = await _client.GetAsync("/api/people/999999");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
    }
}
