namespace Bookline.Application.UnitTests;

using Bookline.Application.Common.Interfaces;

public class TestTenantContext : ITenantContext
{
    public Guid TenantId { get; private set; }
    public bool IsResolved => true;
    public bool IsSystem { get; private set; }

    public TestTenantContext(Guid tenantId)
    {
        TenantId = tenantId;
    }

    public void SetTenantId(Guid tenantId)
    {
        TenantId = tenantId;
    }

    public void EnableSystemMode()
    {
        IsSystem = true;
    }
}
