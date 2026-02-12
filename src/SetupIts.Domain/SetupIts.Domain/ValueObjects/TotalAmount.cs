using SetupIts.Shared.Primitives;

namespace SetupIts.Domain.ValueObjects;

public readonly record struct TotalAmount
{

    private readonly static PrimitiveResult<TotalAmount> InvalidPrice =
        PrimitiveResult.Failure<TotalAmount>("Error", "Invalid total amount");

    public readonly static TotalAmount Zero = new(0);

    public decimal Value { get; }

    public TotalAmount() : this(0) { }
    private TotalAmount(decimal value)
    {
        if (!IsValid(value))
            throw new ArgumentException("Invalid total amount");

        this.Value = value;
    }

    public static PrimitiveResult<TotalAmount> Create(decimal value)
    {
        if (!IsValid(value))
            return InvalidPrice;

        return new TotalAmount(value);
    }
    public static TotalAmount CreateUnsafe(decimal value)
    {
        return new TotalAmount(value);
    }

    static bool IsValid(decimal value)
    {
        return value > 0;
    }

    public override string ToString() => this.Value.ToString("0.00");
}
