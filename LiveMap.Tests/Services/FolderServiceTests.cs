using CloudinaryDotNet;
using LiveMap.Core.DTOs.Folders;
using LiveMap.Core.Services;
using LiveMap.Data.Models;
using LiveMap.Tests.Helpers;

namespace LiveMap.Tests.Services;

public class FolderServiceTests
{
    private static Cloudinary CreateCloudinary() => new(new Account("demo", "key", "secret"));

    [Fact]
    public async Task GetAllAsync_ReturnsAllFolders_WhenCurrentUserIsNull()
    {
        using var context = TestDbFactory.CreateContext();
        var owner = SeedUserWithProfile(context, "owner");
        var another = SeedUserWithProfile(context, "another");
        var alpha = new Folder { Id = Guid.NewGuid(), Name = "Alpha", ProfileId = owner.Profile!.Id, Profile = owner.Profile, Acssesability = Acssesability.Public };
        var beta = new Folder { Id = Guid.NewGuid(), Name = "Beta", ProfileId = another.Profile!.Id, Profile = another.Profile, Acssesability = Acssesability.Private };
        context.Folders.AddRange(alpha, beta);
        await context.SaveChangesAsync();

        var service = new FolderService(context, CreateCloudinary());

        var result = (await service.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { "Alpha", "Beta" }, result.Select(x => x.Name));
    }

    [Fact]
    public async Task GetAllAsync_FiltersByCurrentUser()
    {
        using var context = TestDbFactory.CreateContext();
        var owner = SeedUserWithProfile(context, "owner");
        var another = SeedUserWithProfile(context, "another");
        context.Folders.AddRange(
            new Folder { Id = Guid.NewGuid(), Name = "Mine", ProfileId = owner.Profile!.Id, Profile = owner.Profile, Acssesability = Acssesability.Public },
            new Folder { Id = Guid.NewGuid(), Name = "NotMine", ProfileId = another.Profile!.Id, Profile = another.Profile, Acssesability = Acssesability.Public });
        await context.SaveChangesAsync();

        var service = new FolderService(context, CreateCloudinary());

        var result = (await service.GetAllAsync(owner.Id)).ToList();

        Assert.Single(result);
        Assert.Equal("Mine", result[0].Name);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenProfileIsMissing()
    {
        using var context = TestDbFactory.CreateContext();
        var service = new FolderService(context, CreateCloudinary());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(new FolderCreateDto { Name = "Trips" }, Guid.NewGuid()));

        Assert.Equal("Profile not found for the current user.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_CreatesRootFolder_WhenParentIsNull()
    {
        using var context = TestDbFactory.CreateContext();
        var user = SeedUserWithProfile(context, "owner");
        await context.SaveChangesAsync();
        var service = new FolderService(context, CreateCloudinary());

        var folder = await service.CreateAsync(new FolderCreateDto { Name = "Trips", Acssesability = Acssesability.FriendsOnly }, user.Id);

        Assert.Equal("Trips", folder.Name);
        Assert.Equal(user.Profile!.Id, folder.ProfileId);
        Assert.Equal(Acssesability.FriendsOnly, folder.Acssesability);
        Assert.Empty(context.FolderStructures);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenParentFolderDoesNotExist()
    {
        using var context = TestDbFactory.CreateContext();
        var user = SeedUserWithProfile(context, "owner");
        await context.SaveChangesAsync();
        var service = new FolderService(context, CreateCloudinary());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(new FolderCreateDto
        {
            Name = "Child",
            ParentFolderId = Guid.NewGuid(),
            Acssesability = Acssesability.Public
        }, user.Id));

        Assert.Equal("Parent folder not found.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenParentFolderBelongsToAnotherProfile()
    {
        using var context = TestDbFactory.CreateContext();
        var owner = SeedUserWithProfile(context, "owner");
        var another = SeedUserWithProfile(context, "another");
        var parent = new Folder { Id = Guid.NewGuid(), Name = "Parent", ProfileId = another.Profile!.Id, Profile = another.Profile, Acssesability = Acssesability.Public };
        context.Folders.Add(parent);
        await context.SaveChangesAsync();
        var service = new FolderService(context, CreateCloudinary());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(new FolderCreateDto
        {
            Name = "Child",
            ParentFolderId = parent.Id,
            Acssesability = Acssesability.Public
        }, owner.Id));

        Assert.Equal("You can only create subfolders in your own folders.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ForcesPrivateAccessibility_WhenRootTreeIsPrivate()
    {
        using var context = TestDbFactory.CreateContext();
        var user = SeedUserWithProfile(context, "owner");
        var parent = new Folder { Id = Guid.NewGuid(), Name = "PrivateRoot", ProfileId = user.Profile!.Id, Profile = user.Profile, Acssesability = Acssesability.Private };
        context.Folders.Add(parent);
        await context.SaveChangesAsync();
        var service = new FolderService(context, CreateCloudinary());

        var folder = await service.CreateAsync(new FolderCreateDto
        {
            Name = "Child",
            ParentFolderId = parent.Id,
            Acssesability = Acssesability.Public
        }, user.Id);

        Assert.Equal(Acssesability.Private, folder.Acssesability);
        Assert.Single(context.FolderStructures);
        Assert.Equal(parent.Id, context.FolderStructures.Single().FolderId);
        Assert.Equal(folder.Id, context.FolderStructures.Single().SubfolderId);
    }

    [Fact]
    public async Task GetAvailableAccessibilitiesForCreateAsync_ReturnsAllValues_ForRootFolderCreation()
    {
        using var context = TestDbFactory.CreateContext();
        var service = new FolderService(context, CreateCloudinary());

        var result = (await service.GetAvailableAccessibilitiesForCreateAsync(null)).ToArray();

        Assert.Equal(Enum.GetValues<Acssesability>(), result);
    }

    [Fact]
    public async Task GetAvailableAccessibilitiesForCreateAsync_ReturnsOnlyPrivate_WhenRootTreeIsPrivate()
    {
        using var context = TestDbFactory.CreateContext();
        var user = SeedUserWithProfile(context, "owner");
        var root = new Folder { Id = Guid.NewGuid(), Name = "Root", ProfileId = user.Profile!.Id, Profile = user.Profile, Acssesability = Acssesability.Private };
        var child = new Folder { Id = Guid.NewGuid(), Name = "Child", ProfileId = user.Profile.Id, Profile = user.Profile, Acssesability = Acssesability.Public };
        context.Folders.AddRange(root, child);
        context.FolderStructures.Add(new FolderStructure { FolderId = root.Id, SubfolderId = child.Id });
        await context.SaveChangesAsync();
        var service = new FolderService(context, CreateCloudinary());

        var result = (await service.GetAvailableAccessibilitiesForCreateAsync(child.Id)).ToArray();

        Assert.Single(result);
        Assert.Equal(Acssesability.Private, result[0]);
    }

    [Fact]
    public async Task GetUploadAccessibilityAsync_Throws_WhenFolderDoesNotExistAndRootIsNotPrivate()
    {
        using var context = TestDbFactory.CreateContext();
        var service = new FolderService(context, CreateCloudinary());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUploadAccessibilityAsync(Guid.NewGuid(), Acssesability.Public));
    }

    [Fact]
    public async Task GetUploadAccessibilityAsync_ReturnsPrivate_WhenRootTreeIsPrivate()
    {
        using var context = TestDbFactory.CreateContext();
        var user = SeedUserWithProfile(context, "owner");
        var root = new Folder { Id = Guid.NewGuid(), Name = "Root", ProfileId = user.Profile!.Id, Profile = user.Profile, Acssesability = Acssesability.Private };
        var child = new Folder { Id = Guid.NewGuid(), Name = "Child", ProfileId = user.Profile.Id, Profile = user.Profile, Acssesability = Acssesability.Public };
        context.Folders.AddRange(root, child);
        context.FolderStructures.Add(new FolderStructure { FolderId = root.Id, SubfolderId = child.Id });
        await context.SaveChangesAsync();
        var service = new FolderService(context, CreateCloudinary());

        var result = await service.GetUploadAccessibilityAsync(child.Id, Acssesability.Public);

        Assert.Equal(Acssesability.Private, result);
    }

    [Fact]
    public async Task GetUploadAccessibilityAsync_ReturnsPrivate_WhenCurrentFolderIsPrivate()
    {
        using var context = TestDbFactory.CreateContext();
        var user = SeedUserWithProfile(context, "owner");
        var folder = new Folder { Id = Guid.NewGuid(), Name = "PrivateLeaf", ProfileId = user.Profile!.Id, Profile = user.Profile, Acssesability = Acssesability.Private };
        context.Folders.Add(folder);
        await context.SaveChangesAsync();
        var service = new FolderService(context, CreateCloudinary());

        var result = await service.GetUploadAccessibilityAsync(folder.Id, Acssesability.Public);

        Assert.Equal(Acssesability.Private, result);
    }

    [Fact]
    public async Task GetUploadAccessibilityAsync_ReturnsRequestedAccessibility_WhenFolderAllowsIt()
    {
        using var context = TestDbFactory.CreateContext();
        var user = SeedUserWithProfile(context, "owner");
        var folder = new Folder { Id = Guid.NewGuid(), Name = "Open", ProfileId = user.Profile!.Id, Profile = user.Profile, Acssesability = Acssesability.Public };
        context.Folders.Add(folder);
        await context.SaveChangesAsync();
        var service = new FolderService(context, CreateCloudinary());

        var result = await service.GetUploadAccessibilityAsync(folder.Id, Acssesability.FriendsOnly);

        Assert.Equal(Acssesability.FriendsOnly, result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsFolderWithNestedData()
    {
        using var context = TestDbFactory.CreateContext();
        var user = SeedUserWithProfile(context, "owner");
        var folder = new Folder { Id = Guid.NewGuid(), Name = "Main", ProfileId = user.Profile!.Id, Profile = user.Profile, Acssesability = Acssesability.Public };
        var child = new Folder { Id = Guid.NewGuid(), Name = "Child", ProfileId = user.Profile.Id, Profile = user.Profile, Acssesability = Acssesability.Public };
        var picture = new Picture { Id = Guid.NewGuid(), FolderId = folder.Id, Folder = folder, URL = "x", Acssesability = Acssesability.Public };
        var like = new PictureLike { PictureId = picture.Id, Picture = picture, UserId = user.Id, User = user };
        var comment = new PictureComment { Id = Guid.NewGuid(), PictureId = picture.Id, Picture = picture, UserId = user.Id, User = user, Content = "nice" };
        context.Folders.AddRange(folder, child);
        context.FolderStructures.Add(new FolderStructure { FolderId = folder.Id, SubfolderId = child.Id });
        context.Pictures.Add(picture);
        context.PictureLikes.Add(like);
        context.PictureComments.Add(comment);
        await context.SaveChangesAsync();
        var service = new FolderService(context, CreateCloudinary());

        var result = await service.GetByIdAsync(folder.Id);

        Assert.NotNull(result);
        Assert.Equal("owner", result!.Profile.User.UserName);
        Assert.Single(result.Pictures);
        Assert.Single(result.Pictures.First().Likes);
        Assert.Single(result.Pictures.First().Comments);
        Assert.Single(result.Subfolders);
    }

    [Fact]
    public async Task GetPicturesAsync_ReturnsOnlyPicturesForRequestedFolder()
    {
        using var context = TestDbFactory.CreateContext();
        var user = SeedUserWithProfile(context, "owner");
        var folder1 = new Folder { Id = Guid.NewGuid(), Name = "One", ProfileId = user.Profile!.Id, Profile = user.Profile, Acssesability = Acssesability.Public };
        var folder2 = new Folder { Id = Guid.NewGuid(), Name = "Two", ProfileId = user.Profile.Id, Profile = user.Profile, Acssesability = Acssesability.Public };
        context.Folders.AddRange(folder1, folder2);
        context.Pictures.AddRange(
            new Picture { Id = Guid.NewGuid(), FolderId = folder1.Id, Folder = folder1, URL = "1", Acssesability = Acssesability.Public },
            new Picture { Id = Guid.NewGuid(), FolderId = folder1.Id, Folder = folder1, URL = "2", Acssesability = Acssesability.Public },
            new Picture { Id = Guid.NewGuid(), FolderId = folder2.Id, Folder = folder2, URL = "3", Acssesability = Acssesability.Public });
        await context.SaveChangesAsync();
        var service = new FolderService(context, CreateCloudinary());

        var result = (await service.GetPicturesAsync(folder1.Id)).ToList();

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, x => x.FolderId == folder2.Id);
    }

    [Fact]
    public async Task UploadPictureAsync_Throws_WhenFolderDoesNotExist()
    {
        using var context = TestDbFactory.CreateContext();
        var service = new FolderService(context, CreateCloudinary());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadPictureAsync(Guid.NewGuid(), TestFileFactory.Create("a.jpg", "image/jpeg", "abc"), Acssesability.Public, null));

        Assert.Equal("Folder not found.", ex.Message);
    }

    [Fact]
    public async Task UploadPictureAsync_Throws_WhenFileIsEmpty()
    {
        using var context = TestDbFactory.CreateContext();
        var user = SeedUserWithProfile(context, "owner");
        var folder = new Folder { Id = Guid.NewGuid(), Name = "Open", ProfileId = user.Profile!.Id, Profile = user.Profile, Acssesability = Acssesability.Public };
        context.Folders.Add(folder);
        await context.SaveChangesAsync();
        var service = new FolderService(context, CreateCloudinary());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadPictureAsync(folder.Id, TestFileFactory.Empty(), Acssesability.Public, null));

        Assert.Equal("No file selected.", ex.Message);
    }

    [Fact]
    public async Task UploadPictureAsync_Throws_WhenFileIsNotImage()
    {
        using var context = TestDbFactory.CreateContext();
        var user = SeedUserWithProfile(context, "owner");
        var folder = new Folder { Id = Guid.NewGuid(), Name = "Open", ProfileId = user.Profile!.Id, Profile = user.Profile, Acssesability = Acssesability.Public };
        context.Folders.Add(folder);
        await context.SaveChangesAsync();
        var service = new FolderService(context, CreateCloudinary());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadPictureAsync(folder.Id, TestFileFactory.Create("a.txt", "text/plain", "abc"), Acssesability.Public, null));

        Assert.Equal("Only image files are allowed.", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenFolderDoesNotExist()
    {
        using var context = TestDbFactory.CreateContext();
        var service = new FolderService(context, CreateCloudinary());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(Guid.NewGuid()));

        Assert.Equal("Folder not found.", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_RemovesFolderTreePicturesAndStructures()
    {
        using var context = TestDbFactory.CreateContext();
        var user = SeedUserWithProfile(context, "owner");
        var root = new Folder { Id = Guid.NewGuid(), Name = "Root", ProfileId = user.Profile!.Id, Profile = user.Profile, Acssesability = Acssesability.Public };
        var child = new Folder { Id = Guid.NewGuid(), Name = "Child", ProfileId = user.Profile.Id, Profile = user.Profile, Acssesability = Acssesability.Public };
        var grandChild = new Folder { Id = Guid.NewGuid(), Name = "Grand", ProfileId = user.Profile.Id, Profile = user.Profile, Acssesability = Acssesability.Public };
        context.Folders.AddRange(root, child, grandChild);
        context.FolderStructures.AddRange(
            new FolderStructure { FolderId = root.Id, SubfolderId = child.Id },
            new FolderStructure { FolderId = child.Id, SubfolderId = grandChild.Id });
        context.Pictures.AddRange(
            new Picture { Id = Guid.NewGuid(), FolderId = root.Id, Folder = root, URL = "1", Acssesability = Acssesability.Public },
            new Picture { Id = Guid.NewGuid(), FolderId = child.Id, Folder = child, URL = "2", Acssesability = Acssesability.Public },
            new Picture { Id = Guid.NewGuid(), FolderId = grandChild.Id, Folder = grandChild, URL = "3", Acssesability = Acssesability.Public });
        await context.SaveChangesAsync();
        var service = new FolderService(context, CreateCloudinary());

        await service.DeleteAsync(root.Id);

        Assert.Empty(context.Folders);
        Assert.Empty(context.FolderStructures);
        Assert.Empty(context.Pictures);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenFolderDoesNotExist()
    {
        using var context = TestDbFactory.CreateContext();
        var service = new FolderService(context, CreateCloudinary());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(Guid.NewGuid(), "Updated", Acssesability.Public));

        Assert.Equal("Folder not found.", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesNameAndAccessibility_WhenFolderIsNotInPrivateTree()
    {
        using var context = TestDbFactory.CreateContext();
        var user = SeedUserWithProfile(context, "owner");
        var folder = new Folder { Id = Guid.NewGuid(), Name = "Old", ProfileId = user.Profile!.Id, Profile = user.Profile, Acssesability = Acssesability.Public };
        context.Folders.Add(folder);
        await context.SaveChangesAsync();
        var service = new FolderService(context, CreateCloudinary());

        await service.UpdateAsync(folder.Id, "New", Acssesability.FriendsOnly);

        var updated = context.Folders.Single();
        Assert.Equal("New", updated.Name);
        Assert.Equal(Acssesability.FriendsOnly, updated.Acssesability);
    }

    [Fact]
    public async Task UpdateAsync_ForcesPrivateAndCascadesToChildrenAndPictures_WhenFolderBecomesPrivate()
    {
        using var context = TestDbFactory.CreateContext();
        var user = SeedUserWithProfile(context, "owner");
        var root = new Folder { Id = Guid.NewGuid(), Name = "Root", ProfileId = user.Profile!.Id, Profile = user.Profile, Acssesability = Acssesability.Public };
        var child = new Folder { Id = Guid.NewGuid(), Name = "Child", ProfileId = user.Profile.Id, Profile = user.Profile, Acssesability = Acssesability.Public };
        var grandChild = new Folder { Id = Guid.NewGuid(), Name = "Grand", ProfileId = user.Profile.Id, Profile = user.Profile, Acssesability = Acssesability.Public };
        var rootPicture = new Picture { Id = Guid.NewGuid(), FolderId = root.Id, Folder = root, URL = "1", Acssesability = Acssesability.Public };
        var childPicture = new Picture { Id = Guid.NewGuid(), FolderId = child.Id, Folder = child, URL = "2", Acssesability = Acssesability.Public };
        var grandPicture = new Picture { Id = Guid.NewGuid(), FolderId = grandChild.Id, Folder = grandChild, URL = "3", Acssesability = Acssesability.Public };
        context.Folders.AddRange(root, child, grandChild);
        context.FolderStructures.AddRange(
            new FolderStructure { FolderId = root.Id, SubfolderId = child.Id },
            new FolderStructure { FolderId = child.Id, SubfolderId = grandChild.Id });
        context.Pictures.AddRange(rootPicture, childPicture, grandPicture);
        await context.SaveChangesAsync();
        var service = new FolderService(context, CreateCloudinary());

        await service.UpdateAsync(root.Id, "PrivateRoot", Acssesability.Private);

        Assert.All(context.Folders.ToList(), folder => Assert.Equal(Acssesability.Private, folder.Acssesability));
        Assert.All(context.Pictures.ToList(), picture => Assert.Equal(Acssesability.Private, picture.Acssesability));
        Assert.Equal("PrivateRoot", context.Folders.Single(f => f.Id == root.Id).Name);
    }

    [Fact]
    public async Task UpdateAsync_LeavesFolderPrivate_WhenAncestorIsPrivate()
    {
        using var context = TestDbFactory.CreateContext();
        var user = SeedUserWithProfile(context, "owner");
        var parent = new Folder { Id = Guid.NewGuid(), Name = "Parent", ProfileId = user.Profile!.Id, Profile = user.Profile, Acssesability = Acssesability.Private };
        var child = new Folder { Id = Guid.NewGuid(), Name = "Child", ProfileId = user.Profile.Id, Profile = user.Profile, Acssesability = Acssesability.Public };
        context.Folders.AddRange(parent, child);
        context.FolderStructures.Add(new FolderStructure { FolderId = parent.Id, SubfolderId = child.Id });
        await context.SaveChangesAsync();
        var service = new FolderService(context, CreateCloudinary());

        await service.UpdateAsync(child.Id, "ChildUpdated", Acssesability.Public);

        var updatedChild = context.Folders.Single(f => f.Id == child.Id);
        Assert.Equal("ChildUpdated", updatedChild.Name);
        Assert.Equal(Acssesability.Private, updatedChild.Acssesability);
    }

    private static User SeedUserWithProfile(LiveMap.Data.LiveMapDbContext context, string username)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            Email = $"{username}@test.com",
            NormalizedEmail = $"{username}@TEST.COM"
        };

        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            ProfilePicture = $"{username}.png"
        };

        user.Profile = profile;
        context.Users.Add(user);
        context.Profiles.Add(profile);
        return user;
    }
}
