using Microsoft.EntityFrameworkCore;

namespace Backend.Middleware;

/// <summary>
/// Middleware global de tratamento de exceções.
///
/// Captura qualquer exceção não tratada lançada por middlewares posteriores
/// (MVC, CORS, etc.) e devolve uma resposta padronizada no formato
/// { "message": "..." }, que é o mesmo contrato usado pelos erros de negócio
/// e de validação — assim o frontend lê sempre a mesma propriedade.
///
/// Mapeamento:
/// - <see cref="ArgumentException"/>       → 400 com a mensagem da regra de negócio;
/// - <see cref="DbUpdateException"/>       → 409 (conflito ao persistir);
/// - qualquer outra exceção                → 500 com mensagem genérica.
///
/// Este middleware deve ser registrado como o PRIMEIRO do pipeline HTTP.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private const string GenericMessage = "Ocorreu um erro inesperado no servidor.";
    private const string ConflictMessage = "Não foi possível salvar os dados. Verifique se o registro ainda existe.";

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // O cliente desistiu da requisição: não é um erro do servidor e não há
            // ninguém para receber a resposta. Registramos e propagamos o cancelamento
            // para que o host encerre a requisição normalmente.
            _logger.LogInformation("Requisição {Method} {Path} cancelada pelo cliente.",
                context.Request.Method, context.Request.Path);
            throw;
        }
        catch (Exception ex)
        {
            // Registra o erro completo no log para auditoria/depuração.
            _logger.LogError(ex, "Erro não tratado em {Method} {Path}.",
                context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
            {
                // A resposta já começou a ser enviada: escrever agora produziria um
                // corpo corrompido e mascararia a falha. Propagamos para o host, que
                // aborta a conexão — o cliente percebe a resposta incompleta.
                _logger.LogWarning("Resposta já iniciada; não é possível devolver o corpo de erro padronizado.");
                throw;
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Escreve a resposta de erro padronizada.
    /// Só a mensagem de erros de negócio (<see cref="ArgumentException"/>) é
    /// repassada ao cliente; demais exceções recebem uma mensagem genérica
    /// para não vazar detalhes internos.
    /// </summary>
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            ArgumentException argEx => (StatusCodes.Status400BadRequest, argEx.Message),
            DbUpdateException => (StatusCodes.Status409Conflict, ConflictMessage),
            _ => (StatusCodes.Status500InternalServerError, GenericMessage)
        };

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        await context.Response.WriteAsJsonAsync(new { message });
    }
}
