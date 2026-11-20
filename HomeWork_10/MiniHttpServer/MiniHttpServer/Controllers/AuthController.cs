using System.Net;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.Services;

namespace MiniHttpServer.Controllers;

[Controller]
public sealed class AuthController : BaseController
{
    private readonly IEmailService _emailService;

    public AuthController(IEmailService emailService)
        => _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));

    [Get]
    public IHttpResult Index() => Html("""
        <!doctype html>
        <html lang="ru"><head><meta charset="utf-8"><title>Вход</title>
        <style>body{font:16px Arial;max-width:420px;margin:40px auto}label{display:block;margin:12px 0}input{padding:8px;width:100%;box-sizing:border-box}button{padding:10px 18px}</style>
        </head><body><h1>Учебный вход</h1>
        <form action="/Auth/Login" method="post">
          <label>Логин <input name="login" required value="student"></label>
          <label>Пароль <input name="password" type="password" required value="test123"></label>
          <button type="submit">Войти</button>
        </form>
        <p>Тестовые данные: student / test123</p>
        <p><a href="/User/Profile">Личный кабинет</a></p>
        <form action="/Auth/Logout" method="post"><button type="submit">Выйти</button></form>
        </body></html>
        """);

    [Post]
    public IHttpResult Login(string login, string password)
    {
        const string testLogin = "student";
        const string testPassword = "test123";

        if (!string.Equals(login, testLogin, StringComparison.Ordinal)
            || !string.Equals(password, testPassword, StringComparison.Ordinal))
        {
            return Json(new { success = false, message = "Неверный логин или пароль" }, 401);
        }

        const int userId = 1;
        string sessionId = SessionService.CreateSession(userId);
        var cookie = new Cookie("sessionId", sessionId, "/")
        {
            HttpOnly = true,
            Secure = Context.Request.IsSecureConnection,
            Expires = DateTime.UtcNow.AddMinutes(30)
        };
        Context.Response.SetCookie(cookie);

        return Json(new { success = true, message = "Вход выполнен", userId });
    }

    [Post]
    public IHttpResult Logout()
    {
        string? sessionId = Context.Request.Cookies["sessionId"]?.Value;
        SessionService.RemoveSession(sessionId);

        var cookie = new Cookie("sessionId", string.Empty, "/")
        {
            HttpOnly = true,
            Secure = Context.Request.IsSecureConnection,
            Expires = DateTime.UtcNow.AddDays(-1)
        };
        Context.Response.SetCookie(cookie);

        return Json(new { success = true, message = "Выход выполнен" });
    }

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
