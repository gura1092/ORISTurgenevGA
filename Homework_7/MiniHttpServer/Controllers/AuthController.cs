using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.Framework.Routing;
using MiniHttpServer.Services;

namespace MiniHttpServer.Controllers;

[Controller]
public sealed class AuthController : BaseController
{
    private readonly IEmailService _emailService;
    public AuthController(IEmailService emailService) => _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));

    [Post]
    public async Task<IHttpResult> SendEmail(string demoUser = "", string message = "")
    {
        demoUser = (demoUser ?? string.Empty).Trim();
        message = (message ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(demoUser))
            return Json(new { success = false, message = "Поле demoUser обязательно." }, 400);
        if (string.IsNullOrWhiteSpace(message))
            return Json(new { success = false, message = "Поле message обязательно." }, 400);
        if (demoUser.Length > 100 || message.Length > 2000)
            return Json(new { success = false, message = "Слишком большая длина переданных данных." }, 400);

        try
        {
            await _emailService.SendAsync("Учебная заявка из формы", $"Учебные данные формы:\nПользователь: {demoUser}\nСообщение: {message}");
            return Json(new { success = true, message = "Сообщение отправлено" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EMAIL ERROR] {ex.GetType().Name}: {ex.Message}");
            return Json(new { success = false, message = "Не удалось отправить сообщение" }, 500);
        }
    }
}
