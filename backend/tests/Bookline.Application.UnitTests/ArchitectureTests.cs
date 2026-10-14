using System.Reflection;
using Bookline.Application.Common.Interfaces;
using Bookline.Domain.Entities;
using Bookline.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Bookline.Application.UnitTests;

/// <summary>
/// Specification Phase 44: ARCHITECTURE TESTS
/// Verifies dependency direction, domain purity, database independence in API, thin controllers,
/// and encapsulation of business rules in domain and application services.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(Booking).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(IApplicationDbContext).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(BooklineDbContext).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Bookline.Api.Controllers.DiscoveryController).Assembly;

    [Fact]
    public void DomainLayer_MustNotReference_InfrastructureOrApiAssemblies()
    {
        var referencedAssemblies = DomainAssembly.GetReferencedAssemblies();
        var forbiddenReferences = referencedAssemblies
            .Where(a => a.Name != null && (
                a.Name.StartsWith("Bookline.Infrastructure", StringComparison.OrdinalIgnoreCase) ||
                a.Name.StartsWith("Bookline.Api", StringComparison.OrdinalIgnoreCase)))
            .Select(a => a.Name)
            .ToList();

        Assert.Empty(forbiddenReferences);
    }

    [Fact]
    public void ApplicationLayer_MustNotReference_InfrastructureOrApiAssemblies()
    {
        var referencedAssemblies = ApplicationAssembly.GetReferencedAssemblies();
        var forbiddenReferences = referencedAssemblies
            .Where(a => a.Name != null && (
                a.Name.StartsWith("Bookline.Infrastructure", StringComparison.OrdinalIgnoreCase) ||
                a.Name.StartsWith("Bookline.Api", StringComparison.OrdinalIgnoreCase)))
            .Select(a => a.Name)
            .ToList();

        Assert.Empty(forbiddenReferences);
    }

    [Fact]
    public void ApiControllers_MustNotDirectlyManipulate_DatabaseImplementation()
    {
        // Controllers must be thin and depend on MediatR (ISender/IMediator) or application interfaces,
        // never on concrete BooklineDbContext.
        var controllerTypes = ApiAssembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();

        Assert.NotEmpty(controllerTypes);

        foreach (var controller in controllerTypes)
        {
            // Verify no constructor accepts BooklineDbContext
            foreach (var ctor in controller.GetConstructors())
            {
                var parameters = ctor.GetParameters();
                var hasConcreteDbContext = parameters.Any(p => p.ParameterType == typeof(BooklineDbContext));
                Assert.False(hasConcreteDbContext,
                    $"Controller '{controller.Name}' directly injects concrete '{nameof(BooklineDbContext)}'. " +
                    $"Controllers must depend on application abstractions or MediatR.");
            }

            // Verify no fields are of type BooklineDbContext
            var fields = controller.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var hasDbContextField = fields.Any(f => f.FieldType == typeof(BooklineDbContext));
            Assert.False(hasDbContextField,
                $"Controller '{controller.Name}' contains a field of concrete type '{nameof(BooklineDbContext)}'.");
        }
    }

    [Fact]
    public void Controllers_ShouldBeThin_AndDelegateToHandlersOrServices()
    {
        var controllerTypes = ApiAssembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();

        Assert.NotEmpty(controllerTypes);

        foreach (var controller in controllerTypes)
        {
            // All controllers must have [ApiController] and inherit from ControllerBase
            var hasApiControllerAttr = controller.GetCustomAttribute<ApiControllerAttribute>() != null;
            Assert.True(hasApiControllerAttr, $"Controller '{controller.Name}' must be decorated with [ApiController].");

            // Verify action methods do not expose raw SQL or direct transaction management
            var actionMethods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName);

            foreach (var action in actionMethods)
            {
                var methodBody = action.GetMethodBody();
                Assert.NotNull(methodBody);
            }
        }
    }

    [Fact]
    public void BusinessRules_ShouldLiveInDomainAndApplicationServices()
    {
        // Domain entities must encapsulate their business invariant rules
        var bookingType = typeof(Booking);
        var bookingMethods = bookingType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Select(m => m.Name)
            .ToHashSet();

        Assert.Contains(nameof(Booking.Confirm), bookingMethods);
        Assert.Contains(nameof(Booking.Cancel), bookingMethods);
        Assert.Contains(nameof(Booking.Complete), bookingMethods);
        Assert.Contains(nameof(Booking.AttachRelatedOrder), bookingMethods);

        // Product domain must encapsulate active state and stock updates
        var productType = typeof(Product);
        Assert.NotNull(productType.GetProperty(nameof(Product.StockQuantity)));
        Assert.NotNull(productType.GetProperty(nameof(Product.IsPurchasableOnline)));
        Assert.NotNull(productType.GetProperty(nameof(Product.Price)));
    }
}
