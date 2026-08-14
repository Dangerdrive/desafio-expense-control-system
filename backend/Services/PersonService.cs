using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Serviço responsável pela lógica de negócio de Pessoas.
/// 
/// Por que injetamos IRepository&lt;Person&gt; em vez do AppDbContext diretamente?
/// - Desacoplamento: o Service não sabe se os dados vêm de SQLite, PostgreSQL ou memória.
/// - Testabilidade: nos testes unitários, podemos passar um mock do IRepository
///   sem precisar configurar um banco de dados real.
/// - Princípio da Inversão de Dependência (SOLID): dependemos de abstrações, não de concretos.
/// </summary>
public class PersonService
{
    private readonly IRepository<Person> _repository;

    public PersonService(IRepository<Person> repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Cria uma nova pessoa no sistema.
    /// </summary>
    /// <param name="dto">Dados da pessoa a ser criada.</param>
    /// <returns>A pessoa criada como DTO de resposta.</returns>
    public async Task<PersonResponseDto> CreateAsync(CreatePersonDto dto)
    {
        var person = new Person
        {
            Name = dto.Name,
            Age = dto.Age
        };

        await _repository.AddAsync(person);
        await _repository.SaveChangesAsync();

        return MapToResponse(person);
    }

    /// <summary>
    /// Lista todas as pessoas cadastradas no sistema.
    /// </summary>
    /// <returns>Lista de pessoas, ordenadas por nome.</returns>
    public async Task<List<PersonResponseDto>> GetAllAsync()
    {
        var people = await _repository.GetAllAsync();

        // Ordenação e projeção são feitas em memória após obter os dados.
        // Para conjuntos muito grandes, seria melhor fazer no banco com IQueryable,
        // mas para este escopo, listar tudo e ordenar em memória é perfeitamente aceitável.
        return people
            .OrderBy(p => p.Name)
            .Select(MapToResponse)
            .ToList();
    }

    /// <summary>
    /// Remove uma pessoa e, por cascata (configurada no AppDbContext),
    /// todas as transações associadas a ela.
    /// </summary>
    /// <param name="id">Identificador da pessoa a ser removida.</param>
    /// <returns>True se a pessoa foi encontrada e removida; False caso contrário.</returns>
    public async Task<bool> DeleteAsync(int id)
    {
        var person = await _repository.GetByIdAsync(id);
        if (person == null)
            return false;

        // A exclusão em cascata é gerenciada pelo banco de dados
        // (DeleteBehavior.Cascade). Não precisamos deletar as transações manualmente.
        _repository.Delete(person);
        await _repository.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Busca nome e idade de uma pessoa pelo ID em uma única consulta.
    /// Utilizado pelo TransactionService para validar a regra de menor de idade
    /// e preencher o nome da pessoa na resposta — evita uma segunda consulta.
    /// Retorna null se a pessoa não existir.
    /// </summary>
    public async Task<(string Name, int Age)?> GetInfoAsync(int id)
    {
        var person = await _repository.GetByIdAsync(id);
        return person == null ? null : (person.Name, person.Age);
    }

    /// <summary>
    /// Busca uma pessoa pelo ID e a devolve como DTO de resposta.
    /// Retorna null se a pessoa não existir.
    /// </summary>
    public async Task<PersonResponseDto?> GetByIdAsync(int id)
    {
        var person = await _repository.GetByIdAsync(id);
        return person == null ? null : MapToResponse(person);
    }

    /// <summary>
    /// Converte uma entidade Person para DTO de resposta.
    /// Método privado e estático: não depende de estado da instância,
    /// então pode ser estático (pequena otimização).
    /// </summary>
    private static PersonResponseDto MapToResponse(Person p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Age = p.Age
    };
}
