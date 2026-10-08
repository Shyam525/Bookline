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
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: 'Invalid credentials' }));
      throw new Error(err.message || err.Message || 'Authentication failed');
    }
    return res.json();
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
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: 'Registration failed' }));
      throw new Error(err.message || err.Message || 'Registration failed');
    }
    return res.json();
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
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: 'Registration failed' }));
      throw new Error(err.message || err.Message || 'Registration failed');
    }
    return res.json();
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
    if (!res.ok) {
      throw new Error('Failed to switch business');
    }
    return res.json();
  },

  async refreshToken(refreshToken: string): Promise<AuthResponse> {
    const res = await fetch(`${API_BASE}/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken }),
    });
    if (!res.ok) {
      throw new Error('Refresh token expired or invalid');
    }
    return res.json();
  },

  async forgotPassword(email: string): Promise<{ message: string }> {
    const res = await fetch(`${API_BASE}/forgot-password`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email }),
    });
    return res.json();
  },

  async resetPassword(email: string, resetToken: string, newPassword: string): Promise<{ message: string }> {
    const res = await fetch(`${API_BASE}/reset-password`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, resetToken, newPassword }),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: 'Password reset failed' }));
      throw new Error(err.message || err.Message || 'Reset failed');
    }
    return res.json();
  },

  async getCurrentUser(token: string): Promise<AuthUser> {
    const res = await fetch(`${API_BASE}/me`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });
    if (!res.ok) {
      throw new Error('Unauthorized');
    }
    return res.json();
  },
};
