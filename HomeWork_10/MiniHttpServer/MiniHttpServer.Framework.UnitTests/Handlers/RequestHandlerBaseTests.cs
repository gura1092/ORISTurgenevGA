using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Framework.Handlers;
namespace MiniHttpServer.Framework.UnitTests.Handlers;
[TestClass] public class RequestHandlerBaseTests { [TestMethod] public void SetNext_Self_Throws(){var h=new Dummy();Assert.ThrowsException<InvalidOperationException>(()=>h.SetNext(h));} [TestMethod] public void SetNext_Null_Throws(){var h=new Dummy();Assert.ThrowsException<ArgumentNullException>(()=>h.SetNext(null!));} private sealed class Dummy:RequestHandlerBase { public override Task HandleAsync(System.Net.HttpListenerContext context)=>Task.CompletedTask; } }
