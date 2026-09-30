using Bookline.Application.Common.Interfaces;

namespace Bookline.Application.Common.Models;

public class TenantContext : ITenantContext
{
    private Guid _tenantId = Guid.Empty;

    public Guid TenantId => _tenantId;

    public bool IsResolved => _tenantId != Guid.Empty;

    public void SetTenantId(Guid tenantId)
    {
        if (_tenantId != Guid.Empty && _tenantId != tenantId)
        {
            throw new InvalidOperationException("TenantId cannot be re-assigned once set within a request scope.");
        }

        _tenantId = tenantId;
    }
}
