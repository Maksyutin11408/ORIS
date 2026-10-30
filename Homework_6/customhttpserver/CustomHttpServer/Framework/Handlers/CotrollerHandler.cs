using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using System.Linq;
using System.Collections.Specialized;
using CustomHttpServer.Framework.Attributes;
using CustomHttpServer.Controllers;
using CustomHttpServer.Framework.Results;

namespace CustomHttpServer.Framework.Handlers;

public class ControllerHandler : Handler
{
    public override async Task HandleAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        string localPath = request.Url?.LocalPath ?? "";
        var parts = localPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            if (NextHandler != null) await NextHandler.HandleAsync(context);
            return;
        }

        string controllerName = parts[0].ToLower();
        string methodName = parts[1].ToLower();

        var controllerType = Assembly.GetExecutingAssembly().GetTypes()
            .FirstOrDefault(t => t.GetCustomAttribute<ControllerAttribute>()?.Name.ToLower() == controllerName);

        if (controllerType == null)
        {
            if (NextHandler != null) await NextHandler.HandleAsync(context);
            return;
        }

        MethodInfo? targetMethod = null;
        string httpMethod = request.HttpMethod.ToUpper();

        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        foreach (var method in methods)
        {
            if (httpMethod == "GET")
            {
                var attr = method.GetCustomAttribute<GetAttribute>();
                if (attr != null && attr.Route.ToLower() == methodName)
                {
                    targetMethod = method;
                    break;
                }
            }
            else if (httpMethod == "POST")
            {
                var attr = method.GetCustomAttribute<PostAttribute>();
                if (attr != null && attr.Route.ToLower() == methodName)
                {
                    targetMethod = method;
                    break;
                }
            }
        }

        if (targetMethod == null)
        {
            if (NextHandler != null) await NextHandler.HandleAsync(context);
            return;
        }
        try
        {
            object?[] arguments = await BindParametersAsync(targetMethod, request);

            ControllerBase? controllerInstance = Activator.CreateInstance(controllerType) as ControllerBase;
            controllerInstance?.SetContext(context);
            
            object? result = targetMethod.Invoke(controllerInstance, arguments);

            if (result is IHttpResponseTypeResult responseResult)
            {
                var buffer = responseResult.Execute(context);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            else if (result is string htmlString)
            {
                byte[] buffer = Encoding.UTF8.GetBytes(htmlString);
                response.ContentType = "text/html; charset=utf-8";
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }

            response.OutputStream.Close();
        }
        catch (Exception ex)
        {
            response.StatusCode = 400;
            byte[] errorBuffer = Encoding.UTF8.GetBytes($"<h1>400 - Bad Request</h1><p>{WebUtility.HtmlEncode(ex.InnerException?.Message ?? ex.Message)}</p>");
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = errorBuffer.Length;
            await response.OutputStream.WriteAsync(errorBuffer, 0, errorBuffer.Length);
            response.OutputStream.Close();
        }
    }

    private async Task<object?[]> BindParametersAsync(MethodInfo method, HttpListenerRequest request)
    {
        var parametersCollection = HttpUtility.ParseQueryString(request.Url?.Query ?? string.Empty);
        string httpMethod = request.HttpMethod.ToUpper();

        if (request.HasEntityBody && (httpMethod == "POST" || httpMethod == "PUT"))
        {
            var encoding = request.ContentEncoding ?? Encoding.UTF8;
            using var reader = new StreamReader(request.InputStream, encoding);
            string body = await reader.ReadToEndAsync();
            var bodyParams = HttpUtility.ParseQueryString(body);
            parametersCollection.Add(bodyParams);
        }

        var methodParams = method.GetParameters();
        var arguments = new object?[methodParams.Length];

        for (int i = 0; i < methodParams.Length; i++)
        {
            var param = methodParams[i];
            string? value = parametersCollection[param.Name!];
            
            if (value != null)
            {
                arguments[i] = Convert.ChangeType(value, param.ParameterType);
            }
            else
            {
                arguments[i] = param.HasDefaultValue ? param.DefaultValue : null;
            }
        }

        return arguments;
    }
}