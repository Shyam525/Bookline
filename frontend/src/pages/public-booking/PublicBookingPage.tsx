import React from 'react';
import { useParams } from 'react-router-dom';

export const PublicBookingPage: React.FC = () => {
  const { organizationSlug } = useParams<{ organizationSlug: string }>();

  return (
    <div className="max-w-4xl mx-auto px-6 py-12 space-y-8">
      {/* Header */}
      <div className="bg-[#111520] border border-[#212638] rounded-2xl p-8 flex items-center justify-between">
        <div className="space-y-1">
          <div className="inline-flex items-center gap-2 px-2.5 py-0.5 rounded bg-[#181D2C] border border-[#212638] text-xs font-mono text-[#34D399]">
            Online Booking Portal
          </div>
          <h1 className="font-heading text-3xl font-bold text-white uppercase tracking-tight">
            {organizationSlug || 'acme-salon'}
          </h1>
          <p className="text-sm text-[#7E88A8]">Select your desired service and preferred staff member.</p>
        </div>
      </div>

      {/* Booking Flow Steps */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="bg-[#111520] border border-[#E8546A] rounded-2xl p-6 space-y-3">
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-lg bg-[#E8546A] text-white flex items-center justify-center font-bold text-sm">
              1
            </div>
            <h3 className="font-heading font-semibold text-white">Select Service</h3>
          </div>
          <p className="text-xs text-[#7E88A8]">Haircut & Style (45m • $50.00)</p>
        </div>

        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-3">
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-lg bg-[#181D2C] text-[#7E88A8] flex items-center justify-center font-bold text-sm">
              2
            </div>
            <h3 className="font-heading font-semibold text-[#7E88A8]">Select Staff</h3>
          </div>
          <p className="text-xs text-[#7E88A8]">Alex Johnson</p>
        </div>

        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-3">
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-lg bg-[#181D2C] text-[#7E88A8] flex items-center justify-center font-bold text-sm">
              3
            </div>
            <h3 className="font-heading font-semibold text-[#7E88A8]">Select Slot</h3>
          </div>
          <p className="text-xs text-[#7E88A8]">Real-time Availability Engine</p>
        </div>
      </div>
    </div>
  );
};
