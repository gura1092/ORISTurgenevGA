using MiniHttpServer.Framework.Attributes;

namespace MiniHttpServer.Controllers;

[Controller]
public sealed class TestController
{
    [Get]
    public string Index() => "Hello from controller!";

    [Get]
    public string Hello() => "Hello World!";

    [Get]
    public string Details(int id, string name) =>
        $"ID: {id}, Name: {name}";

    [Post]
    public string Submit(string demoUser, string message) =>
        $"POST request processed. User: {demoUser}; Message: {message}";
}
