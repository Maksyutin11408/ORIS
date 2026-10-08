using System.Net;
using System.Text;
using CustomHttpServer.Framework.Configuration;

namespace CustomHttpServer.Core;

public class HttpServer
{
    private readonly HttpListener _listener = new ();
    private readonly SettingsModel _settings;
    private bool _isRunning;
    
    public HttpServer(SettingsModel settings)
    {
        _settings = settings;

        foreach (var prefix in settings.Prefixes)
            _listener.Prefixes.Add(prefix);
    }

    public async Task Start()
    {
        _listener.Start();
        _isRunning = true;
        Console.WriteLine("Сервер начал свою работу");
        await ListenAsync();
    }

    public void Stop()
    {
        _listener.Stop();
        Console.WriteLine("Сервер завершил свою работу");
    }

    private async Task ListenAsync()
    {
        while (_isRunning)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = ProcessRequestAsync(context); // обрабатываем в фоне
            }
            catch (HttpListenerException)
            {
                break; // сервер остановлен
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        Console.WriteLine("Пришел запрос");
        var response = context.Response;
        if (request.HttpMethod == "POST")
        {
            await HandlePostRequestAsync(context);
            return;
        }
        else if (request.HttpMethod == "GET")
        {
            await HandleGetRequestAsync(context); 
        }
        else
        {
            response.StatusCode = 405;
            response.OutputStream.Close();
        }
    }

    private async Task HandleGetRequestAsync(HttpListenerContext context)
    {
        var response = context.Response;
        var request = context.Request;
        string path = request.Url.LocalPath;
        try
        {
            if (path == "/")
            {
                path = "/index.html";
            }
            else if (!Path.HasExtension(path))
            {
                path += ".html";
            }
            // Читаем файл как байты
            string filePath = Directory.GetCurrentDirectory() + $"/{_settings.StaticPath}{path}";
            FileInfo fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists)
            {
                response.StatusCode = 404;
                filePath = Directory.GetCurrentDirectory() + $"/static/404.html";
            }

            switch (fileInfo.Extension)
            {
                case ".html":
                    response.ContentType = "text/html; charset=utf-8";
                    break;
                case ".css":
                    response.ContentType = "text/css; charset=utf-8";
                    break;
                case ".js":
                    response.ContentType = "text/javascript; charset=utf-8";
                    break;
                case ".png":
                    response.ContentType = "image/png";
                    break;
                case ".ico":
                    response.ContentType = "image/x-icon";
                    break;
                case ".svg":
                    response.ContentType = "image/svg+xml";
                    break;
                case ".jpg":
                    response.ContentType = "image/jpeg";
                    break;
            }

            byte[] buffer = await File.ReadAllBytesAsync(filePath);
            response.ContentLength64 = buffer.Length;
            using Stream output = response.OutputStream;
            await output.WriteAsync(buffer);
            await output.FlushAsync();
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            await WriteResponseAsync(response, $"Ошибка при обработке запроса: {ex.Message}");
        }

        Console.WriteLine($"Обработан запрос: {context.Request.Url}");
    }
    private async Task HandlePostRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            string body;

            var encoding = request.ContentEncoding ?? Encoding.UTF8;

            using (var reader = new StreamReader(request.InputStream, encoding))
            {
                body = await reader.ReadToEndAsync();
            }
            string decodedBody = System.Web.HttpUtility.UrlDecode(body);
            var parsedParams = System.Web.HttpUtility.ParseQueryString(body);

            string email = parsedParams["email"];
            string password = parsedParams["password"];

            Console.WriteLine($"Email пользователя: {email}");
            Console.WriteLine($"Пароль пользователя: {password}");
            
            response.StatusCode = 200;
            response.ContentLength64 = 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при чтении POST-запроса: {ex.Message}");
            response.StatusCode = 500;
        }
        finally
        {
            response.OutputStream.Close();
        }
    }

    private static async Task WriteResponseAsync(HttpListenerResponse response, string content)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(content);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.OutputStream.Close();
    }
}