[Fact]
public async Task IsSubmittedIdea_IdeaInProcess_ReturnsFalse()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetByIdAsync<Idea, int>(1)).ReturnsAsync(new Idea { Id = 1, IdeaStatusId = 1 });
    mockRepo.Setup(r => r.GetByIdAsync<IdeaStatus, int>(1)).ReturnsAsync(new IdeaStatus { Id = 1, GroupId = (int)IdeaStatusGroupEnum.InProcess });
    var service = new YourServiceClass(mockRepo.Object); // Replace with your service class

    // Act
    var result = await service.IsSubmittedIdea(1);

    // Assert
    Assert.False(result);
}

[Fact]
public async Task IsSubmittedIdea_IdeaSubmitted_ReturnsTrue()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetByIdAsync<Idea, int>(1)).ReturnsAsync(new Idea { Id = 1, IdeaStatusId = 2 });
    mockRepo.Setup(r => r.GetByIdAsync<IdeaStatus, int>(2)).ReturnsAsync(new IdeaStatus { Id = 2, GroupId = (int)IdeaStatusGroupEnum.Submitted });
    var service = new YourServiceClass(mockRepo.Object);

    // Act
    var result = await service.IsSubmittedIdea(1);

    // Assert
    Assert.True(result);
}


[Fact]
public async Task IsIdeaContact_IdeaIsNull_FetchesIdeaAndChecksContact()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetByIdAsync<Idea, int>(1)).ReturnsAsync(new Idea { Id = 1, UserId = "user1", SecondaryContactUserId = "user2" });
    var service = new YourServiceClass(mockRepo.Object);

    // Act
    var result = await service.IsIdeaContact("user1", 1);

    // Assert
    Assert.True(result);
}

[Fact]
public async Task IsIdeaContact_IdeaIsNotNull_ChecksContactDirectly()
{
    // Arrange
    var idea = new Idea { Id = 1, UserId = "user1", SecondaryContactUserId = "user2" };
    var service = new YourServiceClass(new Mock<IRepository>().Object); // No repository calls in this case

    // Act
    var result = await service.IsIdeaContact("user2", 1, idea);

    // Assert
    Assert.True(result);
}

[Fact]
public async Task IsIdeaContact_NotContact_ReturnsFalse()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetByIdAsync<Idea, int>(1)).ReturnsAsync(new Idea { Id = 1, UserId = "user1", SecondaryContactUserId = "user2" });
    var service = new YourServiceClass(mockRepo.Object);

    // Act
    var result = await service.IsIdeaContact("user3", 1);

    // Assert
    Assert.False(result);
}



[Fact]
public async Task IsUserWithRole_UserHasRole_ReturnsTrue()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetAllAsync<UserAccess, int>(It.IsAny<Expression<Func<UserAccess, bool>>>()))
        .ReturnsAsync(new List<UserAccess> { new UserAccess { UserId = "user1", AccessLevel = (int)AccessRolesEnum.Admin } });
    var service = new YourServiceClass(mockRepo.Object);

    // Act
    var result = await service.IsUserWithRole("user1", AccessRolesEnum.Admin);

    // Assert
    Assert.True(result);
}

[Fact]
public async Task IsUserWithRole_UserDoesNotHaveRole_ReturnsFalse()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetAllAsync<UserAccess, int>(It.IsAny<Expression<Func<UserAccess, bool>>>()))
        .ReturnsAsync(new List<UserAccess> { new UserAccess { UserId = "user1", AccessLevel = (int)AccessRolesEnum.User } });
    var service = new YourServiceClass(mockRepo.Object);

    // Act
    var result = await service.IsUserWithRole("user1", AccessRolesEnum.Admin);

    // Assert
    Assert.False(result);
}

[Fact]
public async Task IsUserWithRole_LoginUserIdNull_ReturnsFalse()
{
    // Arrange
    var service = new YourServiceClass(new Mock<IRepository>().Object);

    // Act
    var result = await service.IsUserWithRole(null, AccessRolesEnum.Admin);

    // Assert
    Assert.False(result);
}


[Fact]
public async Task IsValidReviewer_IsContact_ReturnsFalse()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    var mockService = new Mock<YourServiceClass>(mockRepo.Object);
    mockService.Setup(s => s.IsIdeaContact(It.IsAny<string>(), It.IsAny<int>(), null)).ReturnsAsync(true); // Simulate isContact = true
    mockService.CallBase = true; // Allow other methods to be called

    // Act
    var result = await mockService.Object.IsValidReviewer(1, "user1");

    // Assert
    Assert.False(result);
}

[Fact]
public async Task IsValidReviewer_IsValid_ReturnsTrue()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    var mockService = new Mock<YourServiceClass>(mockRepo.Object);
    mockService.Setup(s => s.IsIdeaContact(It.IsAny<string>(), It.IsAny<int>(), null)).ReturnsAsync(false); // Simulate isContact = false
    mockRepo.Setup(r => r.GetByIdAsync<Idea, int>(1)).ReturnsAsync(new Idea { Id = 1 });
    mockRepo.Setup(r => r.GetAllAsync<IdeaReview, int>(It.IsAny<Expression<Func<IdeaReview, bool>>>()))
        .ReturnsAsync(new List<IdeaReview> { new IdeaReview { IdeaId = 1, ReviewGroupId = 1 } });
    mockRepo.Setup(r => r.GetAllAsync<ReviewGroup, int>())
        .ReturnsAsync(new List<ReviewGroup> { new ReviewGroup { Id = 1 } });
    mockRepo.Setup(r => r.GetAllAsync<ReviewGroupReviewer, int>())
        .ReturnsAsync(new List<ReviewGroupReviewer> { new ReviewGroupReviewer { GroupId = 1, UserId = "user1" } });
    mockService.CallBase = true;

    // Act
    var result = await mockService.Object.IsValidReviewer(1, "user1"); in

    // Assert
    Assert.True(result);
}

// Add more test cases for other scenarios (e.g., no reviews, no review groups, etc.)



[Fact]
public async Task GetSurveyTypeByIdeaTypeId_SurveyTypeIdExists_ReturnsSurveyTypeId()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetByIdAsync<IdeaType, int>(1))
        .ReturnsAsync(new IdeaType { Id = 1, SurveyTypeId = 5 });
    var service = new YourServiceClass(mockRepo.




[Fact]
public async Task GetGnasUserAsync_UserFound_ReturnsUserInfo()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetByIdAsync<UserInfo, string>("user1"))
        .ReturnsAsync(new UserInfo { Id = "user1" });

    var service = new YourServiceClass(mockRepo.Object);

    // Act
    var result = await service.GetGnasUserAsync("user1");

    // Assert
    Assert.NotNull(result);
    Assert.Equal("user1", result.Id);
}

[Fact]
public async Task GetGnasUserAsync_UserNotFound_ReturnsNull()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetByIdAsync<UserInfo, string>("user2"))
        .ReturnsAsync((UserInfo)null);

    var service = new YourServiceClass(mockRepo.Object);

    // Act
    var result = await service.GetGnasUserAsync("user2");

    // Assert
    Assert.Null(result);
}


[Fact]
public async Task GetIdea_IdeaFound_ReturnsIdea()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetByIdAsync<Idea, int>(1))
        .ReturnsAsync(new Idea { Id = 1 });

    var service = new YourServiceClass(mockRepo.Object);

    // Act
    var result = await service.GetIdea(1);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(1, result.Id);
}

[Fact]
public async Task GetIdea_IdeaNotFound_ThrowIfNullTrue_ThrowsException()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetByIdAsync<Idea, int>(2))
        .ReturnsAsync((Idea)null);

    var service = new YourServiceClass(mockRepo.Object);

    // Act & Assert
    await Assert.ThrowsAsync<IdeaDetailsNotFoundException>(() => service.GetIdea(2, true));
}

[Fact]
public async Task GetIdea_IdeaNotFound_ThrowIfNullFalse_ReturnsNull()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetByIdAsync<Idea, int>(3))
        .ReturnsAsync((Idea)null);

    var service = new YourServiceClass(mockRepo.Object);

    // Act
    var result = await service.GetIdea(3, false);

    // Assert
    Assert.Null(result);
}


[Fact]
public async Task IsUserAuthorizedToEditIdea_IdeaNull_ReturnsTrue()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetByIdAsync<Idea, int>(1)).ReturnsAsync((Idea)null);
    var service = new YourServiceClass(mockRepo.Object);

    // Act
    var result = await service.IsUserAuthorizedToEditIdea(1, "user1");

    // Assert
    Assert.True(result);
}

[Fact]
public async Task IsUserAuthorizedToEditIdea_StatusApproved_ReturnsFalse()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetByIdAsync<Idea, int>(1)).ReturnsAsync(new Idea { Id = 1, IdeaStatusId = 1 });
    mockRepo.Setup(r => r.GetByIdAsync<IdeaStatusGroup, int>(1)).ReturnsAsync(new IdeaStatusGroup { Id = 1, GroupId = (int)IdeaStatusGroupEnum.Approved });

    var service = new YourServiceClass(mockRepo.Object);

    // Act
    var result = await service.IsUserAuthorizedToEditIdea(1, "user1");

    // Assert
    Assert.False(result);
}

[Fact]
public async Task IsUserAuthorizedToEditIdea_StatusRejected_ReturnsFalse()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetByIdAsync<Idea, int>(1)).ReturnsAsync(new Idea { Id = 1, IdeaStatusId = 2 });
    mockRepo.Setup(r => r.GetByIdAsync<IdeaStatusGroup, int>(2)).ReturnsAsync(new IdeaStatusGroup { Id = 2, GroupId = (int)IdeaStatusGroupEnum.Rejected });

    var service = new YourServiceClass(mockRepo.Object);

    // Act
    var result = await service.IsUserAuthorizedToEditIdea(1, "user1");

    // Assert
    Assert.False(result);
}

[Fact]
public async Task IsUserAuthorizedToEditIdea_StatusCancelled_ReturnsFalse()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetByIdAsync<Idea, int>(1)).ReturnsAsync(new Idea { Id = 1, IdeaStatusId = 3 });
    mockRepo.Setup(r => r.GetByIdAsync<IdeaStatusGroup, int>(3)).ReturnsAsync(new IdeaStatusGroup { Id = 3, GroupId = (int)IdeaStatusGroupEnum.Cancelled });

    var service = new YourServiceClass(mockRepo.Object);

    // Act
    var result = await service.IsUserAuthorizedToEditIdea(1, "user1");

    // Assert
    Assert.False(result);
}

[Fact]
public async Task IsUserAuthorizedToEditIdea_UserIsOwner_ReturnsTrue()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetByIdAsync<Idea, int>(1)).ReturnsAsync(new Idea { Id = 1, UserId = "user1", IdeaStatusId = 4 });
    mockRepo.Setup(r => r.GetByIdAsync<IdeaStatusGroup, int>(4)).ReturnsAsync(new IdeaStatusGroup { Id = 4, GroupId = 1 });

    var service = new YourServiceClass(mockRepo.Object);

    // Act
    var result = await service.IsUserAuthorizedToEditIdea(1, "user1");

    // Assert
    Assert.True(result);
}

[Fact]
public async Task IsUserAuthorizedToEditIdea_UserIsSecondaryContact_ReturnsTrue()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetByIdAsync<Idea, int>(1)).ReturnsAsync(new Idea { Id = 1, SecondaryContactUserId = "user2", IdeaStatusId = 4 });
    mockRepo.Setup(r => r.GetByIdAsync<IdeaStatusGroup, int>(4)).ReturnsAsync(new IdeaStatusGroup { Id = 4, GroupId = 1 });

    var service = new YourServiceClass(mockRepo.Object);

    // Act
    var result = await service.IsUserAuthorizedToEditIdea(1, "user2");

    // Assert
    Assert.True(result);
}

[Fact]
public async Task IsUserAuthorizedToEditIdea_IsValidReviewer_ReturnsTrue()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    var mockService = new Mock<YourServiceClass>(mockRepo.Object);
    mockService.Setup(s => s.IsValidReviewer(1, "user3")).ReturnsAsync(true);
    mockService.CallBase = true;

    // Act
    var result = await mockService.Object.IsUserAuthorizedToEditIdea(1, "user3");

    // Assert
    Assert.True(result);
}


[Fact]
public async Task IsUserAuthorizedToEditIdea_IdeaNull_ReturnsTrue()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    var mockService = new Mock<YourServiceClass>(mockRepo.Object);
    mockService.Setup(s => s.GetIdea(1, false)).ReturnsAsync((Idea)null);
    mockService.CallBase = true;

    // Act
    var result = await mockService.Object.IsUserAuthorizedToEditIdea(1, "user1");

    // Assert
    Assert.True(result);
}

[Fact]
public async Task IsUserAuthorizedToEditIdea_StatusApproved_ReturnsFalse()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    var mockService = new Mock<YourServiceClass>(mockRepo.Object);
    mockService.Setup(s => s.GetIdea(1, false)).ReturnsAsync(new Idea { Id = 1, IdeaStatusId = 1 });
    mockRepo.Setup(r => r.GetAll<IdeaStatusGroup, int>(It.IsAny<Expression<Func<IdeaStatusGroup, bool>>>()))
        .Returns(new List<IdeaStatusGroup> { new IdeaStatusGroup { Id = (int)IdeaStatusGroupEnum.Approved } }.AsQueryable());
    mockRepo.Setup(r => r.GetAll<IdeaStatus, int>(It.IsAny<Expression<Func<IdeaStatus, bool>>>()))
        .Returns(new List<IdeaStatus> { new IdeaStatus { Id = 1, GroupId = (int)IdeaStatusGroupEnum.Approved } }.AsQueryable());
    mockService.CallBase = true;

    // Act
    var result = await mockService.Object.IsUserAuthorizedToEditIdea(1, "user1");

    // Assert
    Assert.False(result);
}

// Add similar tests for Rejected and Cancelled statuses

[Fact]
public async Task IsUserAuthorizedToEditIdea_UserIsOwner_ReturnsTrue()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    var mockService = new Mock<YourServiceClass>(mockRepo.Object);
    mockService.Setup(s => s.GetIdea(1, false)).ReturnsAsync(new Idea { Id = 1, UserId = "user1", IdeaStatusId = 4 });
    mockRepo.Setup(r => r.GetAll<IdeaStatusGroup, int>(It.IsAny<Expression<Func<IdeaStatusGroup, bool>>>()))
        .Returns(new List<IdeaStatusGroup> { new IdeaStatusGroup { Id = 4, GroupId = 1 } }.AsQueryable());
    mockRepo.Setup(r => r.GetAll<IdeaStatus, int>(It.IsAny<Expression<Func<IdeaStatus, bool>>>()))
        .Returns(new List<IdeaStatus> { new IdeaStatus { Id = 4, GroupId = 1 } }.AsQueryable());
    mockService.CallBase = true;

    // Act
    var result = await mockService.Object.IsUserAuthorizedToEditIdea(1, "user1");

    // Assert
    Assert.True(result);
}

// Add similar test for SecondaryContactUserId

[Fact]
public async Task IsUserAuthorizedToEditIdea_IsValidReviewer_ReturnsTrue()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    var mockService = new Mock<YourServiceClass>(mockRepo.Object);
    mockService.Setup(s => s.GetIdea(1, false)).ReturnsAsync(new Idea { Id = 1, IdeaStatusId = 4 });
    mockRepo.Setup(r => r.GetAll<IdeaStatusGroup, int>(It.IsAny<Expression<Func<IdeaStatusGroup, bool>>>()))
        .Returns(new List<IdeaStatusGroup> { new IdeaStatusGroup { Id = 4, GroupId = 1 } }.AsQueryable());
    mockRepo.Setup(r => r.GetAll<IdeaStatus, int>(It.IsAny<Expression<Func<IdeaStatus, bool>>>()))
        .Returns(new List<IdeaStatus> { new IdeaStatus { Id = 4, GroupId = 1 } }.AsQueryable());
    mockService.Setup(s => s.IsValidReviewer(1, "user3")).ReturnsAsync(true);
    mockService.CallBase = true;

    // Act
    var result = await mockService.Object.IsUserAuthorizedToEditIdea(1, "user3");

    // Assert
    Assert.True(result);
}


[Fact]
public async Task GetIdeaStatusGroup_StatusFound_ReturnsStatusGroup()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetAll<IdeaStatusGroup, int>(It.IsAny<Expression<Func<IdeaStatusGroup, bool>>>()))
        .Returns(new List<IdeaStatusGroup> { new IdeaStatusGroup { Id = 1, IsActive = true } }.AsQueryable());
    mockRepo.Setup(r => r.GetAll<IdeaStatus, int>(It.IsAny<Expression<Func<IdeaStatus, bool>>>()))
        .Returns(new List<IdeaStatus> { new IdeaStatus { Id = 1, GroupId = 1, IsActive = true } }.AsQueryable());

    var service = new YourServiceClass(mockRepo.Object);

    // Act
    var result = await service.GetIdeaStatusGroup(1);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(1, result.Id);
}

[Fact]
public async Task GetIdeaStatusGroup_StatusNotFound_ReturnsNull()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetAll<IdeaStatusGroup, int>(It.IsAny<Expression<Func<IdeaStatusGroup, bool>>>()))
        .Returns(new List<IdeaStatusGroup>().AsQueryable());
    mockRepo.Setup(r => r.GetAll<IdeaStatus, int>(It.IsAny<Expression<Func<IdeaStatus, bool>>>()))
        .Returns(new List<IdeaStatus>().AsQueryable());

    var service = new YourServiceClass(mockRepo.Object);

    // Act
    var result = await service.GetIdeaStatusGroup(1);

    // Assert
    Assert.Null(result);
}


[Fact]
public async Task IsIdeaStatusPendingReview_PendingApproval_ReturnsTrue()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    var mockService = new Mock<YourServiceClass>(mockRepo.Object);
    mockService.Setup(s => s.GetIdeaStatusGroup(1))
        .ReturnsAsync(new IdeaStatusGroup { Id = (int)IdeaStatusGroupEnum.PendingApproval });
    mockService.CallBase = true;

    // Act
    var result = await mockService.Object.IsIdeaStatusPendingReview(1);

    // Assert
    Assert.True(result);
}

[Fact]
public async Task IsIdeaStatusPendingReview_NotPendingApproval_ReturnsFalse()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    var mockService = new Mock<YourServiceClass>(mockRepo.Object);
    mockService.Setup(s => s.GetIdeaStatusGroup(1))
        .ReturnsAsync(new IdeaStatusGroup { Id = (int)IdeaStatusGroupEnum.Approved });
    mockService.CallBase = true;

    // Act
    var result = await mockService.Object.IsIdeaStatusPendingReview(1);

    // Assert
    Assert.False(result);
}


[Fact]
public async Task IsIdeaStatusInprogress_InProcess_ReturnsTrue()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    var mockService = new Mock<YourServiceClass>(mockRepo.Object);
    mockService.Setup(s => s.





using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;

// Assuming these are in your project
// using YourProjectNamespace; // Replace with your actual namespace
// using YourProjectNamespace.Enums; // Replace with your actual enum namespace
// using YourProjectNamespace.Models; // Replace with your actual model namespace

public class ServiceBaseTests
{
    [Fact]
    public async Task IsSubmittedIdea_IdeaInProcess_ReturnsFalse()
    {
        // Arrange
        var mockRepo = new Mock<IRepository>();
        var mockLogger = new Mock<ILogger>();
        var mockMapper = new Mock<IMapper>();
        var mockEnv = new Mock<IHostingEnvironment>();

        // Create a partial mock of ServiceBase
        var mockService = new Mock<ServiceBase>(mockRepo.Object, mockLogger.Object, mockMapper.Object, mockEnv.Object) { CallBase = true };

        // Mock GetIdea method (now accessible through partial mocking)
        mockService.Protected().Setup<Task<Idea>>("GetIdea", ItExpr.IsAny<int>(), ItExpr.IsAny<bool>())
            .ReturnsAsync(new Idea { Id = 1, IdeaStatusId = 1 });

        // Mock Repository.GetByIdAsync for IdeaStatus
        mockRepo.Setup(r => r.GetByIdAsync<IdeaStatus, int>(It.IsAny<System.Linq.Expressions.Expression<Func<IdeaStatus, bool>>>()))
            .ReturnsAsync(new IdeaStatus { Id = 1, GroupId = (int)IdeaStatusGroupEnum.InProcess });

        // Act
        var result = await mockService.Object.IsSubmittedIdea(1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsSubmittedIdea_IdeaSubmitted_ReturnsTrue()
    {
        // Arrange
        var mockRepo = new Mock<IRepository>();
        var mockLogger = new Mock<ILogger>();
        var mockMapper = new Mock<IMapper>();
        var mockEnv = new Mock<IHostingEnvironment>();

        // Create a partial mock of ServiceBase
        var mockService = new Mock<ServiceBase>(mockRepo.Object, mockLogger.Object, mockMapper.Object, mockEnv.Object) { CallBase = true };

        // Mock GetIdea method (now accessible through partial mocking)
        mockService.Protected().Setup<Task<Idea>>("GetIdea", ItExpr.IsAny<int>(), ItExpr.IsAny<bool>())
            .ReturnsAsync(new Idea { Id = 1, IdeaStatusId = 2 });

        // Mock Repository.GetByIdAsync for IdeaStatus
        mockRepo.Setup(r => r.GetByIdAsync<IdeaStatus, int>(It.IsAny<System.Linq.Expressions.Expression<Func<IdeaStatus, bool>>>()))
            .ReturnsAsync(new IdeaStatus { Id = 2, GroupId = (int)IdeaStatusGroupEnum.Submitted }); // Assuming "Submitted" is another enum value

        // Act
        var result = await mockService.Object.IsSubmittedIdea(1);

        // Assert
        Assert.True(result);
    }

    // Add more test cases for other scenarios (e.g., IdeaStatus not found, GetIdea returns null, etc.)
}
