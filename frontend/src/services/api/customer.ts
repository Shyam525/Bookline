export interface CustomerAppointmentDto {
  id: string;
  bookingReference: string;
  tenantId: string;
  providerName: string;
  providerSlug: string;
  locationName: string;
  serviceName: string;
  staffName: string;
  startUtc: string;
  endUtc: string;
  status: string;
  totalPrice: number;
  depositPaid: number;
  currency: string;
  cancellationReason?: string;
  customerNotes?: string;
  createdAtUtc: string;
  minimumNoticeHours: number;
}

export interface CustomerProfileDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  phone?: string;
  avatarUrl?: string;
  stats: {
    totalBookings: number;
    totalOrders: number;
    favoritesCount: number;
  };
}

const API_BASE = '/api/v1/customer';

export const customerApi = {
  async getProfile(token: string): Promise<CustomerProfileDto> {
    const res = await fetch(`${API_BASE}/profile`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to load profile');
    return res.json();
  },

  async updateProfile(data: { firstName: string; lastName: string; phone?: string }, token: string) {
    const res = await fetch(`${API_BASE}/profile`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to update profile');
    return res.json();
  },

  async getAppointments(token: string): Promise<CustomerAppointmentDto[]> {
    const res = await fetch(`${API_BASE}/appointments`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) return [];
    return res.json();
  },

  async rescheduleAppointment(id: string, newStartUtc: string, newEndUtc: string, token: string) {
    const res = await fetch(`${API_BASE}/appointments/${id}/reschedule`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({ newStartUtc, newEndUtc }),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || err.Message || 'Failed to reschedule');
    }
    return res.json();
  },

  async cancelAppointment(id: string, reason: string, token: string) {
    const res = await fetch(`${API_BASE}/appointments/${id}/cancel`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({ reason }),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || err.Message || 'Failed to cancel');
    }
    return res.json();
  },

  async getNotifications(token: string) {
    const res = await fetch(`${API_BASE}/notifications`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) return [];
    return res.json();
  },
};
