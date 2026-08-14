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
            return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, transaction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lista transações com paginação e filtros opcionais:
    /// - page/pageSize: paginação (padrão 1/10).
    /// - from/to: período (inclusivo) pelo campo Date.
    /// - sort: "date_asc" (crescente) ou "date_desc" (padrão, mais recente primeiro).
    /// </summary>
    /// <example>GET /api/transactions?page=1&amp;pageSize=10&amp;from=2026-01-01&amp;to=2026-12-31&amp;sort=date_asc</example>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TransactionResponseDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] string? sort = null)
    {
        var transactions = await _service.GetAllAsync(page, pageSize, from, to, sort);
        return Ok(transactions);
    }

    /// <summary>
    /// Busca uma transação pelo ID.
    /// </summary>
    /// <param name="id">Identificador da transação.</param>
    /// <returns>200 com a transação; 404 se não encontrada.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionResponseDto>> GetById(int id)
    {
        var transaction = await _service.GetByIdAsync(id);
        if (transaction == null)
            return NotFound(new { message = "Transação não encontrada." });

        return Ok(transaction);
    }

    /// <summary>
    /// Atualiza uma transação existente (mesmas regras de negócio do POST).
    /// </summary>
    /// <param name="id">Identificador da transação.</param>
    /// <param name="dto">Novos dados da transação.</param>
    /// <returns>200 com a transação atualizada; 400/404 em caso de erro.</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<TransactionResponseDto>> Update(int id, [FromBody] CreateTransactionDto dto)
    {
        try
        {
            var transaction = await _service.UpdateAsync(id, dto);
            if (transaction == null)
                return NotFound(new { message = "Transação não encontrada." });

            return Ok(transaction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove uma transação pelo ID.
    /// </summary>
    /// <param name="id">Identificador da transação.</param>
    /// <returns>204 No Content; 404 se não encontrada.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { message = "Transação não encontrada." });

        return NoContent();
    }
}
