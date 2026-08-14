using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller responsável pelo CRUD de Pessoas.
/// Endpoints: POST (criar), GET (listar), DELETE (remover).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PeopleController : ControllerBase
{
    private readonly PersonService _service;

    public PeopleController(PersonService service)
    {
        _service = service;
    }

    /// <summary>
    /// Cria uma nova pessoa.
    /// </summary>
    /// <param name="dto">Dados da pessoa (nome e idade).</param>
    /// <returns>201 Created com os dados da pessoa criada.</returns>
    [HttpPost]
    public async Task<ActionResult<PersonResponseDto>> Create([FromBody] CreatePersonDto dto)
    {
        var person = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = person.Id }, person);
    }

    /// <summary>
    /// Lista pessoas cadastradas com paginação, ordenadas por nome.
    /// </summary>
    /// <example>GET /api/people?page=1&amp;pageSize=10</example>
    [HttpGet]
    public async Task<ActionResult<PagedResult<PersonResponseDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var people = await _service.GetAllAsync(page, pageSize);
        return Ok(people);
    }

    /// <summary>
    /// Busca uma pessoa pelo ID.
    /// </summary>
    /// <param name="id">Identificador da pessoa.</param>
    /// <returns>200 com a pessoa; 404 se não encontrada.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<PersonResponseDto>> GetById(int id)
    {
        var person = await _service.GetByIdAsync(id);
        if (person == null)
            return NotFound(new { message = "Pessoa não encontrada." });

        return Ok(person);
    }

    /// <summary>
    /// Remove uma pessoa pelo ID.
    /// Todas as transações associadas são removidas em cascata.
    /// </summary>
    /// <param name="id">Identificador da pessoa.</param>
    /// <returns>204 No Content se removida; 404 se não encontrada.</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { message = "Pessoa não encontrada." });

        return NoContent();
    }
}
