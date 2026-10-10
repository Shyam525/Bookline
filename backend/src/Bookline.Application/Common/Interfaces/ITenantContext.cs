namespace Bookline.Application.Common.Interfaces;

public interface ITenantContext
{
    Guid TenantId { get; }
    bool IsResolved { get; }
    bool IsSystem { get; }
    void SetTenantId(Guid tenantId);
    void EnableSystemMode();
}
