using Xunit;
using Moq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Assuming these are in your project
// using YourProjectNamespace; // Replace with your actual namespace
// using YourProjectNamespace.Constants; // If AppConstants is in a separate file

public class ClaimsTransformationTests
{
    [Fact]
    public async Task TransformAsync_NameClaimFromUiClient_ReplacedWithUserIdHeader()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "test@example.com") // Email address
        }));

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.Request.Headers["UserID"])
            .Returns(new StringValues("12345")); // User ID from header

        var mockReqContext = new Mock<IHttpContextAccessor>(); // Assuming you're using IHttpContextAccessor
        mockReqContext.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var transformation = new YourClaimsTransformationClass(mockReqContext.Object, /* other dependencies */); // Replace with your class

        // Act
        var transformedPrincipal = await transformation.TransformAsync(principal);

        // Assert
        Assert.NotNull(transformedPrincipal);
        var nameClaim = transformedPrincipal.FindFirst(ClaimTypes.Name);
        Assert.NotNull(nameClaim);
        Assert.Equal("12345", nameClaim.Value); // Assert that the name claim is replaced with UserID
    }

    [Fact]
    public async Task TransformAsync_NameClaimFromUiClient_UserIdHeaderEmpty_ReturnsOriginalPrincipal()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "test@example.com") // Email address
        }));

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.Request.Headers["UserID"])
            .Returns(StringValues.Empty); // No UserID in header

        var mockReqContext = new Mock<IHttpContextAccessor>();
        mockReqContext.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

         var transformation = new YourClaimsTransformationClass(mockReqContext.Object, /* other dependencies */);

        // Act
        var transformedPrincipal = await transformation.TransformAsync(principal);

        // Assert
        Assert.Same(principal, transformedPrincipal); // Should return the original principal if UserID is empty
    }

    [Fact]
    public async Task TransformAsync_NameClaimFromRegisteredApp_AppIdClaimExists_NameClaimFilledWithAppName()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(AppConstants.CLAIM_TYPE_APPID, "app123") // App ID claim
        }));

        var mockHttpContext = new Mock<HttpContext>();
        var mockReqContext = new Mock<IHttpContextAccessor>();
        mockReqContext.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        // Mock the registered apps retrieval (replace with your actual logic)
        var registeredApps = new List<YourAppClass> { // Replace YourAppClass with your app class
            new YourAppClass { AppId = "app123", AppName = "TestApp" }
        };
        // Assuming you have a way to access registered apps in your transformation class
        // For example, through dependency injection or a repository
        var mockAppRepository = new Mock<IAppRepository>(); // Replace IAppRepository with your interface
        mockAppRepository.Setup(x => x.GetRegisteredApps()).Returns(registeredApps);

        var transformation = new YourClaimsTransformationClass(mockReqContext.Object, mockAppRepository.Object, /* other dependencies */);

        // Act
        var transformedPrincipal = await transformation.TransformAsync(principal);

        // Assert
        Assert.NotNull(transformedPrincipal);
        var nameClaim = transformedPrincipal.FindFirst(ClaimTypes.Name);
        Assert.NotNull(nameClaim);
        Assert.Equal("TestApp", nameClaim.Value); // Assert that the name claim is filled with AppName
    }

    [Fact]
    public async Task TransformAsync_NameClaimFromRegisteredApp_AppIdClaimEmpty_ReturnsOriginalPrincipal()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity()); // No App ID claim

        var mockHttpContext = new Mock<HttpContext>();
        var mockReqContext = new Mock<IHttpContextAccessor>();
        mockReqContext.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var transformation = new YourClaimsTransformationClass(mockReqContext.Object, /* other dependencies */);

        // Act
        var transformedPrincipal = await transformation.TransformAsync(principal);

        // Assert
        Assert.Same(principal, transformedPrincipal); // Should return the original principal if AppID is empty
    }

    // ... Add more test cases for other scenarios and edge cases
}
