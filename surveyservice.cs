using Xunit;
using Moq;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

// Assuming you have these models and interfaces defined
public class Attachment {
    public int Id { get; set; }
    public string AttachmentName { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public string AttachmentType { get; set; } // Assuming you have this property
}

public class IdeaAttachment {
    public int Id { get; set; }
    public string AttachmentName { get; set; }
    public int IdeaId { get; set; }
    public bool IsActive { get; set; }
    public DateTime UpdatedDate { get; set; }
    public long AttachmentSize { get; set; }
    public string UpdatedBy { get; set; }
}

public class AppConfig {
    public int Id { get; set; }
    public bool IsActive { get; set; }
    public string ParanCd { get; set; }
    public string ParanValue { get; set; }
}

public interface IRepository<T> {
    Task<T> GetByIdAsync(int id);
    Task<T> UpdateAsync(T entity);
    Task<List<T>> GetAllAsync();
    Task<T> GetByIdAsync<T, TKey>(System.Linq.Expressions.Expression<Func<T, bool>> predicate);
    Task<T> InsentAsync<T, TKey>(string loginUserId, T entity);
    Task<bool> InactivateAsync<T, TKey>(string loginUserId, TKey id);
    T GetById<T, TKey>(System.Linq.Expressions.Expression<Func<T, bool>> predicate);
}

public class FileUploadDownloadException : Exception {
    public FileUploadDownloadException(string message) : base(message) { }
}

public static class AppConstants {
    public static class ErrorMessages {
        public const string FILE_NOT_FOUND = "File not found.";
        public const string NOT_FOUND = "Not found.";
        public const string FILE_UPLOAD_DUPLICATE = "File upload duplicate.";
        public const string FILE_UPLOAD_UNSUPPORTED_EXTENSION = "File upload unsupported extension.";
        public const string FILE_UPLOAD_FAILED = "File upload failed.";
    }

    public static class AppConfigLabels {
        public const string AllowedFileTypes = "AllowedFileTypes";
        public const string UploadPath = "UploadPath";
    }
}

public class AttachedFile {
    public int Id { get; set; }
    public string Name { get; set; }
}

public class BlobContainerService {
    public Task<bool> DeleteFileAsync(string fileName) { return Task.FromResult(true); }
    public Task<Stream> DownloadFileAsync(string fileName) { return Task.FromResult<Stream>(new MemoryStream()); }
    public Task<string> UploadFileAsync(IFormFile file, string blobName) { return Task.FromResult("http://testblob.com/testfile.txt"); }
}

public class FileService : ServiceBase, IFileService {
    private readonly BlobContainerService _blobContainerService;

    public FileService(IRepository<Attachment> repository, ILogger<FileService> logger, IMapper mapper, IWebHostEnvironment hostingEnvironment, BlobContainerService blobContainerService)
        : base(repository, logger, mapper, hostingEnvironment) {
        _blobContainerService = blobContainerService;
        InitLogger(logger);
    }

    public async Task<bool> DeleteFileAsync(int fileId, string loginUserId) {
        var attachment = await Repository.GetByIdAsync(fileId);
        if (attachment == null) {
            throw new FileUploadDownloadException(AppConstants.ErrorMessages.NOT_FOUND);
        }

        attachment.IsDeleted = true;
        await Repository.UpdateAsync(attachment);
        await _blobContainerService.DeleteFileAsync($"[{attachment.Id}] {attachment.AttachmentName}");
        return true;
    }

    public async Task<Stream> DownloadFileAsync(int attachmentId) {
        var attachment = await Repository.GetByIdAsync(attachmentId);
        if (attachment == null || !attachment.IsActive) {
            throw new FileUploadDownloadException(AppConstants.ErrorMessages.FILE_NOT_FOUND);
        }

        var fileStream = await _blobContainerService.DownloadFileAsync($"[{attachment.Id}] {attachment.AttachmentName}");
        if (fileStream == null) {
            throw new FileUploadDownloadException(AppConstants.ErrorMessages.FILE_NOT_FOUND);
        }

        return fileStream;
    }

    public async Task<List<AttachedFile>> GetIdeaFiles(int ideaId) {
        var results = await Repository.GetAllAsync();
        var attachedFiles = results.Where(x => x.AttachmentType == "idea" && x.Id == ideaId && x.IsActive).ToList();

        if (attachedFiles == null || !attachedFiles.Any()) {
            return new List<AttachedFile>();
        }

        var attachedFilesList = new List<AttachedFile>();
        foreach (var item in attachedFiles) {
            attachedFilesList.Add(new AttachedFile { Id = item.Id, Name = item.AttachmentName });
        }

        return attachedFilesList;
    }

    public async Task<bool> UploadFileAsync(IFormFile file, int ideaId, string loginUserId) {
        var ideaAttachment = await Repository.GetByIdAsync<IdeaAttachment, int>(i => i.IdeaId == ideaId && i.AttachmentName == file.FileName && i.IsActive);
        if (ideaAttachment != null) {
            throw new FileUploadDownloadException(AppConstants.ErrorMessages.FILE_UPLOAD_DUPLICATE);
        }

        string fileExtension = Path.GetExtension(file.FileName).Replace(".", "").ToLower();
        AppConfig allowedFileTypeConfiguration = await Repository.GetByIdAsync<AppConfig, int>(x => x.IsActive && x.ParanCd == AppConstants.AppConfigLabels.AllowedFileTypes);
        bool isValidFileExtension = allowedFileTypeConfiguration?.ParanValue?.Split(',')?.Any(x => x.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)) ?? false;
        if (!isValidFileExtension) {
            throw new FileUploadDownloadException(AppConstants.ErrorMessages.FILE_UPLOAD_UNSUPPORTED_EXTENSION);
        }

        IdeaAttachment savedAttachment = await SaveFileInfoToDatabaseAsync(ideaId, file.FileName, loginUserId, file.Length);
        if (savedAttachment == null) {
            throw new FileUploadDownloadException(AppConstants.ErrorMessages.FILE_UPLOAD_FAILED);
        }

        string fileUri = await _blobContainerService.UploadFileAsync(file, $"({ideaId.ToString()}) ({savedAttachment.Id}) {file.FileName}");
        if (string.IsNullOrEmpty(fileUri)) {
            await Repository.InactivateAsync<IdeaAttachment, int>(loginUserId, savedAttachment.Id);
            throw new FileUploadDownloadException(AppConstants.ErrorMessages.FILE_UPLOAD_FAILED);
        }

        return savedAttachment != null && !string.IsNullOrEmpty(fileUri);
    }

    public async Task<IdeaAttachment> SaveFileInfoToDatabaseAsync(int ideaId, string fileName, string loginUserId, long fileSizeBytes) {
        IdeaAttachment attachment = new IdeaAttachment();
        attachment.AttachmentName = fileName;
        attachment.IdeaId = ideaId;
        attachment.IsActive = true;
        attachment.UpdatedDate = DateTime.Now;
        attachment.AttachmentSize = fileSizeBytes;
        attachment.UpdatedBy = loginUserId;

        attachment = await Repository.InsentAsync<IdeaAttachment, int>(loginUserId, attachment);
        return attachment;
    }

    public string GetContentType(string path) {
        var provider = new FileExtensionContentTypeProvider();
        string contentType = string.Empty;
        if (provider.TryGetContentType(path, out contentType)) {
            return contentType;
        }

        return "application/octet-stream";
    }

    public async Task<string> GetIdeaFilePathAsync(int fileId) {
        IdeaAttachment file = await Repository.GetByIdAsync<IdeaAttachment, int>(fileId);
        string uploadPath = GetFileUploadPath(file.IdeaId);
        string filePath = Path.Combine(uploadPath, file.AttachmentName);
        return filePath;
    }

    private string GetFileUploadPath(int? ideaId) {
        string? path = string.Empty;
        if (HostingEnvironment.IsEnvironment("local")) {
            path = "C:\\temp";
        } else {
            AppConfig uploadPathConfiguration = Repository.GetById<AppConfig, int>(x => x.IsActive && x




using Xunit;
using Moq;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

// Assuming you have these models and interfaces defined
public class Attachment {
    public int Id { get; set; }
    public string AttachmentName { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public string AttachmentType { get; set; } // Assuming you have this property
}

public class IdeaAttachment {
    public int Id { get; set; }
    public string AttachmentName { get; set; }
    public int IdeaId { get; set; }
    public bool IsActive { get; set; }
    public DateTime UpdatedDate { get; set; }
    public long AttachmentSize { get; set; }
    public string UpdatedBy { get; set; }
}

public class AppConfig {
    public int Id { get; set; }
    public bool IsActive { get; set; }
    public string ParanCd { get; set; }
    public string ParanValue { get; set; }
}

public interface IRepository<T> {
    Task<T> GetByIdAsync(int id);
    Task<T> UpdateAsync(T entity);
    Task<List<T>> GetAllAsync();
    Task<T> GetByIdAsync<T, TKey>(System.Linq.Expressions.Expression<Func<T, bool>> predicate);
    Task<T> InsentAsync<T, TKey>(string loginUserId, T entity);
    Task<bool> InactivateAsync<T, TKey>(string loginUserId, TKey id);
}

public class FileUploadDownloadException : Exception {
    public FileUploadDownloadException(string message) : base(message) { }
}

public static class AppConstants {
    public static class ErrorMessages {
        public const string FILE_NOT_FOUND = "File not found.";
        public const string NOT_FOUND = "Not found.";
        public const string FILE_UPLOAD_DUPLICATE = "File upload duplicate.";
        public const string FILE_UPLOAD_UNSUPPORTED_EXTENSION = "File upload unsupported extension.";
        public const string FILE_UPLOAD_FAILED = "File upload failed.";
    }

    public static class AppConfigLabels {
        public const string AllowedFileTypes = "AllowedFileTypes";
    }
}

public class AttachedFile {
    public int Id { get; set; }
    public string Name { get; set; }
}

public class BlobContainerService {
    public Task<bool> DeleteFileAsync(string fileName) { return Task.FromResult(true); }
    public Task<Stream> DownloadFileAsync(string fileName) { return Task.FromResult<Stream>(new MemoryStream()); }
    public Task<string> UploadFileAsync(IFormFile file, string blobName) { return Task.FromResult("http://testblob.com/testfile.txt"); }
}

public class FileService : ServiceBase, IFileService {
    private readonly BlobContainerService _blobContainerService;

    public FileService(IRepository<Attachment> repository, ILogger<FileService> logger, IMapper mapper, IWebHostEnvironment hostingEnvironment, BlobContainerService blobContainerService)
        : base(repository, logger, mapper, hostingEnvironment) {
        _blobContainerService = blobContainerService;
        InitLogger(logger);
    }

    public async Task<bool> DeleteFileAsync(int fileId, string loginUserId) {
        var attachment = await Repository.GetByIdAsync(fileId);
        if (attachment == null) {
            throw new FileUploadDownloadException(AppConstants.ErrorMessages.NOT_FOUND);
        }

        attachment.IsDeleted = true;
        await Repository.UpdateAsync(attachment);
        await _blobContainerService.DeleteFileAsync($"[{attachment.Id}] {attachment.AttachmentName}");
        return true;
    }

    public async Task<Stream> DownloadFileAsync(int attachmentId) {
        var attachment = await Repository.GetByIdAsync(attachmentId);
        if (attachment == null || !attachment.IsActive) {
            throw new FileUploadDownloadException(AppConstants.ErrorMessages.FILE_NOT_FOUND);
        }

        var fileStream = await _blobContainerService.DownloadFileAsync($"[{attachment.Id}] {attachment.AttachmentName}");
        if (fileStream == null) {
            throw new FileUploadDownloadException(AppConstants.ErrorMessages.FILE_NOT_FOUND);
        }

        return fileStream;
    }

    public async Task<List<AttachedFile>> GetIdeaFiles(int ideaId) {
        var results = await Repository.GetAllAsync();
        var attachedFiles = results.Where(x => x.AttachmentType == "idea" && x.Id == ideaId && x.IsActive).ToList();

        if (attachedFiles == null || !attachedFiles.Any()) {
            return new List<AttachedFile>();
        }

        var attachedFilesList = new List<AttachedFile>();
        foreach (var item in attachedFiles) {
            attachedFilesList.Add(new AttachedFile { Id = item.Id, Name = item.AttachmentName });
        }

        return attachedFilesList;
    }

    public async Task<bool> UploadFileAsync(IFormFile file, int ideaId, string loginUserId) {
        var ideaAttachment = await Repository.GetByIdAsync<IdeaAttachment, int>(i => i.IdeaId == ideaId && i.AttachmentName == file.FileName && i.IsActive);
        if (ideaAttachment != null) {
            throw new FileUploadDownloadException(AppConstants.ErrorMessages.FILE_UPLOAD_DUPLICATE);
        }

        string fileExtension = Path.GetExtension(file.FileName).Replace(".", "").ToLower();
        AppConfig allowedFileTypeConfiguration = await Repository.GetByIdAsync<AppConfig, int>(x => x.IsActive && x.ParanCd == AppConstants.AppConfigLabels.AllowedFileTypes);
        bool isValidFileExtension = allowedFileTypeConfiguration?.ParanValue?.Split(',')?.Any(x => x.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)) ?? false;
        if (!isValidFileExtension) {
            throw new FileUploadDownloadException(AppConstants.ErrorMessages.FILE_UPLOAD_UNSUPPORTED_EXTENSION);
        }

        IdeaAttachment savedAttachment = await SaveFileInfoToDatabaseAsync(ideaId, file.FileName, loginUserId, file.Length);
        if (savedAttachment == null) {
            throw new FileUploadDownloadException(AppConstants.ErrorMessages.FILE_UPLOAD_FAILED);
        }

        string fileUri = await _blobContainerService.UploadFileAsync(file, $"({ideaId.ToString()}) ({savedAttachment.Id}) {file.FileName}");
        if (string.IsNullOrEmpty(fileUri)) {
            await Repository.InactivateAsync<IdeaAttachment, int>(loginUserId, savedAttachment.Id);
            throw new FileUploadDownloadException(AppConstants.ErrorMessages.FILE_UPLOAD_FAILED);
        }

        return savedAttachment != null && !string.IsNullOrEmpty(fileUri);
    }

    public async Task<IdeaAttachment> SaveFileInfoToDatabaseAsync(int ideaId, string fileName, string loginUserId, long fileSizeBytes) {
        IdeaAttachment attachment = new IdeaAttachment();
        attachment.AttachmentName = fileName;
        attachment.IdeaId = ideaId;
        attachment.IsActive = true;
        attachment.UpdatedDate = DateTime.Now;
        attachment.AttachmentSize = fileSizeBytes;
        attachment.UpdatedBy = loginUserId;

        attachment = await Repository.InsentAsync<IdeaAttachment, int>(loginUserId, attachment);
        return attachment;
    }

    public string GetContentType(string path) {
        var provider = new FileExtensionContentTypeProvider();
        string contentType = string.Empty;
        if (provider.TryGetContentType(path, out contentType)) {
            return contentType;
        }

        return "application/octet-stream";
    }
}

// Mock ServiceBase and IFileService for testing
public class ServiceBase {
    protected IRepository<Attachment> Repository { get; }
    protected ILogger<FileService> Logger { get; }
    protected IMapper Mapper { get; }
    protected IWebHostEnvironment HostingEnvironment { get; }

    public ServiceBase(IRepository<Attachment> repository, ILogger<FileService> logger, IMapper mapper, IWebHostEnvironment hostingEnvironment) {
        Repository = repository;
        Logger = logger;
        Mapper = mapper;
        HostingEnvironment = hostingEnvironment;
    }

    public void InitLogger(ILogger<FileService> logger) { }
}

public interface IFileService {
    Task<bool> DeleteFileAsync(int fileId, string loginUserId);
    Task<Stream> DownloadFileAsync(int attachmentId);
    Task<List


using Xunit;
using Moq;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Assuming you have these models and interfaces defined
public class Attachment {
    public int Id { get; set; }
    public string AttachmentName { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public string AttachmentType { get; set; } // Assuming you have this property
}

public interface IRepository<T> {
    Task<T> GetByIdAsync(int id);
    Task<T> UpdateAsync(T entity);
    Task<List<T>> GetAllAsync();
}

public class FileUploadDownloadException : Exception {
    public FileUploadDownloadException(string message) : base(message) { }
}

public static class AppConstants {
    public static class ErrorMessages {
        public const string FILE_NOT_FOUND = "File not found.";
        public const string NOT_FOUND = "Not found.";
    }
}

public class AttachedFile {
    public int Id { get; set; }
    public string Name { get; set; }
}

public class BlobContainerService {
    public Task<bool> DeleteFileAsync(string fileName) { return Task.FromResult(true); }
    public Task<Stream> DownloadFileAsync(string fileName) { return Task.FromResult<Stream>(new MemoryStream()); }
}

public class FileService : ServiceBase, IFileService {
    private readonly BlobContainerService _blobContainerService;

    public FileService(IRepository<Attachment> repository, ILogger<FileService> logger, IMapper mapper, IWebHostEnvironment hostingEnvironment, BlobContainerService blobContainerService)
        : base(repository, logger, mapper, hostingEnvironment) {
        _blobContainerService = blobContainerService;
        InitLogger(logger);
    }

    public async Task<bool> DeleteFileAsync(int fileId, string loginUserId) {
        var attachment = await Repository.GetByIdAsync(fileId);
        if (attachment == null) {
            throw new FileUploadDownloadException(AppConstants.ErrorMessages.NOT_FOUND);
        }

        attachment.IsDeleted = true;
        await Repository.UpdateAsync(attachment);
        await _blobContainerService.DeleteFileAsync($"[{attachment.Id}] {attachment.AttachmentName}");
        return true;
    }

    public async Task<Stream> DownloadFileAsync(int attachmentId) {
        var attachment = await Repository.GetByIdAsync(attachmentId);
        if (attachment == null || !attachment.IsActive) {
            throw new FileUploadDownloadException(AppConstants.ErrorMessages.FILE_NOT_FOUND);
        }

        var fileStream = await _blobContainerService.DownloadFileAsync($"[{attachment.Id}] {attachment.AttachmentName}");
        if (fileStream == null) {
            throw new FileUploadDownloadException(AppConstants.ErrorMessages.FILE_NOT_FOUND);
        }

        return fileStream;
    }

    public async Task<List<AttachedFile>> GetIdeaFiles(int ideaId) {
        var results = await Repository.GetAllAsync();
        var attachedFiles = results.Where(x => x.AttachmentType == "idea" && x.Id == ideaId && x.IsActive).ToList();

        if (attachedFiles == null || !attachedFiles.Any()) {
            return new List<AttachedFile>();
        }

        var attachedFilesList = new List<AttachedFile>();
        foreach (var item in attachedFiles) {
            attachedFilesList.Add(new AttachedFile { Id = item.Id, Name = item.AttachmentName });
        }

        return attachedFilesList;
    }
}

// Mock ServiceBase and IFileService for testing
public class ServiceBase {
    protected IRepository<Attachment> Repository { get; }
    protected ILogger<FileService> Logger { get; }
    protected IMapper Mapper { get; }
    protected IWebHostEnvironment HostingEnvironment { get; }

    public ServiceBase(IRepository<Attachment> repository, ILogger<FileService> logger, IMapper mapper, IWebHostEnvironment hostingEnvironment) {
        Repository = repository;
        Logger = logger;
        Mapper = mapper;
        HostingEnvironment = hostingEnvironment;
    }

    public void InitLogger(ILogger<FileService> logger) { }
}

public interface IFileService {
    Task<bool> DeleteFileAsync(int fileId, string loginUserId);
    Task<Stream> DownloadFileAsync(int attachmentId);
    Task<List<AttachedFile>> GetIdeaFiles(int ideaId);
}

public class FileServiceTests {
    [Fact]
    public async Task DeleteFileAsync_SuccessfulDeletion_ReturnsTrue() {
        // Arrange
        var mockRepository = new Mock<IRepository<Attachment>>();
        var mockLogger = new Mock<ILogger<FileService>>();
        var mockMapper = new Mock<IMapper>();
        var mockHostingEnvironment = new Mock<IWebHostEnvironment>();
        var mockBlobContainerService = new Mock<BlobContainerService>();

        var attachment = new Attachment { Id = 1, AttachmentName = "test.txt" };
        mockRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(attachment);
        mockRepository.Setup(repo => repo.UpdateAsync(attachment)).ReturnsAsync(attachment);
        mockBlobContainerService.Setup(blob => blob.DeleteFileAsync($"[{attachment.Id}] {attachment.AttachmentName}")).ReturnsAsync(true);

        var fileService = new FileService(mockRepository.Object, mockLogger.Object, mockMapper.Object, mockHostingEnvironment.Object, mockBlobContainerService.Object);

        // Act
        var result = await fileService.DeleteFileAsync(1, "user1");

        // Assert
        Assert.True(result);
        mockRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        mockRepository.Verify(repo => repo.UpdateAsync(attachment), Times.Once);
        mockBlobContainerService.Verify(blob => blob.DeleteFileAsync($"[{attachment.Id}] {attachment.AttachmentName}"), Times.Once);
    }

    [Fact]
    public async Task DeleteFileAsync_AttachmentNotFound_ThrowsException() {
        // Arrange
        var mockRepository = new Mock<IRepository<Attachment>>();
        var mockLogger = new Mock<ILogger<FileService>>();
        var mockMapper = new Mock<IMapper>();
        var mockHostingEnvironment = new Mock<IWebHostEnvironment>();
        var mockBlobContainerService = new Mock<BlobContainerService>();

        mockRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((Attachment)null);

        var fileService = new FileService(mockRepository.Object, mockLogger.Object, mockMapper.Object, mockHostingEnvironment.Object, mockBlobContainerService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<FileUploadDownloadException>(() => fileService.DeleteFileAsync(1, "user1"));
    }

    [Fact]
    public async Task DownloadFileAsync_SuccessfulDownload_ReturnsStream() {
        // Arrange
        var mockRepository = new Mock<IRepository<Attachment>>();
        var mockLogger = new Mock<ILogger<FileService>>();
        var mockMapper = new Mock<IMapper>();
        var mockHostingEnvironment = new Mock<IWebHostEnvironment>();
        var mockBlobContainerService = new Mock<BlobContainerService>();

        var attachment = new Attachment { Id = 1, AttachmentName = "test.txt", IsActive = true };
        mockRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(attachment);
        mockBlobContainerService.Setup(blob => blob.DownloadFileAsync($"[{attachment.Id}] {attachment.AttachmentName}")).ReturnsAsync(new MemoryStream());

        var fileService = new FileService(mockRepository.Object, mockLogger.Object, mockMapper.Object, mockHostingEnvironment.Object, mockBlobContainerService.Object);

        // Act
        var result = await fileService.DownloadFileAsync(1);

        // Assert
        Assert.NotNull(result);
        mockRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        mockBlobContainerService.Verify(blob => blob.DownloadFileAsync($"[{attachment.Id}] {attachment.AttachmentName}"), Times.Once);
    }

    [Fact]
    public async Task DownloadFileAsync_AttachmentNotFound_ThrowsException() {
        // Arrange
        var mockRepository = new Mock<IRepository<Attachment>>();
        var mockLogger = new Mock<ILogger<FileService>>();
        var mockMapper = new Mock<IMapper>();
        var mockHostingEnvironment = new Mock<IWebHostEnvironment>();
        var mockBlobContainerService = new Mock<BlobContainerService>();

        mockRepository.Setup(repo => repo.Get
