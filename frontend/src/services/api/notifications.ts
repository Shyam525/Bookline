export interface NotificationLogItem {
  id: string;
  tenantId: string;
  bookingId?: string;
  customerId?: string;
  recipientEmail: string;
  recipientPhone?: string;
  notificationType: string;
  channel: string;
  status: string;
  subject: string;
  body: string;
  errorMessage?: string;
  sentAtUtc?: string;
  createdAtUtc: string;
}

export interface PagedNotificationLogsResponse {
  items: NotificationLogItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface NotificationSettingItem {
  id: string;
  tenantId: string;
  emailNotificationsEnabled: boolean;
  smsNotificationsEnabled: boolean;
  reminder24hEnabled: boolean;
  reminder1hEnabled: boolean;
  senderEmail: string;
  senderName: string;
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface UpdateNotificationSettingPayload {
  emailNotificationsEnabled: boolean;
  smsNotificationsEnabled: boolean;
  reminder24hEnabled: boolean;
  reminder1hEnabled: boolean;
  senderEmail: string;
  senderName: string;
}

export interface SendTestNotificationPayload {
  recipientEmail: string;
  recipientPhone?: string;
  channel: string;
}

const API_BASE = '/api/v1/notifications';

export const notificationsApi = {
  async getLogs(
    token: string,
    params?: { pageNumber?: number; pageSize?: number; status?: string; channel?: string }
  ): Promise<PagedNotificationLogsResponse> {
    const query = new URLSearchParams();
    if (params?.pageNumber) query.append('pageNumber', params.pageNumber.toString());
    if (params?.pageSize) query.append('pageSize', params.pageSize.toString());
    if (params?.status) query.append('status', params.status);
    if (params?.channel) query.append('channel', params.channel);

    const res = await fetch(`${API_BASE}/logs?${query.toString()}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to load notification logs');
    return res.json();
  },

  async getSettings(token: string): Promise<NotificationSettingItem> {
    const res = await fetch(`${API_BASE}/settings`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to load notification settings');
    return res.json();
  },

  async updateSettings(token: string, data: UpdateNotificationSettingPayload): Promise<NotificationSettingItem> {
    const res = await fetch(`${API_BASE}/settings`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to update notification settings');
    return res.json();
  },

  async sendTestNotification(token: string, data: SendTestNotificationPayload): Promise<{ message: string }> {
    const res = await fetch(`${API_BASE}/send-test`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to dispatch test notification');
    return res.json();
  }
};
