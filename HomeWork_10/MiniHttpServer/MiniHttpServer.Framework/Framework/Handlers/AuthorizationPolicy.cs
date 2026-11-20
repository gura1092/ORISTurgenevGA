using System.Reflection;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Authentication;

namespace MiniHttpServer.Framework.Handlers;

/// <summary>Общая политика проверки атрибута Authorize.</summary>
internal static class AuthorizationPolicy
{
    internal static bool IsAuthorized(
        Type controllerType,
        MethodInfo action,
        string? sessionId,
        IUserSessionService sessionService)
    {
        ArgumentNullException.ThrowIfNull(controllerType);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(sessionService);

        bool requiresAuthorization =
            controllerType.IsDefined(typeof(AuthorizeAttribute), inherit: true)
            || action.IsDefined(typeof(AuthorizeAttribute), inherit: true);

        return !requiresAuthorization || sessionService.GetUserId(sessionId).HasValue;
    }
}
