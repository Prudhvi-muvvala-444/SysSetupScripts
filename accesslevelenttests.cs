using Xunit;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AutoMapper;

// Assuming the necessary namespaces and classes are defined elsewhere
// For example:
// namespace YourNamespace
// {
//     public class AccessLevelEntitlementService : ServiceBase, IEntitlementService
//     {
//         // ... (your service code) ...
//     }
//
//     public interface IRepository
//     {
//         T GetById<T, TKey>(TKey id);
//         Task InsertAsync<T, TKey>(TKey id, T entity);
//         Task UpdateAsync<T, TKey>(T entity, string loginUser);
//     }
//
//     public interface IEntitlementService
//     {
//         List<EntitlementDto> GetList();
//         Dictionary<UserInfo, List<string>> GetEntitlements(List<UserInfo> users);
//         Task<string> AddAsync(UserInfo userInfo, string entitlement, string loginUser);
//     }
//
//     public class ServiceBase
//     {
//         public ServiceBase(IRepository repo, ILogger logger, IMapper mapper) { }
//     }
//
//     public class EntitlementDto
//     {
//         public string Name { get; set; }
//         public string Description { get; set; }
//         public int Id { get; set; }
//         public EntitlementDto(string name, string description, int id) { Name = name; Description = description; Id = id; }
//     }
//
//     public class UserInfo
//     {
//         public string Id { get; set; }
//         public bool IsActive { get; set; }
//         public bool IsInitialized { get; set; }
//         public string AccountName { get; set; }
//     }
//
//     public class UserAccess
//     {
//         public string UserId { get; set; }
//         public int AccessLevel { get; set; }
//         public bool IsActive { get; set; }
//     }
// }

namespace YourNamespace.Tests
{
    public class AccessLevelEntitlementServiceTests
    {
        private readonly Mock<IRepository> _mockRepo;
        private readonly Mock<ILogger<AccessLevelEntitlementService>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly AccessLevelEntitlementService _service;

        public AccessLevelEntitlementServiceTests()
        {
            _mockRepo = new Mock<IRepository>();
            _mockLogger = new Mock<ILogger<AccessLevelEntitlementService>>();
            _mockMapper = new Mock<IMapper>();
            _service = new AccessLevelEntitlementService(_mockRepo.Object, _mockLogger.Object, _mockMapper.Object);
        }

        [Fact]
        public void GetList_ReturnsListOfEntitlements()
        {
            // Act
            var result = _service.GetList();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(3, result.Count); // Assuming you have 3 entitlements defined
        }

        [Fact]
        public void GetEntitlements_ReturnsEmptyDictionary()
        {
            // Arrange
            var users = new List<UserInfo>();

            // Act
            var result = _service.GetEntitlements(users);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task AddAsync_EntitlementNotFound_ReturnsErrorMessage()
        {
            // Arrange
            var userInfo = new UserInfo { Id = "123", IsActive = true, IsInitialized = true };
            var entitlementName = "NON_EXISTENT_ENTITLEMENT";
            var loginUser = "testUser";

            // Act
            var result = await _service.AddAsync(userInfo, entitlementName, loginUser);

            // Assert
            Assert.StartsWith("SailPoint.EntitlementNotFound", result);
        }

        [Fact]
        public async Task AddAsync_UserAccountDoesNotExist_ReturnsErrorMessage()
        {
            // Arrange
            var userInfo = new UserInfo { Id = "123", IsActive = false, IsInitialized = false };
            var entitlementName = "IPO_BASIC_USER_ACCESS";
            var loginUser = "testUser";

            // Act
            var result = await _service.AddAsync(userInfo, entitlementName, loginUser);

            // Assert
            Assert.StartsWith("User Account does not exist", result);
        }

        [Fact]
        public async Task AddAsync_NewUserAccess_InsertsAndReturnsEmptyString()
        {
            // Arrange
            var userInfo = new UserInfo { Id = "123", IsActive = true, IsInitialized = true };
            var entitlementName = "IPO_BASIC_USER_ACCESS";
            var loginUser = "testUser";

            _mockRepo.Setup(repo => repo.GetById<UserAccess, int>(It.IsAny<int>())).Returns((UserAccess)null);

            // Act
            var result = await _service.AddAsync(userInfo, entitlementName, loginUser);

            // Assert
            Assert.Equal(string.Empty, result);
            _mockRepo.Verify(repo => repo.InsertAsync(It.IsAny<string>(), It.IsAny<UserAccess>()), Times.Once);
        }

        [Fact]
        public async Task AddAsync_ExistingInactiveUserAccess_UpdatesAndReturnsEmptyString()
        {
            // Arrange
            var userInfo = new UserInfo { Id = "123", IsActive = true, IsInitialized = true };
            var entitlementName = "IPO_BASIC_USER_ACCESS";
            var loginUser = "testUser";

            var existingUserAccess = new UserAccess { UserId = "123", AccessLevel = 1, IsActive = false };
            _mockRepo.Setup(repo => repo.GetById<UserAccess, int>(It.IsAny<int>())).Returns(existingUserAccess);

            // Act
            var result = await _service.AddAsync(userInfo, entitlementName, loginUser);

            // Assert
            Assert.Equal(string.Empty, result);
            _mockRepo.Verify(repo => repo.UpdateAsync(existingUserAccess, loginUser), Times.Once);
        }

        [Fact]
        public async Task AddAsync_ExistingActiveUserAccess_ReturnsAlreadyExistsMessage()
        {
            // Arrange
            var userInfo = new UserInfo { Id = "123", IsActive = true, IsInitialized = true, AccountName = "testAccount" };
            var entitlementName = "IPO_BASIC_USER_ACCESS";
            var loginUser = "testUser";

            var existingUserAccess = new UserAccess { UserId = "123", AccessLevel = 1, IsActive = true };
            _mockRepo.Setup(repo => repo.GetById<UserAccess, int>(It.IsAny<int>())).Returns(existingUserAccess);

            // Act
            var result = await _service.AddAsync(userInfo, entitlementName, loginUser);

            // Assert
            Assert.StartsWith("Entitlement (IPO_BASIC_USER_ACCESS) Already exists for testAccount", result);
        }
    }
}



using Xunit;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Assuming the necessary namespaces and classes are defined elsewhere
// For example:
// namespace YourNamespace
// {
//     public interface IRepository
//     {
//         Task<List<T>> GetAllAsync<T, TKey>(System.Linq.Expressions.Expression<Func<T, bool>> predicate);
//         Task<T> GetByIdAsync<T, TKey>(System.Linq.Expressions.Expression<Func<T, bool>> predicate);
//         Task UpdateAsync<T, TKey>(T entity, string userId);
//     }
//
//     public class SailPointService
//     {
//         private readonly IRepository Repository;
//         private readonly Dictionary<EntitlementDto, int> _entitlements;
//
//         public SailPointService(IRepository repository, Dictionary<EntitlementDto, int> entitlements)
//         {
//             Repository = repository;
//             _entitlements = entitlements;
//         }
//
//         public async Task<List<Tuple<string, string>>> GetEntitlementsAsync() { /* ... */ }
//         public Task<List<EntitlementDto>> GetListAsync() { /* ... */ }
//         public async Task<List<string>> GetUserEntitlementsAsync(UserInfo gmasUserInfo) { /* ... */ }
//         public async Task<string> RemoveAsync(UserInfo userInfo, string entitlement, string loginUser) { /* ... */ }
//     }
//
//     public class EntitlementDto
//     {
//         public string Name { get; set; }
//         public int Id { get; set; }
//     }
//
//     public class UserInfo
//     {
//         public string Id { get; set; }
//     }
//
//     public class UserAccess
//     {
//         public string UserId { get; set; }
//         public int AccessLevel { get; set; }
//         public bool IsActive { get; set; }
//     }
// }

namespace YourNamespace.Tests
{
    public class SailPointServiceTests
    {
        private readonly Mock<IRepository> _mockRepo;
        private readonly Dictionary<EntitlementDto, int> _entitlements;
        private readonly SailPointService _service;

        public SailPointServiceTests()
        {
            _mockRepo = new Mock<IRepository>();
            _entitlements = new Dictionary<EntitlementDto, int>
            {
                { new EntitlementDto { Name = "Entitlement1", Id = 1 }, 1 },
                { new EntitlementDto { Name = "Entitlement2", Id = 2 }, 2 },
                { new EntitlementDto { Name = "Entitlement3", Id = 3 }, 3 }
            };
            _service = new SailPointService(_mockRepo.Object, _entitlements);
        }

        [Fact]
        public async Task GetEntitlementsAsync_ReturnsListOfTuples()
        {
            // Arrange
            var userAccesses = new List<UserAccess>
            {
                new UserAccess { UserId = "user1", AccessLevel = 1 },
                new UserAccess { UserId = "user2", AccessLevel = 2 },
                new UserAccess { UserId = "user3", AccessLevel = 3 }
            };
            _mockRepo.Setup(repo => repo.GetAllAsync<UserAccess, int>(It.IsAny<System.Linq.Expressions.Expression<Func<UserAccess, bool>>>()))
                .ReturnsAsync(userAccesses);

            // Act
            var result = await _service.GetEntitlementsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(result, tuple => tuple.Item1 == "user1" && tuple.Item2 == "Entitlement1");
        }

        [Fact]
        public void GetListAsync_ReturnsListOfEntitlements()
        {
            // Act
            var result = _service.GetListAsync().Result;

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(result, dto => dto.Name == "Entitlement1");
        }

        [Fact]
        public async Task GetUserEntitlementsAsync_ReturnsListOfEntitlementNames()
        {
            // Arrange
            var userInfo = new UserInfo { Id = "user1" };
            var userAccesses = new List<UserAccess>
            {
                new UserAccess { UserId = "user1", AccessLevel = 1, IsActive = true },
                new UserAccess { UserId = "user1", AccessLevel = 3, IsActive = true }
            };
            _mockRepo.Setup(repo => repo.GetAllAsync<UserAccess, int>(It.IsAny<System.Linq.Expressions.Expression<Func<UserAccess, bool>>>()))
                .ReturnsAsync(userAccesses);

            // Act
            var result = await _service.GetUserEntitlementsAsync(userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, "Entitlement1");
            Assert.Contains(result, "Entitlement3");
        }

        [Fact]
        public async Task RemoveAsync_EntitlementNotFound_ReturnsErrorMessage()
        {
            // Arrange
            var userInfo = new UserInfo { Id = "user1" };
            var entitlementName = "NonExistentEntitlement";

            // Act
            var result = await _service.RemoveAsync(userInfo, entitlementName, "loginUser");

            // Assert
            Assert.StartsWith("SailPoint.EntitlementNotFound", result);
        }

        [Fact]
        public async Task RemoveAsync_UserDoesNotHaveEntitlement_ReturnsErrorMessage()
        {
            // Arrange
            var userInfo = new UserInfo { Id = "user1" };
            var entitlementName = "Entitlement1";
            _mockRepo.Setup(repo => repo.GetByIdAsync<UserAccess, int>(It.IsAny<System.Linq.Expressions.Expression<Func<UserAccess, bool>>>()))
                .ReturnsAsync((UserAccess)null);

            // Act
            var result = await _service.RemoveAsync(userInfo, entitlementName, "loginUser");

            // Assert
            Assert.StartsWith("user does not have entitlement", result);
        }

        [Fact]
        public async Task RemoveAsync_Success_UpdatesUserAccessAndReturnsEmptyString()
        {
            // Arrange
            var userInfo = new UserInfo { Id = "user1" };
            var entitlementName = "Entitlement1";
            var userAccess = new UserAccess { UserId = "user1", AccessLevel = 1, IsActive = true };
            _mockRepo.Setup(repo => repo.GetByIdAsync<UserAccess, int>(It.IsAny<System.Linq.Expressions.Expression<Func<UserAccess, bool>>>()))
                .ReturnsAsync(userAccess);

            // Act
            var result = await _service.RemoveAsync(userInfo, entitlementName, "loginUser");

            // Assert
            Assert.Equal(string.Empty, result);
            Assert.False(userAccess.IsActive);
            _mockRepo.Verify(repo => repo.UpdateAsync(userAccess, userInfo.Id), Times.Once);
        }
    }
}
