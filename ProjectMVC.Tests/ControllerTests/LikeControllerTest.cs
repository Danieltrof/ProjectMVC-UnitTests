using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using ProjectMVC.Controllers;
using ProjectMVC.Services.Interfaces;
using ProjectMVC.Models;
using ProjectMVC.DAL.Entities;
using ProjectMVC.DAL.Repository.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

[TestClass]
public class LikeControllerTest
{
    private LikeController _likeController;
    private Mock<ILikeService> _mockLikeService;
    private Mock<IPostRepository> _mockPostRepository;
    private Mock<ILikeRepository> _mockLikeRepository;

    [TestInitialize]
    public void Setup()
    {
        _mockLikeService = new Mock<ILikeService>();
        _mockPostRepository = new Mock<IPostRepository>();
        _mockLikeRepository = new Mock<ILikeRepository>();

        _likeController = new LikeController(_mockLikeService.Object, Mock.Of<ILogger<LikeController>>());
    }

    [TestMethod]
    public async Task Toggle_Post_Like_Success()
    {
        // Arrange
        string userId = "testUserId";
        int postId = 1;

        // Mock the ClaimsPrincipal (user) with NameIdentifier claim
        List<Claim> claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        ClaimsIdentity identity = new ClaimsIdentity(claims, "test");
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

        _likeController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Mock the post repository to return an existing post
        Post post = new Post { Id = postId, LikesCount = 0 };
        _mockPostRepository.Setup(repo => repo.GetPostByIdAsync(postId))
                        .ReturnsAsync(post);

        // Mock the LikeRepository to return null (no like exists yet)
        _mockLikeRepository.Setup(repo => repo.GetLikeAsync(postId, userId))
                        .ReturnsAsync((Like?)null);

        // Mock the service to return a successful result
        _mockLikeService.Setup(service => service.ToggleLikeAsync(postId, userId))
                        .ReturnsAsync(Result.Success("Liked post successfully"));

        // Act
        IActionResult result = await _likeController.Toggle(postId);

        // Assert
        RedirectToActionResult redirectResult = result as RedirectToActionResult;
        Assert.IsNotNull(redirectResult);
        Assert.AreEqual("Index", redirectResult.ActionName);
        Assert.AreEqual("Post", redirectResult.ControllerName);

        // Verifying that AddLikeAsync was called once
        _mockLikeRepository.Verify(repo => repo.AddLikeAsync(It.IsAny<Like>()), Times.Once);
        _mockLikeRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);

        // Ensure the LikesCount is increased by 1
        Assert.AreEqual(1, post.LikesCount); 
    }


    [TestMethod]
    public async Task Toggle_Post_Like_Fail()
    {
        string userId = "testUserId";
        int postId = 1;

        List<Claim> claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        ClaimsIdentity identity = new ClaimsIdentity(claims, "test");
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

        _likeController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        Post post = new Post { Id = postId, LikesCount = 0 };
        _mockPostRepository.Setup(repo => repo.GetPostByIdAsync(postId))
                           .ReturnsAsync(post);

        _mockLikeRepository.Setup(repo => repo.GetLikeAsync(postId, userId))
                           .ReturnsAsync((Like?)null);

        // Mock the service to return a failure result
        _mockLikeService.Setup(service => service.ToggleLikeAsync(postId, userId))
                        .ReturnsAsync(Result.Failure("Failed to like the post"));

        IActionResult result = await _likeController.Toggle(postId);

        RedirectToActionResult redirectResult = result as RedirectToActionResult;
        Assert.IsNotNull(redirectResult);
        Assert.AreEqual("Index", redirectResult.ActionName);
        Assert.AreEqual("Post", redirectResult.ControllerName);

        // Verify that AddLikeAsync was not called
        _mockLikeRepository.Verify(repo => repo.AddLikeAsync(It.IsAny<Like>()), Times.Never);
    }

    [TestMethod]
    public async Task Toggle_Post_Like_Exception_Handled()
    {
        string userId = "testUserId";
        int postId = 1;

        List<Claim> claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        ClaimsIdentity identity = new ClaimsIdentity(claims, "test");
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

        _likeController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        Post post = new Post { Id = postId, LikesCount = 0 };
        _mockPostRepository.Setup(repo => repo.GetPostByIdAsync(postId))
                           .ReturnsAsync(post);

        _mockLikeRepository.Setup(repo => repo.GetLikeAsync(postId, userId))
                           .ReturnsAsync((Like?)null);

        // Mock the service to throw an exception
        _mockLikeService.Setup(service => service.ToggleLikeAsync(postId, userId))
                        .ThrowsAsync(new Exception("An error occurred"));

        IActionResult result = await _likeController.Toggle(postId);

        RedirectToActionResult redirectResult = result as RedirectToActionResult;
        Assert.IsNotNull(redirectResult);
        Assert.AreEqual("Error", redirectResult.ActionName);
        Assert.AreEqual("Error", redirectResult.ControllerName);

        _mockLikeRepository.Verify(repo => repo.AddLikeAsync(It.IsAny<Like>()), Times.Never);
    }
}
