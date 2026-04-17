using Microsoft.AspNetCore.Http;
using System.Text;

namespace LiveMap.Tests.Helpers;

public static class TestFileFactory
{
    public static IFormFile Create(string fileName, string contentType, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    public static IFormFile Empty(string fileName = "empty.jpg", string contentType = "image/jpeg")
    {
        var stream = new MemoryStream(Array.Empty<byte>());
        return new FormFile(stream, 0, 0, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
