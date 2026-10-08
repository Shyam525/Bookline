export interface PlatformMetricsDto {
  totalProviders: number;
  activeVerifiedProviders: number;
  totalCustomers: number;
  totalBookings: number;
  totalOrders: number;
  grossMerchandiseValue: number;
  platformCommissionRevenue: number;
  activeCities: number;
  currency: string;
}

export interface AdminProviderDto {
  id: string;
  name: string;
  slug: string;
  category: string;
  businessType: string;
  city: string;
  address: string;
  phone?: string;
  averageRating: number;
  reviewCount: number;
  verificationStatus: string;
  isActive: boolean;
  commissionRatePercentage: number;
  availablePayoutBalance: number;
  paidOutBalance: number;
  createdAtUtc: string;
}

const API_BASE = '/api/v1/admin';

export const adminApi = {
  async getMetrics(token: string): Promise<PlatformMetricsDto> {
    const res = await fetch(`${API_BASE}/metrics`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to load platform metrics');
    return res.json();
  },

  async getProviders(token: string): Promise<AdminProviderDto[]> {
    const res = await fetch(`${API_BASE}/providers`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) return [];
    return res.json();
  },

  async updateProviderVerification(id: string, status: string, token: string): Promise<void> {
    const res = await fetch(`${API_BASE}/providers/${id}/verification`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({ status }),
    });
    if (!res.ok) throw new Error('Failed to update verification status');
  },

  async getCustomers(token: string) {
    const res = await fetch(`${API_BASE}/customers`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) return [];
    return res.json();
  },

  async getCommissions(token: string) {
    const res = await fetch(`${API_BASE}/commissions`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) return [];
    return res.json();
  },

  async getPayouts(token: string) {
    const res = await fetch(`${API_BASE}/payouts`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) return [];
    return res.json();
  },

  async triggerPayout(tenantId: string, amount: number, token: string) {
    const res = await fetch(`${API_BASE}/payouts/process`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({ tenantId, amount }),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || err.Message || 'Failed to trigger payout');
    }
    return res.json();
  },

  async getReviews(token: string) {
    const res = await fetch(`${API_BASE}/reviews`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) return [];
    return res.json();
  },

  async getHealth(token: string) {
    const res = await fetch(`${API_BASE}/health`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) return { status: 'Degraded' };
    return res.json();
  },
};
