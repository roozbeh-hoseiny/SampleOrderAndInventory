using SetupIts.Shared.Primitives;

namespace SetupIts.Domain.ValueObjects;

public readonly record struct Sku
{
    public const int MAX_LENGTH = 255;

    readonly static PrimitiveResult<Sku> InvalidSku = PrimitiveResult.Failure<Sku>("Error", "Sku can not be empty");
    readonly static PrimitiveResult<Sku> InvalidSkuLength = PrimitiveResult.Failure<Sku>("Error", $"Sku can not be more than {MAX_LENGTH} characters");
    public string Value { get; }

    private Sku(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Sku cannot be empty");

        this.Value = value;
    }

    public static PrimitiveResult<Sku> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return InvalidSku;

        value = value.Trim();

        if (value.Length > MAX_LENGTH)
            return InvalidSkuLength;

        return CreateUnsafe(value);
    }
    public static Sku CreateUnsafe(string value) => new Sku(value?.Trim() ?? string.Empty);
    public override string ToString() => this.Value;
}
