using System.Text.Json.Serialization;

namespace Backend.Models;

/// <summary>
/// Tipo de uma transação financeira: receita (entrada) ou despesa (saída).
///
/// Os valores começam em 1 (não em 0) de propósito: assim, um campo "type"
/// ausente no payload JSON resulta no valor padrão 0, que não é um valor
/// válido do enum — permitindo que a validação de modelo detecte a omissão.
/// </summary>
[JsonConverter(typeof(TransactionTypeJsonConverter))]
public enum TransactionType
{
    Receita = 1,
    Despesa = 2
}
