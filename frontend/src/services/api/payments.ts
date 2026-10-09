export interface PaymentItem {
  id: string;
  tenantId: string;
  bookingId?: string;
  customerId?: string;
  customerName?: string;
  amount: number;
  currency: string;
  paymentType: string;
  status: string;
  paymentMethod: string;
  stripePaymentIntentId?: string;
  stripeCheckoutSessionId?: string;
  receiptUrl?: string;
  notes?: string;
  createdAtUtc: string;
  completedAtUtc?: string;
}

export interface PagedPaymentsResponse {
  items: PaymentItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface PaymentSummaryItem {
  totalRevenue: number;
  totalDeposits: number;
  totalRefunds: number;
  totalTransactionsCount: number;
  completedCount: number;
  pendingCount: number;
  refundedCount: number;
}

export interface CreateCheckoutSessionPayload {
  bookingId: string;
  amount: number;
  currency?: string;
}

export interface ProcessRefundPayload {
  paymentId: string;
  refundAmount: number;
  reason: string;
}

export interface RecordInStorePaymentPayload {
  bookingId?: string;
  customerId?: string;
  amount: number;
  paymentMethod?: string;
  notes?: string;
}

const API_BASE = '/api/v1/payments';

export const paymentsApi = {
  async getPayments(
    token: string,
    params?: { pageNumber?: number; pageSize?: number; status?: string; type?: string }
  ): Promise<PagedPaymentsResponse> {
    const query = new URLSearchParams();
    if (params?.pageNumber) query.append('pageNumber', params.pageNumber.toString());
    if (params?.pageSize) query.append('pageSize', params.pageSize.toString());
    if (params?.status) query.append('status', params.status);
    if (params?.type) query.append('type', params.type);

    const res = await fetch(`${API_BASE}?${query.toString()}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to load payment transactions');
    return res.json();
  },

  async getSummary(token: string): Promise<PaymentSummaryItem> {
    const res = await fetch(`${API_BASE}/summary`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to load payment summary');
    return res.json();
  },

  async createCheckoutSession(token: string, data: CreateCheckoutSessionPayload): Promise<PaymentItem> {
    const res = await fetch(`${API_BASE}/checkout-session`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to create checkout session');
    return res.json();
  },

  async processRefund(token: string, data: ProcessRefundPayload): Promise<PaymentItem> {
    const res = await fetch(`${API_BASE}/refund`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to process refund');
    return res.json();
  },

  async recordInStorePayment(token: string, data: RecordInStorePaymentPayload): Promise<PaymentItem> {
    const res = await fetch(`${API_BASE}/pos`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to record in-store POS payment');
    return res.json();
  },

  async getPayoutBalance(token: string, tenantId?: string): Promise<{ pendingPayoutBalance: number; availablePayoutBalance: number; paidOutBalance: number; currency: string }> {
    const query = tenantId ? `?tenantId=${tenantId}` : '';
    const res = await fetch(`${API_BASE}/payout-balance${query}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) return { pendingPayoutBalance: 0, availablePayoutBalance: 0, paidOutBalance: 0, currency: 'INR' };
    return res.json();
  },

  async requestPayout(token: string, data: { tenantId?: string; amount: number; currency?: string; method?: string; destinationAccount?: string }): Promise<any> {
    const res = await fetch(`${API_BASE}/payouts`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.errorMessage || err.Message || 'Failed to request payout');
    }
    return res.json();
  },

  async getPayouts(token: string, tenantId?: string): Promise<any[]> {
    const query = tenantId ? `?tenantId=${tenantId}` : '';
    const res = await fetch(`${API_BASE}/payouts${query}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) return [];
    return res.json();
  }
};
