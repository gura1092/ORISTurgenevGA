using System;
using System.Collections.Specialized;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Framework.Routing;

namespace UnitTests
{
    [TestClass]
    public class RequestParameterBinderTests
    {
        private RequestParameterBinder _binder;

        [TestInitialize]
        public void Setup()
        {
            _binder = new RequestParameterBinder();
        }

        [TestMethod]
        public void ParseFormOrQuery_QueryString_ParsesKeyValues()
        {
            // Arrange
            string query = "?login=student&age=19";

            // Act
            NameValueCollection result = _binder.ParseFormOrQuery(query);

            // Assert
            Assert.AreEqual("student", result["login"]);
            Assert.AreEqual("19", result["age"]);
        }

        [TestMethod]
        public void BindParameters_FormBodyValues_ConvertsToTargetTypes()
        {
            // Arrange
            MethodInfo method = typeof(SampleDemoController).GetMethod(nameof(SampleDemoController.Info));
            var values = _binder.ParseFormOrQuery("id=42");

            // Act
            object[] args = _binder.BindParameters(method, values);

            // Assert
            Assert.AreEqual(1, args.Length);
            Assert.AreEqual(42, args[0]);
        }

        [TestMethod]
        public void BindParameters_InvalidIntFormat_ThrowsFormatException()
        {
            // Arrange
            MethodInfo method = typeof(SampleDemoController).GetMethod(nameof(SampleDemoController.Info));
            var values = _binder.ParseFormOrQuery("id=not_a_number");

            // Act & Assert
            Assert.Throws<FormatException>(() => _binder.BindParameters(method, values));
        }
    }
}
