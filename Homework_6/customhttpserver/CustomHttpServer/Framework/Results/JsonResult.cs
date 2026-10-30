using System.Net;
using System.Text;
using System.Text.Json;

namespace CustomHttpServer.Framework.Results;

public class JsonResult : IHttpResponseTypeResult
{
    private readonly object _data;

    public JsonResult(object data)
    {
        _data = data;
    }

    public byte[] Execute(HttpListenerContext context)
    {
        // Логика преобразования
        string json = JsonSerializer.Serialize(_data);
        context.Response.ContentType = "application/json";
        byte[] buffer = Encoding.UTF8.GetBytes(json);

        return buffer;
    }
}