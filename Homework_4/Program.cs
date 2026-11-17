using MiniHttpServer.Framework;
using MiniHttpServer.Framework.Configuration;
using MiniHttpServer.Framework.Handlers;

namespace MiniHttpServer;

internal static class Program
{
    private static async Task Main()
    {
        try
        {
            Settings settings = ConfigurationManager.Instance.Settings;
            string prefix = $"http://{settings.Server.Host}:{settings.Server.Port}/";
            string staticRoot = Path.Combine(AppContext.BaseDirectory, "static");

            var staticFilesHandler = new StaticFilesHandler(staticRoot);
            var notFoundHandler = new NotFoundHandler(staticRoot);
            staticFilesHandler.SetNext(notFoundHandler);

            using var server = new HttpServer(prefix, staticFilesHandler);
            using var cancellation = new CancellationTokenSource();

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cancellation.Cancel();
            };

            Task serverTask = server.StartAsync(cancellation.Token);
            Console.WriteLine($"Каталог статических файлов: {staticRoot}");
            Console.WriteLine("Нажмите Enter для остановки сервера...");
            Console.ReadLine();

            cancellation.Cancel();
            server.Stop();
            await serverTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка запуска: {ex.Message}");
        }
    }
}
