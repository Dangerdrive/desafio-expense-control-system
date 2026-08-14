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
        // ============================================================
        // Validação 1: Pessoa existe?
        // Buscamos nome + idade em uma única consulta para também
        // preencher o PersonName da resposta sem uma segunda query.
        // ============================================================
        var personInfo = await _personService.GetInfoAsync(dto.PersonId);
        if (personInfo == null)
            throw new ArgumentException("A pessoa informada não existe no cadastro.");

        // ============================================================
        // Validação 2: REGRA DE NEGÓCIO CRÍTICA
        // Menor de 18 anos NÃO pode ter receita.
        // Por que < 18 e não <= 17? Porque a lei considera maioridade
        // a partir dos 18 anos completos. 18 pode; 17 não.
        // ============================================================
        if (personInfo.Value.Age < 18 && dto.Type == TransactionType.Receita)
            throw new ArgumentException("Menores de 18 anos não podem cadastrar receitas, apenas despesas.");

        var transaction = new Transaction
        {
            Description = dto.Description,
            Amount = dto.Amount,
            Type = dto.Type,
            PersonId = dto.PersonId
        };

        await _repository.AddAsync(transaction);
        await _repository.SaveChangesAsync();

        return new TransactionResponseDto
        {
            Id = transaction.Id,
            Description = transaction.Description,
            Amount = transaction.Amount,
            Type = transaction.Type,
            PersonId = transaction.PersonId,
            PersonName = personInfo.Value.Name
        };
    }

    /// <summary>
    /// Lista todas as transações cadastradas, ordenadas por ID decrescente
    /// (mais recentes primeiro), incluindo o nome da pessoa associada.
    /// </summary>
    public async Task<List<TransactionResponseDto>> GetAllAsync()
    {
        // Inclui a navegação Person para popular o PersonName na resposta.
        var transactions = await _repository.GetAllAsync(t => t.Person);

        // Ordenação da mais recente para a mais antiga.
        return transactions
            .OrderByDescending(t => t.Id)
            .Select(t => new TransactionResponseDto
            {
                Id = t.Id,
                Description = t.Description,
                Amount = t.Amount,
                Type = t.Type,
                PersonId = t.PersonId,
                PersonName = t.Person?.Name ?? "Desconhecida"
            })
            .ToList();
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

        return new TransactionResponseDto
        {
            Id = transaction.Id,
            Description = transaction.Description,
            Amount = transaction.Amount,
            Type = transaction.Type,
            PersonId = transaction.PersonId,
            PersonName = personInfo?.Name ?? "Desconhecida"
        };
    }
}
