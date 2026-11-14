using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Framework;

namespace UnitTests
{
    [TestClass]
    public class MimeTypeResolverTests
    {
        [TestMethod]
        public void GetMimeType_HtmlExtension_ReturnsTextHtml()
        {
            // Arrange
            string fileName = "index.html";

            // Act
            string mime = MimeTypeResolver.GetMimeType(fileName);

            // Assert
            Assert.AreEqual("text/html; charset=utf-8", mime);
        }

        [TestMethod]
        public void GetMimeType_UnknownExtension_ReturnsOctetStream()
        {
            // Arrange
            string fileName = "archive.unknown";

            // Act
            string mime = MimeTypeResolver.GetMimeType(fileName);

            // Assert
            Assert.AreEqual("application/octet-stream", mime);
        }
    }
}
