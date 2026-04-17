using CloudinaryDotNet;
using LiveMap.Core.Services;
using LiveMap.Data.Models;
using LiveMap.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace LiveMap.Tests.Services;

public class AdminServiceTests
{
    private static Cloudinary CreateCloudinary() => new(new Account("demo", "key", "secret"));

    [Fact]
    public async Task GetDashboardAsync_ReturnsProfilesFoldersAndPhotosOrdered()
    {
        using var context = TestDbFactory.CreateContext();
        var anna = SeedUserWithProfile(context, "anna", "anna@test.com", "p1.png");
        var bella = SeedUserWithProfile(context, "bella", "bella@test.com", "p2.png");

        var zFolder = new Folder { Id = Guid.NewGuid(), Name = "Zoo", ProfileId = anna.Profile!.Id, Profile = anna.Profile, Acssesability = Acssesability.Public };
        var aFolder = new Folder { Id = Guid.NewGuid(), Name = "Alps", ProfileId = bella.Profile!.Id, Profile = bella.Profile, Acssesability = Acssesability.Public };
        var oldPhoto = new Picture { Id = Guid.NewGuid(), FolderId = zFolder.Id, Folder = zFolder, URL = "old", CreatedOn = new DateTime(2024, 1, 1), Acssesability = Acssesability.Public };
        var newPhoto = new Picture { Id = Guid.NewGuid(), FolderId = aFolder.Id, Folder = aFolder, URL = "new", CreatedOn = new DateTime(2025, 1, 1), Acssesability = Acssesability.Public };

        context.Folders.AddRange(zFolder, aFolder);
        context.Pictures.AddRange(oldPhoto, newPhoto);
        await context.SaveChangesAsync();

        var service = new AdminService(context, IdentityMockFactory.CreateUserManagerMock().Object, IdentityMockFactory.CreateRoleManagerMock().Object, CreateCloudinary());

        var result = await service.GetDashboardAsync();

        Assert.Equal(new[] { "anna", "bella" }, result.Profiles.Select(x => x.Username));
        Assert.Equal(new[] { "Alps", "Zoo" }, result.Folders.Select(x => x.Name));
        Assert.Equal(new[] { "new", "old" }, result.Photos.Select(x => x.Url));
        Assert.Equal("bella", result.Folders.First().Username);
        Assert.Equal("anna@test.com", result.Profiles.First().Email);
    }

    [Fact]
    public async Task EnsureRolesAndAdminsAsync_CreatesRoleAndAddsOnlyMissingUsers()
    {
        using var context = TestDbFactory.CreateContext();
        var userManager = IdentityMockFactory.CreateUserManagerMock();
        var roleManager = IdentityMockFactory.CreateRoleManagerMock();
        var maria = new User { Id = Guid.NewGuid(), Email = "maria@test.com", UserName = "maria" };

        roleManager.Setup(x => x.RoleExistsAsync(AdminService.AdminRoleName)).ReturnsAsync(false);
        roleManager.Setup(x => x.CreateAsync(It.IsAny<IdentityRole<Guid>>())).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(x => x.FindByEmailAsync("maria@test.com")).ReturnsAsync(maria);
        userManager.Setup(x => x.FindByEmailAsync("missing@test.com")).ReturnsAsync((User?)null);
        userManager.Setup(x => x.IsInRoleAsync(maria, AdminService.AdminRoleName)).ReturnsAsync(false);
        userManager.Setup(x => x.AddToRoleAsync(maria, AdminService.AdminRoleName)).ReturnsAsync(IdentityResult.Success);

        var service = new AdminService(context, userManager.Object, roleManager.Object, CreateCloudinary());

        await service.EnsureRolesAndAdminsAsync(new[] { " maria@test.com ", "MARIA@test.com", "", "missing@test.com" });

        roleManager.Verify(x => x.CreateAsync(It.Is<IdentityRole<Guid>>(r => r.Name == AdminService.AdminRoleName)), Times.Once);
        userManager.Verify(x => x.FindByEmailAsync("maria@test.com"), Times.Once);
        userManager.Verify(x => x.FindByEmailAsync("missing@test.com"), Times.Once);
        userManager.Verify(x => x.AddToRoleAsync(maria, AdminService.AdminRoleName), Times.Once);
    }

    [Fact]
    public async Task EnsureRolesAndAdminsAsync_DoesNotCreateRoleOrAdd_WhenAlreadyConfigured()
    {
        using var context = TestDbFactory.CreateContext();
        var userManager = IdentityMockFactory.CreateUserManagerMock();
        var roleManager = IdentityMockFactory.CreateRoleManagerMock();
        var maria = new User { Id = Guid.NewGuid(), Email = "maria@test.com", UserName = "maria" };

        roleManager.Setup(x => x.RoleExistsAsync(AdminService.AdminRoleName)).ReturnsAsync(true);
        userManager.Setup(x => x.FindByEmailAsync("maria@test.com")).ReturnsAsync(maria);
        userManager.Setup(x => x.IsInRoleAsync(maria, AdminService.AdminRoleName)).ReturnsAsync(true);

        var service = new AdminService(context, userManager.Object, roleManager.Object, CreateCloudinary());

        await service.EnsureRolesAndAdminsAsync(new[] { "maria@test.com" });

        roleManager.Verify(x => x.CreateAsync(It.IsAny<IdentityRole<Guid>>()), Times.Never);
        userManager.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProfileAsync_Throws_WhenProfileIsMissing()
    {
        using var context = TestDbFactory.CreateContext();
        var service = new AdminService(context, IdentityMockFactory.CreateUserManagerMock().Object, IdentityMockFactory.CreateRoleManagerMock().Object, CreateCloudinary());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteProfileAsync(Guid.NewGuid()));

        Assert.Equal("Profile not found.", ex.Message);
    }

    

    [Fact]
    public async Task DeleteFolderAsync_Throws_WhenFolderIsMissing()
    {
        using var context = TestDbFactory.CreateContext();
        var service = new AdminService(context, IdentityMockFactory.CreateUserManagerMock().Object, IdentityMockFactory.CreateRoleManagerMock().Object, CreateCloudinary());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteFolderAsync(Guid.NewGuid()));

        Assert.Equal("Folder not found.", ex.Message);
    }

    [Fact]
    public async Task DeleteFolderAsync_RemovesFolderPicturesAndLinks()
    {
        using var context = TestDbFactory.CreateContext();
        var owner = SeedUserWithProfile(context, "owner", "owner@test.com", "pic");
        var parent = new Folder { Id = Guid.NewGuid(), Name = "Parent", ProfileId = owner.Profile!.Id, Profile = owner.Profile, Acssesability = Acssesability.Public };
        var child = new Folder { Id = Guid.NewGuid(), Name = "Child", ProfileId = owner.Profile.Id, Profile = owner.Profile, Acssesability = Acssesability.Public };
        var photo = new Picture { Id = Guid.NewGuid(), FolderId = child.Id, Folder = child, URL = "non-absolute", Acssesability = Acssesability.Public };
        context.Folders.AddRange(parent, child);
        context.FolderStructures.Add(new FolderStructure { FolderId = parent.Id, SubfolderId = child.Id });
        context.Pictures.Add(photo);
        await context.SaveChangesAsync();

        var service = new AdminService(context, IdentityMockFactory.CreateUserManagerMock().Object, IdentityMockFactory.CreateRoleManagerMock().Object, CreateCloudinary());

        await service.DeleteFolderAsync(child.Id);

        Assert.DoesNotContain(context.Folders, x => x.Id == child.Id);
        Assert.Empty(context.Pictures);
        Assert.Empty(context.FolderStructures);
    }

    [Fact]
    public async Task DeletePhotoAsync_Throws_WhenPhotoIsMissing()
    {
        using var context = TestDbFactory.CreateContext();
        var service = new AdminService(context, IdentityMockFactory.CreateUserManagerMock().Object, IdentityMockFactory.CreateRoleManagerMock().Object, CreateCloudinary());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeletePhotoAsync(Guid.NewGuid()));

        Assert.Equal("Photo not found.", ex.Message);
    }

    [Fact]
    public async Task DeletePhotoAsync_RemovesPhoto()
    {
        using var context = TestDbFactory.CreateContext();
        var owner = SeedUserWithProfile(context, "owner", "owner@test.com", "pic");
        var folder = new Folder { Id = Guid.NewGuid(), Name = "Trips", ProfileId = owner.Profile!.Id, Profile = owner.Profile, Acssesability = Acssesability.Public };
        var photo = new Picture { Id = Guid.NewGuid(), FolderId = folder.Id, Folder = folder, URL = "plain-string", Acssesability = Acssesability.Public };
        context.Folders.Add(folder);
        context.Pictures.Add(photo);
        await context.SaveChangesAsync();
        var service = new AdminService(context, IdentityMockFactory.CreateUserManagerMock().Object, IdentityMockFactory.CreateRoleManagerMock().Object, CreateCloudinary());

        await service.DeletePhotoAsync(photo.Id);

        Assert.Empty(context.Pictures);
    }

    [Fact]
    public async Task DeleteProfilePictureAsync_Throws_WhenProfileIsMissing()
    {
        using var context = TestDbFactory.CreateContext();
        var service = new AdminService(context, IdentityMockFactory.CreateUserManagerMock().Object, IdentityMockFactory.CreateRoleManagerMock().Object, CreateCloudinary());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteProfilePictureAsync(Guid.NewGuid()));

        Assert.Equal("Profile not found.", ex.Message);
    }

    [Fact]
    public async Task DeleteProfilePictureAsync_ClearsPicture_WhenPresent()
    {
        using var context = TestDbFactory.CreateContext();
        var owner = SeedUserWithProfile(context, "owner", "owner@test.com", "not-an-absolute-uri");
        await context.SaveChangesAsync();
        var service = new AdminService(context, IdentityMockFactory.CreateUserManagerMock().Object, IdentityMockFactory.CreateRoleManagerMock().Object, CreateCloudinary());

        await service.DeleteProfilePictureAsync(owner.Profile!.Id);

        Assert.Equal(string.Empty, context.Profiles.Single().ProfilePicture);
    }

    [Fact]
    public async Task DeleteProfilePictureAsync_DoesNothing_WhenPictureIsBlank()
    {
        using var context = TestDbFactory.CreateContext();
        var owner = SeedUserWithProfile(context, "owner", "owner@test.com", string.Empty);
        await context.SaveChangesAsync();
        var service = new AdminService(context, IdentityMockFactory.CreateUserManagerMock().Object, IdentityMockFactory.CreateRoleManagerMock().Object, CreateCloudinary());

        await service.DeleteProfilePictureAsync(owner.Profile!.Id);

        Assert.Equal(string.Empty, context.Profiles.Single().ProfilePicture);
    }

    private static User SeedUserWithProfile(LiveMap.Data.LiveMapDbContext context, string username, string email, string profilePicture)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant()
        };

        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            ProfilePicture = profilePicture,
            Bio = $"{username} bio"
        };

        user.Profile = profile;
        context.Users.Add(user);
        context.Profiles.Add(profile);
        return user;
    }
}
