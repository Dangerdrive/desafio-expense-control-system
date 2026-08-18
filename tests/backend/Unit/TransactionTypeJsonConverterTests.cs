using System.Text.Json;
using Backend.Models;

namespace Backend.Tests.Unit;

/// <summary>
/// Testes unitários do TransactionTypeJsonConverter.
/// Garante o contrato da API em minúsculas ("receita"/"despesa"), a leitura
/// case-insensitive, o aceite de valores numéricos definidos no enum e a
/// mensagem de erro em PT-BR para entradas inválidas.
/// </summary>
public class TransactionTypeJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new();

    [Theory]
    [InlineData("\"receita\"", TransactionType.Receita)]
    [InlineData("\"despesa\"", TransactionType.Despesa)]
    [InlineData("\"Receita\"", TransactionType.Receita)]
    [InlineData("\"DESPESA\"", TransactionType.Despesa)]
    public void Read_WithStringValue_ShouldParseCaseInsensitive(string json, TransactionType expected)
    {
        var result = JsonSerializer.Deserialize<TransactionType>(json, Options);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1", TransactionType.Receita)]
    [InlineData("2", TransactionType.Despesa)]
    public void Read_WithDefinedNumericValue_ShouldParse(string json, TransactionType expected)
    {
        var result = JsonSerializer.Deserialize<TransactionType>(json, Options);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("\"investimento\"")]
    [InlineData("\"\"")]
    [InlineData("0")]
    [InlineData("3")]
    [InlineData("1.5")]
    [InlineData("true")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[]")]
    public void Read_WithInvalidValue_ShouldThrowWithPtBrMessage(string json)
    {
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TransactionType>(json, Options));

        Assert.Contains("O tipo deve ser 'receita' ou 'despesa'.", ex.Message);
    }

    [Theory]
    [InlineData(TransactionType.Receita, "\"receita\"")]
    [InlineData(TransactionType.Despesa, "\"despesa\"")]
    public void Write_ShouldSerializeAsLowercaseString(TransactionType value, string expected)
    {
        var json = JsonSerializer.Serialize(value, Options);

        Assert.Equal(expected, json);
    }

    [Fact]
    public void Write_WithUndefinedEnumValue_ShouldSerializeLowercasedName()
    {
        var json = JsonSerializer.Serialize((TransactionType)99, Options);

        Assert.Equal("\"99\"", json);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveValue()
    {
        var json = JsonSerializer.Serialize(TransactionType.Despesa, Options);

        var result = JsonSerializer.Deserialize<TransactionType>(json, Options);

        Assert.Equal(TransactionType.Despesa, result);
    }
}
