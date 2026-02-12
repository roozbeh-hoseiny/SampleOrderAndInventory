using SetupIts.Shared.Primitives;

namespace SetupIts.Presentation.AppCore;

public interface IResultHandler
{
    IResult Handle<T>(PrimitiveResult<T> result);
    IResult Handle<T>(PrimitiveResult<T> result, Func<T, IResult> func);
}
