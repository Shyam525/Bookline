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
  categoryName?: string;
  name: string;
  slug?: string;
  description?: string;
  durationMinutes: number;
  bufferBeforeMinutes: number;
  bufferAfterMinutes: number;
  totalDurationMinutes: number;
  price: number;
  currency: string;
  isActive: boolean;
  isOnlineBookingEnabled: boolean;
  isPublished?: boolean;
  status?: string;
  isArchived: boolean;
  colorHex?: string;
  requiresResource?: boolean;
  requiredResourceType?: string;
  requiredResourceId?: string;
  requiredResourceName?: string;
  imageUrl?: string;
  staffIds?: string[];
  locationIds?: string[];
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface CreateServicePayload {
  categoryId: string;
  name: string;
  slug?: string;
  description?: string;
  durationMinutes: number;
  bufferBeforeMinutes?: number;
  bufferAfterMinutes?: number;
  price: number;
  currency?: string;
  isOnlineBookingEnabled?: boolean;
  isPublished?: boolean;
  colorHex?: string;
  requiresResource?: boolean;
  requiredResourceType?: string;
  requiredResourceId?: string;
  imageUrl?: string;
  staffIds?: string[];
  locationIds?: string[];
}

export interface UpdateServicePayload {
  categoryId: string;
  name: string;
  slug?: string;
  description?: string;
  durationMinutes: number;
  bufferBeforeMinutes?: number;
  bufferAfterMinutes?: number;
  price: number;
  currency?: string;
  isActive: boolean;
  isOnlineBookingEnabled?: boolean;
  isPublished?: boolean;
  colorHex?: string;
  requiresResource?: boolean;
  requiredResourceType?: string;
  requiredResourceId?: string;
  imageUrl?: string;
  staffIds?: string[];
  locationIds?: string[];
}

export interface ResourceItem {
  id: string;
  tenantId: string;
  locationId?: string;
  locationName?: string;
  name: string;
  description: string;
  type: string;
  capacity: number;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface CreateResourcePayload {
  name: string;
  description?: string;
  type?: string;
  capacity?: number;
  locationId?: string;
}

export interface UpdateResourcePayload {
  name: string;
  description?: string;
  type?: string;
  capacity?: number;
  isActive?: boolean;
  locationId?: string;
}

const API_BASE = '/api/v1/services';
const RESOURCE_API_BASE = '/api/v1/resources';

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
  async getServices(
    token: string,
    params?: { categoryId?: string; locationId?: string; staffId?: string; status?: string; includeArchived?: boolean; search?: string }
  ): Promise<ServiceItem[]> {
    let url = `${API_BASE}?includeArchived=${params?.includeArchived ?? false}`;
    if (params?.categoryId) url += `&categoryId=${encodeURIComponent(params.categoryId)}`;
    if (params?.locationId) url += `&locationId=${encodeURIComponent(params.locationId)}`;
    if (params?.staffId) url += `&staffId=${encodeURIComponent(params.staffId)}`;
    if (params?.status) url += `&status=${encodeURIComponent(params.status)}`;
    if (params?.search) url += `&search=${encodeURIComponent(params.search)}`;

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
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: 'Failed to create service' }));
      throw new Error(err.message || 'Failed to create service');
    }
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
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: 'Failed to update service' }));
      throw new Error(err.message || 'Failed to update service');
    }
    return res.json();
  },

  async setStatus(token: string, id: string, status: string): Promise<ServiceItem> {
    const res = await fetch(`${API_BASE}/${id}/status`, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({ status }),
    });
    if (!res.ok) throw new Error('Failed to update service status');
    return res.json();
  },

  async setPublishStatus(token: string, id: string, isPublished: boolean): Promise<ServiceItem> {
    const res = await fetch(`${API_BASE}/${id}/publish`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({ isPublished }),
    });
    if (!res.ok) throw new Error('Failed to update publish status');
    return res.json();
  },

  async assignStaff(token: string, id: string, staffIds: string[]): Promise<ServiceItem> {
    const res = await fetch(`${API_BASE}/${id}/staff`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(staffIds),
    });
    if (!res.ok) throw new Error('Failed to assign staff');
    return res.json();
  },

  async assignLocations(token: string, id: string, locationIds: string[]): Promise<ServiceItem> {
    const res = await fetch(`${API_BASE}/${id}/locations`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(locationIds),
    });
    if (!res.ok) throw new Error('Failed to assign locations');
    return res.json();
  },

  async configureResources(
    token: string,
    id: string,
    data: { requiresResource: boolean; requiredResourceType?: string; requiredResourceId?: string }
  ): Promise<ServiceItem> {
    const res = await fetch(`${API_BASE}/${id}/resources`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to configure service resources');
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

  // --- Resources ---
  async getResources(
    token: string,
    params?: { locationId?: string; type?: string; isActive?: boolean }
  ): Promise<ResourceItem[]> {
    let url = `${RESOURCE_API_BASE}`;
    const queryParts: string[] = [];
    if (params?.locationId) queryParts.push(`locationId=${encodeURIComponent(params.locationId)}`);
    if (params?.type) queryParts.push(`type=${encodeURIComponent(params.type)}`);
    if (params?.isActive !== undefined) queryParts.push(`isActive=${params.isActive}`);
    if (queryParts.length > 0) url += `?${queryParts.join('&')}`;

    const res = await fetch(url, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to load resources');
    return res.json();
  },

  async getResourceById(token: string, id: string): Promise<ResourceItem> {
    const res = await fetch(`${RESOURCE_API_BASE}/${id}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Resource not found');
    return res.json();
  },

  async createResource(token: string, data: CreateResourcePayload): Promise<ResourceItem> {
    const res = await fetch(RESOURCE_API_BASE, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: 'Failed to create resource' }));
      throw new Error(err.message || 'Failed to create resource');
    }
    return res.json();
  },

  async updateResource(token: string, id: string, data: UpdateResourcePayload): Promise<ResourceItem> {
    const res = await fetch(`${RESOURCE_API_BASE}/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: 'Failed to update resource' }));
      throw new Error(err.message || 'Failed to update resource');
    }
    return res.json();
  },

  async deleteResource(token: string, id: string): Promise<void> {
    const res = await fetch(`${RESOURCE_API_BASE}/${id}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: 'Failed to delete resource' }));
      throw new Error(err.message || 'Failed to delete resource');
    }
  },
};
