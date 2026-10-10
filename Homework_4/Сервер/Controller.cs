using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORISFigmaServer
{
    [HttpController]
    internal class Controller
    {
        public void Login(string login, string password)
        {
            Console.WriteLine("\nДАННЫЕ ИЗ ФОРМЫ STEAM:");
            Console.WriteLine($"Логин: {login}");
            Console.WriteLine($"Пароль: {password}\n");
        }
    }
}
