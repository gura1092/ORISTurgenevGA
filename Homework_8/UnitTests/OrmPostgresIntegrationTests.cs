using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.ORM;

namespace MiniHttpServer.ORM.UnitTests
{
    public class DbUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    public class DbProduct
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    [TestClass]
    public class OrmPostgresIntegrationTests
    {
        // Укажи тот же порт и пароль, что и в Setting.json
        private const string ConnectionString = "Host=localhost;Port=5433;Database=OrisDb;Username=postgres;Password=postgres";
        private IOrmContext _orm;

        [TestInitialize]
        public void Setup()
        {
            _orm = new OrmContext(ConnectionString);
        }

        // Полный цикл CRUD для таблицы Users + проверка удаления несуществующей записи
        [TestMethod]
        public async Task CrudLifecycle_UsersTable_PerformsInsertSelectUpdateDelete()
        {
            // 1. Insert
            var newUser = new DbUser { Name = "TestUser", Email = "test@oris.ru", Age = 25 };
            int inserted = await _orm.InsertAsync("Users", newUser);
            Assert.AreEqual(1, inserted);

            // 2. GetAll
            var users = await _orm.GetAllAsync<DbUser>("Users");
            var created = users.FirstOrDefault(u => u.Email == "test@oris.ru");
            Assert.IsNotNull(created);

            // 3. Update
            created.Age = 26;
            int updated = await _orm.UpdateAsync("Users", created.Id, created);
            Assert.AreEqual(1, updated);

            // 4. Delete
            int deleted = await _orm.DeleteAsync("Users", created.Id);
            Assert.AreEqual(1, deleted);

            // Проверка удаления уже удалённой (несуществующей) записи
            int deletedAgain = await _orm.DeleteAsync("Users", created.Id);
            Assert.AreEqual(0, deletedAgain);
        }

        // Проверка универсальности CRUD для второй таблицы Products
        [TestMethod]
        public async Task CrudLifecycle_ProductsTable_WorksUniversallyWithSecondModel()
        {
            // 1. Insert
            var newProduct = new DbProduct { Name = "TestMonitor", Price = 15990.50m };
            int inserted = await _orm.InsertAsync("Products", newProduct);
            Assert.AreEqual(1, inserted);

            // 2. GetAll
            var products = await _orm.GetAllAsync<DbProduct>("Products");
            var created = products.FirstOrDefault(p => p.Name == "TestMonitor");
            Assert.IsNotNull(created);

            // 3. Update
            created.Price = 14500.00m;
            int updated = await _orm.UpdateAsync("Products", created.Id, created);
            Assert.AreEqual(1, updated);

            // 4. Delete
            int deleted = await _orm.DeleteAsync("Products", created.Id);
            Assert.AreEqual(1, deleted);
        }
    }
}
