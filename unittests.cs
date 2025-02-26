using Xunit;
using Moq;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public class BlobContainerServiceTests
{
    [Fact]
    public async Task DeleteFileAsync_SuccessfulDeletion_ReturnsTrue()
    {
        // Arrange
        var mockBlobContainerClient = new Mock<BlobContainerClient>();
        var mockBlobClient = new Mock<BlobClient>();
        var mockLogger = new Mock<ILogger<BlobContainerService>>();

        mockBlobContainerClient.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
        mockBlobClient.Setup(c => c.DeleteAsync(default, default, default)).ReturnsAsync(Response.FromValue(default(Azure.Response), default(ResponseHeaders)));

        var service = new BlobContainerService(mockBlobContainerClient.Object, mockLogger.Object);

        // Act
        var result = await service.DeleteFileAsync("testfile.txt");

        // Assert
        Assert.True(result);
        mockBlobClient.Verify(c => c.DeleteAsync(default, default, default), Times.Once);
    }

    [Fact]
    public async Task DeleteFileAsync_DeletionFails_ReturnsFalse()
    {
        // Arrange
        var mockBlobContainerClient = new Mock<BlobContainerClient>();
        var mockBlobClient = new Mock<BlobClient>();
        var mockLogger = new Mock<ILogger<BlobContainerService>>();

        mockBlobContainerClient.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
        mockBlobClient.Setup(c => c.DeleteAsync(default, default, default)).ThrowsAsync(new RequestFailedException("Deletion failed"));

        var service = new BlobContainerService(mockBlobContainerClient.Object, mockLogger.Object);

        // Act
        var result = await service.DeleteFileAsync("testfile.txt");

        // Assert
        Assert.False(result);
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error deleting file")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task DownloadFileAsync_SuccessfulDownload_ReturnsStream()
    {
        // Arrange
        var mockBlobContainerClient = new Mock<BlobContainerClient>();
        var mockBlobClient = new Mock<BlobClient>();
        var mockLogger = new Mock<ILogger<BlobContainerService>>();
        var mockResponse = Response.FromValue(BlobsModelFactory.BlobDownloadResult(content: new MemoryStream(Encoding.UTF8.GetBytes("test content"))), default(ResponseHeaders));

        mockBlobContainerClient.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
        mockBlobClient.Setup(c => c.DownloadStreamingAsync(default, default, default)).ReturnsAsync(mockResponse);

        var service = new BlobContainerService(mockBlobContainerClient.Object, mockLogger.Object);

        // Act
        var result = await service.DownloadFileAsync("testfile.txt");

        // Assert
        Assert.NotNull(result);
        using (var reader = new StreamReader(result))
        {
            Assert.Equal("test content", reader.ReadToEnd());
        }
        mockBlobClient.Verify(c => c.DownloadStreamingAsync(default, default, default), Times.Once);
    }

    [Fact]
    public async Task DownloadFileAsync_DownloadFails_ReturnsNull()
    {
        // Arrange
        var mockBlobContainerClient = new Mock<BlobContainerClient>();
        var mockBlobClient = new Mock<BlobClient>();
        var mockLogger = new Mock<ILogger<BlobContainerService>>();

        mockBlobContainerClient.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
        mockBlobClient.Setup(c => c.DownloadStreamingAsync(default, default, default)).ThrowsAsync(new RequestFailedException("Download failed"));

        var service = new BlobContainerService(mockBlobContainerClient.Object, mockLogger.Object);

        // Act
        var result = await service.DownloadFileAsync("testfile.txt");

        // Assert
        Assert.Null(result);
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error downloading file")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task UploadFileAsync_SuccessfulUpload_ReturnsBlobUri()
    {
        // Arrange
        var mockBlobContainerClient = new Mock<BlobContainerClient>();
        var mockBlobClient = new Mock<BlobClient>();
        var mockLogger = new Mock<ILogger<BlobContainerService>>();
        var mockFormFile = new Mock<IFormFile>();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("test upload content"));

        mockBlobContainerClient.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
        mockFormFile.Setup(f => f.OpenReadStream()).Returns(stream);
        mockBlobClient.Setup(c => c.UploadAsync(It.IsAny<Stream>(), default, default, default, default, default, default, default)).ReturnsAsync(Response.FromValue(default(Azure.Storage.Blobs.Models.BlobContentInfo), default(ResponseHeaders)));
        mockBlobClient.Setup(c => c.Uri).Returns(new Uri("http://testblob.com/testfile.txt"));

        var service = new BlobContainerService(mockBlobContainerClient.Object, mockLogger.Object);

        // Act
        var result = await service.UploadFileAsync(mockFormFile.Object, "testfile.txt");

        // Assert
        Assert.Equal("http://testblob.com/testfile.txt", result);
        mockBlobClient.Verify(c => c.UploadAsync(It.IsAny<Stream>(), default, default, default, default, default, default, default), Times.Once);
    }

    [Fact]
    public async Task UploadFileAsync_UploadFails_ReturnsNull()
    {
        // Arrange
        var mockBlobContainerClient = new Mock<BlobContainerClient>();
        var mockBlobClient = new Mock<BlobClient>();
        var mockLogger = new Mock<ILogger<BlobContainerService>>();
        var mockFormFile = new Mock<IFormFile>();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("test upload content"));

        mockBlobContainerClient.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
        mockFormFile.Setup(f => f.OpenReadStream()).Returns(stream);
        mockBlobClient.Setup(c => c.UploadAsync(It.IsAny<Stream>(), default, default, default, default, default, default, default)).ThrowsAsync(new RequestFailedException("Upload failed"));

        var service = new BlobContainerService(mockBlobContainerClient.Object, mockLogger.Object);

        // Act
        var result = await service.UploadFileAsync(mockFormFile.Object, "testfile.txt");

        // Assert
        Assert.Null(result);
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error uploading file")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once);
    }
}
