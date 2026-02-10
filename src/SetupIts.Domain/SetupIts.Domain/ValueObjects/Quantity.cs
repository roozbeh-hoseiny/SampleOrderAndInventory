using SetupIts.Shared.Primitives;

namespace SetupIts.Domain.ValueObjects;

public readonly record struct Quantity : IComparable<Quantity>
{
    public readonly static Quantity Zero = new Quantity(0);

    readonly static PrimitiveResult<Quantity> InvalidQuantity = PrimitiveResult.Failure<Quantity>("Error", "Quantity can not be less than zero");
    public int Value { get; }

    private Quantity(int value)
    {
        if (value < 0)
            throw new ArgumentException("Quantity can not be less than zero");

        this.Value = value;
    }

    public static PrimitiveResult<Quantity> Create(int value)
    {
        if (value < 0)
            return InvalidQuantity;

        return new Quantity(value);
    }
    public static Quantity CreateUnsafe(int value)
    {
        return new Quantity(value);
    }

    public PrimitiveResult<Quantity> Increase(int input)
    {
        if (input < 0) return PrimitiveResult.Failure<Quantity>("Error", "invalid increase value");

        if (input == 0) return this;

        return Create(this.Value + input);
    }
    public PrimitiveResult<Quantity> Decrease(int input)
    {
        if (input < 0) return PrimitiveResult.Failure<Quantity>("Error", "invalid decrease value");

        if (input == 0) return this;

        if (input > this.Value) return PrimitiveResult.Failure<Quantity>("Error", "Cannot decrease by more than the current quantity");

        return Create(this.Value - input);
    }
    public Quantity TryIncrease(int input)
    {
        var result = this.Increase(input);
        return result.IsSuccess ? result.Value : throw new InvalidOperationException(result.Error.Message);
    }
    public Quantity TryDecrease(int input)
    {
        var result = this.Decrease(input);
        return result.IsSuccess ? result.Value : throw new InvalidOperationException(result.Error.Message);
    }

    public PrimitiveResult<Quantity> Increase(Quantity input)
    {
        return this.Increase(input.Value);
    }
    public PrimitiveResult<Quantity> Decrease(Quantity input)
    {
        return this.Decrease(input.Value);
    }
    public Quantity TryIncrease(Quantity input)
    {
        var result = this.Increase(input);
        return result.IsSuccess ? result.Value : throw new InvalidOperationException(result.Error.Message);
    }
    public Quantity TryDecrease(Quantity input)
    {
        var result = this.Decrease(input);
        return result.IsSuccess ? result.Value : throw new InvalidOperationException(result.Error.Message);
    }

    public int CompareTo(Quantity other) => this.Value.CompareTo(other.Value);

    public static bool operator >(Quantity left, Quantity right) => left.CompareTo(right) > 0;

    public static bool operator <(Quantity left, Quantity right) => left.CompareTo(right) < 0;

    public static bool operator >=(Quantity left, Quantity right) => left.CompareTo(right) >= 0;

    public static bool operator <=(Quantity left, Quantity right) => left.CompareTo(right) <= 0;
}
