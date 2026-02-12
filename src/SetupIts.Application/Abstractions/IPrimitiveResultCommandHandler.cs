using MediatR;
using SetupIts.Shared.Primitives;

namespace SetupIts.Application.Abstractions;
public interface IPrimitiveResultCommand<TResponse> : IRequest<PrimitiveResult<TResponse>> { }
public interface IPrimitiveResultCommand : IRequest<PrimitiveResult> { }
public interface IPrimitiveResultQuery<TResponse> : IRequest<PrimitiveResult<TResponse>> { }
public interface IPrimitiveResultCommandHandler<TQuery, TResponse> : IRequestHandler<TQuery, PrimitiveResult<TResponse>>
    where TQuery : IPrimitiveResultCommand<TResponse>
{ }
public interface IPrimitiveResultCommandHandler<TQuery> : IRequestHandler<TQuery, PrimitiveResult>
    where TQuery : IPrimitiveResultCommand
{ }
public interface IPrimitiveResultQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, PrimitiveResult<TResponse>>
    where TQuery : IPrimitiveResultQuery<TResponse>
{ }

