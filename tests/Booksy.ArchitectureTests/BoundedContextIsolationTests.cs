using Booksy.UserManagement.Application.Services.Interfaces;
using FluentAssertions;
using NetArchTest.Rules;

namespace Booksy.ArchitectureTests;

/// <summary>
/// UserManagement reaches ServiceCatalog only through ports it owns (<see cref="IProviderInfoService"/>), which the
/// Host implements in-process (<c>InProcessProviderInfoService</c>) — see openspec/project.md, cross-context
/// composition. The project reference from UserManagement.Application to ServiceCatalog.Application exists, so the
/// compiler does not stop a handler from reaching into ServiceCatalog's domain; this test does. It was added when a
/// customer's favourites/recent-visits handlers read ServiceCatalog's Provider aggregate directly
/// (customer-app-ux-review-fixes), which also left them unresolvable in the standalone UserManagement host.
/// </summary>
public class BoundedContextIsolationTests
{
    [Fact]
    public void UserManagement_application_does_not_depend_on_ServiceCatalog_types()
    {
        var result = Types.InAssembly(typeof(IProviderInfoService).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Booksy.ServiceCatalog.Domain",
                "Booksy.ServiceCatalog.Application",
                "Booksy.ServiceCatalog.Infrastructure")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty(
            "UserManagement must go through IProviderInfoService, not ServiceCatalog's own types");
    }
}
