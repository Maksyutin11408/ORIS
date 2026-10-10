using System.Net;
using System.Text;
using CustomHttpServer.Framework.Configuration;
using CustomHttpServer.Framework.Handlers;

namespace CustomHttpServer.Http;

public class HttpServer
{
    private readonly HttpListener _listener = new ();
    private readonly SettingsModel _settings;
    private bool _isRunning;
    private Handler _rootHandler = null!;
    
    public HttpServer(SettingsModel settings)
    {
        _settings = settings;

        if (settings?.Prefixes != null)
        {
            foreach (var prefix in settings.Prefixes)
                _listener.Prefixes.Add(prefix);
        }
        BuildHandlerChain();
    }

    private void BuildHandlerChain()
    {
        var staticHandler = new StaticFileHandler(_settings);
        var controllerHandler = new ControllerHandler();
        var notFoundHandler = new NotFoundHandler();

        staticHandler.SetNext(controllerHandler);
        controllerHandler.SetNext(notFoundHandler);

        _rootHandler = staticHandler;
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
                _ = ProcessRequestAsync(context);
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
        try
        {
            Console.WriteLine($"[Запрос] {context.Request.HttpMethod} {context.Request.Url?.LocalPath}");
            
            await _rootHandler.HandleAsync(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Глобальный сбой (500): {ex.Message}\n{ex.StackTrace}");
            try
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "text/plain; charset=utf-8";
                byte[] buffer = Encoding.UTF8.GetBytes($"Внутренняя ошибка сервера: {ex.Message}");
                context.Response.ContentLength64 = buffer.Length;
                await context.Response.OutputStream.WriteAsync(buffer);
            }
            catch
            {
                
            }
        }
        finally
        {
            context.Response.OutputStream.Close();
        }
    }
}