export interface OnboardingStatus {
  tenantId: string;
  businessName: string;
  businessSlug: string;
  businessType: string;
  category?: string;
  description?: string;
  website?: string;
  phone?: string;
  address: string;
  city?: string;
  state?: string;
  postalCode?: string;
  country?: string;
  timeZoneId: string;
  currency: string;
  currentStep: number;
  isCompleted: boolean;
  isPublished: boolean;
  verificationStatus: string;
  moderationStatus: string;
  firstServiceName?: string;
  firstServiceDurationMinutes?: number;
  firstServicePrice?: number;
  firstProductName?: string;
  firstProductPrice?: number;
  firstProductStock?: number;
  firstProductSku?: string;
  firstStaffName?: string;
  firstStaffTitle?: string;
  startTime?: string;
  endTime?: string;
  holdDurationMinutes: number;
  minimumNoticeHours: number;
  bookingHorizonDays: number;
  cancellationWindowHours: number;
  completedSteps: number[];
}

export interface SaveStepData {
  step: number;
  businessName?: string;
  businessSlug?: string;
  businessType?: string;
  category?: string;
  description?: string;
  website?: string;
  phone?: string;
  locationName?: string;
  address?: string;
  city?: string;
  state?: string;
  postalCode?: string;
  country?: string;
  timeZoneId?: string;
  currency?: string;
  firstServiceName?: string;
  firstServiceDurationMinutes?: number;
  firstServicePrice?: number;
  firstProductName?: string;
  firstProductPrice?: number;
  firstProductStock?: number;
  firstProductSku?: string;
  firstStaffName?: string;
  firstStaffTitle?: string;
  startTime?: string;
  endTime?: string;
  workingDays?: number[];
  holdDurationMinutes?: number;
  minimumNoticeHours?: number;
  bookingHorizonDays?: number;
  cancellationWindowHours?: number;
}

export interface OnboardingReadinessRequirement {
  key: string;
  title: string;
  description: string;
  stepNumber: number;
  isSatisfied: boolean;
  isMandatory: boolean;
  actionRoute: string;
}

export interface OnboardingReadiness {
  isReady: boolean;
  totalMandatory: number;
  satisfiedMandatory: number;
  allMandatorySatisfied: boolean;
  requirements: OnboardingReadinessRequirement[];
}

export interface PublishResult {
  success: boolean;
  isPublished: boolean;
  storefrontUrl: string;
  message: string;
  missingRequirements?: string[];
}

const API_BASE = '/api/v1/onboarding';

export const onboardingApi = {
  async getStatus(token: string): Promise<OnboardingStatus> {
    const res = await fetch(`${API_BASE}/status`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) {
      const err = await res.json().catch(() => null);
      throw new Error(err?.message || err?.detail || 'Failed to load onboarding status');
    }
    return res.json();
  },

  async saveStep(token: string, data: SaveStepData): Promise<OnboardingStatus> {
    const res = await fetch(`${API_BASE}/step`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => null);
      const msg = err?.message || err?.detail || err?.title || 'Failed to save onboarding progress';
      throw new Error(msg);
    }
    return res.json();
  },

  async getReadiness(token: string): Promise<OnboardingReadiness> {
    const res = await fetch(`${API_BASE}/readiness`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) {
      const err = await res.json().catch(() => null);
      throw new Error(err?.message || err?.detail || 'Failed to evaluate publication readiness');
    }
    return res.json();
  },

  async publishStorefront(token: string): Promise<PublishResult> {
    const res = await fetch(`${API_BASE}/publish`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok || !data.success) {
      const msg = data.missingRequirements?.length
        ? `${data.message || 'Readiness requirements unsatisfied.'} Missing: ${data.missingRequirements.join(', ')}`
        : data.message || 'Failed to publish storefront.';
      throw new Error(msg);
    }
    return data;
  },

  async complete(token: string): Promise<OnboardingStatus> {
    const res = await fetch(`${API_BASE}/complete`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) {
      const err = await res.json().catch(() => null);
      throw new Error(err?.message || err?.detail || 'Failed to complete onboarding');
    }
    return res.json();
  },
};
