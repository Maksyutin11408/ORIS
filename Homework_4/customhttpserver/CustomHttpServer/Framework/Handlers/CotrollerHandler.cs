using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using CustomHttpServer.Framework.Attributes;

namespace CustomHttpServer.Framework.Handlers;

public class ControllerHandler : Handler
{
    public override async Task HandleAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        string localPath = context.Request.Url?.LocalPath ?? "";
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
        var parametersCollection = HttpUtility.ParseQueryString(request.Url?.Query ?? string.Empty);

        if (request.HasEntityBody && (httpMethod == "POST" || httpMethod == "PUT"))
        {
            var encoding = request.ContentEncoding ?? Encoding.UTF8;
            using var reader = new StreamReader(request.InputStream, encoding);
            string body = await reader.ReadToEndAsync();
            var bodyParams = HttpUtility.ParseQueryString(body);
            parametersCollection.Add(bodyParams);
        }
        var methodParams = targetMethod.GetParameters();
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
        var controllerInstance = Activator.CreateInstance(controllerType);
        
        if (targetMethod.ReturnType == typeof(Task))
        {
            await (Task)targetMethod.Invoke(controllerInstance, arguments)!;
        }
        else
        {
            targetMethod.Invoke(controllerInstance, arguments);
        }
        if (response.StatusCode == 0 || response.StatusCode == 200)
        {
            response.StatusCode = 200;
        }
    }
}