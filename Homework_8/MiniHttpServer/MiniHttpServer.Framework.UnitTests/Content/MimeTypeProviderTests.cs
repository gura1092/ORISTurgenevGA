using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Framework.Content;
namespace MiniHttpServer.Framework.UnitTests.Content;
[TestClass] public class MimeTypeProviderTests { [DataTestMethod][DataRow(".html","text/html; charset=utf-8")][DataRow(".css","text/css; charset=utf-8")][DataRow(".js","text/javascript; charset=utf-8")][DataRow(".json","application/json; charset=utf-8")][DataRow(".png","image/png")][DataRow(".unknown","application/octet-stream")] public void GetMimeType_KnownOrUnknownExtension_ReturnsExpected(string ext,string expected){Assert.AreEqual(expected,MimeTypeProvider.GetMimeType(ext));} }
