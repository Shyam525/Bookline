import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  BarChart3,
  TrendingUp,
  DollarSign,
  Users,
  Download,
  Award,
  Scissors,
  Clock,
  RefreshCw
} from 'lucide-react';
import { analyticsApi, StaffUtilizationItem, ServicePerformanceItem } from '../../services/api/analytics';

export const AnalyticsPage: React.FC = () => {
  const [period, setPeriod] = useState<string>('30days');

  // Queries
  const { data: summaryData, refetch: refetchSummary } = useQuery({
    queryKey: ['analyticsSummary', period],
    queryFn: () => analyticsApi.getSummary('mock-token')
  });

  const { data: chartData, refetch: refetchChart } = useQuery({
    queryKey: ['revenueChart', period],
    queryFn: () => analyticsApi.getRevenueChart('mock-token', period)
  });

  const { data: staffData, isLoading: isStaffLoading, refetch: refetchStaff } = useQuery({
    queryKey: ['staffUtilization', period],
    queryFn: () => analyticsApi.getStaffUtilization('mock-token')
  });

  const { data: serviceData, isLoading: isServiceLoading, refetch: refetchService } = useQuery({
    queryKey: ['servicePerformance', period],
    queryFn: () => analyticsApi.getServicePerformance('mock-token')
  });

  const handleRefresh = () => {
    refetchSummary();
    refetchChart();
    refetchStaff();
    refetchService();
  };

  const handleExportCsv = () => {
    window.open(analyticsApi.getExportUrl(), '_blank');
  };

  const maxRevenue = Math.max(...(chartData?.map((d) => d.revenue) || [100]), 100);

  return (
    <div className="space-y-6 text-slate-100">
      {/* Top Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-slate-800 pb-5">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-white flex items-center gap-3">
            <BarChart3 className="w-7 h-7 text-[#E8546A]" />
            Analytics & Financial Reporting Dashboard
          </h1>
          <p className="text-sm text-slate-400 mt-1">
            Real-time business performance, staff productivity metrics, and revenue growth analytics.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <select
            value={period}
            onChange={(e) => setPeriod(e.target.value)}
            className="bg-[#181D2C] border border-slate-700 text-slate-200 text-sm rounded-lg px-3 py-2 focus:outline-none focus:border-[#E8546A]"
          >
            <option value="7days">Last 7 Days</option>
            <option value="14days">Last 14 Days</option>
            <option value="30days">Last 30 Days</option>
          </select>

          <button
            onClick={handleRefresh}
            className="p-2 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-lg border border-slate-700 transition"
            title="Refresh analytics data"
          >
            <RefreshCw className="w-4 h-4" />
          </button>

          <button
            onClick={handleExportCsv}
            className="flex items-center gap-2 px-4 py-2 bg-[#E8546A] hover:bg-[#d44359] text-white font-medium rounded-lg text-sm transition shadow-sm"
          >
            <Download className="w-4 h-4" />
            Export CSV Report
          </button>
        </div>
      </div>

      {/* Financial Overview KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
        <div className="bg-[#111520] border border-slate-800 rounded-xl p-5 space-y-2">
          <div className="flex justify-between items-center text-slate-400 text-xs font-medium uppercase tracking-wider">
            <span>Total Gross Revenue</span>
            <div className="p-2 bg-emerald-500/10 text-emerald-400 rounded-lg">
              <DollarSign className="w-4 h-4" />
            </div>
          </div>
          <div className="text-2xl font-bold text-white">${summaryData?.totalRevenue?.toFixed(2) || '0.00'}</div>
          <div className="text-xs text-emerald-400 flex items-center gap-1">
            <TrendingUp className="w-3.5 h-3.5" />
            <span>Includes deposits & completed sales</span>
          </div>
        </div>

        <div className="bg-[#111520] border border-slate-800 rounded-xl p-5 space-y-2">
          <div className="flex justify-between items-center text-slate-400 text-xs font-medium uppercase tracking-wider">
            <span>Average Ticket Size</span>
            <div className="p-2 bg-blue-500/10 text-blue-400 rounded-lg">
              <BarChart3 className="w-4 h-4" />
            </div>
          </div>
          <div className="text-2xl font-bold text-white">${summaryData?.averageTicketSize?.toFixed(2) || '0.00'}</div>
          <div className="text-xs text-slate-400">Revenue per completed appointment</div>
        </div>

        <div className="bg-[#111520] border border-slate-800 rounded-xl p-5 space-y-2">
          <div className="flex justify-between items-center text-slate-400 text-xs font-medium uppercase tracking-wider">
            <span>Occupancy Rate</span>
            <div className="p-2 bg-purple-500/10 text-purple-400 rounded-lg">
              <Clock className="w-4 h-4" />
            </div>
          </div>
          <div className="text-2xl font-bold text-white">{summaryData?.occupancyRatePercentage || 0}%</div>
          <div className="text-xs text-slate-400">{summaryData?.completedBookings || 0} of {summaryData?.totalBookings || 0} slots fulfilled</div>
        </div>

        <div className="bg-[#111520] border border-slate-800 rounded-xl p-5 space-y-2">
          <div className="flex justify-between items-center text-slate-400 text-xs font-medium uppercase tracking-wider">
            <span>Top Performer Staff</span>
            <div className="p-2 bg-amber-500/10 text-amber-400 rounded-lg">
              <Award className="w-4 h-4" />
            </div>
          </div>
          <div className="text-lg font-bold text-white truncate">{summaryData?.topStaffName || 'N/A'}</div>
          <div className="text-xs text-slate-400">Highest booked team member</div>
        </div>
      </div>

      {/* Visual Revenue Trend Chart Card */}
      <div className="bg-[#111520] border border-slate-800 rounded-xl p-6 space-y-4">
        <div className="flex justify-between items-center">
          <h2 className="text-lg font-semibold text-white flex items-center gap-2">
            <TrendingUp className="w-5 h-5 text-[#E8546A]" />
            Revenue & Deposit Growth Trend
          </h2>
          <span className="text-xs text-slate-400">Daily performance breakdown</span>
        </div>

        {/* Visual Bar Graph */}
        <div className="h-48 flex items-end justify-between gap-2 pt-6 border-b border-slate-800 pb-2">
          {chartData?.map((point, index) => {
            const heightPct = Math.max(10, (point.revenue / maxRevenue) * 100);
            return (
              <div key={index} className="flex-1 flex flex-col items-center gap-2 group relative">
                {/* Tooltip */}
                <div className="absolute -top-10 opacity-0 group-hover:opacity-100 transition bg-slate-900 text-white text-xs px-2.5 py-1 rounded shadow border border-slate-700 whitespace-nowrap z-10 pointer-events-none">
                  {point.periodLabel}: ${point.revenue.toFixed(2)} ({point.bookingsCount} appts)
                </div>

                <div
                  className="w-full bg-[#E8546A]/80 group-hover:bg-[#E8546A] transition rounded-t"
                  style={{ height: `${heightPct}%` }}
                />
                <span className="text-[10px] text-slate-400 truncate w-full text-center">{point.periodLabel}</span>
              </div>
            );
          })}
        </div>
      </div>

      {/* Staff Utilization & Service Popularity Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Staff Productivity Table */}
        <div className="lg:col-span-2 bg-[#111520] border border-slate-800 rounded-xl p-6 space-y-4">
          <h2 className="text-lg font-semibold text-white flex items-center gap-2">
            <Users className="w-5 h-5 text-[#E8546A]" />
            Staff Utilization & Revenue Generation
          </h2>

          <div className="overflow-x-auto border border-slate-800 rounded-lg">
            <table className="w-full text-left text-sm text-slate-300">
              <thead className="bg-[#181D2C] text-xs uppercase text-slate-400 font-semibold border-b border-slate-800">
                <tr>
                  <th className="px-4 py-3">Staff Member</th>
                  <th className="px-4 py-3">Appointments</th>
                  <th className="px-4 py-3">Hours Booked</th>
                  <th className="px-4 py-3">Revenue</th>
                  <th className="px-4 py-3">Utilization %</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800/60">
                {isStaffLoading ? (
                  <tr>
                    <td colSpan={5} className="text-center py-6 text-slate-500">Loading staff utilization metrics...</td>
                  </tr>
                ) : staffData?.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="text-center py-6 text-slate-500">No staff performance data available.</td>
                  </tr>
                ) : (
                  staffData?.map((staff: StaffUtilizationItem) => (
                    <tr key={staff.staffId} className="hover:bg-slate-800/40 transition">
                      <td className="px-4 py-3 font-medium text-white">{staff.staffName}</td>
                      <td className="px-4 py-3">{staff.totalAppointments}</td>
                      <td className="px-4 py-3">{staff.totalHoursBooked} hrs</td>
                      <td className="px-4 py-3 font-semibold text-emerald-400">${staff.revenueGenerated.toFixed(2)}</td>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-2">
                          <div className="w-20 bg-slate-800 h-2 rounded-full overflow-hidden">
                            <div
                              className="bg-[#E8546A] h-full rounded-full"
                              style={{ width: `${Math.min(100, staff.utilizationPercentage)}%` }}
                            />
                          </div>
                          <span className="text-xs text-slate-300 font-mono">{staff.utilizationPercentage}%</span>
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>

        {/* Service Share Breakdown */}
        <div className="bg-[#111520] border border-slate-800 rounded-xl p-6 space-y-4">
          <h2 className="text-lg font-semibold text-white flex items-center gap-2">
            <Scissors className="w-5 h-5 text-[#E8546A]" />
            Service Popularity Distribution
          </h2>

          <div className="space-y-4">
            {isServiceLoading ? (
              <div className="text-center py-6 text-slate-500">Loading service analytics...</div>
            ) : serviceData?.length === 0 ? (
              <div className="text-center py-6 text-slate-500">No service booking records.</div>
            ) : (
              serviceData?.map((service: ServicePerformanceItem) => (
                <div key={service.serviceId} className="p-3 bg-[#181D2C] rounded-lg border border-slate-800/80 space-y-2">
                  <div className="flex justify-between items-center">
                    <span className="text-sm font-medium text-white">{service.serviceName}</span>
                    <span className="text-xs font-semibold text-emerald-400">${service.revenueGenerated.toFixed(2)}</span>
                  </div>
                  <div className="flex justify-between text-xs text-slate-400">
                    <span>{service.totalBookings} bookings</span>
                    <span>{service.sharePercentage}% market share</span>
                  </div>
                  <div className="w-full bg-slate-800 h-1.5 rounded-full overflow-hidden">
                    <div
                      className="bg-emerald-500 h-full rounded-full"
                      style={{ width: `${Math.min(100, service.sharePercentage)}%` }}
                    />
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
