using Samostaytelnay;
using static Samostaytelnay.MemoryStorage;

class Program
{
    static void Main(string[] args)
    {
        int port = 8080;

        var storage = new InMemoryStorage();
        var logic = new MovieLogic();
        var template = new HtmlTemp();
        var handler = new HttpHandler();

        var server = new HttpServer(port, storage, logic, template, handler);

        Thread server = new Thread(server.Start);
        server.Start();

        Console.ReadLine();

        server.Stop();
    }
}
