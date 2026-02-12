namespace SetupIts.Shared.Primitives;

public interface IPrimitiveResult
{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    PrimitiveError Error { get; }
    PrimitiveError[] Errors { get; }
}
