using Backend.Data;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// Configuração dos serviços (injeção de dependência)
// ============================================

// Registra os controllers (API REST)
builder.Services.AddControllers();

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

// Configura CORS para permitir requisições do frontend React
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Vite dev server
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ============================================
// Configuração do pipeline HTTP
// ============================================

// Garante que o banco de dados e as tabelas sejam criados automaticamente
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

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
