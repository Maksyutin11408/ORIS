using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections;
using System.Text;

namespace CustomHttpServer.Framework.TemplateEngine;

public class HtmlTemplateRenderer : IHtmlTemplateRenderer
{
    public string RenderFromString(string htmlTemplate, object dataModel)
    {
        if (string.IsNullOrEmpty(htmlTemplate)) return string.Empty;
        if (dataModel == null) return htmlTemplate;

        string result = ProcessForeachLoops(htmlTemplate, dataModel);

        result = ProcessIfConditions(result, dataModel);

        result = ProcessVariables(result, dataModel);

        return result;
    }

    public string RenderFromFile(string filePath, object dataModel)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Шаблон не найден по пути: {filePath}");

        string template = File.ReadAllText(filePath);
        return RenderFromString(template, dataModel);
    }

    public string RenderToFile(string inputFilePath, string outputFilePath, object dataModel)
    {
        string renderedHtml = RenderFromFile(inputFilePath, dataModel);
        File.WriteAllText(outputFilePath, renderedHtml);
        return renderedHtml;
    }

    private string ProcessVariables(string template, object model)
    {
        var regex = new Regex(@"\${(?<prop>\w+)}");

        return regex.Replace(template, match =>
        {
            string propertyName = match.Groups["prop"].Value;

            PropertyInfo? propInfo = model.GetType().GetProperty(propertyName);

            if (propInfo != null)
            {
                var value = propInfo.GetValue(model);
                
                return value?.ToString() ?? string.Empty;
            }
            return match.Value;
        });
    }
    private string ProcessIfConditions(string template, object model)
    {
    var ifRegex = new Regex(@"\$if\s*\((?<cond>\w+)\)(?<body>.*?)(?:\$else(?<elseBody>.*?))?\$endif", RegexOptions.Singleline);

        return ifRegex.Replace(template, match =>
        {
            string conditionProp = match.Groups["cond"].Value;
            string body = match.Groups["body"].Value;
            string elseBody = match.Groups["elseBody"].Value;

            PropertyInfo? propInfo = model.GetType().GetProperty(conditionProp);
            bool isTrue = false;

            if (propInfo != null)
            {
                var val = propInfo.GetValue(model);
                if (val is bool b) isTrue = b;
                else if (val != null) isTrue = true;
            }

            return isTrue ? body : elseBody;
        });
    }
    private string ProcessForeachLoops(string template, object model)
    {
        var foreachRegex = new Regex(@"\$foreach\s*\(\s*\w+\s+in\s+(?<coll>\w+)\s*\)(?<body>.*?)\$endfor", RegexOptions.Singleline);

        return foreachRegex.Replace(template, match =>
        {
            string collectionProp = match.Groups["coll"].Value;
            string body = match.Groups["body"].Value;

            PropertyInfo? propInfo = model.GetType().GetProperty(collectionProp);
            if (propInfo == null) return string.Empty;

            var collection = propInfo.GetValue(model) as IEnumerable;
            if (collection == null) return string.Empty;

            var sb = new StringBuilder();

            foreach (var item in collection)
            {
                string renderedRow = ProcessVariables(body, item);
                sb.Append(renderedRow);
            }

            return sb.ToString();
        });
    }
}
