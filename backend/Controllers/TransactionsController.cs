using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller responsável pelo cadastro de Transações.
/// Endpoints: POST (criar) e GET (listar).
/// Não há endpoints de edição ou exclusão, conforme especificação.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly TransactionService _service;

    public TransactionsController(TransactionService service)
    {
        _service = service;
    }

    /// <summary>
    /// Cria uma nova transação (receita ou despesa).
    /// 
    /// Regras de negócio aplicadas:
    /// - A pessoa informada deve existir.
    /// - Menores de 18 anos só podem cadastrar despesas.
    /// </summary>
    /// <param name="dto">Dados da transação.</param>
    /// <returns>201 Created ou 400 Bad Request com mensagem de erro.</returns>
    [HttpPost]
    public async Task<ActionResult<TransactionResponseDto>> Create([FromBody] CreateTransactionDto dto)
    {
        try
        {
            var transaction = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetAll), new { id = transaction.Id }, transaction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lista todas as transações cadastradas, ordenadas da mais recente para a mais antiga.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TransactionResponseDto>>> GetAll()
    {
        var transactions = await _service.GetAllAsync();
        return Ok(transactions);
    }
}
