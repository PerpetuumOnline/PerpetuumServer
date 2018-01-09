namespace Perpetuum.Host.Requests
{
    public delegate IRequestHandler<T> RequestHandlerFactory<in T>(Command command) where T : IRequest;
}