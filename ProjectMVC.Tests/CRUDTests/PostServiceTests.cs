using Moq;
using Xunit;
using ProjectMVC.Services;
using ProjectMVC.DAL.Repository.Interfaces;
using ProjectMVC.DTO;
using ProjectMVC.DAL.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

public class PostServiceTests
{
    private Mock<IPostRepository> _postRepositoryMock;
    private Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private Mock<IAuthorizationService> _authorizationServiceMock;
    private Mock<ILogger<PostService>> _loggerMock;
    private PostService _postService;

    public PostServiceTests()
    {
        _postRepositoryMock = new Mock<IPostRepository>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _authorizationServiceMock = new Mock<IAuthorizationService>();
        _loggerMock = new Mock<ILogger<PostService>>();
        _postService = new PostService(_postRepositoryMock.Object, _httpContextAccessorMock.Object, _authorizationServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllPostsAsync_ShouldReturnPosts_WhenUserIsAuthenticated()
    {
        // Arrange
        var userId = "user123";
        var mockUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }));
        _httpContextAccessorMock.Setup(h => h.HttpContext.User).Returns(mockUser);

        var user = new ApplicationUser { Id = userId, Name = "Test User", ProfilePicture = "/images/user.jpg" };

        var posts = new List<Post>
        {
            new Post 
            { 
                Id = 1, 
                Title = "Post 1", 
                UserId = userId, 
                User = user, 
                Comments = new List<Comment> 
                {
                    new Comment 
                    { 
                        Id = 1, 
                        Content = "Nice post!", 
                        User = user, 
                        Created = DateTime.UtcNow 
                    }
                } 
            },
            new Post 
            { 
                Id = 2, 
                Title = "Post 2", 
                UserId = userId, 
                User = user, 
                Comments = new List<Comment>() 
            }
        };

        _postRepositoryMock.Setup(repo => repo.GetAllPostsAsync()).ReturnsAsync(posts);
        _postRepositoryMock.Setup(repo => repo.GetLikedPostIdsAsync(userId)).ReturnsAsync(new List<int> { 1 });

        _authorizationServiceMock.Setup(auth => auth.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), null, "CanEdit"))
            .ReturnsAsync(AuthorizationResult.Success());

        // Act
        var result = await _postService.GetAllPostsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result[0].IsLikedByUser);
        Assert.Single(result[0].Comments);
    }

    [Fact]
    public async Task CreatePostAsync_ShouldReturnFailure_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _httpContextAccessorMock.Setup(h => h.HttpContext.User).Returns((ClaimsPrincipal)null);
        var dto = new PostCreateDto { Title = "New Post" };

        // Act
        var result = await _postService.CreatePostAsync(dto);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("You need to be logged in to create posts", result.Error);
    }

    [Fact]
    public async Task CreatePostAsync_ShouldCreatePost_WhenValidInputProvided()
    {
        // Arrange
        var userId = "user123";
        var mockUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }));
        _httpContextAccessorMock.Setup(h => h.HttpContext.User).Returns(mockUser);

        var dto = new PostCreateDto { Title = "New Post", ImageFile = null };

        _postRepositoryMock.Setup(repo => repo.AddPostAsync(It.IsAny<Post>())).Returns(Task.CompletedTask);

        // Act
        var result = await _postService.CreatePostAsync(dto);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("Post created successfully", result.SuccessMessage);
        _postRepositoryMock.Verify(repo => repo.AddPostAsync(It.IsAny<Post>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePostAsync_ShouldReturnFailure_WhenPostDoesNotExist()
    {
        // Arrange
        _postRepositoryMock.Setup(repo => repo.GetPostByIdAsync(1)).ReturnsAsync((Post)null);
        var dto = new PostUpdateDto { Title = "Updated Title" };

        // Act
        var result = await _postService.UpdatePostAsync(1, dto);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("Post does not exist", result.Error);
    }

    [Fact]
    public async Task DeletePostAsync_ShouldReturnFailure_WhenUserNotAuthorized()
    {
        // Arrange
        var userId = "user123";
        var post = new Post { Id = 1, UserId = "differentUser" };

        _httpContextAccessorMock.Setup(h => h.HttpContext.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) })));
        _postRepositoryMock.Setup(repo => repo.GetPostByIdAsync(1)).ReturnsAsync(post);

        _authorizationServiceMock.Setup(auth => auth.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), post, "CanEdit"))
            .ReturnsAsync(AuthorizationResult.Failed());

        // Act
        var result = await _postService.DeletePostAsync(1);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("You are not authorized to delete this post", result.Error);
    }
}
