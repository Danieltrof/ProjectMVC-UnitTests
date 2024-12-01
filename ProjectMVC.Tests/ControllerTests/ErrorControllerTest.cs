using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProjectMVC.Controllers;
using System;

namespace ProjectMVC.Tests.ControllerTests
{
    [TestClass]
    public class ErrorControllerTest
    {
        //Positive Test for Error View
        [TestMethod]
        public void Error_ReturnsViewWithErrorMessage()
        {
            // Arrange
            var controller = new ErrorController();

            // Act
            var result = controller.Error() as ViewResult;

            // Assert
            Assert.IsNotNull(result);  // Ensure the result is a ViewResult
            Assert.AreEqual(string.Empty, result.ViewName);  // By default, no specific view name is set (empty string)
            Assert.IsNotNull(controller.ModelState[""].Errors);  // Ensure ModelState contains an error message
            Assert.AreEqual("An unexpected error occurred Please try again later", controller.ModelState[""].Errors[0].ErrorMessage);  // Ensure correct error message is set
        }

        //Negative Test for Error View - No Error Message
        [TestMethod]
        public void Error_NoErrorMessage_SetsDefaultMessage()
        {
            var controller = new ErrorController();
            
            // Manually set ViewBag.ErrorMessage to null (simulate error condition)
            controller.ViewBag.ErrorMessage = null;

            var result = controller.Error() as ViewResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("An unexpected error occurred Please try again later", controller.ModelState[""].Errors[0].ErrorMessage);  // Check that the default message is used
        }
    }
}
