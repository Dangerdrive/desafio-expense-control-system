using Backend.Data;
using Backend.DTOs;
using Backend.Models;

namespace Backend.Services;

/// <summary>
/// Serviço responsável pela lógica de negócio de Transações.
/// Implementa a regra de negócio crítica: menores de 18 anos só podem ter despesas.
/// 
/// Por que TransactionService depende de PersonService e não do IRepository&lt;Person&gt;?
/// - PersonService encapsula a lógica de "obter idade". Se no futuro a idade
///   for calculada a partir da data de nascimento, o TransactionService não muda.
/// - Isso é o princípio "Tell, Don't Ask": dizemos ao PersonService o que
///   queremos (a idade), em vez de pedir a pessoa inteira e calcular nós mesmos.
/// </summary>
public class TransactionService
{
    private readonly IRepository<Transaction> _repository;
    private readonly PersonService _personService;

    public TransactionService(IRepository<Transaction> repository, PersonService personService)
    {
        _repository = repository;
        _personService = personService;
    }

    /// <summary>
    /// Cria uma nova transação, aplicando as seguintes regras de negócio:
    /// 1. A pessoa informada deve existir no cadastro.
    /// 2. Se a pessoa for menor de 18 anos, apenas DESPESAS são permitidas.
    ///    Esta validação acontece AQUI (camada de serviço), não no Controller,
    ///    porque é uma regra de negócio — ela deve valer independentemente
    ///    de quem chama (API REST, CLI, outro serviço, etc.).
    /// </summary>
    /// <param name="dto">Dados da transação a ser criada.</param>
    /// <returns>A transação criada como DTO.</returns>
    /// <exception cref="ArgumentException">
    /// Lançada se a pessoa não existe ou se um menor tenta criar receita.
    /// O Controller captura esta exceção e a converte em HTTP 400.
    /// </exception>
    public async Task<TransactionResponseDto> CreateAsync(CreateTransactionDto dto)
    {
        var personInfo = await GetValidatedPersonInfoAsync(dto);

        var transaction = new Transaction
        {
            Description = dto.Description,
            Amount = dto.Amount,
            Date = dto.Date!.Value,
            Type = dto.Type,
            PersonId = dto.PersonId
        };

        await _repository.AddAsync(transaction);
        await _repository.SaveChangesAsync();

        return MapToResponse(transaction, personInfo.Name);
    }

    /// <summary>
    /// Lista transações com paginação, incluindo o nome da pessoa associada.
    ///
    /// Suporta filtros opcionais por período (from/to) e ordenação por data
    /// (sort = "date_asc" | "date_desc"; padrão: mais recente primeiro).
    /// </summary>
    /// <param name="page">Número da página (1-based).</param>
    /// <param name="pageSize">Quantidade por página (1–100).</param>
    /// <param name="from">Data inicial do filtro (inclusiva), opcional.</param>
    /// <param name="to">Data final do filtro (inclusiva), opcional.</param>
    /// <param name="sort">Ordem: "date_asc" ou "date_desc" (padrão).</param>
    public async Task<PagedResult<TransactionResponseDto>> GetAllAsync(int page = 1, int pageSize = 10, DateOnly? from = null, DateOnly? to = null, string? sort = null)
    {
        // Inclui a navegação Person para popular o PersonName na resposta.
        var transactions = await _repository.GetAllAsync(t => t.Person);

        // Filtro por período (inclusivo)
        IEnumerable<Transaction> query = transactions;
        if (from.HasValue)
            query = query.Where(t => t.Date >= from.Value);
        if (to.HasValue)
            query = query.Where(t => t.Date <= to.Value);

        // Ordenação por data (padrão: mais recente primeiro)
        var ascending = string.Equals(sort, "date_asc", StringComparison.OrdinalIgnoreCase);
        var ordered = ascending
            ? query.OrderBy(t => t.Date).ThenBy(t => t.Id)
            : query.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id);
        var orderedList = ordered.ToList();

        return PagedResult<TransactionResponseDto>.FromSource(
            orderedList,
            page,
            pageSize,
            t => MapToResponse(t, t.Person?.Name));
    }

    /// <summary>
    /// Busca uma transação pelo ID, preenchendo o nome da pessoa associada.
    /// Retorna null se a transação não existir.
    /// </summary>
    public async Task<TransactionResponseDto?> GetByIdAsync(int id)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction == null)
            return null;

        var personInfo = await _personService.GetInfoAsync(transaction.PersonId);

        return MapToResponse(transaction, personInfo?.Name);
    }

    /// <summary>
    /// Atualiza uma transação existente, aplicando as MESMAS regras de negócio
    /// do CreateAsync (pessoa existe; menor de 18 só pode ter despesas).
    /// </summary>
    /// <param name="id">Identificador da transação a atualizar.</param>
    /// <param name="dto">Novos dados da transação.</param>
    /// <returns>A transação atualizada, ou null se o ID não existir.</returns>
    /// <exception cref="ArgumentException">Se a pessoa não existe ou menor tenta receita.</exception>
    public async Task<TransactionResponseDto?> UpdateAsync(int id, CreateTransactionDto dto)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction == null)
            return null;

        var personInfo = await GetValidatedPersonInfoAsync(dto);

        transaction.Description = dto.Description;
        transaction.Amount = dto.Amount;
        transaction.Date = dto.Date!.Value;
        transaction.Type = dto.Type;
        transaction.PersonId = dto.PersonId;

        await _repository.SaveChangesAsync();

        return MapToResponse(transaction, personInfo.Name);
    }

    /// <summary>
    /// Remove uma transação pelo ID.
    /// </summary>
    /// <param name="id">Identificador da transação.</param>
    /// <returns>True se a transação foi encontrada e removida; False caso contrário.</returns>
    public async Task<bool> DeleteAsync(int id)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction == null)
            return false;

        _repository.Delete(transaction);
        await _repository.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Obtém a pessoa e valida as regras de negócio da transação.
    /// </summary>
    private async Task<(string Name, int Age)> GetValidatedPersonInfoAsync(CreateTransactionDto dto)
    {
        var personInfo = await _personService.GetInfoAsync(dto.PersonId);
        if (personInfo == null)
            throw new ArgumentException("A pessoa informada não existe no cadastro.");

        if (personInfo.Value.Age < 18 && dto.Type == TransactionType.Receita)
            throw new ArgumentException("Menores de 18 anos não podem cadastrar receitas, apenas despesas.");

        return personInfo.Value;
    }

    /// <summary>
    /// Converte uma entidade Transaction para DTO de resposta.
    /// </summary>
    private static TransactionResponseDto MapToResponse(Transaction transaction, string? personName) => new()
    {
        Id = transaction.Id,
        Description = transaction.Description,
        Amount = transaction.Amount,
        Date = transaction.Date,
        Type = transaction.Type,
        PersonId = transaction.PersonId,
        PersonName = personName ?? "Desconhecida"
    };
}
