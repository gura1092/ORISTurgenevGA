using System;
using System.Linq;
using System.Reflection;
using MiniHttpServer.Framework;
using MiniHttpServer.Framework.Attributes;

namespace MiniHttpServer.Framework.Routing
{
    public class ControllerRouter
    {
        private readonly Assembly _assembly;

        public ControllerRouter(Assembly assembly)
        {
            _assembly = assembly;
        }

        public (Type ControllerType, MethodInfo Method) Resolve(string urlPath, string httpMethod = "GET")
        {
            string cleanPath = (urlPath ?? string.Empty).Trim('/');
            string[] parts = cleanPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
                return (null, null);

            string controllerName = parts[0].Replace("-", "");
            string actionName = parts[1];

            Type controllerType = _assembly.GetTypes()
                .FirstOrDefault(t =>
                    (t.Name.Equals(controllerName, StringComparison.OrdinalIgnoreCase) ||
                     t.Name.Equals(controllerName + "Controller", StringComparison.OrdinalIgnoreCase)) &&
                    Attribute.IsDefined(t, typeof(HttpController)));

            if (controllerType == null)
                return (null, null);

            MethodInfo method = controllerType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    if (!m.Name.Equals(actionName, StringComparison.OrdinalIgnoreCase))
                        return false;

                    var httpAttr = m.GetCustomAttribute<HttpMethodAttribute>();
                    return httpAttr == null || httpAttr.Method.Equals(httpMethod, StringComparison.OrdinalIgnoreCase);
                });

            return (controllerType, method);
        }
    }
}
