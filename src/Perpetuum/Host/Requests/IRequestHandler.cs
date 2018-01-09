using Perpetuum.Log;

namespace Perpetuum.Host.Requests
{
    public interface IRequestHandler<in T> where T:IRequest
    {
        void HandleRequest(T request);
    }

    public interface IRequestHandler : IRequestHandler<IRequest>
    {
    }

    public class RequestHandlerProfiler<T> : IRequestHandler<T> where T : IRequest
    {
        private readonly IRequestHandler<T> _requestHandler;

        public RequestHandlerProfiler(IRequestHandler<T> requestHandler)
        {
            _requestHandler = requestHandler;
        }

        public void HandleRequest(T request)
        {
            var t = Profiler.ExecutionTimeOf(() =>
            {
                _requestHandler.HandleRequest(request);
            });

            Logger.Info($"[PROFILE] {request.Command.Text} time:{t.TotalMilliseconds:F2}ms");
        }
    }

}