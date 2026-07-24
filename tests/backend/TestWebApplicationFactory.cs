using Backend.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Backend.Tests;

/// <summary>
/// Factory customizada para testes de integração.
/// 
/// Substitui o SQLite real por um banco InMemory, garantindo que:
/// 1. Testes não criem/escrevam no arquivo ExpenseControl.db real
/// 2. Todos os testes na mesma classe compartilham o mesmo banco (IClassFixture)
/// 3. O comportamento seja o mais próximo possível do SQLite real
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"IntegrationTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove todas as registrações do AppDbContext (DbContext, DbContextOptions, etc.)
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            // Registra o AppDbContext com banco InMemory (nome fixo por fixture)
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });
        });
    }

    /// <summary>
    /// Garante que o banco InMemory tenha as tabelas criadas.
    /// Deve ser chamado após o CreateClient().
    /// </summary>
    public void EnsureDatabaseCreated()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }
}
