import React from 'react';
import { Link } from 'react-router-dom';
import { Shield, Zap, Globe } from 'lucide-react';

export const HomePage: React.FC = () => {
  return (
    <div className="max-w-6xl mx-auto px-6 py-20 space-y-20">
      {/* Hero Section */}
      <div className="text-center space-y-6 max-w-3xl mx-auto">
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-[#181D2C] border border-[#212638] text-xs font-mono text-[#E8546A]">
          <span>PHASE 0 FOUNDATION</span>
        </div>
        <h1 className="font-heading text-5xl font-extrabold tracking-tight text-white leading-tight">
          Elite Production-Grade Scheduling Engine
        </h1>
        <p className="text-lg text-[#7E88A8] leading-relaxed">
          Bookline provides high-concurrency multi-tenant scheduling, real-time slot locking, double-booking prevention, and timezone alignment for modern service enterprises.
        </p>
        <div className="flex items-center justify-center gap-4 pt-4">
          <Link
            to="/app"
            className="px-6 py-3 rounded-xl bg-[#E8546A] text-white font-medium hover:bg-[#D44359] transition-colors"
          >
            Launch Operations Dashboard
          </Link>
          <Link
            to="/book/acme-salon"
            className="px-6 py-3 rounded-xl bg-[#181D2C] border border-[#212638] text-white font-medium hover:bg-[#212638] transition-colors"
          >
            Public Booking Client
          </Link>
        </div>
      </div>

      {/* Feature Grid */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="bg-[#111520] border border-[#212638] p-6 rounded-2xl space-y-3">
          <div className="w-10 h-10 rounded-lg bg-[#181D2C] flex items-center justify-center text-[#E8546A]">
            <Zap className="w-5 h-5" />
          </div>
          <h3 className="font-heading text-lg font-semibold text-white">Double-Booking Lock</h3>
          <p className="text-sm text-[#7E88A8]">
            Redis atomic slot holds paired with PostgreSQL GiST exclusion constraints guarantee strict concurrency.
          </p>
        </div>
        <div className="bg-[#111520] border border-[#212638] p-6 rounded-2xl space-y-3">
          <div className="w-10 h-10 rounded-lg bg-[#181D2C] flex items-center justify-center text-[#34D399]">
            <Globe className="w-5 h-5" />
          </div>
          <h3 className="font-heading text-lg font-semibold text-white">NodaTime Precision</h3>
          <p className="text-sm text-[#7E88A8]">
            Canonical UTC instant storage mapped to staff and tenant business timezones prevents offset errors.
          </p>
        </div>
        <div className="bg-[#111520] border border-[#212638] p-6 rounded-2xl space-y-3">
          <div className="w-10 h-10 rounded-lg bg-[#181D2C] flex items-center justify-center text-[#FBBF24]">
            <Shield className="w-5 h-5" />
          </div>
          <h3 className="font-heading text-lg font-semibold text-white">Transactional Outbox</h3>
          <p className="text-sm text-[#7E88A8]">
            Reliable event-driven notifications, emails, and background worker jobs without lost transactions.
          </p>
        </div>
      </div>
    </div>
  );
};
