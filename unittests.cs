using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

public class HttpContextExtensionsTests
{
    [Fact]
    public void GetLoginUserId_NameClaimIsEmail_ReturnsUserIdFromDatabase()
    {
        // Arrange
        var httpContextMock = new Mock<HttpContext>();
        var userMock = new Mock<System.Security.Claims.ClaimsPrincipal>();
        var identityMock = new Mock<System.Security.Principal.IIdentity>();
        identityMock.Setup(i => i.Name).Returns("test@example.com");
        userMock.Setup(u => u.Identity).Returns(identityMock.Object);
        httpContextMock.Setup(c => c.User).Returns(userMock.Object);

        var serviceProviderMock = new Mock<IServiceProvider>();
        var serviceScopeMock = new Mock<IServiceScope>();
        var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        var dbContextMock = new Mock<AppDbContext>();
        var userInfoMock = new Mock<Microsoft.EntityFrameworkCore.DbSet<UserInfo>>();

        // Simulate a user found in the database
        var userInfo = new UserInfo { Id = "123", Email = "test@example.com" };
        userInfoMock.Setup(u => u.Where(It.IsAny<System.Linq.Expressions.Expression<Func<UserInfo, bool>>>()))
            .Returns(new List<UserInfo> { userInfo }.AsQueryable());

        dbContextMock.Setup(d => d.UserInfo).Returns(userInfoMock.Object);
        serviceProviderMock.Setup(s => s.GetService(typeof(AppDbContext))).Returns(dbContextMock.Object);
        serviceScopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
        serviceScopeFactoryMock.Setup(s => s.CreateScope()).Returns(serviceScopeMock.Object);
        httpContextMock.Setup(c => c.RequestServices.GetService(typeof(IServiceScopeFactory))).Returns(serviceScopeFactoryMock.Object);

        // Act
        string userId = httpContextMock.Object.GetLoginUserId();

        // Assert
        Assert.Equal("123", userId);
    }

    [Fact]
    public void GetLoginUserId_NameClaimIsNotEmail_ReturnsNameClaim()
    {
        // Arrange
        var httpContextMock = new Mock<HttpContext>();
        var userMock = new Mock<System.Security.Claims.ClaimsPrincipal>();
        var identityMock = new Mock<System.Security.Principal.IIdentity>();
        identityMock.Setup(i => i.Name).Returns("testuser");
        userMock.Setup(u => u.Identity).Returns(identityMock.Object);
        httpContextMock.Setup(c => c.User).Returns(userMock.Object);

        // Act
        string userId = httpContextMock.Object.GetLoginUserId();

        // Assert
        Assert.Equal("testuser", userId);
    }

    [Fact]
    public void GetLoginUserId_NameClaimIsNull_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var httpContextMock = new Mock<HttpContext>();
        var userMock = new Mock<System.Security.Claims.ClaimsPrincipal>();
        var identityMock = new Mock<System.Security.Principal.IIdentity>();
        identityMock.Setup(i => i.Name).Returns((string)null); // Name claim is null
        userMock.Setup(u => u.Identity).Returns(identityMock.Object);
        httpContextMock.Setup(c => c.User).Returns(userMock.Object);

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() => httpContextMock.Object.GetLoginUserId());
    }

    [Fact]
    public void GetLoginUserId_UserNotFoundInDatabase_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var httpContextMock = new Mock<HttpContext>();
        var userMock = new Mock<System.Security.Claims.ClaimsPrincipal>();
        var identityMock = new Mock<System.Security.Principal.IIdentity>();
        identityMock.Setup(i => i.Name).Returns("test@example.com");
        userMock.Setup(u => u.Identity).Returns(identityMock.Object);
        httpContextMock.Setup(c => c.User).Returns(userMock.Object);

        var serviceProviderMock = new Mock<IServiceProvider>();
        var serviceScopeMock = new Mock<IServiceScope>();
        var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        var dbContextMock = new Mock<AppDbContext>();
        var userInfoMock = new Mock<Microsoft.EntityFrameworkCore.DbSet<UserInfo>>();

        // Simulate no user found in the database
        userInfoMock.Setup(u => u.Where(It.IsAny<System.Linq.Expressions.Expression<Func<UserInfo, bool>>>()))
            .Returns(new List<UserInfo>().AsQueryable()); // Empty result

        dbContextMock.Setup(d => d.UserInfo).Returns(userInfoMock.Object);
        serviceProviderMock.Setup(s => s.GetService(typeof(AppDbContext))).Returns(dbContextMock.Object);
        serviceScopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
        serviceScopeFactoryMock.Setup(s => s.CreateScope()).Returns(serviceScopeMock.Object);
        httpContextMock.Setup(c => c.RequestServices.GetService(typeof(IServiceScopeFactory))).Returns(serviceScopeFactoryMock.Object);

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() => httpContextMock.Object.GetLoginUserId());
    }
}
