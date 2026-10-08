export interface AnalyticsSummaryItem {
  totalRevenue: number;
  totalDeposits: number;
  totalBookings: number;
  completedBookings: number;
  cancelledBookings: number;
  averageTicketSize: number;
  occupancyRatePercentage: number;
  topStaffName: string;
  topServiceName: string;
}

export interface RevenueChartDataPoint {
  periodLabel: string;
  revenue: number;
  deposits: number;
  bookingsCount: number;
}

export interface StaffUtilizationItem {
  staffId: string;
  staffName: string;
  totalAppointments: number;
  totalHoursBooked: number;
  revenueGenerated: number;
  utilizationPercentage: number;
}

export interface ServicePerformanceItem {
  serviceId: string;
  serviceName: string;
  totalBookings: number;
  revenueGenerated: number;
  sharePercentage: number;
}

const API_BASE = '/api/v1/analytics';

export const analyticsApi = {
  async getSummary(token: string, params?: { startDate?: string; endDate?: string }): Promise<AnalyticsSummaryItem> {
    const query = new URLSearchParams();
    if (params?.startDate) query.append('startDate', params.startDate);
    if (params?.endDate) query.append('endDate', params.endDate);

    const res = await fetch(`${API_BASE}/summary?${query.toString()}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to load analytics summary');
    return res.json();
  },

  async getRevenueChart(token: string, period = '30days'): Promise<RevenueChartDataPoint[]> {
    const res = await fetch(`${API_BASE}/revenue-chart?period=${period}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to load revenue chart data');
    return res.json();
  },

  async getStaffUtilization(token: string, params?: { startDate?: string; endDate?: string }): Promise<StaffUtilizationItem[]> {
    const query = new URLSearchParams();
    if (params?.startDate) query.append('startDate', params.startDate);
    if (params?.endDate) query.append('endDate', params.endDate);

    const res = await fetch(`${API_BASE}/staff-utilization?${query.toString()}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to load staff utilization analytics');
    return res.json();
  },

  async getServicePerformance(token: string, params?: { startDate?: string; endDate?: string }): Promise<ServicePerformanceItem[]> {
    const query = new URLSearchParams();
    if (params?.startDate) query.append('startDate', params.startDate);
    if (params?.endDate) query.append('endDate', params.endDate);

    const res = await fetch(`${API_BASE}/service-performance?${query.toString()}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error('Failed to load service performance analytics');
    return res.json();
  },

  getExportUrl(): string {
    return `${API_BASE}/export`;
  }
};
