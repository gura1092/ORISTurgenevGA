using System.Net;
using System.Net.Mail;
using MiniHttpServer.Framework.Configuration;

namespace MiniHttpServer.Services;

internal sealed class EmailService : IEmailService
{
    private readonly EmailConfig _settings;

    internal EmailService(EmailConfig settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task SendAsync(
        string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Тема письма не может быть пустой.", nameof(subject));
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Текст письма не может быть пустым.", nameof(body));

        if (!_settings.UseSmtp)
        {
            Console.WriteLine();
            Console.WriteLine("========== УЧЕБНОЕ EMAIL-СООБЩЕНИЕ ==========");
            Console.WriteLine($"Тема: {subject}");
            Console.WriteLine(body);
            Console.WriteLine("Режим: безопасный тестовый вывод. Внешнее письмо не отправлялось.");
            Console.WriteLine("=============================================");
            Console.WriteLine();
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.Host) ||
            string.IsNullOrWhiteSpace(_settings.From) ||
            string.IsNullOrWhiteSpace(_settings.To))
            throw new InvalidOperationException("Проверьте Email.Host, Email.From и Email.To в settings.json.");

        using var mail = new MailMessage(_settings.From, _settings.To, subject, body);
        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 10000
        };

        string? username = Environment.GetEnvironmentVariable("MINIHTTPSERVER_SMTP_USER");
        string? password = Environment.GetEnvironmentVariable("MINIHTTPSERVER_SMTP_PASSWORD");

        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, password ?? string.Empty);

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(mail).WaitAsync(cancellationToken);
    }
}
