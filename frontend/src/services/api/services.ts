export interface ServiceCategoryItem {
  id: string;
  tenantId: string;
  name: string;
  description?: string;
  sortOrder: number;
  isActive: boolean;
  servicesCount: number;
}

export interface CreateServiceCategoryPayload {
  name: string;
  description?: string;
  sortOrder?: number;
}

export interface UpdateServiceCategoryPayload {
  name: string;
  description?: string;
  sortOrder?: number;
  isActive?: boolean;
}

export interface ServiceItem {
  id: string;
  tenantId: string;
  categoryId: string;
  categoryName: string;
  name: string;
  description?: string;
  durationMinutes: number;
  bufferBeforeMinutes: number;
  bufferAfterMinutes: number;
  totalDurationMinutes: number;
  price: number;
  currency: string;
  isActive: boolean;
  isOnlineBookingEnabled: boolean;
  isArchived: boolean;
  colorHex?: string;
  createdAtUtc: string;
}

export interface CreateServicePayload {
  categoryId: string;
  name: string;
  description?: string;
  durationMinutes: number;
  bufferBeforeMinutes?: number;
  bufferAfterMinutes?: number;
  price: number;
  currency?: string;
  isOnlineBookingEnabled?: boolean;
  colorHex?: string;
}

export interface UpdateServicePayload {
  categoryId: string;
  name: string;
  description?: string;
  durationMinutes: number;
  bufferBeforeMinutes?: number;
  bufferAfterMinutes?: number;
  price: number;
  currency?: string;
  isActive: boolean;
  isOnlineBookingEnabled?: boolean;
  colorHex?: string;
}

const API_BASE = '/api/v1/services';

export const servicesApi = {
  // --- Categories ---
  async getCategories(token: string): Promise<ServiceCategoryItem[]> {
    const res = await fetch(`${API_BASE}/categories`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to load service categories');
    return res.json();
  },

  async createCategory(token: string, data: CreateServiceCategoryPayload): Promise<ServiceCategoryItem> {
    const res = await fetch(`${API_BASE}/categories`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to create category');
    return res.json();
  },

  async updateCategory(token: string, id: string, data: UpdateServiceCategoryPayload): Promise<ServiceCategoryItem> {
    const res = await fetch(`${API_BASE}/categories/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to update category');
    return res.json();
  },

  async deleteCategory(token: string, id: string): Promise<void> {
    const res = await fetch(`${API_BASE}/categories/${id}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: 'Failed to delete category' }));
      throw new Error(err.message || 'Failed to delete category');
    }
  },

  // --- Services ---
  async getServices(token: string, categoryId?: string, includeArchived = false): Promise<ServiceItem[]> {
    let url = `${API_BASE}?includeArchived=${includeArchived}`;
    if (categoryId) {
      url += `&categoryId=${encodeURIComponent(categoryId)}`;
    }
    const res = await fetch(url, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to load services');
    return res.json();
  },

  async getServiceById(token: string, id: string): Promise<ServiceItem> {
    const res = await fetch(`${API_BASE}/${id}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Service not found');
    return res.json();
  },

  async createService(token: string, data: CreateServicePayload): Promise<ServiceItem> {
    const res = await fetch(API_BASE, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to create service');
    return res.json();
  },

  async updateService(token: string, id: string, data: UpdateServicePayload): Promise<ServiceItem> {
    const res = await fetch(`${API_BASE}/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to update service');
    return res.json();
  },

  async archiveService(token: string, id: string): Promise<void> {
    const res = await fetch(`${API_BASE}/${id}/archive`, {
      method: 'PUT',
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to archive service');
  },

  async duplicateService(token: string, id: string): Promise<ServiceItem> {
    const res = await fetch(`${API_BASE}/${id}/duplicate`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to duplicate service');
    return res.json();
  },
};
