export interface OrderItemDto {
  id: string;
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
}

export interface OrderDto {
  id: string;
  tenantId: string;
  orderNumber: string;
  status: string;
  subtotal: number;
  tax: number;
  totalAmount: number;
  currency: string;
  customerName: string;
  customerEmail: string;
  customerPhone?: string;
  shippingAddress?: string;
  createdAtUtc: string;
  items: OrderItemDto[];
}

const API_BASE = '/api/v1/orders';

export const ordersApi = {
  async createOrder(data: {
    tenantId: string;
    customerName: string;
    customerEmail: string;
    customerPhone?: string;
    shippingAddress?: string;
    items: Array<{ productId: string; quantity: number }>;
  }, token?: string): Promise<{ order: OrderDto }> {
    const headers: Record<string, string> = { 'Content-Type': 'application/json' };
    if (token) headers['Authorization'] = `Bearer ${token}`;

    const res = await fetch(API_BASE, {
      method: 'POST',
      headers,
      body: JSON.stringify(data),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || err.Message || 'Order checkout failed');
    }
    return res.json();
  },

  async getCustomerOrders(token: string): Promise<OrderDto[]> {
    const res = await fetch(`${API_BASE}/my`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) return [];
    return res.json();
  },

  async getProviderOrders(token: string, tenantId: string): Promise<OrderDto[]> {
    const res = await fetch(`${API_BASE}/provider`, {
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': tenantId,
      },
    });
    if (!res.ok) return [];
    return res.json();
  },

  async updateOrderStatus(id: string, status: string, token: string, tenantId: string): Promise<OrderDto> {
    const res = await fetch(`${API_BASE}/${id}/status`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': tenantId,
      },
      body: JSON.stringify({ status }),
    });
    if (!res.ok) throw new Error('Failed to update status');
    return res.json();
  },
};
