using LiveMap.Core.DTOs.Profiles;
using LiveMap.Core.Services;
using LiveMap.Data.Models;
using LiveMap.Tests.Helpers;

namespace LiveMap.Tests.Services;

public class ProfileServiceTests
{
    [Fact]
    public async Task GetProfileAsync_ReturnsNull_WhenUserIdIsInvalid()
    {
        using var context = TestDbFactory.CreateContext();
        var service = new ProfileService(context);

        var result = await service.GetProfileAsync("not-a-guid");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsMappedProfile_WithFolders()
    {
        using var context = TestDbFactory.CreateContext();
        var user = new User { Id = Guid.NewGuid(), UserName = "maria", NormalizedUserName = "MARIA" };
        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            Bio = "Traveler",
            ProfilePicture = "pic-url"
        };
        var folderA = new Folder { Id = Guid.NewGuid(), Name = "Bulgaria", ProfileId = profile.Id, Profile = profile };
        var folderB = new Folder { Id = Guid.NewGuid(), Name = "Italy", ProfileId = profile.Id, Profile = profile };
        profile.Folders.Add(folderA);
        profile.Folders.Add(folderB);

        context.Users.Add(user);
        context.Profiles.Add(profile);
        context.Folders.AddRange(folderA, folderB);
        await context.SaveChangesAsync();

        var service = new ProfileService(context);

        var result = await service.GetProfileAsync(user.Id.ToString());

        Assert.NotNull(result);
        Assert.Equal(profile.Id, result!.Id);
        Assert.Equal("maria", result.Username);
        Assert.Equal("Traveler", result.Bio);
        Assert.Equal("pic-url", result.ProfilePicture);
        Assert.Equal(2, result.Folders.Count);
        Assert.Contains(result.Folders, x => x.Name == "Bulgaria");
        Assert.Contains(result.Folders, x => x.Name == "Italy");
    }

    [Fact]
    public async Task GetProfileForEditAsync_ReturnsNull_WhenUserIdIsInvalid()
    {
        using var context = TestDbFactory.CreateContext();
        var service = new ProfileService(context);

        var result = await service.GetProfileForEditAsync("bad-id");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProfileForEditAsync_ReturnsEditableData()
    {
        using var context = TestDbFactory.CreateContext();
        var user = new User { Id = Guid.NewGuid(), UserName = "desi", NormalizedUserName = "DESI" };
        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            Bio = "Bio text",
            ProfilePicture = "avatar.png"
        };

        context.Users.Add(user);
        context.Profiles.Add(profile);
        await context.SaveChangesAsync();

        var service = new ProfileService(context);

        var result = await service.GetProfileForEditAsync(user.Id.ToString());

        Assert.NotNull(result);
        Assert.Equal(profile.Id, result!.Id);
        Assert.Equal("desi", result.Username);
        Assert.Equal("Bio text", result.Bio);
        Assert.Equal("avatar.png", result.ProfilePicture);
    }

    [Fact]
    public async Task EditProfileAsync_DoesNothing_WhenUserIdIsInvalid()
    {
        using var context = TestDbFactory.CreateContext();
        var user = new User { Id = Guid.NewGuid(), UserName = "user", NormalizedUserName = "USER" };
        var profile = new Profile { Id = Guid.NewGuid(), UserId = user.Id, User = user, Bio = "Bio", ProfilePicture = "pic" };
        context.Users.Add(user);
        context.Profiles.Add(profile);
        await context.SaveChangesAsync();

        var service = new ProfileService(context);

        await service.EditProfileAsync(new ProfileEditDto { Username = "changed", Bio = "Changed", ProfilePicture = "changed" }, "invalid-guid");

        Assert.Equal("Bio", context.Profiles.Single().Bio);
        Assert.Equal("user", context.Users.Single().UserName);
    }

    [Fact]
    public async Task EditProfileAsync_DoesNothing_WhenProfileDoesNotExist()
    {
        using var context = TestDbFactory.CreateContext();
        var service = new ProfileService(context);

        var dto = new ProfileEditDto { Username = "ghost", Bio = "none", ProfilePicture = "none" };
        await service.EditProfileAsync(dto, Guid.NewGuid().ToString());

        Assert.Empty(context.Profiles);
        Assert.Empty(context.Users);
    }
}
