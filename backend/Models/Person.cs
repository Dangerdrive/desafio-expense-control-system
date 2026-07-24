using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

/// <summary>
/// Representa uma pessoa cadastrada no sistema de controle de gastos.
/// Cada pessoa possui um identificador único gerado automaticamente,
/// nome e idade. Uma pessoa pode ter várias transações associadas.
/// </summary>
public class Person
{
    /// <summary>
    /// Identificador único da pessoa, gerado automaticamente pelo banco de dados.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Nome da pessoa. Campo obrigatório.
    /// </summary>
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [MaxLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Idade da pessoa. Utilizada para validar a regra de negócio:
    /// menores de 18 anos só podem ter despesas cadastradas.
    /// </summary>
    [Required(ErrorMessage = "A idade é obrigatória.")]
    [Range(0, 150, ErrorMessage = "A idade deve estar entre 0 e 150.")]
    public int Age { get; set; }

    /// <summary>
    /// Coleção de transações associadas a esta pessoa.
    /// Configurada para exclusão em cascata: ao deletar uma pessoa,
    /// todas as suas transações são removidas automaticamente.
    /// </summary>
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
