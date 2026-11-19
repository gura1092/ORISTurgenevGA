namespace MiniHttpServer.Services;

public interface IEmailService
{
    Task SendAsync(string subject, string body, CancellationToken cancellationToken = default);
}
