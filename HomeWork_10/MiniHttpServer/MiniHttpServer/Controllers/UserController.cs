using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.Models;
using MiniHttpServer.ORM;
namespace MiniHttpServer.Controllers;
[Controller]
public sealed class UserController : BaseController
{
    private readonly IOrmContext? _orm;
    public UserController(IOrmContext? orm = null) => _orm = orm;

    private static readonly IReadOnlyList<User> DemoUsers = new List<User>
    {
        new() { Id = 1, Name = "Alex", Email = "alex@example.com", Age = 20 },
        new() { Id = 2, Name = "Maria", Email = "maria@example.com", Age = 21 },
        new() { Id = 3, Name = "Ivan", Email = "ivan@example.com", Age = 19 }
    };
    [Get]
    [Authorize]
    public IHttpResult Profile() => Page("User/Profile.html", new { Name="Alex", Age=20, Email="alex@example.com", IsActive=true, User=new{Name="Alex",Email="alex@example.com"}, Skills=new[]{"C#","HTML","HTTP"}, Projects=new[]{new{Title="HTTP Server",IsCompleted=true},new{Title="HTML-шаблонизатор",IsCompleted=false},new{Title="Учебный проект",IsCompleted=true}} });
    [Get]
    public async Task<IHttpResult> List()
    {
        IReadOnlyList<User> users = _orm is null
            ? DemoUsers
            : await _orm.GetAllAsync<User>("Users");
        return Page("User/List.html", new { Users = users });
    }
}
