export interface BusinessProfile {
  businessId: string;
  name: string;
  slug: string;
  description: string;
  category: string;
  businessType: string;
  phone?: string;
  email?: string;
  website?: string;
  address: string;
  city: string;
  area: string;
  state: string;
  country: string;
  postalCode: string;
  latitude: number;
  longitude: number;
  timeZoneId: string;
  currency: string;
  isPublished: boolean;
  isActive: boolean;
  logoUrl?: string;
  coverImageUrl?: string;
  galleryImages: string[];
  primaryLocationId?: string;
  primaryLocationName?: string;
}

export interface UpdateBusinessProfilePayload {
  name: string;
  slug: string;
  description: string;
  category: string;
  businessType: string;
  phone?: string;
  email?: string;
  website?: string;
  address: string;
  city: string;
  area: string;
  state: string;
  country: string;
  postalCode: string;
  latitude?: number;
  longitude?: number;
  timeZoneId: string;
  logoUrl?: string;
  coverImageUrl?: string;
  galleryImages?: string[];
  isPublished?: boolean;
}

export interface BusinessBreakPeriod {
  startTime: string;
  endTime: string;
  label?: string;
}

export interface BusinessDayHours {
  dayOfWeek: number;
  isOpen: boolean;
  openTime?: string;
  closeTime?: string;
  breaks?: BusinessBreakPeriod[];
}

export interface BusinessPolicies {
  holdDurationMinutes: number;
  minimumNoticeHours: number;
  bookingHorizonDays: number;
  cancellationWindowHours: number;
  cancellationPolicy?: string;
  reschedulingWindowHours: number;
  reschedulingPolicy?: string;
  weeklyHours: BusinessDayHours[];
}

export interface UpdateBusinessPoliciesPayload {
  holdDurationMinutes: number;
  minimumNoticeHours: number;
  bookingHorizonDays: number;
  cancellationWindowHours: number;
  cancellationPolicy?: string;
  reschedulingWindowHours: number;
  reschedulingPolicy?: string;
  weeklyHours?: BusinessDayHours[];
}

export interface SlugCheckResult {
  isAvailable: boolean;
  slug: string;
  message?: string;
}

const API_BASE = '/api/v1/business';

export const businessApi = {
  async getProfile(token: string): Promise<BusinessProfile> {
    const res = await fetch(`${API_BASE}/profile`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to load business profile');
    return res.json();
  },

  async updateProfile(token: string, payload: UpdateBusinessProfilePayload): Promise<BusinessProfile> {
    const res = await fetch(`${API_BASE}/profile`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(payload),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Failed to update business profile');
    }
    return res.json();
  },

  async getPolicies(token: string): Promise<BusinessPolicies> {
    const res = await fetch(`${API_BASE}/policies`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to load business policies');
    return res.json();
  },

  async updatePolicies(token: string, payload: UpdateBusinessPoliciesPayload): Promise<BusinessPolicies> {
    const res = await fetch(`${API_BASE}/policies`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(payload),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Failed to update business policies');
    }
    return res.json();
  },

  async checkSlug(token: string, slug: string): Promise<SlugCheckResult> {
    const res = await fetch(`${API_BASE}/slug/check?slug=${encodeURIComponent(slug)}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to check slug availability');
    return res.json();
  },
};

