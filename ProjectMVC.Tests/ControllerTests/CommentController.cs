using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.AspNetCore.Mvc;
using ProjectMVC.Controllers;
using ProjectMVC.Services.Interfaces;
using ProjectMVC.DTO;
using ProjectMVC.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ProjectMVC.Tests.ControllerTests
{
    [TestClass]
    public class CommentControllerTest
    {
        private Mock<ICommentService> _mockCommentService;
        private Mock<ILogger<CommentController>> _mockLogger;
        private CommentController _commentController;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockCommentService = new Mock<ICommentService>();
            _mockLogger = new Mock<ILogger<CommentController>>();

            // Creating a mock user with a dummy user ID for the logged-in user.
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "12345")
            };
            var identity = new ClaimsIdentity(claims, "mock");
            var principal = new ClaimsPrincipal(identity);
            
            // Injecting mock user into the controller
            _commentController = new CommentController(_mockCommentService.Object, _mockLogger.Object)
            {
                ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = principal }
                }
            };
        }

        // Positive Test for Create Comment
        [TestMethod]
        public async Task Create_ValidComment_ReturnsRedirectToActionResult()
        {
            var postId = 1;
            var content = "This is a test comment";

            // Create a mock result for the create comment operation
            var result = new ProjectMVC.Models.Result { Succeeded = true };  

            // Setup the mock to return the result wrapped in a Task
            _mockCommentService.Setup(service => service.CreateCommentAsync(postId, content, "12345"))
                            .ReturnsAsync(result);  

            var actionResult = await _commentController.Create(postId, content);

            var redirectResult = actionResult as RedirectToActionResult;
            Assert.IsNotNull(redirectResult);
            Assert.AreEqual("Index", redirectResult.ActionName);
            Assert.AreEqual("Post", redirectResult.ControllerName);
        }


        // Negative Test for Create Comment - Empty Content
        [TestMethod]
        public async Task Create_EmptyContent_ReturnsRedirectToActionWithError()
        {
            var postId = 1;
            var content = ""; 

            var result = await _commentController.Create(postId, content);

            var redirectResult = result as RedirectToActionResult;
            Assert.IsNotNull(redirectResult);
            Assert.AreEqual("Index", redirectResult.ActionName);
            Assert.AreEqual("Post", redirectResult.ControllerName);
            Assert.IsTrue(_commentController.ModelState.ErrorCount > 0); // Ensure error was added
        }

        // Negative Test for Create Comment - Null Content
        [TestMethod]
        public async Task Create_NullContent_ReturnsRedirectToActionWithError()
        {
            var postId = 1;
            string content = null; // Null content

            var result = await _commentController.Create(postId, content);

            var redirectResult = result as RedirectToActionResult;
            Assert.IsNotNull(redirectResult);
            Assert.AreEqual("Index", redirectResult.ActionName);
            Assert.AreEqual("Post", redirectResult.ControllerName);
            Assert.IsTrue(_commentController.ModelState.ErrorCount > 0); // Ensure error was added
        }

        // Positive Test for Delete Comment
        [TestMethod]
        public async Task Delete_ValidComment_ReturnsRedirectToActionResult()
        {
            var commentId = 1;
            var userId = "12345"; 

            _mockCommentService.Setup(service => service.DeleteCommentAsync(commentId, userId))
                               .ReturnsAsync(new Result { Succeeded = true });

            var result = await _commentController.Delete(commentId);

            var redirectResult = result as RedirectToActionResult;
            Assert.IsNotNull(redirectResult);
            Assert.AreEqual("Index", redirectResult.ActionName);
            Assert.AreEqual("Post", redirectResult.ControllerName);
        }

        // Negative Test for Delete Comment - Failure in Delete
        [TestMethod]
        public async Task Delete_FailedDelete_ReturnsRedirectToActionWithError()
        {
            var commentId = 1;
            var userId = "12345"; 

            _mockCommentService.Setup(service => service.DeleteCommentAsync(commentId, userId))
                               .ReturnsAsync(new Result { Succeeded = false, Error = "Delete failed" });

            var result = await _commentController.Delete(commentId);

            var redirectResult = result as RedirectToActionResult;
            Assert.IsNotNull(redirectResult);
            Assert.AreEqual("Index", redirectResult.ActionName);
            Assert.AreEqual("Post", redirectResult.ControllerName);
            Assert.AreEqual("Delete failed", _commentController.TempData["ErrorMessage"]);
        }

        // Negative Test for Delete Comment - Exception Handling
        [TestMethod]
        public async Task Delete_ExceptionThrown_ReturnsRedirectToErrorAction()
        {
            var commentId = 1;
            var userId = "12345"; 

            _mockCommentService.Setup(service => service.DeleteCommentAsync(commentId, userId))
                               .ThrowsAsync(new Exception("Something went wrong"));

            var result = await _commentController.Delete(commentId);

            var redirectResult = result as RedirectToActionResult;
            Assert.IsNotNull(redirectResult);
            Assert.AreEqual("Error", redirectResult.ActionName);
            Assert.AreEqual("Error", redirectResult.ControllerName);
        }
    }
}
