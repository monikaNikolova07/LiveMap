using LiveMap.Data;
using Microsoft.EntityFrameworkCore;

namespace LiveMap.Tests.Helpers;

public static class TestDbFactory
{
    public static LiveMapDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LiveMapDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        var context = new LiveMapDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
