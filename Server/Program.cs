using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
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
            HttpServerFigm server = new HttpServerFigm($"http://{setting.Server.Host}:{setting.Server.Port}/", "C:\\Users\\user\\Desktop\\Орис\\Untitled-1.html");
            Task serverTask = Task.Run(() => server.Start());
            Console.WriteLine($"Адрес: http://{setting.Server.Host}:{setting.Server.Port}:/");
            Console.WriteLine("Нажмите Enter для остановки сервера...");
            Console.ReadLine();
            server.Stop();
        }
    }
}
