using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ORISFigmaServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string settings = File.ReadAllText("Setting.json");
            Settings setting = JsonSerializer.Deserialize<Settings>(settings);
            HttpServer server = new HttpServer($"http://{setting.Server.Host}:{setting.Server.Port}/", "C:\\Users\\user\\Desktop\\Орис\\Steam.html");
            Task.Run(() => server.Start());
            Console.WriteLine($"Адрес: http://{setting.Server.Host}:{setting.Server.Port}/");
            Console.WriteLine("Нажмите Enter для остановки сервера...");
            Console.ReadLine();
            server.Stop();
        }
    }
}
