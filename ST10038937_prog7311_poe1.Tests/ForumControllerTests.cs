using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using ST10038937_prog7311_poe1.Controllers;
using ST10038937_prog7311_poe1.Data;
using ST10038937_prog7311_poe1.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

public class ForumControllerTests
{
    [Fact]
    public async Task Index_ReturnsViewResult_WithListOfForumPosts()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "ForumControllerTestDb")
            .Options;
        using var context = new ApplicationDbContext(options);
        var user = new ApplicationUser { Id = "test-user-id", UserName = "testuser@agrienergy.com", Email = "testuser@agrienergy.com" };
        context.Users.Add(user);
        context.ForumPosts.Add(new ForumPost { Title = "Test Post", Content = "Test Content", User = user });
        context.SaveChanges();

        var userManager = MockUserManager();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var controller = new ForumController(context, userManager.Object, memoryCache);

        // Set up a fake user for the controller context
        var claimsUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Name, "testuser@agrienergy.com"),
            new Claim(ClaimTypes.Role, "Employee")
        }, "mock"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsUser }
        };

        // Act
        var result = await controller.Index(null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<ForumPost>>(viewResult.Model);
        Assert.Single(model);
        Assert.Equal("Test Post", model.First().Title);
    }

    // Helper to mock UserManager
    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
    }
} 