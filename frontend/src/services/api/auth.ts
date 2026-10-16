import { parseApiResponse, apiClient } from '../../utils/apiErrors';

export interface AuthUser {
  id: string;
  tenantId: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName?: string;
  phone?: string;
  avatarUrl?: string;
  role: string;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiryUtc: string;
  refreshTokenExpiryUtc: string;
}

export interface AuthResponse extends AuthTokens {
  userId: string;
  tenantId: string;
  email: string;
  role: string;
}

export interface BusinessSummary {
  id: string;
  name: string;
  slug: string;
  category: string;
  city: string;
  logoUrl?: string;
  role: string;
}

const API_BASE = '/api/v1/auth';

export const authApi = {
  async login(email: string, password: string): Promise<AuthResponse> {
    const res = await fetch(`${API_BASE}/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    });
    return parseApiResponse<AuthResponse>(res);
  },

  async registerCustomer(data: {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
    phone?: string;
  }): Promise<AuthResponse> {
    const res = await fetch(`${API_BASE}/register-customer`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    return parseApiResponse<AuthResponse>(res);
  },

  async registerTenant(data: {
    tenantName: string;
    tenantSlug: string;
    ownerEmail: string;
    password: string;
    firstName: string;
    lastName: string;
  }): Promise<AuthResponse> {
    const res = await fetch(`${API_BASE}/register-tenant`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    return parseApiResponse<AuthResponse>(res);
  },

  async getMyBusinesses(token: string): Promise<BusinessSummary[]> {
    const res = await fetch(`${API_BASE}/my-businesses`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) return [];
    return res.json();
  },

  async switchBusiness(tenantId: string, token: string): Promise<AuthResponse> {
    const res = await fetch(`${API_BASE}/switch-business`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({ tenantId }),
    });
    return parseApiResponse<AuthResponse>(res);
  },

  async refreshToken(refreshToken: string): Promise<AuthResponse> {
    const res = await fetch(`${API_BASE}/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken }),
    });
    return parseApiResponse<AuthResponse>(res);
  },

  async forgotPassword(email: string): Promise<{ message: string }> {
    const res = await fetch(`${API_BASE}/forgot-password`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email }),
    });
    return parseApiResponse<{ message: string }>(res);
  },

  async resetPassword(email: string, resetToken: string, newPassword: string): Promise<{ message: string }> {
    const res = await fetch(`${API_BASE}/reset-password`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, resetToken, newPassword }),
    });
    return parseApiResponse<{ message: string }>(res);
  },

  async getCurrentUser(token: string): Promise<AuthUser> {
    const res = await fetch(`${API_BASE}/me`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });
    return parseApiResponse<AuthUser>(res);
  },
};
