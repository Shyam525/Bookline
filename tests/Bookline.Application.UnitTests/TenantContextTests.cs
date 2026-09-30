using Bookline.Application.Common.Models;
using Xunit;

namespace Bookline.Application.UnitTests;

public class TenantContextTests
{
    [Fact]
    public void SetTenantId_WhenUnset_ShouldSetTenantIdAndMarkResolved()
    {
        // Arrange
        var context = new TenantContext();
        var tenantId = Guid.NewGuid();

        // Act
        context.SetTenantId(tenantId);

        // Assert
        Assert.Equal(tenantId, context.TenantId);
        Assert.True(context.IsResolved);
    }

    [Fact]
    public void SetTenantId_WhenAlreadySetWithDifferentValue_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var context = new TenantContext();
        var tenantId1 = Guid.NewGuid();
        var tenantId2 = Guid.NewGuid();
        context.SetTenantId(tenantId1);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => context.SetTenantId(tenantId2));
    }
}
