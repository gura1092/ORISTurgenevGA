using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Framework.Results;
namespace MiniHttpServer.Framework.UnitTests.Results;
[TestClass] public class HttpResultTests { [TestMethod] public void HtmlResult_InvalidStatus_Throws(){Assert.ThrowsException<ArgumentOutOfRangeException>(()=>new HtmlResult("",99));} [TestMethod] public void JsonResult_InvalidStatus_Throws(){Assert.ThrowsException<ArgumentOutOfRangeException>(()=>new JsonResult(new{},600));} [TestMethod] public void HtmlResult_ValidStatus_StoresStatus(){var r=new HtmlResult("hi",201);Assert.AreEqual(201,r.StatusCode);} [TestMethod] public void JsonResult_ValidStatus_StoresStatus(){var r=new JsonResult(new {Name="Alex"},202);Assert.AreEqual(202,r.StatusCode);} }
