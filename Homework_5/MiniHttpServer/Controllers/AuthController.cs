using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Services;
using MiniHttpServer.Framework.Routing;

namespace MiniHttpServer.Controllers;

[Controller]
public sealed class AuthController
{
    private readonly IEmailService _emailService;

    public AuthController(IEmailService emailService)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
    }

    [Post]
    public async Task<string> SendEmail(string demoUser, string message)
    {
        demoUser = (demoUser ?? string.Empty).Trim();
        message = (message ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(demoUser))
            throw new ControllerActionException(400, "Поле demoUser обязательно.");

        if (string.IsNullOrWhiteSpace(message))
            throw new ControllerActionException(400, "Поле message обязательно.");

        if (demoUser.Length > 100 || message.Length > 2000)
            throw new ControllerActionException(400, "Слишком большая длина переданных данных.");

        await _emailService.SendAsync(
            "Учебная заявка из формы Steam",
            $"Учебные данные формы:\nПользователь: {demoUser}\nСообщение: {message}");

        return "Тестовое сообщение принято сервером. Проверьте консоль сервера или тестовый SMTP-сервер.";
    }
}
