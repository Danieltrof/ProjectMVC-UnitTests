using Microsoft.AspNetCore.Mvc;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using ProjectMVC.Controllers;
using ProjectMVC.Services.Interfaces;
using ProjectMVC.Models;  
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace ProjectMVC.Tests.ControllerTests
{
    [TestClass]
    public class ProfileControllerTest
    {
        [TestMethod]
        public async Task Index_ReturnsRedirectToPost_Index_WhenProfileServiceSucceeds()
        {
            // Arrange
            var userId = "testUserId";
            var actualUserId = "actualUserId";
            var mockProfileService = new Mock<IProfileService>();
            mockProfileService.Setup(service => service.GetProfile(userId, actualUserId))
                              .ReturnsAsync(new Result { Succeeded = true });

            var mockLogger = new Mock<ILogger<ProfileController>>();
            var controller = new ProfileController(mockProfileService.Object, mockLogger.Object);

            // Mock the User principal
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, actualUserId)  // Set the mocked user ID
            };
            var identity = new ClaimsIdentity(claims, "mock");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }  // Set the mocked user principal
            };

            // Act
            var result = await controller.Index(userId);

            // Assert
            var redirectToActionResult = result as RedirectToActionResult;
            Assert.IsNotNull(redirectToActionResult);
            Assert.AreEqual("Index", redirectToActionResult?.ActionName);
            Assert.AreEqual("Post", redirectToActionResult?.ControllerName);
        }

        [TestMethod]
        public async Task Index_ReturnsRedirectToPost_Index_WhenProfileServiceFails()
        {
            // Arrange
            var userId = "testUserId";
            var actualUserId = "actualUserId";
            var mockProfileService = new Mock<IProfileService>();
            mockProfileService.Setup(service => service.GetProfile(userId, actualUserId))
                              .ReturnsAsync(new Result { Succeeded = false, Error = "Profile not found" });

            var mockLogger = new Mock<ILogger<ProfileController>>();
            var controller = new ProfileController(mockProfileService.Object, mockLogger.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, actualUserId) 
            };
            var identity = new ClaimsIdentity(claims, "mock");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }  
            };

            var result = await controller.Index(userId);

            var redirectToActionResult = result as RedirectToActionResult;
            Assert.IsNotNull(redirectToActionResult);
            Assert.AreEqual("Index", redirectToActionResult?.ActionName);
            Assert.AreEqual("Post", redirectToActionResult?.ControllerName);
            Assert.AreEqual("Profile not found", controller.TempData["ErrorMessage"]);  // Ensure error message is set
        }
    }
}
