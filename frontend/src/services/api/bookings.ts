export interface CalendarBookingItem {
  id: string;
  staffId: string;
  staffName: string;
  serviceId: string;
  serviceName: string;
  customerId: string;
  customerName: string;
  startUtc: string;
  endUtc: string;
  status: 'Pending' | 'Confirmed' | 'Cancelled' | 'Completed' | 'NoShow';
}

export interface CreateBookingPayload {
  serviceId: string;
  staffId: string;
  customerId: string;
  startUtc: string;
  holdId?: string;
}

export interface RescheduleBookingPayload {
  startUtc: string;
  holdId?: string;
}

const API_BASE = '/api/v1/bookings';

export const bookingsApi = {
  async getCalendarBookings(token: string, fromUtc: string, toUtc: string, staffId?: string): Promise<CalendarBookingItem[]> {
    let url = `${API_BASE}?fromUtc=${encodeURIComponent(fromUtc)}&toUtc=${encodeURIComponent(toUtc)}`;
    if (staffId) url += `&staffId=${encodeURIComponent(staffId)}`;

    const res = await fetch(url, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to load calendar appointments');
    return res.json();
  },

  async confirmBooking(token: string, id: string): Promise<void> {
    const res = await fetch(`${API_BASE}/${id}/confirm`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to confirm booking');
  },

  async cancelBooking(token: string, id: string): Promise<void> {
    const res = await fetch(`${API_BASE}/${id}/cancel`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to cancel booking');
  },

  async rescheduleBooking(token: string, id: string, data: RescheduleBookingPayload): Promise<CalendarBookingItem> {
    const res = await fetch(`${API_BASE}/${id}/reschedule`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to reschedule booking');
    return res.json();
  },
};
