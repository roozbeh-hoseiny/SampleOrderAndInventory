using SetupIts.Domain.ValueObjects;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SetupIts.Presentation.AppCore.JsonConverters;

public sealed class ProductIdJsonConverter : JsonConverter<ProductId>
{
    public override ProductId Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("ProductId must be a string");

        return ProductId.Create(reader.GetString()!);
    }

    public override void Write(
        Utf8JsonWriter writer,
        ProductId value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
public sealed class QuantityJsonConverter : JsonConverter<Quantity>
{
    public override Quantity Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return Quantity.CreateUnsafe(reader.GetInt32());
    }

    public override void Write(
        Utf8JsonWriter writer,
        Quantity value,
        JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
public sealed class UnitPriceJsonConverter : JsonConverter<UnitPrice>
{
    public override UnitPrice Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return UnitPrice.CreateUnsafe(reader.GetDecimal());
    }

    public override void Write(
        Utf8JsonWriter writer,
        UnitPrice value,
        JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}