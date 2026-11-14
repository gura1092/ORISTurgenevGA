using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Framework;
using MiniHttpServer.Framework.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestClass]
    public class HttpResultsTests
    {
        [TestMethod]
        public void JsonResult_BuildJson_SerializesObjectCorrectly()
        {
            // Arrange
            var payload = new { Status = "Ok", Code = 200 };
            var jsonResult = new JsonResult(payload);

            // Act
            string json = jsonResult.BuildJson();

            // Assert
            Assert.AreEqual("{\"Status\":\"Ok\",\"Code\":200}", json);
        }

        [TestMethod]
        public void HtmlResult_Constructor_StoresHtmlContent()
        {
            // Arrange
            string expectedHtml = "<p>Hello</p>";

            // Act
            var htmlResult = new HtmlResult(expectedHtml);

            // Assert
            Assert.AreEqual(expectedHtml, htmlResult.HtmlContent);
        }

        [TestMethod]
        public void PageResult_RenderContent_UsesInjectedTemplateRenderer()
        {
            // Arrange
            var stubRenderer = new StubTemplateRenderer();
            var model = new { User = "Григорий" };
            var pageResult = new PageResult("Templates/UserProfile.html", model, stubRenderer);

            // Act
            string html = pageResult.RenderContent();

            // Assert
            Assert.AreEqual("<h1>Stub Rendered Page</h1>", html);
            Assert.AreEqual("Templates/UserProfile.html", stubRenderer.LastFilePath);
            Assert.AreSame(model, stubRenderer.LastModel);
        }
    }
}
