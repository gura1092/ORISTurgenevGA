using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestClass]
    public class RequestHandlerChainTests
    {
        [TestMethod]
        public void Handle_UnhandledByFirstHandler_DelegatesToNextHandler()
        {
            // Arrange
            var first = new ControllerMockHandler();
            var second = new StaticFileMockHandler();
            first.SetNext(second);
            var context = new RequestContextMock { Path = "/index.html" };

            // Act
            first.Handle(context);

            // Assert
            Assert.IsTrue(context.IsHandled);
            Assert.AreEqual("StaticFileResponse", context.ResponseBody);
        }
    }
}
