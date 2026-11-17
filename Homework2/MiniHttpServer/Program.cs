using Microsoft.VisualBasic;
using System;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiniHttpServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string settings = File.ReadAllText("Setting.json");
            ServerSettings setting = JsonSerializer.Deserialize<ServerSettings>(settings);

            HttpServerFigm server = new HttpServerFigm($"http://{setting.Server.Host}:{setting.Server.Port}/", "C:\\Users\\Gregory\\Desktop\\Homework_2\\MiniHttpServer\\FigmaSearch.html");

            Task serverTask = Task.Run(() => server.Start());

            Console.WriteLine($"Адрес: http://{setting.Server.Host}:{setting.Server.Port}/");
            Console.WriteLine("Для остановки сервера введите команду 'stop':");

            while (true)
            {
                string command = Console.ReadLine();
                if (command?.Trim().ToLower() == "stop")
                {
                    server.Stop();
                    break;
                }
            }
            serverTask.Wait();
        }
    }
}
