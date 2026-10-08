import React, { useState } from 'react';
import { useAuth } from '../../app/providers/AuthProvider';
import {
  Calendar,
  Clock,
  User,
  CheckCircle,
  XCircle,
  AlertCircle,
  Search,
  Filter,
  Scissors,
  MapPin,
} from 'lucide-react';

interface OperationalBooking {
  id: string;
  reference: string;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  serviceName: string;
  staffName: string;
  startTime: string;
  status: 'Pending' | 'Confirmed' | 'CheckedIn' | 'Completed' | 'Cancelled' | 'NoShow';
  totalPrice: number;
  depositPaid: number;
}

export const ProviderBookingsPage: React.FC = () => {
  const { activeBusiness } = useAuth();
  const [filter, setFilter] = useState('ALL');
  const [searchQuery, setSearchQuery] = useState('');

  const [bookings, setBookings] = useState<OperationalBooking[]>([
    {
      id: 'bk-101',
      reference: 'BL-849201',
      customerName: 'Aarav Patel',
      customerEmail: 'aarav.patel@example.com',
      customerPhone: '+91 98250 12345',
      serviceName: 'Holistic Aromatherapy Massage (60m)',
      staffName: 'Elena Vance',
      startTime: 'Today, 10:00 AM',
      status: 'Confirmed',
      totalPrice: 2400,
      depositPaid: 500,
    },
    {
      id: 'bk-102',
      reference: 'BL-938102',
      customerName: 'Ananya Desai',
      customerEmail: 'ananya.d@example.com',
      customerPhone: '+91 98765 99881',
      serviceName: 'Deep Hydration Facial (45m)',
      staffName: 'Sophia Chen',
      startTime: 'Today, 11:30 AM',
      status: 'CheckedIn',
      totalPrice: 1800,
      depositPaid: 0,
    },
    {
      id: 'bk-103',
      reference: 'BL-620194',
      customerName: 'Vikram Singhania',
      customerEmail: 'vikram.s@example.com',
      customerPhone: '+91 99000 44556',
      serviceName: 'Reflexology Foot Therapy (45m)',
      staffName: 'Elena Vance',
      startTime: 'Today, 02:00 PM',
      status: 'Pending',
      totalPrice: 1500,
      depositPaid: 500,
    },
    {
      id: 'bk-104',
      reference: 'BL-519283',
      customerName: 'Meera Iyer',
      customerEmail: 'meera.i@example.com',
      customerPhone: '+91 98123 77889',
      serviceName: 'Signature Scalp & Neck Ritual (30m)',
      staffName: 'Marcus Brody',
      startTime: 'Yesterday, 04:30 PM',
      status: 'Completed',
      totalPrice: 1200,
      depositPaid: 0,
    },
  ]);

  const handleUpdateStatus = (id: string, newStatus: OperationalBooking['status']) => {
    setBookings((prev) =>
      prev.map((b) => (b.id === id ? { ...b, status: newStatus } : b))
    );
  };

  const filtered = bookings.filter((b) => {
    if (filter !== 'ALL' && b.status !== filter) return false;
    if (
      searchQuery &&
      !b.reference.toLowerCase().includes(searchQuery.toLowerCase()) &&
      !b.customerName.toLowerCase().includes(searchQuery.toLowerCase())
    ) {
      return false;
    }
    return true;
  });

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="font-heading text-2xl font-bold text-white">Operations & Bookings Roster</h1>
        <p className="text-xs text-[#7E88A8]">
          Manage client arrivals, check-in flows, and appointment status for{' '}
          <strong className="text-white">{activeBusiness?.name || 'Aura Wellness'}</strong>
        </p>
      </div>

      {/* Filter Tabs & Search */}
      <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
        <div className="flex items-center gap-1.5 overflow-x-auto w-full sm:w-auto">
          {['ALL', 'Confirmed', 'CheckedIn', 'Pending', 'Completed', 'Cancelled', 'NoShow'].map((st) => (
            <button
              key={st}
              onClick={() => setFilter(st)}
              className={`px-3 py-1.5 rounded-xl text-xs font-semibold whitespace-nowrap transition-colors ${
                filter === st
                  ? 'bg-[#E8546A] text-white shadow-md'
                  : 'bg-[#111520] text-[#7E88A8] hover:text-white border border-[#212638]'
              }`}
            >
              {st === 'ALL' ? 'All Bookings' : st}
            </button>
          ))}
        </div>

        <div className="relative w-full sm:w-64">
          <Search className="w-4 h-4 text-[#7E88A8] absolute left-3 top-2.5" />
          <input
            type="text"
            placeholder="Search reference or client..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-9 pr-3 py-2 rounded-xl bg-[#111520] border border-[#212638] text-white text-xs focus:outline-none focus:border-[#E8546A]"
          />
        </div>
      </div>

      {/* Bookings Table */}
      <div className="bg-[#111520] border border-[#212638] rounded-2xl overflow-hidden shadow-xl">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs text-[#ECEFFE]">
            <thead className="bg-[#181D2C] text-[#7E88A8] uppercase tracking-wider font-semibold border-b border-[#212638]">
              <tr>
                <th className="py-3 px-4">Booking Ref</th>
                <th className="py-3 px-4">Client Details</th>
                <th className="py-3 px-4">Service & Specialist</th>
                <th className="py-3 px-4">Scheduled Slot</th>
                <th className="py-3 px-4">Fee / Deposit</th>
                <th className="py-3 px-4">State</th>
                <th className="py-3 px-4 text-right">Operational Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#212638]">
              {filtered.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-8 text-center text-[#7E88A8]">
                    No bookings found in this status view.
                  </td>
                </tr>
              ) : (
                filtered.map((b) => (
                  <tr key={b.id} className="hover:bg-[#181D2C]/40 transition-colors">
                    <td className="py-3.5 px-4 font-mono font-bold text-white">
                      {b.reference}
                    </td>

                    <td className="py-3.5 px-4">
                      <p className="font-semibold text-white">{b.customerName}</p>
                      <p className="text-[10px] text-[#7E88A8]">{b.customerEmail}</p>
                      <p className="text-[10px] text-[#34D399] font-mono">{b.customerPhone}</p>
                    </td>

                    <td className="py-3.5 px-4">
                      <p className="font-medium text-white">{b.serviceName}</p>
                      <p className="text-[10px] text-[#E8546A]">{b.staffName}</p>
                    </td>

                    <td className="py-3.5 px-4 font-medium text-white">
                      {b.startTime}
                    </td>

                    <td className="py-3.5 px-4">
                      <p className="font-heading font-bold text-white">₹{b.totalPrice}</p>
                      {b.depositPaid > 0 && (
                        <p className="text-[10px] text-[#34D399]">Paid Adv: ₹{b.depositPaid}</p>
                      )}
                    </td>

                    <td className="py-3.5 px-4">
                      <span
                        className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-lg text-[10px] font-bold ${
                          b.status === 'CheckedIn'
                            ? 'bg-blue-500/15 text-blue-400 border border-blue-500/30'
                            : b.status === 'Confirmed'
                            ? 'bg-[#34D399]/15 text-[#34D399] border border-[#34D399]/30'
                            : b.status === 'Completed'
                            ? 'bg-purple-500/15 text-purple-300 border border-purple-500/30'
                            : b.status === 'Cancelled'
                            ? 'bg-red-500/15 text-red-400 border border-red-500/30'
                            : 'bg-[#FBBF24]/15 text-[#FBBF24] border border-[#FBBF24]/30'
                        }`}
                      >
                        {b.status}
                      </span>
                    </td>

                    <td className="py-3.5 px-4 text-right">
                      <div className="flex items-center justify-end gap-1.5">
                        {b.status === 'Confirmed' && (
                          <button
                            onClick={() => handleUpdateStatus(b.id, 'CheckedIn')}
                            className="px-2.5 py-1 rounded-lg bg-blue-500 hover:bg-blue-600 text-white font-bold text-[11px]"
                          >
                            Check-In
                          </button>
                        )}
                        {b.status === 'CheckedIn' && (
                          <button
                            onClick={() => handleUpdateStatus(b.id, 'Completed')}
                            className="px-2.5 py-1 rounded-lg bg-[#34D399] hover:bg-[#2ebc87] text-black font-bold text-[11px]"
                          >
                            Complete
                          </button>
                        )}
                        {(b.status === 'Confirmed' || b.status === 'Pending') && (
                          <button
                            onClick={() => handleUpdateStatus(b.id, 'NoShow')}
                            className="px-2 py-1 rounded-lg bg-[#181D2C] hover:bg-red-500/20 text-[#7E88A8] hover:text-red-400 text-[11px]"
                            title="Mark No-Show"
                          >
                            No-Show
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
