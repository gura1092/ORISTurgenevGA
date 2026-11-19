using System.Net;
using System.Reflection;
using MiniHttpServer.Controllers;
using MiniHttpServer.Framework;
using MiniHttpServer.Framework.Configuration;
using MiniHttpServer.Framework.Handlers;
using MiniHttpServer.Services;

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

            IEmailService emailService = new EmailService(settings.Email);

            var staticFilesHandler = new StaticFilesHandler(staticRoot);
            var controllersHandler = new ControllersHandler(
                Assembly.GetExecutingAssembly(),
                controllerType => controllerType == typeof(AuthController)
                    ? new AuthController(emailService)
                    : Activator.CreateInstance(controllerType)
                        ?? throw new InvalidOperationException($"Не удалось создать {controllerType.Name}."));

            var notFoundHandler = new NotFoundHandler(staticRoot);

            staticFilesHandler.SetNext(controllersHandler).SetNext(notFoundHandler);

            using var server = new HttpServer(prefix, staticFilesHandler);
            using var cancellation = new CancellationTokenSource();

            var stopSignal = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            ConsoleCancelEventHandler cancelHandler = (_, e) =>
            {
                e.Cancel = true;
                stopSignal.TrySetResult(true);
            };
            Console.CancelKeyPress += cancelHandler;

            Console.WriteLine($"Каталог статических файлов: {staticRoot}");
            Console.WriteLine("Маршруты для проверки:");
            Console.WriteLine("  GET  /test/index");
            Console.WriteLine("  GET  /test/info");
            Console.WriteLine("  GET  /test/requestinfo");
            Console.WriteLine("  GET  /test/hello");
            Console.WriteLine("  GET  /test/details?id=15&name=Alex");
            Console.WriteLine("  POST /test/submit");
            Console.WriteLine("  POST /auth/sendemail");
            Console.WriteLine("Нажмите Enter или Ctrl+C для остановки сервера.");

            Task serverTask = server.StartAsync(cancellation.Token);
            Task inputTask = Task.Run(Console.ReadLine);
            await Task.WhenAny(serverTask, inputTask, stopSignal.Task);

            Console.CancelKeyPress -= cancelHandler;
            cancellation.Cancel();
            server.Stop();
            await serverTask;
        }
        catch (HttpListenerException ex)
        {
            Console.WriteLine($"Не удалось запустить HTTP-сервер: {ex.Message}");
            Console.WriteLine("Проверьте, не занят ли порт и разрешён ли доступ к адресу.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка запуска приложения: {ex}");
        }
    }
}
