namespace Backend.Middleware;

/// <summary>
/// Middleware global de tratamento de exceções.
///
/// Captura qualquer exceção não tratada lançada por middlewares posteriores
/// (MVC, CORS, etc.) e devolve uma resposta 500 padronizada no formato
/// { "message": "..." }, que é o mesmo contrato usado pelos erros de negócio
/// e de validação — assim o frontend lê sempre a mesma propriedade.
///
/// Este middleware deve ser registrado como o PRIMEIRO do pipeline HTTP.
/// </summary>
public class ExceptionHandlingMiddleware
{
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
        catch (Exception ex)
        {
            // Registra o erro completo no log para auditoria/depuração.
            _logger.LogError(ex, "Erro não tratado na API.");
            await HandleExceptionAsync(context);
        }
    }

    /// <summary>
    /// Escreve a resposta de erro padronizada.
    /// Não vaza detalhes internos da exceção para o cliente.
    /// </summary>
    private static async Task HandleExceptionAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new { message = "Ocorreu um erro inesperado no servidor." };
        await context.Response.WriteAsJsonAsync(payload);
    }
}
