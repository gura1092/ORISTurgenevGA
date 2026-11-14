using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MiniHttpServer.Framework;
using MiniHttpServer.ORM;
using ORISFigmaServer.Models;

namespace ORISFigmaServer
{
    [HttpController]
    public class UserController : BaseController
    {
        // Загрузка пользователей из PostgreSQL через ORM
        public async Task<IHttpResult> List()
        {
            string json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Setting.json"));
            var settings = JsonSerializer.Deserialize<Settings>(json);

            IOrmContext orm = new OrmContext(settings.Server.ConnectionString);
            var users = await orm.GetAllAsync<User>("Users");

            string templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "UserList.html");
            return Page(templatePath, new { Users = users });
        }

        public IHttpResult Profile()
        {
            var userModel = new
            {
                Name = "Григорий",
                Age = 18,
                IsOnline = true,
                Address = new { City = "Казань", Group = "11-541" },
                Skills = new[]
                {
                    new { Title = "C# / .NET", IsPrimary = true },
                    new { Title = "HTML / CSS", IsPrimary = false }
                }
            };

            string templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "UserProfile.html");
            return Page(templatePath, userModel);
        }
    }
}
