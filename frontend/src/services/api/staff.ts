export interface WorkingHourItem {
  id: string;
  staffId: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
}

export interface StaffItem {
  id: string;
  tenantId: string;
  name: string;
  email: string;
  phone?: string;
  title?: string;
  bio?: string;
  avatarUrl?: string;
  timeZoneId: string;
  isActive: boolean;
  isArchived: boolean;
  assignedServiceIds: string[];
  workingHours: WorkingHourItem[];
  createdAtUtc: string;
}

export interface CreateStaffPayload {
  name: string;
  email: string;
  phone?: string;
  title?: string;
  bio?: string;
  avatarUrl?: string;
  timeZoneId?: string;
  assignedServiceIds?: string[];
}

export interface UpdateStaffPayload {
  name: string;
  email: string;
  phone?: string;
  title?: string;
  bio?: string;
  avatarUrl?: string;
  timeZoneId?: string;
  isActive: boolean;
  assignedServiceIds?: string[];
}

const API_BASE = '/api/v1/staff';

export const staffApi = {
  async getAllStaff(token: string, includeArchived = false): Promise<StaffItem[]> {
    const res = await fetch(`${API_BASE}?includeArchived=${includeArchived}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to load staff members');
    return res.json();
  },

  async getStaffById(token: string, id: string): Promise<StaffItem> {
    const res = await fetch(`${API_BASE}/${id}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Staff member not found');
    return res.json();
  },

  async createStaff(token: string, data: CreateStaffPayload): Promise<StaffItem> {
    const res = await fetch(API_BASE, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to create staff member');
    return res.json();
  },

  async updateStaff(token: string, id: string, data: UpdateStaffPayload): Promise<StaffItem> {
    const res = await fetch(`${API_BASE}/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to update staff member');
    return res.json();
  },

  async archiveStaff(token: string, id: string): Promise<void> {
    const res = await fetch(`${API_BASE}/${id}/archive`, {
      method: 'PUT',
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to archive staff member');
  },

  async assignServices(token: string, id: string, serviceIds: string[]): Promise<StaffItem> {
    const res = await fetch(`${API_BASE}/${id}/services`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(serviceIds),
    });
    if (!res.ok) throw new Error('Failed to assign services');
    return res.json();
  },
};
