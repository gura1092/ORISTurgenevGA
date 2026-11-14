using System;

namespace MiniHttpServer.HtmlTemplating
{
    /// <summary>
    /// Определяет контракт HTML-шаблонизатора для формирования HTML-разметки
    /// на основе шаблона и переданной модели данных.
    /// </summary>
    /// <remarks>
    /// Поддерживает подстановку значений свойств модели (${Property}),
    /// условные конструкции ($if, $else, $endif) и циклы ($foreach, $endforeach / $endfor).
    /// </remarks>
    public interface IHtmlTemplateRenderer
    {
        /// <summary>
        /// Выполняет обработку HTML-шаблона, переданного в виде строки.
        /// </summary>
        string RenderFromString(string htmlTemplate, object dataModel);

        /// <summary>
        /// Выполняет обработку HTML-шаблона, загруженного из файла.
        /// </summary>
        string RenderFromFile(string filePath, object dataModel);

        /// <summary>
        /// Выполняет обработку HTML-шаблона из файла и сохраняет результат в указанный файл.
        /// </summary>
        string RenderToFile(string inputFilePath, string outputFilePath, object dataModel);
    }
}
