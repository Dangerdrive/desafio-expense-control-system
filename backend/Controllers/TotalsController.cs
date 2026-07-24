using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller responsável pela consulta de totais (receitas, despesas e saldo).
/// Exibe o resumo financeiro por pessoa e o total geral consolidado.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TotalsController : ControllerBase
{
    private readonly TotalsService _service;

    public TotalsController(TotalsService service)
    {
        _service = service;
    }

    /// <summary>
    /// Consulta os totais de receitas, despesas e saldo de cada pessoa,
    /// além do total geral consolidado de todo o sistema.
    /// 
    /// Estrutura da resposta:
    /// {
    ///     "peopleTotals": [ { personId, personName, totalIncome, totalExpense, balance } ],
    ///     "grandTotalIncome": ...,
    ///     "grandTotalExpense": ...,
    ///     "grandBalance": ...
    /// }
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<TotalsResponseDto>> GetTotals()
    {
        var totals = await _service.GetTotalsAsync();
        return Ok(totals);
    }
}
