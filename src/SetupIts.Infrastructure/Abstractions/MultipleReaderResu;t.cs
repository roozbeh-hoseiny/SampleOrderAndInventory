namespace SetupIts.Infrastructure.Abstractions;
public sealed record MultipleReaderResult<T1, T2>
{
    public T1? Item1 { get; set; } = default;
    public T2? Item2 { get; set; } = default;
}

public sealed record MultipleReaderResult<T1, T2, T3>
{
    public T1? Item1 { get; set; } = default;
    public T2? Item2 { get; set; } = default;
    public T3? Item3 { get; set; } = default;
}

public sealed record MultipleReaderResult<T1, T2, T3, T4>
{
    public T1? Item1 { get; set; } = default;
    public T2? Item2 { get; set; } = default;
    public T3? Item3 { get; set; } = default;
    public T4? Item4 { get; set; } = default;
}