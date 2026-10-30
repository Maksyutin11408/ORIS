using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniTemplateEngine;

// Реализация HTML-шаблонизатора с поддержкой подстановок, условий и циклов.
public class HtmlTemplateRenderer : IHtmlTemplateRenderer
{
    // Регулярное выражение для поиска переменных вида ${PropertyName}.
    private static readonly Regex VariableRegex = new Regex(@"\${(?<prop>\w+)}", RegexOptions.Compiled);

    // Регулярное выражение для поиска блоков условий $if (Cond) ... $else ... $endif.

    private static readonly Regex IfRegex = new Regex(@"\$if\s*\((?<cond>\w+)\)(?<body>.*?)(?:\$else(?<elseBody>.*?))?\$endif", RegexOptions.Compiled | RegexOptions.Singleline);

    // Регулярное выражение для поиска блоков циклов $foreach (item in Coll) ... $endfor.

    private static readonly Regex ForeachRegex = new Regex(@"\$foreach\s*\(\s*\w+\s+in\s+(?<coll>\w+)\s*\)(?<body>.*?)\$endfor", RegexOptions.Compiled | RegexOptions.Singleline);


    // Выполняет обработку HTML-шаблона из строки с подстановкой модели данных
    // Возвращает Готовый HTML-код
    public string RenderFromString(string htmlTemplate, object dataModel)
    {
        // Проверяем входную строку на пустоту
        if (string.IsNullOrEmpty(htmlTemplate)) return string.Empty;

        // Если модель данных не передана, возвращаем шаблон без изменений
        if (dataModel == null) return htmlTemplate;

        // Вызываем рекурсивный метод рендеринга для обработки вложенных структур
        return ProcessTemplateInternal(htmlTemplate, dataModel);
    }

    // Внутренний метод для рекурсивного разбора шаблона. Обеспечивает поддержку вложенности.
    private string ProcessTemplateInternal(string template, object model)
    {
        // Копируем исходный шаблон в локальную строку для поэтапной трансформации
        string result = template;

        // Переменная для отслеживания изменений в строке на текущей итерации
        string previousResult;

        // Запускаем цикл для обработки вложенных циклов и условий (изнутри наружу)
        do
        {
            // Запоминаем состояние строки перед проходом парсера
            previousResult = result;

            // 1. Сначала обрабатываем циклы $foreach
            result = ForeachRegex.Replace(result, match =>
            {
                // Извлекаем имя свойства-коллекции из группы регулярного выражения
                string collectionProp = match.Groups["coll"].Value;
                // Извлекаем внутреннее тело цикла (содержимое между $foreach и $endfor)
                string body = match.Groups["body"].Value;

                // Получаем метаданные свойства из текущей модели через рефлексию
                PropertyInfo? propInfo = model.GetType().GetProperty(collectionProp);
                // Если свойство не найдено в объекте, вырезаем этот блок цикла из HTML
                if (propInfo == null) return string.Empty;

                // Получаем значение свойства и приводим его к интерфейсу перечисления IEnumerable
                var collection = propInfo.GetValue(model) as IEnumerable;
                // Если свойство не является коллекцией или равно null, вырезаем блок цикла
                if (collection == null) return string.Empty;

                // Создаем StringBuilder для эффективной конкатенации строк результатов итераций
                var sb = new StringBuilder();

                // Итерируемся по каждому объекту внутри найденной коллекции
                foreach (var item in collection)
                {
                    // Рекурсивно вызываем парсер для тела цикла, передавая текущий элемент коллекции как модель
                    sb.Append(ProcessTemplateInternal(body, item));
                }

                // Возвращаем склеенный результат всех итераций цикла
                return sb.ToString();
            });

            // 2. Обрабатываем условия $if / $else / $endif
            result = IfRegex.Replace(result, match =>
            {
                // Получаем имя условного свойства
                string conditionProp = match.Groups["cond"].Value;
                // Получаем тело для истинного условия
                string body = match.Groups["body"].Value;
                // Получаем тело для ложного условия (ветка $else может отсутствовать)
                string elseBody = match.Groups["elseBody"].Value;

                // Ищем свойство в модели через механизм рефлексии
                PropertyInfo? propInfo = model.GetType().GetProperty(conditionProp);
                // Инициализируем флаг условия по умолчанию как false
                bool isTrue = false;

                // Если свойство найдено в C#-модели
                if (propInfo != null)
                {
                    // Извлекаем значение свойства из объекта
                    var val = propInfo.GetValue(model);
                    // Если тип значения boolean, записываем его результат напрямую
                    if (val is bool b) isTrue = b;
                    // Если это любой другой объект и он не равен null, интерпретируем как true
                    else if (val != null) isTrue = true;
                }

                // Выбираем ветку контента в зависимости от флага
                string selectedBlock = isTrue ? body : elseBody;

                // Рекурсивно обрабатываем выбранный блок на случай вложенных конструкций
                return ProcessTemplateInternal(selectedBlock, model);
            });

            // Цикл повторяется до тех пор, пока регулярные выражения находят и меняют блоки текста
        } while (result != previousResult);

        // 3. В самом конце, когда структура блоков определена, подставляем значения переменных ${Prop}
        result = VariableRegex.Replace(result, match =>
        {
            // Извлекаем имя запрашиваемого свойства
            string propertyName = match.Groups["prop"].Value;

            // Обращаемся к рефлексии для поиска свойства в переданной модели
            PropertyInfo? propInfo = model.GetType().GetProperty(propertyName);

            // Если свойство найдено в объекте
            if (propInfo != null)
            {
                // Считываем его актуальное значение
                var value = propInfo.GetValue(model);
                // Возвращаем строковое представление значения или пустую строку, если там null
                return value?.ToString() ?? string.Empty;
            }

            // Если свойство не найдено в модели данных, оставляем метку в исходном виде
            return match.Value;
        });

        // Возвращаем итоговую скомпилированную HTML строку
        return result;
    }

    // Выполняет обработку HTML-шаблона, загруженного из файла.
    public string RenderFromFile(string filePath, object dataModel)
    {
        // Проверяем физическое существование файла шаблона по указанному пути
        if (!File.Exists(filePath))
            // Генерируем исключение, если файл не был найден
            throw new FileNotFoundException($"Шаблон не найден по пути: {filePath}");

        // Считываем весь текст из файла в одну строку
        string template = File.ReadAllText(filePath);

        // Передаем прочитанную строку в метод строкового парсинга и возвращаем результат
        return RenderFromString(template, dataModel);
    }

    // Выполняет обработку HTML-шаблона из файла и сохраняет результат в указанный файл.
    public string RenderToFile(string inputFilePath, string outputFilePath, object dataModel)
    {
        // Вызываем метод чтения и обработки исходного файла шаблона
        string renderedHtml = RenderFromFile(inputFilePath, dataModel);

        // Записываем полученную скомпилированную HTML строку в результирующий файл
        File.WriteAllText(outputFilePath, renderedHtml);

        // Возвращаем результат выполнения согласно контракту интерфейса
        return renderedHtml;
    }
}
