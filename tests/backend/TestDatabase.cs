using Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests;

/// <summary>
/// Fixture que fornece um AppDbContext com banco de dados InMemory
/// para testes unitários. Cada teste recebe um contexto limpo e isolado.
/// 
/// Usamos InMemory em vez de mockar o DbContext porque:
/// 1. É mais realista — testamos queries reais do EF Core
/// 2. Menos código de mock para manter
/// 3. Comportamento mais próximo do SQLite real
/// </summary>
public static class TestDatabase
{
    /// <summary>
    /// Cria um novo AppDbContext usando banco InMemory.
    /// Cada chamada gera um banco isolado (nome único via Guid).
    /// </summary>
    public static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }
}
