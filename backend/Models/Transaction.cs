using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

/// <summary>
/// Representa uma transação financeira (receita ou despesa) associada a uma pessoa.
/// </summary>
public class Transaction
{
    /// <summary>
    /// Identificador único da transação, gerado automaticamente.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Descrição da transação (ex: "Salário", "Conta de luz").
    /// </summary>
    [Required(ErrorMessage = "A descrição é obrigatória.")]
    [MaxLength(200, ErrorMessage = "A descrição deve ter no máximo 200 caracteres.")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Valor da transação. Deve ser maior que zero.
    /// </summary>
    [Required(ErrorMessage = "O valor é obrigatório.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Tipo da transação: "receita" (entrada de dinheiro) ou "despesa" (saída de dinheiro).
    /// </summary>
    [Required(ErrorMessage = "O tipo é obrigatório.")]
    [RegularExpression("^(receita|despesa)$", ErrorMessage = "O tipo deve ser 'receita' ou 'despesa'.")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Identificador da pessoa associada a esta transação (chave estrangeira).
    /// </summary>
    [Required(ErrorMessage = "O identificador da pessoa é obrigatório.")]
    public int PersonId { get; set; }

    /// <summary>
    /// Navegação para a entidade Person associada.
    /// </summary>
    [ForeignKey("PersonId")]
    public Person? Person { get; set; }
}
