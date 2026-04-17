using LiveMap.Data.Models;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace LiveMap.Tests.Helpers;

public static class IdentityMockFactory
{
    public static Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    public static Mock<RoleManager<IdentityRole<Guid>>> CreateRoleManagerMock()
    {
        var store = new Mock<IRoleStore<IdentityRole<Guid>>>();
        return new Mock<RoleManager<IdentityRole<Guid>>>(
            store.Object,
            Array.Empty<IRoleValidator<IdentityRole<Guid>>>(),
            null!,
            null!,
            null!);
    }
}
