namespace Backend.Middleware;

/// <summary>
/// Adiciona cabeçalhos de segurança a todas as respostas da API.
///
/// - X-Content-Type-Options: impede MIME sniffing do navegador.
/// - X-Frame-Options / frame-ancestors: impede que respostas sejam embutidas
///   em iframes de terceiros (clickjacking).
/// - Referrer-Policy: não envia a URL da API para outros domínios.
/// - Content-Security-Policy: a API só devolve JSON, então nada pode ser
///   carregado ou executado a partir das respostas.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        return _next(context);
    }
}
