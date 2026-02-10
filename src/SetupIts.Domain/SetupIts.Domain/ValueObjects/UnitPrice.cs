using SetupIts.Shared.Primitives;

namespace SetupIts.Domain.ValueObjects;
public readonly record struct UnitPrice
{

    private readonly static PrimitiveResult<UnitPrice> InvalidPrice =
        PrimitiveResult.Failure<UnitPrice>("Error", "Invalid unit price");

    public readonly static UnitPrice Zero = new(0);
    public decimal Value { get; }

    private UnitPrice(decimal value)
    {
        if (!IsValid(value))
            throw new ArgumentException("Invalid unit price");

        this.Value = value;
    }

    public static PrimitiveResult<UnitPrice> Create(decimal value)
    {
        if (!IsValid(value))
            return InvalidPrice;

        return CreateUnsafe(value);
    }
    public static UnitPrice CreateUnsafe(decimal value)
    {
        return new UnitPrice(value);
    }
    public PrimitiveResult<UnitPrice> Scale(int input)
    {
        if (input < 0) return PrimitiveResult.Failure<UnitPrice>("Error", "invalid scale");

        if (input == 0) return this;

        return Create(this.Value * input);
    }
    public PrimitiveResult<UnitPrice> Increase(int input)
    {
        if (input < 0) return PrimitiveResult.Failure<UnitPrice>("Error", "invalid increase value");

        if (input == 0) return this;

        return Create(this.Value + input);
    }
    public PrimitiveResult<UnitPrice> Decrease(int input)
    {
        if (input < 0) return PrimitiveResult.Failure<UnitPrice>("Error", "invalid decrease value");

        if (input == 0) return this;

        if (input > this.Value) return PrimitiveResult.Failure<UnitPrice>("Error", "Cannot decrease by more than the current unit price");

        return Create(this.Value - input);
    }
    public UnitPrice TryScale(int input)
    {
        var result = this.Scale(input);
        return result.IsSuccess ? result.Value : throw new InvalidOperationException(result.Error.Message);
    }
    public UnitPrice TryIncrease(int input)
    {
        var result = this.Increase(input);
        return result.IsSuccess ? result.Value : throw new InvalidOperationException(result.Error.Message);
    }
    public UnitPrice TryDecrease(int input)
    {
        var result = this.Decrease(input);
        return result.IsSuccess ? result.Value : throw new InvalidOperationException(result.Error.Message);
    }

    static bool IsValid(decimal value)
    {
        return value > 0;
    }

    public override string ToString() => this.Value.ToString("0.00");
}
