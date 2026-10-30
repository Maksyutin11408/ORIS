using System.Net;

namespace CustomHttpServer.Framework.Results
{
    public interface IHttpResponseTypeResult
    {
        byte[] Execute(HttpListenerContext context);
    }
}