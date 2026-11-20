using System.Reflection;
using MiniHttpServer.Controllers;
using MiniHttpServer.Framework;
using MiniHttpServer.Framework.Authentication;
using MiniHttpServer.Framework.Configuration;
using MiniHttpServer.Framework.Handlers;
using MiniHttpServer.HtmlTemplating;
using MiniHttpServer.Services;
using MiniHttpServer.Services.Authentication;
using MiniHttpServer.ORM;
using System.Net;

namespace MiniHttpServer;

internal static class Program
{
    private static async Task Main()
    {
        try
        {
            ServerSettings settings = ConfigurationManager.Instance.Settings;
            string prefix = $"http://{settings.Server.Host}:{settings.Server.Port}/";
            string staticRoot = Path.Combine(AppContext.BaseDirectory, "static");
            string templatesRoot = Path.Combine(AppContext.BaseDirectory, "Templates");

            IEmailService emailService = new EmailService(settings.Email);
            IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
            IUserSessionService sessionService = new UserSessionService();
            IOrmContext? orm = settings.UseDatabase
                ? new OrmContext(settings.ConnectionString)
                : null;

            object CreateController(Type type)
            {
                if (type == typeof(AuthController))
                    return new AuthController(emailService);
                if (type == typeof(CookieController))
                    return new CookieController();
                if (type == typeof(UserController))
                    return new UserController(orm);
                if (type == typeof(ProductController))
                    return new ProductController(orm);

                return Activator.CreateInstance(type)
                    ?? throw new InvalidOperationException($"Не удалось создать {type.Name}.");
            }

            var staticHandler = new StaticFilesHandler(staticRoot);
            var controllerHandler = new ControllersHandler(
                Assembly.GetExecutingAssembly(),
                renderer,
                templatesRoot,
                CreateController,
                sessionService);

            staticHandler.SetNext(controllerHandler).SetNext(new NotFoundHandler(staticRoot));

            using var server = new HttpServer(prefix, staticHandler);
            using var cancellation = new CancellationTokenSource();
            var stop = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            ConsoleCancelEventHandler cancel = (_, e) =>
            {
                e.Cancel = true;
                stop.TrySetResult(true);
            };
            Console.CancelKeyPress += cancel;

            Console.WriteLine(
                $"Static: {staticRoot}\nTemplates: {templatesRoot}\n" +
                "GET /Cookie/Set, /Cookie/Get, /Cookie/Delete\n" +
                "GET /Auth/Index; POST /Auth/Login; POST /Auth/Logout\n" +
                "GET /User/Profile (требует входа); GET /User/List; GET /Product/List\n" +
                $"Режим БД: {(settings.UseDatabase ? "включён" : "демо-данные")}\n" +
                "Логин для демонстрации: student / test123\nНажмите Enter или Ctrl+C для остановки.");

            Task serverTask = server.StartAsync(cancellation.Token);
            Task inputTask = Task.Run(Console.ReadLine);
            await Task.WhenAny(serverTask, inputTask, stop.Task);
            Console.CancelKeyPress -= cancel;
            cancellation.Cancel();
            server.Stop();
            await serverTask;
        }
        catch (HttpListenerException ex)
        {
            Console.WriteLine($"Не удалось запустить HTTP-сервер: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка запуска: {ex}");
        }
    }
}
