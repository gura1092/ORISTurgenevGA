using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.HtmlTemplating;

namespace UnitTests
{
    [TestClass]
    public class HtmlTemplateRendererTests
    {
        private IHtmlTemplateRenderer _renderer;

        [TestInitialize]
        public void Setup()
        {
            _renderer = new HtmlTemplateRenderer();
        }

        // a. Подстановка одного свойства
        [TestMethod]
        public void RenderFromString_ValidProperty_ReplacesPlaceholder()
        {
            // Arrange
            string template = "<h1>${Name}</h1>";
            var model = new { Name = "Alex" };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual("<h1>Alex</h1>", result);
        }

        // b. Подстановка нескольких свойств
        [TestMethod]
        public void RenderFromString_MultipleProperties_ReplacesAllPlaceholders()
        {
            // Arrange
            string template = "${Name} (${Group})";
            var model = new { Name = "Григорий", Group = "11-541" };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual("Григорий (11-541)", result);
        }

        // c. Повторная подстановка одного свойства
        [TestMethod]
        public void RenderFromString_RepeatedProperty_ReplacesAllOccurrences()
        {
            // Arrange
            string template = "${Title} - ${Title}";
            var model = new { Title = "Steam" };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual("Steam - Steam", result);
        }

        // d. Обращение к вложенным свойствам
        [TestMethod]
        public void RenderFromString_NestedProperties_ReplacesPlaceholder()
        {
            // Arrange
            string template = "Город: ${User.Address.City}";
            var model = new { User = new { Address = new { City = "Казань" } } };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual("Город: Казань", result);
        }

        // e. Обработка пустого шаблона
        [TestMethod]
        public void RenderFromString_EmptyTemplate_ReturnsEmptyString()
        {
            // Arrange
            string template = "";
            var model = new { Name = "Alex" };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        // f. Условие $if при истинном значении
        [TestMethod]
        public void RenderFromString_TrueCondition_RendersIfBlock()
        {
            // Arrange
            string template = "$if(IsOnline)В сети$endif";
            var model = new { IsOnline = true };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual("В сети", result);
        }

        // g. Условие $if при ложном значении
        [TestMethod]
        public void RenderFromString_FalseCondition_ReturnsEmptyBlock()
        {
            // Arrange
            string template = "$if(IsOnline)В сети$endif";
            var model = new { IsOnline = false };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        // h. Конструкция $if / $else
        [TestMethod]
        public void RenderFromString_IfElseCondition_RendersElseBlockWhenFalse()
        {
            // Arrange
            string template = "$if(IsAdmin)Админ$elseПользователь$endif";
            var model = new { IsAdmin = false };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual("Пользователь", result);
        }

        // i. Цикл $foreach с несколькими элементами
        [TestMethod]
        public void RenderFromString_CollectionWithItems_RendersAllItems()
        {
            // Arrange
            string template = "<ul>$foreach(var item in Items)<li>${item}</li>$endforeach</ul>";
            var model = new { Items = new[] { "A", "B", "C" } };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual("<ul><li>A</li><li>B</li><li>C</li></ul>", result);
        }

        // j. Цикл $foreach с пустой коллекцией
        [TestMethod]
        public void RenderFromString_EmptyCollection_ReturnsNoItems()
        {
            // Arrange
            string template = "<ul>$foreach(var item in Items)<li>${item}</li>$endforeach</ul>";
            var model = new { Items = Array.Empty<string>() };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual("<ul></ul>", result);
        }

        // k. Вложенные условные конструкции
        [TestMethod]
        public void RenderFromString_NestedIfConditions_EvaluatesCorrectly()
        {
            // Arrange
            string template = "$if(IsAuth)$if(IsVip)VIP$elseБазовый$endif$elseГость$endif";
            var model = new { IsAuth = true, IsVip = false };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual("Базовый", result);
        }

        // l. Вложенные циклы
        [TestMethod]
        public void RenderFromString_NestedForeachLoops_RendersMatrix()
        {
            // Arrange
            string template = "$foreach(var row in Rows)[$foreach(var cell in row)${cell}$endforeach]$endforeach";
            var model = new { Rows = new[] { new[] { 1, 2 }, new[] { 3, 4 } } };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual("[12][34]", result);
        }

        // m. Совместная работа $if и $foreach
        [TestMethod]
        public void RenderFromString_IfInsideForeach_RendersConditionalItems()
        {
            // Arrange
            string template = "$foreach(var u in Users)${u.Name}$if(u.IsAdmin)*$endif;$endforeach";
            var model = new
            {
                Users = new[]
                {
                    new { Name = "Иван", IsAdmin = true },
                    new { Name = "Анна", IsAdmin = false }
                }
            };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual("Иван*;Анна;", result);
        }

        // n. RenderFromFile с временным файлом и его удалением
        [TestMethod]
        public void RenderFromFile_ValidTempFile_ReturnsRenderedHtml()
        {
            // Arrange
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "<p>Привет, ${Name}!</p>");
            var model = new { Name = "Григорий" };

            try
            {
                // Act
                string result = _renderer.RenderFromFile(tempFile, model);

                // Assert
                Assert.AreEqual("<p>Привет, Григорий!</p>", result);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        // o. RenderToFile с временными файлами и их удалением
        [TestMethod]
        public void RenderToFile_ValidFiles_SavesAndReturnsRenderedHtml()
        {
            // Arrange
            string inputFile = Path.GetTempFileName();
            string outputFile = Path.GetTempFileName();
            File.WriteAllText(inputFile, "<div>${Title}</div>");
            var model = new { Title = "Отчёт" };

            try
            {
                // Act
                string returned = _renderer.RenderToFile(inputFile, outputFile, model);
                string saved = File.ReadAllText(outputFile);

                // Assert
                Assert.AreEqual("<div>Отчёт</div>", returned);
                Assert.AreEqual("<div>Отчёт</div>", saved);
            }
            finally
            {
                if (File.Exists(inputFile)) File.Delete(inputFile);
                if (File.Exists(outputFile)) File.Delete(outputFile);
            }
        }

        // p. Некорректные конструкции шаблонов (незакрытые теги и отсутствующее свойство)
        [TestMethod]
        public void RenderFromString_MalformedOrMissingProperty_HandlesSafelyWithoutCrash()
        {
            // Arrange
            string template = "Text ${UnknownProp} $if(BrokenTag without endif";
            var model = new { Name = "Test" };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual("Text  $if(BrokenTag without endif", result);
        }
    }
}
