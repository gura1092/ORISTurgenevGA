using System;

namespace MiniHttpServer.Framework.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HttpMethodAttribute : Attribute
    {
        public string Method { get; }

        public HttpMethodAttribute(string method)
        {
            Method = method.ToUpperInvariant();
        }
    }

    public class HttpGetAttribute : HttpMethodAttribute
    {
        public HttpGetAttribute() : base("GET") { }
    }

    public class HttpPostAttribute : HttpMethodAttribute
    {
        public HttpPostAttribute() : base("POST") { }
    }
}
