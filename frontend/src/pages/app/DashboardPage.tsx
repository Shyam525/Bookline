import React from 'react';
import { Calendar, Users, DollarSign, Clock } from 'lucide-react';

export const DashboardPage: React.FC = () => {
  return (
    <div className="space-y-8">
      {/* Top Banner */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="font-heading text-3xl font-bold text-white">This Week</h2>
          <p className="text-sm text-[#7E88A8]">Overview & appointment operations for Bookline Demo Salon</p>
        </div>
        <div className="flex gap-3">
          <button className="px-4 py-2 bg-[#181D2C] border border-[#212638] text-sm text-white rounded-lg hover:bg-[#212638]">
            Today
          </button>
          <button className="px-4 py-2 bg-[#E8546A] text-sm text-white rounded-lg hover:bg-[#D44359]">
            + New Appointment
          </button>
        </div>
      </div>

      {/* KPI Section */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-2">
          <div className="flex items-center justify-between text-[#7E88A8]">
            <span className="text-xs font-semibold uppercase tracking-wider">Today's Bookings</span>
            <Calendar className="w-4 h-4 text-[#E8546A]" />
          </div>
          <p className="font-heading text-3xl font-bold text-white">12</p>
          <p className="text-xs text-[#34D399] flex items-center gap-1">+18% vs last week</p>
        </div>

        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-2">
          <div className="flex items-center justify-between text-[#7E88A8]">
            <span className="text-xs font-semibold uppercase tracking-wider">Revenue</span>
            <DollarSign className="w-4 h-4 text-[#34D399]" />
          </div>
          <p className="font-heading text-3xl font-bold text-white">$640.00</p>
          <p className="text-xs text-[#34D399] flex items-center gap-1">+12% vs last week</p>
        </div>

        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-2">
          <div className="flex items-center justify-between text-[#7E88A8]">
            <span className="text-xs font-semibold uppercase tracking-wider">No-Show Rate</span>
            <Clock className="w-4 h-4 text-[#FBBF24]" />
          </div>
          <p className="font-heading text-3xl font-bold text-white">0.0%</p>
          <p className="text-xs text-[#7E88A8]">Optimal bounds</p>
        </div>

        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-2">
          <div className="flex items-center justify-between text-[#7E88A8]">
            <span className="text-xs font-semibold uppercase tracking-wider">Utilisation</span>
            <Users className="w-4 h-4 text-[#64748B]" />
          </div>
          <p className="font-heading text-3xl font-bold text-white">85.4%</p>
          <p className="text-xs text-[#34D399] flex items-center gap-1">+5% staff efficiency</p>
        </div>
      </div>
    </div>
  );
};
