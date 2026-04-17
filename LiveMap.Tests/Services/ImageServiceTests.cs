using LiveMap.Core.Services;
using LiveMap.Core.Utilities;
using LiveMap.Tests.Helpers;
using Microsoft.Extensions.Options;

namespace LiveMap.Tests.Services;

public class ImageServiceTests
{
    private static ImageService CreateService()
    {
        var options = Options.Create(new CloudinarySettings
        {
            CloudName = "demo",
            ApiKey = "key",
            ApiSecret = "secret"
        });

        return new ImageService(options);
    }

    [Fact]
    public async Task UploadImageAsync_Throws_WhenFileIsNull()
    {
        var service = CreateService();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.UploadImageAsync(null!, "test", "folder"));

        Assert.Equal("File is empty!", ex.Message);
    }

    [Fact]
    public async Task UploadImageAsync_Throws_WhenFileIsEmpty()
    {
        var service = CreateService();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.UploadImageAsync(TestFileFactory.Empty(), "test", "folder"));

        Assert.Equal("File is empty!", ex.Message);
    }

    [Theory]
    [InlineData("document.pdf")]
    [InlineData("image.gif")]
    [InlineData("noextension")]
    public async Task UploadImageAsync_Throws_WhenExtensionIsInvalid(string fileName)
    {
        var service = CreateService();
        var file = TestFileFactory.Create(fileName, "application/octet-stream", "content");

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.UploadImageAsync(file, "test", "folder"));

        Assert.Equal("Invalid file type. Allowed types are: .jpg, .jpeg, .png", ex.Message);
    }
}
