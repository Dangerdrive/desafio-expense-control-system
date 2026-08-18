using System.ComponentModel.DataAnnotations;
using Backend.Models;

namespace Backend.DTOs;

/// <summary>
/// Resposta padronizada para erros da API.
/// </summary>
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
}

// ===================== PESSOA =====================

/// <summary>
/// DTO para criação de uma nova pessoa.
/// </summary>
public class CreatePersonDto
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "A idade é obrigatória.")]
    [Range(0, 150)]
    public int Age { get; set; }
}

/// <summary>
/// DTO de resposta com os dados de uma pessoa cadastrada.
/// </summary>
public class PersonResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

// ===================== TRANSAÇÃO =====================

/// <summary>
/// DTO para criação de uma nova transação.
/// </summary>
public class CreateTransactionDto
{
    [Required(ErrorMessage = "A descrição é obrigatória.")]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "O valor é obrigatório.")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Data da transação (ex: "2026-08-14"). Obrigatória.
    /// </summary>
    [Required(ErrorMessage = "A data é obrigatória.")]
    public DateOnly? Date { get; set; }

    [Required(ErrorMessage = "O tipo é obrigatório.")]
    [EnumDataType(typeof(TransactionType), ErrorMessage = "O tipo deve ser 'receita' ou 'despesa'.")]
    public TransactionType Type { get; set; }

    [Required(ErrorMessage = "O identificador da pessoa é obrigatório.")]
    public int PersonId { get; set; }
}

/// <summary>
/// DTO de resposta com os dados de uma transação cadastrada.
/// </summary>
public class TransactionResponseDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public TransactionType Type { get; set; }
    public int PersonId { get; set; }
    public string PersonName { get; set; } = string.Empty;
}

// ===================== CONSULTA DE TOTAIS =====================

/// <summary>
/// DTO que representa o resumo financeiro de uma pessoa:
/// total de receitas, total de despesas e saldo líquido.
/// </summary>
public class PersonTotalsDto
{
    public int PersonId { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public decimal TotalIncome { get; set; }   // Total de receitas
    public decimal TotalExpense { get; set; }  // Total de despesas
    public decimal Balance { get; set; }       // Saldo = receitas - despesas
}

/// <summary>
/// DTO que representa o resumo geral do sistema:
/// totais por pessoa + total geral consolidado.
/// </summary>
public class TotalsResponseDto
{
    /// <summary>Lista de totais individuais por pessoa.</summary>
    public List<PersonTotalsDto> PeopleTotals { get; set; } = new();

    /// <summary>Soma de todas as receitas do sistema.</summary>
    public decimal GrandTotalIncome { get; set; }

    /// <summary>Soma de todas as despesas do sistema.</summary>
    public decimal GrandTotalExpense { get; set; }

    /// <summary>Saldo líquido geral = receitas totais - despesas totais.</summary>
    public decimal GrandBalance { get; set; }
}

// ===================== PAGINAÇÃO =====================

/// <summary>
/// Envelope de resposta paginada usado pelos endpoints de listagem
/// (GET /api/people e GET /api/transactions).
/// </summary>
/// <typeparam name="T">Tipo dos itens da página.</typeparam>
public class PagedResult<T>
{
    /// <summary>Itens da página atual.</summary>
    public List<T> Items { get; set; } = new();

    /// <summary>Número da página atual (1-based).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Quantidade de itens por página.</summary>
    public int PageSize { get; set; } = 10;

    /// <summary>Total de itens no conjunto completo.</summary>
    public int TotalItems { get; set; }

    /// <summary>Total de páginas.</summary>
    public int TotalPages { get; set; }

    /// <summary>Existe página seguinte?</summary>
    public bool HasNext { get; set; }

    /// <summary>Existe página anterior?</summary>
    public bool HasPrevious { get; set; }

    /// <summary>
    /// Monta um PagedResult a partir da página já extraída.
    /// Centraliza o cálculo de TotalPages/HasNext/HasPrevious.
    /// </summary>
    public static PagedResult<T> Create(List<T> items, int page, int pageSize, int totalItems)
    {
        var safePageSize = pageSize < 1 ? 1 : pageSize;
        return new PagedResult<T>
        {
            Items = items,
            Page = page < 1 ? 1 : page,
            PageSize = safePageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)safePageSize),
            HasNext = (page < 1 ? 1 : page) * safePageSize < totalItems,
            HasPrevious = page > 1
        };
    }

    /// <summary>
    /// Monta um PagedResult a partir do conjunto completo, aplicando
    /// normalização, paginação e projeção.
    /// </summary>
    public static PagedResult<T> FromSource<TSource>(
        IEnumerable<TSource> source,
        int page,
        int pageSize,
        Func<TSource, T> projection)
    {
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var safePage = Math.Max(page, 1);
        var sourceItems = source.ToList();

        return Create(
            sourceItems
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .Select(projection)
                .ToList(),
            safePage,
            safePageSize,
            sourceItems.Count);
    }
}
