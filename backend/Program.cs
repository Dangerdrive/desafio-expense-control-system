using Backend.Data;
using Backend.Middleware;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// Configuração dos serviços (injeção de dependência)
// ============================================

// Registra os controllers (API REST)
// TransactionType é serializado como "receita"/"despesa" (e não como número)
// graças ao [JsonConverter] declarado diretamente no enum.
builder.Services.AddControllers();

// Padroniza o formato de erros de validação: { message }.
// O frontend lê apenas a propriedade "message"; sem isto, erros de
// validação do [ApiController] retornariam ValidationProblemDetails
// (com a propriedade "errors"), quebrando a consistência do contrato.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var messages = context.ModelState
            .Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct();

        var message = string.Join(" ", messages);
        if (string.IsNullOrWhiteSpace(message))
            message = "Dados inválidos.";

        return new BadRequestObjectResult(new { message });
    };
});

// Configura Swagger/OpenAPI para documentação interativa da API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configura o Entity Framework Core com SQLite (persistência de dados)
// O banco de dados será criado no arquivo ExpenseControl.db na raiz do projeto
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=ExpenseControl.db"));

// Registra o Repository Pattern (abstração sobre o DbContext).
// AddScoped: uma instância por requisição HTTP — o mesmo repositório
// é compartilhado entre serviços dentro da mesma requisição,
// garantindo consistência transacional.
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Registra os serviços da camada de negócio
builder.Services.AddScoped<PersonService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<TotalsService>();

// Configura CORS para permitir requisições do frontend React.
// As origens vêm de Cors:AllowedOrigins (appsettings / variáveis de ambiente),
// para que o deploy aponte para o domínio real sem recompilar. Curingas ("*")
// são rejeitados: liberar qualquer origem permitiria que qualquer site
// chamasse a API pelo navegador da vítima.
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5173"]; // Vite dev server

if (allowedOrigins.Any(origin => origin.Contains('*')))
    throw new InvalidOperationException("Cors:AllowedOrigins não aceita curingas; informe as origens explicitamente.");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ============================================
// Configuração do pipeline HTTP
// ============================================

// Aplica as migrations EF Core no banco (cria as tabelas na primeira execução
// e versiona o esquema via tabela __EFMigrationsHistory).
// Antes usávamos EnsureCreated(), que não evolui o esquema quando o modelo muda.
// O guard IsRelational() garante que isso não rode em provedores não-relacionais
// (ex: banco InMemory usado nos testes de integração).
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (dbContext.Database.IsRelational())
        dbContext.Database.Migrate();
}

// Tratamento global de exceções — deve ser o primeiro middleware do pipeline
// para capturar erros de qualquer middleware posterior (MVC, CORS, etc.).
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Cabeçalhos de segurança nas respostas da API. O Swagger UI é servido pela
// própria aplicação (HTML + JS) e seria bloqueado pela CSP, então fica de fora.
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/swagger"),
    branch => branch.UseMiddleware<SecurityHeadersMiddleware>());

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.MapControllers();

app.Run();

/// <summary>
/// Torna a classe Program acessível ao projeto de testes de integração
/// (necessário para WebApplicationFactory).
/// </summary>
public partial class Program { }
