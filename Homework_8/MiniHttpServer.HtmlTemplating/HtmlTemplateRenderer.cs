using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniHttpServer.HtmlTemplating
{
    public class HtmlTemplateRenderer : IHtmlTemplateRenderer
    {
        public string RenderFromString(string htmlTemplate, object dataModel)
        {
            if (string.IsNullOrEmpty(htmlTemplate))
                return string.Empty;

            // Формируем словарь доступных переменных из свойств модели
            var context = BuildContext(dataModel);
            return ProcessTemplate(htmlTemplate, context);
        }

        public string RenderFromFile(string filePath, object dataModel)
        {
            string template = File.ReadAllText(filePath, Encoding.UTF8);
            return RenderFromString(template, dataModel);
        }

        public string RenderToFile(string inputFilePath, string outputFilePath, object dataModel)
        {
            string result = RenderFromFile(inputFilePath, dataModel);
            File.WriteAllText(outputFilePath, result, Encoding.UTF8);
            return result;
        }

        // Рекурсивная обработка циклов, условий и переменных
        private string ProcessTemplate(string template, Dictionary<string, object> context)
        {
            template = ProcessForeach(template, context);
            template = ProcessIf(template, context);
            template = ProcessVariables(template, context);
            return template;
        }

        // Обработка конструкций $foreach(var item in Collection) ... $endforeach
        private string ProcessForeach(string template, Dictionary<string, object> context)
        {
            const string startTag = "$foreach(";
            int startIndex = template.IndexOf(startTag, StringComparison.Ordinal);

            while (startIndex != -1)
            {
                int headerEnd = template.IndexOf(')', startIndex + startTag.Length);
                if (headerEnd == -1) break;

                // Разбираем заголовок цикла: "var item in Items" или "item in Items"
                string header = template.Substring(startIndex + startTag.Length, headerEnd - (startIndex + startTag.Length)).Trim();
                var match = Regex.Match(header, @"^(?:var\s+)?(\w+)\s+in\s+([\w\.]+)$");
                if (!match.Success) break;

                string itemVarName = match.Groups[1].Value;
                string collectionPath = match.Groups[2].Value;

                // Ищем парный закрывающий тег с учетом вложенных циклов
                var (endIndex, endTagLength) = FindMatchingEndForeach(template, headerEnd + 1);
                if (endIndex == -1) break;

                string body = template.Substring(headerEnd + 1, endIndex - (headerEnd + 1));
                var sb = new StringBuilder();

                object collectionObj = ResolveValue(collectionPath, context);
                if (collectionObj is IEnumerable enumerable && !(collectionObj is string))
                {
                    foreach (var item in enumerable)
                    {
                        // Создаём локальный контекст итерации
                        var loopContext = new Dictionary<string, object>(context)
                        {
                            [itemVarName] = item
                        };
                        sb.Append(ProcessTemplate(body, loopContext));
                    }
                }

                template = template.Remove(startIndex, (endIndex + endTagLength) - startIndex)
                                   .Insert(startIndex, sb.ToString());

                startIndex = template.IndexOf(startTag, startIndex + sb.Length, StringComparison.Ordinal);
            }

            return template;
        }


        // Поиск закрывающего $endforeach или $endfor с подсчётом вложенности
        private (int index, int length) FindMatchingEndForeach(string text, int searchFrom)
        {
            int depth = 1;
            int pos = searchFrom;

            while (pos < text.Length)
            {
                int nextOpen = text.IndexOf("$foreach(", pos, StringComparison.Ordinal);
                int nextClose1 = text.IndexOf("$endforeach", pos, StringComparison.Ordinal);
                int nextClose2 = text.IndexOf("$endfor", pos, StringComparison.Ordinal);

                int nextClose = -1;
                int closeLen = 0;

                if (nextClose1 != -1 && (nextClose2 == -1 || nextClose1 <= nextClose2))
                {
                    nextClose = nextClose1;
                    closeLen = "$endforeach".Length;
                }
                else if (nextClose2 != -1)
                {
                    nextClose = nextClose2;
                    closeLen = "$endfor".Length;
                }

                if (nextClose == -1) return (-1, 0);

                if (nextOpen != -1 && nextOpen < nextClose)
                {
                    depth++;
                    pos = nextOpen + "$foreach(".Length;
                }
                else
                {
                    depth--;
                    if (depth == 0) return (nextClose, closeLen);
                    pos = nextClose + closeLen;
                }
            }

            return (-1, 0);
        }

        // Обработка конструкций $if(Condition) ... $else ... $endif
        private string ProcessIf(string template, Dictionary<string, object> context)
        {
            const string startTag = "$if(";
            int startIndex = template.IndexOf(startTag, StringComparison.Ordinal);

            while (startIndex != -1)
            {
                int headerEnd = template.IndexOf(')', startIndex + startTag.Length);
                if (headerEnd == -1) break;

                string conditionExpr = template.Substring(startIndex + startTag.Length, headerEnd - (startIndex + startTag.Length)).Trim();
                var (elseIndex, endIndex) = FindMatchingIfParts(template, headerEnd + 1);
                if (endIndex == -1) break;

                string trueBlock;
                string falseBlock = string.Empty;

                if (elseIndex != -1)
                {
                    trueBlock = template.Substring(headerEnd + 1, elseIndex - (headerEnd + 1));
                    falseBlock = template.Substring(elseIndex + "$else".Length, endIndex - (elseIndex + "$else".Length));
                }
                else
                {
                    trueBlock = template.Substring(headerEnd + 1, endIndex - (headerEnd + 1));
                }

                bool conditionResult = EvaluateCondition(conditionExpr, context);
                string chosenBlock = conditionResult ? trueBlock : falseBlock;
                string renderedBlock = ProcessTemplate(chosenBlock, context);

                template = template.Remove(startIndex, (endIndex + "$endif".Length) - startIndex)
                                   .Insert(startIndex, renderedBlock);

                startIndex = template.IndexOf(startTag, startIndex + renderedBlock.Length, StringComparison.Ordinal);
            }

            return template;
        }

        // Поиск $else (на текущем уровне) и парного $endif
        private (int elseIndex, int endIndex) FindMatchingIfParts(string text, int searchFrom)
        {
            int depth = 1;
            int pos = searchFrom;
            int foundElse = -1;

            while (pos < text.Length)
            {
                int nextOpen = text.IndexOf("$if(", pos, StringComparison.Ordinal);
                int nextElse = text.IndexOf("$else", pos, StringComparison.Ordinal);
                int nextClose = text.IndexOf("$endif", pos, StringComparison.Ordinal);

                if (nextClose == -1) return (-1, -1);

                // Определяем ближайший тег
                int minPos = nextClose;
                if (nextOpen != -1 && nextOpen < minPos) minPos = nextOpen;
                if (nextElse != -1 && nextElse < minPos) minPos = nextElse;

                if (minPos == nextOpen)
                {
                    depth++;
                    pos = nextOpen + "$if(".Length;
                }
                else if (minPos == nextElse)
                {
                    if (depth == 1 && foundElse == -1)
                        foundElse = nextElse;
                    pos = nextElse + "$else".Length;
                }
                else
                {
                    depth--;
                    if (depth == 0) return (foundElse, nextClose);
                    pos = nextClose + "$endif".Length;
                }
            }

            return (-1, -1);
        }

        // Вычисление логического условия (совместимо с C# 7.3)
        private bool EvaluateCondition(string expr, Dictionary<string, object> context)
        {
            bool negate = false;
            if (expr.StartsWith("!"))
            {
                negate = true;
                expr = expr.Substring(1).Trim();
            }

            object value = ResolveValue(expr, context);
            bool result;

            if (value == null)
                result = false;
            else if (value is bool b)
                result = b;
            else if (value is string s)
                result = !string.IsNullOrEmpty(s);
            else if (value is int i)
                result = i != 0;
            else
                result = true;

            return negate ? !result : result;
        }


        // Подстановка переменных вида ${Property} и ${User.Name}
        private string ProcessVariables(string template, Dictionary<string, object> context)
        {
            return Regex.Replace(template, @"\$\{\s*([\w\.]+)\s*\}", match =>
            {
                string path = match.Groups[1].Value;
                object val = ResolveValue(path, context);
                return val?.ToString() ?? string.Empty;
            });
        }

        // Преобразование корневой модели в словарь свойств
        private Dictionary<string, object> BuildContext(object dataModel)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (dataModel == null) return dict;
            if (dataModel is IDictionary<string, object> existingDict)
                return new Dictionary<string, object>(existingDict, StringComparer.OrdinalIgnoreCase);
            foreach (PropertyInfo prop in dataModel.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                dict[prop.Name] = prop.GetValue(dataModel);
            }
            return dict;
        }
        // Получение значения по цепочке свойств через Reflection
        private object ResolveValue(string path, Dictionary<string, object> context)
        {
            string[] parts = path.Split('.');
            if (!context.TryGetValue(parts[0], out object current) || current == null)
                return null;

            for (int i = 1; i < parts.Length; i++)
            {
                if (current == null) return null;
                PropertyInfo prop = current.GetType().GetProperty(
                    parts[i],
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (prop == null) return null;
                current = prop.GetValue(current);
            }

            return current;
        }

    }
}