namespace SetupIts.Shared.Primitives;
public static class PrimitiveResultExtensions
{
    public static PrimitiveResult<T> Bind<T>(this PrimitiveResult<T> src, Func<PrimitiveResult<T>, PrimitiveResult<T>> func)
    {
        return src.IsFailure
            ? src
            : func(src);
    }
    public static async Task<PrimitiveResult<T>> Bind<T>(this Task<PrimitiveResult<T>> src, Func<PrimitiveResult<T>, Task<PrimitiveResult<T>>> func)
    {
        var srcResult = await src.ConfigureAwait(false);

        if (srcResult.IsFailure) return srcResult;

        return await func(srcResult.Value).ConfigureAwait(false);
    }

    public static PrimitiveResult<TOut> Map<TIn, TOut>(this PrimitiveResult<TIn> src, Func<TIn, PrimitiveResult<TOut>> func)
    {
        return src.IsFailure
            ? PrimitiveResult.Failure<TOut>(src.Errors)
            : func(src.Value);
    }

    public static async Task<PrimitiveResult<TOut>> Map<TIn, TOut>(this Task<PrimitiveResult<TIn>> src,
        Func<TIn, PrimitiveResult<TOut>> func)
    {
        var srcResult = await src.ConfigureAwait(false);

        if (srcResult.IsFailure) return PrimitiveResult.Failure<TOut>(srcResult.Errors);

        return func(srcResult.Value);
    }
    public static async Task<PrimitiveResult<TOut>> Map<TIn, TOut>(this Task<PrimitiveResult<TIn>> src, Func<TIn, Task<PrimitiveResult<TOut>>> func)
    {
        var srcResult = await src.ConfigureAwait(false);

        if (srcResult.IsFailure) return PrimitiveResult.Failure<TOut>(srcResult.Errors);

        return await func(srcResult.Value).ConfigureAwait(false);
    }
    public static PrimitiveResult Map<TIn>(this PrimitiveResult<TIn> src, Func<TIn, PrimitiveResult> func)
    {
        return src.IsFailure
            ? PrimitiveResult.Failure(src.Errors)
            : func(src.Value);
    }
}
