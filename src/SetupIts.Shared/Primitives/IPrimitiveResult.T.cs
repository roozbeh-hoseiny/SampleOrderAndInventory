namespace SetupIts.Shared.Primitives;

public interface IPrimitiveResult<TValue> : IPrimitiveResult
{
    TValue Value { get; }
}
