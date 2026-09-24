using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TheBuilder.AIImageDisclosure.Controllers;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;

namespace TheBuilder.AIImageDisclosure.Tests;

public sealed class RescanAuthorizationTests
{
    [Fact]
    public void RejectsNonAdministratorBeforeResolvingTheScanService()
    {
        var accessor = Substitute.For<IBackOfficeSecurityAccessor>();
        var security = Substitute.For<IBackOfficeSecurity>();
        var user = Substitute.For<IUser>();
        user.Id.Returns(123);
        user.Groups.Returns(Array.Empty<IReadOnlyUserGroup>());
        security.CurrentUser.Returns(user);
        accessor.BackOfficeSecurity.Returns(security);
        using var services = new ServiceCollection().AddSingleton(accessor).BuildServiceProvider();
        Assert.IsType<ForbidResult>(new AiImageDisclosureController(services).Rescan(new RescanRequest()));
    }
}
