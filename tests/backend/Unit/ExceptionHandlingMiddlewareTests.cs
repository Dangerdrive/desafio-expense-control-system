using Backend.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task InvokeAsync_WhenNextThrowsArgumentException_ShouldReturn400WithRuleMessage()
    {
        // Arrange — erro de regra de negócio lançado fora de um try/catch do controller
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ArgumentException("A pessoa informada não existe no cadastro."),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert — 400 com a mensagem da regra (não um 500 genérico)
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        Assert.Contains("A pessoa informada não existe no cadastro.", body);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrowsDbUpdateException_ShouldReturn409()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new DbUpdateException("conflito"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        Assert.Contains("Não foi possível salvar os dados", body);
        Assert.DoesNotContain("conflito", body);
    }

    [Fact]
    public async Task InvokeAsync_WhenResponseAlreadyStarted_ShouldRethrowInsteadOfCorruptingBody()
    {
        // Arrange — a resposta já foi enviada quando a exceção acontece
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("boom"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        // Act + Assert — propaga para o host abortar a conexão
        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
    }

    [Fact]
    public async Task InvokeAsync_WhenClientAborts_ShouldRethrowCancellation()
    {
        // Arrange — cliente cancela a requisição
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var context = new DefaultHttpContext { RequestAborted = cts.Token };
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new OperationCanceledException(cts.Token),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        // Act + Assert — cancelamento não é convertido em 500
        await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.InvokeAsync(context));
        Assert.NotEqual(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    /// <summary>
    /// Feature de resposta que se comporta como já enviada (HasStarted = true).
    /// </summary>
    private sealed class StartedResponseFeature : HttpResponseFeature
    {
        public override bool HasStarted => true;
    }
}
