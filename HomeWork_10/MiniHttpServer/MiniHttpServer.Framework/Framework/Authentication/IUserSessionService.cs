namespace MiniHttpServer.Framework.Authentication;

/// <summary>Контракт управления пользовательскими сеансами.</summary>
public interface IUserSessionService
{
    string CreateSession(int userId);
    int? GetUserId(string? sessionId);
    void RemoveSession(string? sessionId);
}
