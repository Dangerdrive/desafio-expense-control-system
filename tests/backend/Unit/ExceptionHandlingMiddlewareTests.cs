using Backend.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.Tests.Unit;

/// <summary>
/// Testes unitários do ExceptionHandlingMiddleware.
/// Valida que exceções não tratadas viram HTTP 500 com o corpo padronizado { message }.
/// </summary>
public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenNextThrows_ShouldReturn500WithUnifiedMessage()
    {
        // Arrange — próximo middleware lança uma exceção
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("boom"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert — 500 + corpo padronizado sem detalhes internos
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        Assert.Contains("\"message\"", body);
        Assert.Contains("Ocorreu um erro inesperado", body);
        Assert.DoesNotContain("boom", body); // não vaza a mensagem interna
    }

    [Fact]
    public async Task InvokeAsync_WhenNextSucceeds_ShouldNotInterfere()
    {
        // Arrange — próximo middleware responde normalmente
        var context = new DefaultHttpContext();
        var middleware = new ExceptionHandlingMiddleware(
            async ctx => { ctx.Response.StatusCode = StatusCodes.Status200OK; await Task.CompletedTask; },
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert — resposta do próximo middleware preservada
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }
}
