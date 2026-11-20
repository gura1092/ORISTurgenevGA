namespace MiniHttpServer.Framework.Attributes;

/// <summary>Ограничивает выполнение контроллера или Action действующим сеансом.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class AuthorizeAttribute : Attribute
{
}
