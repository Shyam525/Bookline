export interface LocationItem {
  id: string;
  tenantId: string;
  name: string;
  address: string;
  phone: string;
  timezone: string;
  currency: string;
  isActive: boolean;
  isArchived: boolean;
  createdAtUtc: string;
}

export interface CreateLocationPayload {
  name: string;
  address: string;
  phone: string;
  timezone: string;
  currency: string;
}

export interface UpdateLocationPayload {
  name: string;
  address: string;
  phone: string;
  timezone: string;
  currency: string;
  isActive: boolean;
}

const API_BASE = '/api/v1/locations';

export const locationsApi = {
  async getLocations(token: string, includeArchived = false): Promise<LocationItem[]> {
    const res = await fetch(`${API_BASE}?includeArchived=${includeArchived}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to load locations');
    return res.json();
  },

  async getLocationById(token: string, id: string): Promise<LocationItem> {
    const res = await fetch(`${API_BASE}/${id}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Location not found');
    return res.json();
  },

  async createLocation(token: string, data: CreateLocationPayload): Promise<LocationItem> {
    const res = await fetch(API_BASE, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to create location');
    return res.json();
  },

  async updateLocation(token: string, id: string, data: UpdateLocationPayload): Promise<LocationItem> {
    const res = await fetch(`${API_BASE}/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to update location');
    return res.json();
  },

  async archiveLocation(token: string, id: string): Promise<void> {
    const res = await fetch(`${API_BASE}/${id}/archive`, {
      method: 'PUT',
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to archive location');
  },
};
