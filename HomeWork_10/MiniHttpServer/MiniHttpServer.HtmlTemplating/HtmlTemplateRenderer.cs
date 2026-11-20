using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
namespace MiniHttpServer.HtmlTemplating;
public sealed class HtmlTemplateRenderer : IHtmlTemplateRenderer
{
    private static readonly Regex Token = new(@"\$(?:if\s*\([^\)]*\)|foreach\s*\([^\)]*\)|else|endif|endfor)", RegexOptions.Compiled|RegexOptions.IgnoreCase);
    private static readonly Regex Variable = new(@"\$\{\s*([A-Za-z_][\w]*(?:\.[A-Za-z_][\w]*)*)\s*\}", RegexOptions.Compiled);
    private static readonly Regex IfPattern = new(@"^\$if\s*\(\s*(.*?)\s*\)$", RegexOptions.Compiled|RegexOptions.IgnoreCase);
    private static readonly Regex ForeachPattern = new(@"^\$foreach\s*\(\s*var\s+([A-Za-z_][\w]*)\s+in\s+([A-Za-z_][\w]*(?:\.[A-Za-z_][\w]*)*)\s*\)$", RegexOptions.Compiled|RegexOptions.IgnoreCase);
    public string RenderFromString(string htmlTemplate, object dataModel)
    {
        ArgumentNullException.ThrowIfNull(htmlTemplate); ArgumentNullException.ThrowIfNull(dataModel);
        int pos=0; string result=RenderUntil(htmlTemplate,new Context(dataModel),ref pos,Array.Empty<string>(),out var stop);
        if(stop!=null) throw new TemplateException($"Неожиданная директива ${stop}."); return result;
    }
    public string RenderFromFile(string filePath, object dataModel)
    { ArgumentException.ThrowIfNullOrWhiteSpace(filePath); if(!File.Exists(filePath)) throw new FileNotFoundException("HTML-шаблон не найден.",filePath); return RenderFromString(File.ReadAllText(filePath,Encoding.UTF8),dataModel); }
    public string RenderToFile(string inputFilePath,string outputFilePath,object dataModel)
    { string result=RenderFromFile(inputFilePath,dataModel); string full=Path.GetFullPath(outputFilePath); Directory.CreateDirectory(Path.GetDirectoryName(full)!); File.WriteAllText(full,result,new UTF8Encoding(false)); return result; }
    private string RenderUntil(string source,Context ctx,ref int pos,string[] stops,out string? stop)
    {
        var output=new StringBuilder(); stop=null;
        while(pos<source.Length)
        {
            Match m=Token.Match(source,pos);
            if(!m.Success){output.Append(RenderText(source[pos..],ctx));pos=source.Length;break;}
            output.Append(RenderText(source[pos..m.Index],ctx)); string token=m.Value;
            string word=token.StartsWith("$if",StringComparison.OrdinalIgnoreCase)?"if":token.StartsWith("$foreach",StringComparison.OrdinalIgnoreCase)?"foreach":token[1..].ToLowerInvariant(); pos=m.Index;
            if(stops.Contains(word,StringComparer.OrdinalIgnoreCase)){stop=word;return output.ToString();}
            if(word=="if")
            {
                Match c=IfPattern.Match(token);if(!c.Success||string.IsNullOrWhiteSpace(c.Groups[1].Value))throw new TemplateException($"Некорректное условие: {token}.");
                pos=m.Index+m.Length;var parts=ExtractIf(source,ref pos);string selected=ToBool(ctx.Resolve(c.Groups[1].Value.Trim()))?parts.TruePart:parts.FalsePart;
                int branchPos=0;output.Append(RenderUntil(selected,ctx,ref branchPos,Array.Empty<string>(),out var nestedStop));if(nestedStop!=null)throw new TemplateException($"Неожиданная директива ${nestedStop}.");
            }
            else if(word=="foreach")
            {
                Match loop=ForeachPattern.Match(token);if(!loop.Success)throw new TemplateException("Некорректный $foreach. Ожидается $foreach(var item in Items).");
                pos=m.Index+m.Length;string body=ExtractFor(source,ref pos);object? val=ctx.Resolve(loop.Groups[2].Value);
                if(val is not IEnumerable collection||val is string)throw new TemplateException($"'{loop.Groups[2].Value}' не является коллекцией.");
                foreach(object? item in collection){int bp=0;output.Append(RenderUntil(body,ctx.With(loop.Groups[1].Value,item),ref bp,Array.Empty<string>(),out var nestedStop));if(nestedStop!=null)throw new TemplateException($"Неожиданная директива ${nestedStop} внутри цикла.");}
            } else throw new TemplateException($"Неожиданная директива ${word}.");
        } return output.ToString();
    }
    private (string TruePart, string FalsePart) ExtractIf(string source, ref int pos)
    {
        int trueStart = pos;
        int elseIndex = -1;
        int falseStart = -1;
        var stack = new List<string> { "if" };

        while (pos < source.Length)
        {
            Match m = Token.Match(source, pos);
            if (!m.Success) break;
            string token = m.Value;
            string word = token.StartsWith("$if", StringComparison.OrdinalIgnoreCase) ? "if"
                : token.StartsWith("$foreach", StringComparison.OrdinalIgnoreCase) ? "foreach"
                : token[1..].ToLowerInvariant();

            if (word == "else" && stack.Count == 1 && stack[0] == "if")
            {
                if (elseIndex >= 0) throw new TemplateException("Повторная директива $else.");
                elseIndex = m.Index;
                falseStart = m.Index + m.Length;
                pos = falseStart;
                continue;
            }

            if (word is "if" or "foreach") stack.Add(word);
            else if (word is "endif" or "endfor")
            {
                string expected = word == "endif" ? "if" : "foreach";
                if (stack.Count == 0 || stack[^1] != expected)
                    throw new TemplateException($"Неправильная вложенность директив ${word}.");
                stack.RemoveAt(stack.Count - 1);
                if (stack.Count == 0)
                {
                    string yes = elseIndex < 0 ? source[trueStart..m.Index] : source[trueStart..elseIndex];
                    string no = elseIndex < 0 ? string.Empty : source[falseStart..m.Index];
                    pos = m.Index + m.Length;
                    return (yes, no);
                }
            }
            pos = m.Index + m.Length;
        }
        throw new TemplateException("Отсутствует закрывающий $endif.");
    }
    private string ExtractFor(string source,ref int pos)
    {
        int bodyStart=pos;var stack=new List<string>{"foreach"};
        while(pos<source.Length){Match m=Token.Match(source,pos);if(!m.Success)break;string t=m.Value;string w=t.StartsWith("$if",StringComparison.OrdinalIgnoreCase)?"if":t.StartsWith("$foreach",StringComparison.OrdinalIgnoreCase)?"foreach":t[1..].ToLowerInvariant();
            if(w is "if" or "foreach")stack.Add(w);
            else if(w is "endif" or "endfor"){string expected=w=="endif"?"if":"foreach";if(stack.Count==0||stack[^1]!=expected)throw new TemplateException($"Неправильная вложенность директив ${w}.");stack.RemoveAt(stack.Count-1);if(stack.Count==0){string body=source[bodyStart..m.Index];pos=m.Index+m.Length;return body;}}
            pos=m.Index+m.Length;
        }throw new TemplateException("Отсутствует закрывающий $endfor.");
    }
    private static string RenderText(string text,Context ctx)=>Variable.Replace(text,m=>System.Net.WebUtility.HtmlEncode(ctx.Resolve(m.Groups[1].Value)?.ToString()??""));
    private static bool ToBool(object? value){if(value is bool b)return b;if(value==null)return false;if(bool.TryParse(value.ToString(),out bool parsed))return parsed;throw new TemplateException("Значение в $if должно быть логическим.");}
    private sealed class Context
    {
        private readonly object model;private readonly Dictionary<string,object?> vars;
        public Context(object model):this(model,new Dictionary<string,object?>(StringComparer.OrdinalIgnoreCase)){}
        private Context(object model,Dictionary<string,object?> vars){this.model=model;this.vars=vars;}
        public Context With(string name,object? value){var copy=new Dictionary<string,object?>(vars,StringComparer.OrdinalIgnoreCase){[name]=value};return new Context(model,copy);}
        public object? Resolve(string path){string[] parts=path.Split('.',StringSplitOptions.RemoveEmptyEntries);if(parts.Length==0)throw new TemplateException("Пустое выражение.");object? cur;int i;if(vars.TryGetValue(parts[0],out cur))i=1;else{cur=model;i=0;}for(;i<parts.Length;i++){if(cur==null)return null;var prop=cur.GetType().GetProperties(BindingFlags.Public|BindingFlags.Instance).FirstOrDefault(p=>p.CanRead&&p.GetIndexParameters().Length==0&&p.Name.Equals(parts[i],StringComparison.OrdinalIgnoreCase));if(prop==null)throw new TemplateException($"Свойство '{parts[i]}' не найдено (выражение '{path}').");cur=prop.GetValue(cur);}return cur;}
    }
}
public sealed class TemplateException:Exception{public TemplateException(string message):base(message){}}
