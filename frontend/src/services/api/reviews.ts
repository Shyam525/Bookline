export interface ReviewItem {
  id: string;
  tenantId: string;
  customerId: string;
  customerName: string;
  rating: number;
  title?: string;
  comment: string;
  providerResponse?: string;
  moderationStatus: string;
  createdAtUtc: string;
}

const API_BASE = '/api/v1/reviews';

export const reviewsApi = {
  async getProviderReviews(tenantId: string): Promise<ReviewItem[]> {
    const res = await fetch(`${API_BASE}/provider/${tenantId}`);
    if (!res.ok) return [];
    return res.json();
  },

  async getMyReviews(token: string): Promise<ReviewItem[]> {
    const res = await fetch(`${API_BASE}/my`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) return [];
    return res.json();
  },

  async submitReview(data: {
    tenantId: string;
    rating: number;
    title?: string;
    comment: string;
    customerName: string;
    bookingId?: string;
    orderId?: string;
    skipEligibilityCheck?: boolean;
  }, token?: string): Promise<ReviewItem> {
    const headers: Record<string, string> = { 'Content-Type': 'application/json' };
    if (token) headers['Authorization'] = `Bearer ${token}`;

    const res = await fetch(API_BASE, {
      method: 'POST',
      headers,
      body: JSON.stringify(data),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || err.Message || 'Failed to submit review');
    }
    return res.json();
  },

  async respondToReview(id: string, response: string, token: string): Promise<ReviewItem> {
    const res = await fetch(`${API_BASE}/${id}/respond`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({ response }),
    });
    if (!res.ok) throw new Error('Failed to respond to review');
    return res.json();
  },

  async moderateReview(id: string, status: string, token: string): Promise<ReviewItem> {
    const res = await fetch(`${API_BASE}/${id}/moderate`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({ status }),
    });
    if (!res.ok) throw new Error('Failed to moderate review');
    return res.json();
  },
};
