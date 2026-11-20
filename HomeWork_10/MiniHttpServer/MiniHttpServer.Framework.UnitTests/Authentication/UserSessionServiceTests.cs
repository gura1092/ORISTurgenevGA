using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Services.Authentication;

namespace MiniHttpServer.Framework.UnitTests.Authentication;

[TestClass]
public class UserSessionServiceTests
{
    [TestMethod]
    public void CreateSession_ReturnsId_AndMapsToUser()
    {
        var service = new UserSessionService();
        string sessionId = service.CreateSession(42);

        Assert.IsFalse(string.IsNullOrWhiteSpace(sessionId));
        Assert.AreEqual(42, service.GetUserId(sessionId));
    }

    [TestMethod]
    public void GetUserId_MissingOrUnknownSession_ReturnsNull()
    {
        var service = new UserSessionService();

        Assert.IsNull(service.GetUserId(null));
        Assert.IsNull(service.GetUserId(string.Empty));
        Assert.IsNull(service.GetUserId("unknown-session"));
    }

    [TestMethod]
    public void CreateSession_CreatesUniqueIdsForMultipleUsers()
    {
        var service = new UserSessionService();
        var ids = Enumerable.Range(1, 100)
            .Select(service.CreateSession)
            .ToArray();

        Assert.AreEqual(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        for (int userId = 1; userId <= 100; userId++)
            Assert.AreEqual(userId, service.GetUserId(ids[userId - 1]));
    }

    [TestMethod]
    public void RemoveSession_InvalidatesSession()
    {
        var service = new UserSessionService();
        string sessionId = service.CreateSession(7);

        service.RemoveSession(sessionId);

        Assert.IsNull(service.GetUserId(sessionId));
    }

    [TestMethod]
    public void CreateSession_NonPositiveUserId_Throws()
    {
        var service = new UserSessionService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => service.CreateSession(0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => service.CreateSession(-1));
    }

    [TestMethod]
    public void GetUserId_ExpiredSession_ReturnsNull()
    {
        var service = new UserSessionService(TimeSpan.FromMilliseconds(1));
        string sessionId = service.CreateSession(9);
        Thread.Sleep(20);

        Assert.IsNull(service.GetUserId(sessionId));
    }
}
