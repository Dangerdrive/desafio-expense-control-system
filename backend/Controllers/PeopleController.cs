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
        return CreatedAtAction(nameof(GetAll), new { id = person.Id }, person);
    }

    /// <summary>
    /// Lista todas as pessoas cadastradas, ordenadas por nome.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<PersonResponseDto>>> GetAll()
    {
        var people = await _service.GetAllAsync();
        return Ok(people);
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
