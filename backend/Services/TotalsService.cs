using Backend.Data;
using Backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Serviço responsável pela consulta de totais (receitas, despesas e saldo).
/// Calcula os totais por pessoa e o total geral consolidado.
/// </summary>
public class TotalsService
{
    private readonly AppDbContext _context;

    public TotalsService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Calcula os totais financeiros de todas as pessoas cadastradas.
    /// 
    /// Para cada pessoa, calcula:
    /// - Total de receitas (soma das transações do tipo "receita")
    /// - Total de despesas (soma das transações do tipo "despesa")
    /// - Saldo líquido (receitas - despesas)
    /// 
    /// Ao final, consolida o total geral de todas as pessoas.
    /// 
    /// Pessoas sem transações também são exibidas (com valores zerados).
    /// </summary>
    /// <returns>DTO com os totais por pessoa e o total geral.</returns>
    public async Task<TotalsResponseDto> GetTotalsAsync()
    {
        // Busca todas as pessoas com suas transações
        var people = await _context.People
            .Include(p => p.Transactions)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var peopleTotals = new List<PersonTotalsDto>();

        foreach (var person in people)
        {
            // Calcula receitas e despesas para cada pessoa
            var totalIncome = person.Transactions
                .Where(t => t.Type == "receita")
                .Sum(t => t.Amount);

            var totalExpense = person.Transactions
                .Where(t => t.Type == "despesa")
                .Sum(t => t.Amount);

            peopleTotals.Add(new PersonTotalsDto
            {
                PersonId = person.Id,
                PersonName = person.Name,
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                Balance = totalIncome - totalExpense
            });
        }

        // Consolida o total geral
        var grandTotalIncome = peopleTotals.Sum(p => p.TotalIncome);
        var grandTotalExpense = peopleTotals.Sum(p => p.TotalExpense);

        return new TotalsResponseDto
        {
            PeopleTotals = peopleTotals,
            GrandTotalIncome = grandTotalIncome,
            GrandTotalExpense = grandTotalExpense,
            GrandBalance = grandTotalIncome - grandTotalExpense
        };
    }
}
