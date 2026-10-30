namespace MiniTemplateEngine
{
    public interface IHtmlTemplateRenderer
    {
        // Выполняет обработку HTML-шаблона, переданного в виде строки.
        string RenderFromString(string htmlTemplate, object dataModel);

        // Выполняет обработку HTML-шаблона из файла.
        string RenderFromFile(string filePath, object dataModel);

        // Выполняет обработку HTML-шаблона из файла и сохраняет результат в файл.
        string RenderToFile(string inputFilePath, string outputFilePath, object dataModel);
    }
}
