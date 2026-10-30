using System.Net;
using CustomHttpServer.Framework.Results;

namespace CustomHttpServer.Controllers;

public abstract class ControllerBase
{
    protected HttpListenerContext? Context {get; private set;}

    public void SetContext(HttpListenerContext context)
    {
        Context = context;
    }

    protected JsonResult Json(object obj) => new JsonResult(obj);
}