using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Handlers;
using MiniHttpServer.Framework.Handlers;
using MiniHttpServer.Services.Authentication;

namespace MiniHttpServer.Framework.UnitTests.Authentication;

[TestClass]
public class AuthorizationPolicyTests
{
    [TestMethod]
    public void ActionWithoutAuthorize_IsAllowedWithoutSession()
    {
        MethodInfo action = typeof(PublicController).GetMethod(nameof(PublicController.Index))!;
        var sessions = new UserSessionService();

        Assert.IsTrue(AuthorizationPolicy.IsAuthorized(typeof(PublicController), action, null, sessions));
    }

    [TestMethod]
    public void ActionWithAuthorize_IsDeniedWithoutSession()
    {
        MethodInfo action = typeof(ProtectedController).GetMethod(nameof(ProtectedController.Profile))!;
        var sessions = new UserSessionService();

        Assert.IsFalse(AuthorizationPolicy.IsAuthorized(typeof(ProtectedController), action, null, sessions));
    }

    [TestMethod]
    public void ActionWithAuthorize_IsAllowedWithValidSession()
    {
        MethodInfo action = typeof(ProtectedController).GetMethod(nameof(ProtectedController.Profile))!;
        var sessions = new UserSessionService();
        string sessionId = sessions.CreateSession(15);

        Assert.IsTrue(AuthorizationPolicy.IsAuthorized(typeof(ProtectedController), action, sessionId, sessions));
    }

    [TestMethod]
    public void ControllerLevelAuthorize_ProtectsItsActions()
    {
        MethodInfo action = typeof(ControllerProtected).GetMethod(nameof(ControllerProtected.Index))!;
        var sessions = new UserSessionService();

        Assert.IsFalse(AuthorizationPolicy.IsAuthorized(typeof(ControllerProtected), action, null, sessions));
    }

    private sealed class PublicController
    {
        public void Index() { }
    }

    private sealed class ProtectedController
    {
        [Authorize]
        public void Profile() { }
    }

    [Authorize]
    private sealed class ControllerProtected
    {
        public void Index() { }
    }
}
