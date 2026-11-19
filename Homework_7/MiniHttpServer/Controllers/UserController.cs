using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;
namespace MiniHttpServer.Controllers;
[Controller]
public sealed class UserController:BaseController
{
    [Get]
    public IHttpResult Profile()=>Page("User/Profile.html",new
    {
        Name="Alex",Age=20,Email="alex@example.com",IsActive=true,
        User=new{Name="Alex",Email="alex@example.com"},
        Skills=new[]{"C#","HTML","HTTP"},
        Projects=new[]{new{Title="HTTP Server",IsCompleted=true},new{Title="HTML-шаблонизатор",IsCompleted=false},new{Title="Учебный проект",IsCompleted=true}}
    });
    [Get]
    public IHttpResult List()=>Page("User/List.html",new{Users=new[]{
        new{Name="Alex",Email="alex@example.com",IsActive=true,Projects=new[]{"HTTP Server","Templating"}},
        new{Name="Maria",Email="maria@example.com",IsActive=false,Projects=new[]{"HTML","CSS"}},
        new{Name="Ivan",Email="ivan@example.com",IsActive=true,Projects=new[]{"C#","ORM"}}
    }});
}
