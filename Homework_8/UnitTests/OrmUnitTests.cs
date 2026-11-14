using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.ORM;

namespace MiniHttpServer.ORM.UnitTests
{
    public class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public class TestProduct
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
    }

    [TestClass]
    public class OrmUnitTests
    {
        private SqlQueryBuilder _builder;
        private EntityMapper _mapper;

        [TestInitialize]
        public void Setup()
        {
            _builder = new SqlQueryBuilder();
            _mapper = new EntityMapper();
        }

        // 1. Проверка допустимых имён таблиц
        [TestMethod]
        public void EscapeIdentifier_ValidName_ReturnsQuotedIdentifier()
        {
            // Arrange & Act
            string result = _builder.EscapeIdentifier("Users_2026");

            // Assert
            Assert.AreEqual("\"Users_2026\"", result);
        }

        // 2. Проверка недопустимых SQL-идентификаторов
        [TestMethod]
        public void EscapeIdentifier_SqlInjectionAttempt_ThrowsArgumentException()
        {
            // Arrange
            string malicious = "Users; DROP TABLE Users;--";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _builder.EscapeIdentifier(malicious));
        }

        // 3. Сопоставление столбцов с соответствующими свойствами
        [TestMethod]
        public void MapRow_DataTableReader_MapsColumnsToModelProperties()
        {
            // Arrange
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Age", typeof(int));
            table.Rows.Add(5, "Oleg", 22);

            using (DbDataReader reader = table.CreateDataReader())
            {
                reader.Read();
                var cols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Id"] = 0,
                    ["Name"] = 1,
                    ["Age"] = 2
                };

                // Act
                var user = _mapper.MapRow<TestUser>(reader, cols);

                // Assert
                Assert.AreEqual(5, user.Id);
                Assert.AreEqual("Oleg", user.Name);
                Assert.AreEqual(22, user.Age);
            }
        }

        // 4. Преобразование SQL-значений в типы C#
        [TestMethod]
        public void ConvertValue_StringNumberToIntAndDecimal_ConvertsCorrectly()
        {
            // Arrange & Act
            object intVal = _mapper.ConvertValue("42", typeof(int));
            object decVal = _mapper.ConvertValue("199.99", typeof(decimal));

            // Assert
            Assert.AreEqual(42, intVal);
            Assert.AreEqual(199.99m, decVal);
        }

        // 5. Формирование команды INSERT (без Id)
        [TestMethod]
        public void BuildInsert_ValidEntity_GeneratesParameterizedInsertWithoutId()
        {
            // Arrange
            var user = new TestUser { Id = 99, Name = "Oleg", Age = 22 };

            // Act
            var (sql, parameters) = _builder.BuildInsert("Users", user);

            // Assert
            Assert.AreEqual("INSERT INTO \"Users\" (\"Name\", \"Age\") VALUES (@Name, @Age)", sql);
            Assert.IsFalse(parameters.ContainsKey("@Id"));
            Assert.AreEqual("Oleg", parameters["@Name"]);
            Assert.AreEqual(22, parameters["@Age"]);
        }

        // 6 и 8. Формирование команды UPDATE и отсутствие изменения первичного ключа Id
        [TestMethod]
        public void BuildUpdate_ValidEntity_GeneratesUpdateWithoutChangingId()
        {
            // Arrange
            var user = new TestUser { Id = 777, Name = "NewOleg", Age = 23 };

            // Act
            var (sql, parameters) = _builder.BuildUpdate("Users", 1, user);

            // Assert
            Assert.AreEqual("UPDATE \"Users\" SET \"Name\" = @Name, \"Age\" = @Age WHERE \"Id\" = @Id", sql);
            Assert.AreEqual(1, parameters["@Id"]);
            Assert.AreEqual("NewOleg", parameters["@Name"]);
        }

        // 7. Формирование команды DELETE
        [TestMethod]
        public void BuildDelete_ValidId_GeneratesParameterizedDelete()
        {
            // Arrange & Act
            var (sql, parameters) = _builder.BuildDelete("Users", 10);

            // Assert
            Assert.AreEqual("DELETE FROM \"Users\" WHERE \"Id\" = @Id", sql);
            Assert.AreEqual(10, parameters["@Id"]);
        }

        // 9. Обработка пустых и некорректных входных данных
        [TestMethod]
        public void BuildInsert_NullEntity_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => _builder.BuildInsert<TestUser>("Users", null));
        }

        // 10. Работа с несколькими типами моделей
        [TestMethod]
        public void BuildInsert_DifferentModels_WorksUniversally()
        {
            // Arrange
            var product = new TestProduct { Title = "Laptop", Price = 75000m };

            // Act
            var (sql, parameters) = _builder.BuildInsert("Products", product);

            // Assert
            Assert.AreEqual("INSERT INTO \"Products\" (\"Title\", \"Price\") VALUES (@Title, @Price)", sql);
            Assert.AreEqual("Laptop", parameters["@Title"]);
            Assert.AreEqual(75000m, parameters["@Price"]);
        }
    }
}
