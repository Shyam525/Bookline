export interface OnboardingStatus {
  tenantId: string;
  businessName: string;
  businessSlug: string;
  businessType: string;
  address: string;
  timeZoneId: string;
  currency: string;
  currentStep: number;
  isCompleted: boolean;
  holdDurationMinutes: number;
  minimumNoticeHours: number;
  bookingHorizonDays: number;
}

export interface SaveStepData {
  step: number;
  businessName?: string;
  businessType?: string;
  address?: string;
  timeZoneId?: string;
  currency?: string;
  firstServiceName?: string;
  firstServiceDurationMinutes?: number;
  firstServicePrice?: number;
  firstStaffName?: string;
  startTime?: string;
  endTime?: string;
  holdDurationMinutes?: number;
  minimumNoticeHours?: number;
  bookingHorizonDays?: number;
}

const API_BASE = '/api/v1/onboarding';

export const onboardingApi = {
  async getStatus(token: string): Promise<OnboardingStatus> {
    const res = await fetch(`${API_BASE}/status`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to load onboarding status');
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
    if (!res.ok) throw new Error('Failed to save onboarding progress');
    return res.json();
  },

  async complete(token: string): Promise<OnboardingStatus> {
    const res = await fetch(`${API_BASE}/complete`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to complete onboarding');
    return res.json();
  },
};
