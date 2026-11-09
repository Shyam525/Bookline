import React, { useState, useEffect } from 'react';
import { useAuth } from '../../app/providers/AuthProvider';
import { adminApi, PlatformMetricsDto } from '../../services/api/admin';
import { Link } from 'react-router-dom';
import {
  Building2,
  Users,
  Calendar,
  ShoppingBag,
  DollarSign,
  MapPin,
  ShieldCheck,
  TrendingUp,
  Activity,
  ArrowRight,
} from 'lucide-react';

export const AdminDashboardPage: React.FC = () => {
  const { token } = useAuth();
  const [metrics, setMetrics] = useState<PlatformMetricsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchMetrics = () => {
    if (!token) {
      setLoading(false);
      return;
    }
    setLoading(true);
    setError(null);
    adminApi
      .getMetrics(token)
      .then(setMetrics)
      .catch((err) => {
        setError(err.message || 'Failed to load platform metrics');
        setMetrics(null);
      })
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    fetchMetrics();
  }, [token]);

  return (
    <div className="space-y-8">
      {/* Header */}
      <div>
        <h1 className="font-heading text-3xl font-bold text-white">Platform Governance Overview</h1>
        <p className="text-xs text-[#7E88A8]">
          Real-time marketplace economics, multi-city volume, and cross-vendor operational metrics
        </p>
      </div>

      {/* Error state */}
      {error && (
        <div className="bg-red-950/40 border border-red-800/60 rounded-2xl p-4 flex items-center justify-between">
          <p className="text-sm text-red-200">{error}</p>
          <button
            onClick={fetchMetrics}
            className="px-3 py-1.5 bg-red-800 hover:bg-red-700 text-white text-xs font-semibold rounded-lg transition"
          >
            Retry
          </button>
        </div>
      )}

      {/* KPI Cards Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-2">
          <div className="flex items-center justify-between text-[#7E88A8]">
            <span className="text-xs font-semibold uppercase tracking-wider">Gross Marketplace GMV</span>
            <DollarSign className="w-4 h-4 text-[#FBBF24]" />
          </div>
          <p className="font-heading text-3xl font-bold text-white">
            {loading ? '...' : `₹${(metrics?.grossMerchandiseValue ?? 0).toLocaleString()}`}
          </p>
          <p className="text-xs text-[#7E88A8]">Real-time settled GMV</p>
        </div>

        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-2">
          <div className="flex items-center justify-between text-[#7E88A8]">
            <span className="text-xs font-semibold uppercase tracking-wider">Platform Take (10%)</span>
            <TrendingUp className="w-4 h-4 text-[#34D399]" />
          </div>
          <p className="font-heading text-3xl font-bold text-[#34D399]">
            {loading ? '...' : `₹${(metrics?.platformCommissionRevenue ?? 0).toLocaleString()}`}
          </p>
          <p className="text-xs text-[#7E88A8]">Net Bookline commission</p>
        </div>

        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-2">
          <div className="flex items-center justify-between text-[#7E88A8]">
            <span className="text-xs font-semibold uppercase tracking-wider">Registered Providers</span>
            <Building2 className="w-4 h-4 text-[#E8546A]" />
          </div>
          <p className="font-heading text-3xl font-bold text-white">
            {loading ? '...' : (metrics?.totalProviders ?? 0)}
          </p>
          <p className="text-xs text-[#34D399]">
            {loading ? '...' : (metrics?.activeVerifiedProviders ?? 0)} verified by platform
          </p>
        </div>

        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-2">
          <div className="flex items-center justify-between text-[#7E88A8]">
            <span className="text-xs font-semibold uppercase tracking-wider">Active Cities</span>
            <MapPin className="w-4 h-4 text-[#34D399]" />
          </div>
          <p className="font-heading text-3xl font-bold text-white">
            {loading ? '...' : (metrics?.activeCities ?? 0)}
          </p>
          <p className="text-xs text-[#7E88A8]">Gujarat pilot coverage</p>
        </div>
      </div>

      {/* Secondary Metrics */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 flex items-center justify-between">
          <div className="space-y-1">
            <span className="text-xs uppercase text-[#7E88A8] font-bold tracking-wider">Total Appointments</span>
            <p className="font-heading text-2xl font-bold text-white">
              {loading ? '...' : (metrics?.totalBookings ?? 0)}
            </p>
            <p className="text-[11px] text-[#7E88A8]">Processed across multi-tenant scheduling engine</p>
          </div>
          <div className="w-12 h-12 rounded-xl bg-[#E8546A]/10 text-[#E8546A] flex items-center justify-center">
            <Calendar className="w-6 h-6" />
          </div>
        </div>

        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 flex items-center justify-between">
          <div className="space-y-1">
            <span className="text-xs uppercase text-[#7E88A8] font-bold tracking-wider">Retail Orders</span>
            <p className="font-heading text-2xl font-bold text-white">
              {loading ? '...' : (metrics?.totalOrders ?? 0)}
            </p>
            <p className="text-[11px] text-[#7E88A8]">Physical products fulfilled by provider stores</p>
          </div>
          <div className="w-12 h-12 rounded-xl bg-[#FBBF24]/10 text-[#FBBF24] flex items-center justify-center">
            <ShoppingBag className="w-6 h-6" />
          </div>
        </div>
      </div>

      {/* Quick Governance Links */}
      <div className="space-y-4">
        <h2 className="font-heading text-xl font-bold text-white">Governance Operations</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <Link
            to="/admin/providers"
            className="bg-[#111520] border border-[#212638] hover:border-[#E8546A]/50 rounded-2xl p-6 transition-all group flex flex-col justify-between"
          >
            <div className="space-y-2">
              <div className="w-10 h-10 rounded-xl bg-[#E8546A]/10 text-[#E8546A] flex items-center justify-center">
                <ShieldCheck className="w-5 h-5" />
              </div>
              <h3 className="font-heading font-bold text-base text-white group-hover:text-[#E8546A]">
                Provider Verification
              </h3>
              <p className="text-xs text-[#7E88A8]">
                Audit credentials, approve pending businesses, and grant marketplace badges.
              </p>
            </div>
            <div className="pt-4 flex items-center gap-1 text-xs text-[#E8546A] font-bold">
              <span>Manage Providers</span> <ArrowRight className="w-3.5 h-3.5" />
            </div>
          </Link>

          <Link
            to="/admin/reviews"
            className="bg-[#111520] border border-[#212638] hover:border-[#FBBF24]/50 rounded-2xl p-6 transition-all group flex flex-col justify-between"
          >
            <div className="space-y-2">
              <div className="w-10 h-10 rounded-xl bg-[#FBBF24]/10 text-[#FBBF24] flex items-center justify-center">
                <Users className="w-5 h-5" />
              </div>
              <h3 className="font-heading font-bold text-base text-white group-hover:text-[#FBBF24]">
                Review Moderation Queue
              </h3>
              <p className="text-xs text-[#7E88A8]">
                Approve or flag client reviews to prevent spam and maintain trust.
              </p>
            </div>
            <div className="pt-4 flex items-center gap-1 text-xs text-[#FBBF24] font-bold">
              <span>Inspect Reviews</span> <ArrowRight className="w-3.5 h-3.5" />
            </div>
          </Link>

          <Link
            to="/admin/health"
            className="bg-[#111520] border border-[#212638] hover:border-[#34D399]/50 rounded-2xl p-6 transition-all group flex flex-col justify-between"
          >
            <div className="space-y-2">
              <div className="w-10 h-10 rounded-xl bg-[#34D399]/10 text-[#34D399] flex items-center justify-center">
                <Activity className="w-5 h-5" />
              </div>
              <h3 className="font-heading font-bold text-base text-white group-hover:text-[#34D399]">
                System Infrastructure Health
              </h3>
              <p className="text-xs text-[#7E88A8]">
                Inspect PostgreSQL, PostGIS spatial index, Redis coordination, and Outbox queue.
              </p>
            </div>
            <div className="pt-4 flex items-center gap-1 text-xs text-[#34D399] font-bold">
              <span>Check Infrastructure</span> <ArrowRight className="w-3.5 h-3.5" />
            </div>
          </Link>
        </div>
      </div>
    </div>
  );
};
