export interface TimeSlotItem {
  startIso: string;
  endIso: string;
  displayTime: string;
  isAvailable: boolean;
  staffId?: string;
  staffName?: string;
  date?: string;
  localTime?: string;
  instant?: string;
  timezone?: string;
  durationMinutes?: number;
}

export interface TimeOffItem {
  id: string;
  staffId: string;
  startUtc: string;
  endUtc: string;
  reason?: string;
}

export interface CreateTimeOffPayload {
  startUtc: string;
  endUtc: string;
  reason?: string;
}

export interface WorkingHourInputPayload {
  dayOfWeek: number; // 0=Sunday, 1=Monday...
  startTime: string; // HH:mm
  endTime: string;   // HH:mm
}

const API_BASE = '/api/v1';

export const availabilityApi = {
  async getSlots(serviceId: string, staffId?: string, date?: string, timezone = 'UTC'): Promise<TimeSlotItem[]> {
    let url = `${API_BASE}/availability/slots?serviceId=${encodeURIComponent(serviceId)}&timezone=${encodeURIComponent(timezone)}`;
    if (staffId) url += `&staffId=${encodeURIComponent(staffId)}`;
    if (date) url += `&date=${encodeURIComponent(date)}`;

    const res = await fetch(url);
    if (!res.ok) throw new Error('Failed to compute availability slots');
    return res.json();
  },

  async getStaffTimeOff(token: string, staffId: string): Promise<TimeOffItem[]> {
    const res = await fetch(`${API_BASE}/staff/${staffId}/time-off`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to load staff time-off records');
    return res.json();
  },

  async createStaffTimeOff(token: string, staffId: string, payload: CreateTimeOffPayload): Promise<TimeOffItem> {
    const res = await fetch(`${API_BASE}/staff/${staffId}/time-off`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(payload),
    });
    if (!res.ok) throw new Error('Failed to record time-off');
    return res.json();
  },

  async deleteStaffTimeOff(token: string, staffId: string, timeOffId: string): Promise<void> {
    const res = await fetch(`${API_BASE}/staff/${staffId}/time-off/${timeOffId}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to delete time-off entry');
  },

  async setStaffWorkingHours(token: string, staffId: string, workingHours: WorkingHourInputPayload[]): Promise<void> {
    const res = await fetch(`${API_BASE}/staff/${staffId}/working-hours`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(workingHours),
    });
    if (!res.ok) throw new Error('Failed to save working hours');
  },
};
