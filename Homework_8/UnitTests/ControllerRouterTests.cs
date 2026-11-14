using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Framework.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestClass]
    public class ControllerRouterTests
    {
        private ControllerRouter _router;

        [TestInitialize]
        public void Setup()
        {
            _router = new ControllerRouter(Assembly.GetExecutingAssembly());
        }

        [TestMethod]
        public void Resolve_ExistingControllerAndMethod_ReturnsTargetTypeAndMethod()
        {
            // Arrange
            string url = "/sample-demo/info";

            // Act
            var (controllerType, method) = _router.Resolve(url, "GET");

            // Assert
            Assert.AreEqual(typeof(SampleDemoController), controllerType);
            Assert.IsNotNull(method);
            Assert.AreEqual("Info", method.Name);
        }

        [TestMethod]
        public void Resolve_WrongHttpMethodAttribute_ReturnsNullMethod()
        {
            // Arrange
            string url = "/sampledemo/submit";

            // Act
            var (controllerType, method) = _router.Resolve(url, "GET");

            // Assert
            Assert.AreEqual(typeof(SampleDemoController), controllerType);
            Assert.IsNull(method);
        }

        [TestMethod]
        public void Resolve_UnknownController_ReturnsNulls()
        {
            // Arrange
            string url = "/nonexistent/action";

            // Act
            var (controllerType, method) = _router.Resolve(url, "GET");

            // Assert
            Assert.IsNull(controllerType);
            Assert.IsNull(method);
        }
    }
}
