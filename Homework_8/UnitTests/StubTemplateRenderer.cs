using MiniHttpServer.HtmlTemplating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    // Поддельный (Stub) шаблонизатор для изоляции PageResult от файловой системы
    public class StubTemplateRenderer : IHtmlTemplateRenderer
    {
        public string LastFilePath { get; private set; }
        public object LastModel { get; private set; }

        public string RenderFromFile(string filePath, object dataModel)
        {
            LastFilePath = filePath;
            LastModel = dataModel;
            return "<h1>Stub Rendered Page</h1>";
        }

        public string RenderFromString(string htmlTemplate, object dataModel) => htmlTemplate;
        public string RenderToFile(string inputFilePath, string outputFilePath, object dataModel) => string.Empty;
    }
}
