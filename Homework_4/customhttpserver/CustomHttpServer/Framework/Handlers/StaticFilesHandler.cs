using System.Net;
using CustomHttpServer.Framework.Configuration;

namespace CustomHttpServer.Framework.Handlers;

public class StaticFileHandler : Handler
{
    private readonly SettingsModel _settings;

    public StaticFileHandler(SettingsModel settings)
    {
        _settings = settings;
    }

    public override async Task HandleAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        if (request.HttpMethod != "GET" && request.HttpMethod != "HEAD")
        {
            if (NextHandler != null) await NextHandler.HandleAsync(context);
            return;
        }

        string path = request.Url?.LocalPath ?? "/";
        if (path == "/") path = "/index.html";
        else if (!Path.HasExtension(path)) path += ".html";

        string staticFolder = _settings?.StaticPath ?? "static";
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), staticFolder, path.TrimStart('/'));

        if (!File.Exists(filePath))
        {
            if (NextHandler != null) await NextHandler.HandleAsync(context);
            return;
        }

        try
        {
            var extension = Path.GetExtension(filePath).ToLower();
            response.ContentType = extension switch
            {
                ".html" => "text/html; charset=utf-8",
                ".css"  => "text/css; charset=utf-8",
                ".js"   => "text/javascript; charset=utf-8",
                ".png"  => "image/png",
                ".ico"  => "image/x-icon",
                ".svg"  => "image/svg+xml",
                ".jpg"  => "image/jpeg",
                _       => "application/octet-stream"
            };

            byte[] buffer = await File.ReadAllBytesAsync(filePath);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);
        }
        catch
        {
            throw;
        }
    }
}