using SetupIts.Shared.Primitives;

namespace SetupIts.Domain.ValueObjects;
public readonly record struct ProductName
{
    public const int MAX_LENGTH = 255;

    readonly static PrimitiveResult<ProductName> InvalidProductName = PrimitiveResult.Failure<ProductName>("Error", "ProductName can not be empty");
    readonly static PrimitiveResult<ProductName> InvalidProductNameLength = PrimitiveResult.Failure<ProductName>("Error", $"ProductName can not be more than {MAX_LENGTH} characters");
    public string Value { get; }

    private ProductName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ProductName cannot be empty");

        this.Value = value;
    }

    public static PrimitiveResult<ProductName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return InvalidProductName;

        value = value.Trim();

        if (value.Length > MAX_LENGTH)
            return InvalidProductNameLength;

        return CreateUnsafe(value);
    }

    public static ProductName CreateUnsafe(string value) => new ProductName(value?.Trim() ?? string.Empty);

    public override string ToString() => this.Value;
}
