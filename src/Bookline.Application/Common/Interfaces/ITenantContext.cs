namespace Bookline.Application.Common.Interfaces;

public interface ITenantContext
{
    Guid TenantId { get; }
    bool IsResolved { get; }
    void SetTenantId(Guid tenantId);
}
