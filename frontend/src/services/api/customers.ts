export interface CustomerItem {
  id: string;
  tenantId: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phone: string;
  notes?: string;
  avatarUrl?: string;
  totalBookingsCount: number;
  totalSpentAmount: number;
  isArchived: boolean;
  createdAtUtc: string;
}

export interface CreateCustomerPayload {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  notes?: string;
  avatarUrl?: string;
}

export interface UpdateCustomerPayload {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  notes?: string;
  avatarUrl?: string;
}

export interface PagedCustomersResponse {
  items: CustomerItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

const API_BASE = '/api/v1/customers';

export const customersApi = {
  async getCustomers(
    token: string,
    searchQuery = '',
    includeArchived = false,
    page = 1,
    pageSize = 20
  ): Promise<PagedCustomersResponse> {
    let url = `${API_BASE}?includeArchived=${includeArchived}&page=${page}&pageSize=${pageSize}`;
    if (searchQuery) url += `&searchQuery=${encodeURIComponent(searchQuery)}`;

    const res = await fetch(url, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to load customers');
    return res.json();
  },

  async getCustomerById(token: string, id: string): Promise<CustomerItem> {
    const res = await fetch(`${API_BASE}/${id}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Customer not found');
    return res.json();
  },

  async createCustomer(token: string, data: CreateCustomerPayload): Promise<CustomerItem> {
    const res = await fetch(API_BASE, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to create customer');
    return res.json();
  },

  async updateCustomer(token: string, id: string, data: UpdateCustomerPayload): Promise<CustomerItem> {
    const res = await fetch(`${API_BASE}/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to update customer');
    return res.json();
  },

  async archiveCustomer(token: string, id: string): Promise<void> {
    const res = await fetch(`${API_BASE}/${id}/archive`, {
      method: 'PUT',
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to archive customer');
  },
};
