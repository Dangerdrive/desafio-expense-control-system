using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend.Models;

/// <summary>
/// Converte <see cref="TransactionType"/> para/desde JSON mantendo o
/// contrato da API em minúsculas: "receita" / "despesa".
///
/// Sem este conversor, o System.Text.Json serializaria o enum como número
/// (1/2) e rejeitaria valores inválidos com uma mensagem em inglês.
/// Com ele:
/// - O cliente continua enviando/recebendo "receita" e "despesa" (contrato estável);
/// - A comparação é case-insensitive (aceita "Receita", "RECEITA", etc.);
/// - Um valor inválido ("investimento") produz uma mensagem clara em PT-BR.
/// </summary>
public class TransactionTypeJsonConverter : JsonConverter<TransactionType>
{
    private const string ErrorMessage = "O tipo deve ser 'receita' ou 'despesa'.";

    public override TransactionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Aceita strings "receita"/"despesa" (case-insensitive)
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (value != null && Enum.TryParse<TransactionType>(value, ignoreCase: true, out var parsed))
                return parsed;

            throw new JsonException(ErrorMessage);
        }

        // Aceita o valor numérico apenas se corresponder a um membro definido
        if (reader.TokenType == JsonTokenType.Number &&
            reader.TryGetInt32(out var intValue) &&
            Enum.IsDefined(typeof(TransactionType), intValue))
        {
            return (TransactionType)intValue;
        }

        throw new JsonException(ErrorMessage);
    }

    public override void Write(Utf8JsonWriter writer, TransactionType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value switch
        {
            TransactionType.Receita => "receita",
            TransactionType.Despesa => "despesa",
            _ => value.ToString().ToLowerInvariant()
        });
    }
}
