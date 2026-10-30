using System.Net;

namespace CustomHttpServer.Framework.Handlers;

public class NotFoundHandler : Handler
{
    public override async Task HandleAsync(HttpListenerContext context)
    {
        var response = context.Response;
        response.StatusCode = 404;

        string errorPagePath = Path.Combine(Directory.GetCurrentDirectory(), "static", "404.html");
        
        if (File.Exists(errorPagePath))
        {
            response.ContentType = "text/html; charset=utf-8";
            byte[] buffer = await File.ReadAllBytesAsync(errorPagePath);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);
        }
        else
        {
            response.ContentType = "text/plain; charset=utf-8";
            byte[] buffer = "404 - Page Not Found"u8.ToArray();
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);
        }
    }
}