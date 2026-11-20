using System.Net;
using System.Reflection;
using MiniHttpServer.Controllers;
using MiniHttpServer.Framework;
using MiniHttpServer.Framework.Configuration;
using MiniHttpServer.Framework.Handlers;
using MiniHttpServer.HtmlTemplating;
using MiniHttpServer.Services;
using MiniHttpServer.ORM;
using Npgsql;
namespace MiniHttpServer;
internal static class Program
{
    private static async Task Main()
    {
        try
        {
            ServerSettings settings=ConfigurationManager.Instance.Settings;
            string prefix=$"http://{settings.Server.Host}:{settings.Server.Port}/";
            string staticRoot=Path.Combine(AppContext.BaseDirectory,"static"),templatesRoot=Path.Combine(AppContext.BaseDirectory,"Templates");
            IEmailService emailService=new EmailService(settings.Email);IHtmlTemplateRenderer renderer=new HtmlTemplateRenderer();
            IOrmContext? orm = null;
            var staticHandler=new StaticFilesHandler(staticRoot);
            var controllerHandler=new ControllersHandler(Assembly.GetExecutingAssembly(),renderer,templatesRoot,
                type=>type==typeof(AuthController)?new AuthController(emailService):type==typeof(UserController)?new UserController(orm):type==typeof(ProductController)?new ProductController(orm):Activator.CreateInstance(type)??throw new InvalidOperationException($"Не удалось создать {type.Name}."));
            staticHandler.SetNext(controllerHandler).SetNext(new NotFoundHandler(staticRoot));
            using var server=new HttpServer(prefix,staticHandler);using var cancellation=new CancellationTokenSource();
            var stop=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            ConsoleCancelEventHandler cancel=(_,e)=>{e.Cancel=true;stop.TrySetResult(true);};Console.CancelKeyPress+=cancel;
            Console.WriteLine($"Static: {staticRoot}\nTemplates: {templatesRoot}\nGET /user/profile\nGET /user/list\nРежим БД: {(settings.UseDatabase ? "включён" : "выключен (демо-данные)")}\nНажмите Enter или Ctrl+C для остановки.");
            Task serverTask=server.StartAsync(cancellation.Token),inputTask=Task.Run(Console.ReadLine);await Task.WhenAny(serverTask,inputTask,stop.Task);Console.CancelKeyPress-=cancel;cancellation.Cancel();server.Stop();await serverTask;
        }
        catch(HttpListenerException ex){Console.WriteLine($"Не удалось запустить HTTP-сервер: {ex.Message}");}
        catch(Exception ex){Console.WriteLine($"Ошибка запуска: {ex}");}
    }
}
