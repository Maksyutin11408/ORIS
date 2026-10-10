using System.Net;
using CustomHttpServer.Framework.Configuration;

namespace CustomHttpServer.Framework.Handlers
{
    public abstract class Handler
    {
        protected Handler? NextHandler;

        public Handler SetNext(Handler handler)
        {
            NextHandler = handler;
            return handler;
        }

        public abstract Task HandleAsync(HttpListenerContext context);
    }
}