namespace Bookline.Application.UnitTests;

using Bookline.Application.Common.Interfaces;

public class TestTenantContext : ITenantContext
{
    public Guid TenantId { get; private set; }
    public bool IsResolved => true;

    public TestTenantContext(Guid tenantId)
    {
        TenantId = tenantId;
    }

    public void SetTenantId(Guid tenantId)
    {
        TenantId = tenantId;
    }
}
