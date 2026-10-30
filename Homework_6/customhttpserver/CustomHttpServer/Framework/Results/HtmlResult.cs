using System.Net;
using System.Text;

namespace CustomHttpServer.Framework.Results
{
    public class HtmlResult : IHttpResponseTypeResult
    {
        private readonly string _html;

        public HtmlResult(object html)
        {
            _html = html?.ToString() ?? string.Empty;
        }

        public byte[] Execute(HttpListenerContext context)
        {
            context.Response.ContentType = "text/html";
            
            byte[] buffer = Encoding.UTF8.GetBytes(_html);

            return buffer;
        }
    }
}