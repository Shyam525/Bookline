import React, { useState } from 'react';
import { useAuth } from '../../app/providers/AuthProvider';
import {
  ShieldAlert,
  Search,
  Filter,
  Download,
  Calendar,
  CheckCircle,
  AlertTriangle,
  XCircle,
  Info,
  Clock,
  User,
  Activity,
  FileText,
  Lock,
  RefreshCw,
} from 'lucide-react';

interface AuditEvent {
  id: string;
  timestamp: string;
  actor: {
    name: string;
    email: string;
    role: string;
  };
  category: 'Booking' | 'Catalog' | 'Financial' | 'Security' | 'Staff' | 'Location';
  action: string;
  description: string;
  targetResource: string;
  ipAddress: string;
  status: 'SUCCESS' | 'WARNING' | 'FAILED';
  metadata?: Record<string, any>;
}

export const ProviderAuditPage: React.FC = () => {
  const { activeBusiness } = useAuth();
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>('All');
  const [selectedStatus, setSelectedStatus] = useState<string>('All');
  const [selectedEvent, setSelectedEvent] = useState<AuditEvent | null>(null);

  const [auditEvents, setAuditEvents] = useState<AuditEvent[]>([
    {
      id: 'aud-1092',
      timestamp: new Date(Date.now() - 1000 * 60 * 12).toISOString(),
      actor: { name: 'Aarav Singhania', email: 'provider@bookline.local', role: 'Owner' },
      category: 'Booking',
      action: 'BOOKING_CONFIRMED',
      description: 'Confirmed appointment BL-77401 for Jane Customer (Signature Haircut & Style)',
      targetResource: 'Booking: BL-77401',
      ipAddress: '103.212.144.18',
      status: 'SUCCESS',
      metadata: { serviceId: 'srv-1', staffId: 'st-1', depositAmount: 25.0, holdId: 'h-992' },
    },
    {
      id: 'aud-1091',
      timestamp: new Date(Date.now() - 1000 * 60 * 45).toISOString(),
      actor: { name: 'System Automations', email: 'scheduler@bookline.internal', role: 'System' },
      category: 'Booking',
      action: 'HOLD_EXPIRED',
      description: 'Auto-released unconfirmed slot hold h-990 after 5-minute timeout window',
      targetResource: 'Hold: h-990',
      ipAddress: '10.0.4.12',
      status: 'SUCCESS',
      metadata: { durationSeconds: 300, slotTime: '10:30 AM' },
    },
    {
      id: 'aud-1090',
      timestamp: new Date(Date.now() - 1000 * 60 * 120).toISOString(),
      actor: { name: 'Aarav Singhania', email: 'provider@bookline.local', role: 'Owner' },
      category: 'Catalog',
      action: 'PRICE_MODIFIED',
      description: 'Updated service "Full Balayage & Gloss Treatment" starting price to ₹2,800',
      targetResource: 'Service: srv-3',
      ipAddress: '103.212.144.18',
      status: 'SUCCESS',
      metadata: { oldPrice: 2600, newPrice: 2800, currency: 'INR' },
    },
    {
      id: 'aud-1089',
      timestamp: new Date(Date.now() - 1000 * 60 * 240).toISOString(),
      actor: { name: 'Priya Sharma', email: 'priya.s@aurawellness.com', role: 'Receptionist' },
      category: 'Security',
      action: 'AUTH_LOGIN_SUCCESS',
      description: 'Provider OS session established with multi-factor token verification',
      targetResource: 'Auth: SessionToken',
      ipAddress: '103.212.144.19',
      status: 'SUCCESS',
      metadata: { browser: 'Chrome 122.0 Windows 11', tenant: activeBusiness?.id || 'aura-1' },
    },
    {
      id: 'aud-1088',
      timestamp: new Date(Date.now() - 1000 * 60 * 360).toISOString(),
      actor: { name: 'Aarav Singhania', email: 'provider@bookline.local', role: 'Owner' },
      category: 'Financial',
      action: 'PAYOUT_REQUESTED',
      description: 'Submitted automated merchant payout batch for ₹42,500 via Razorpay Route',
      targetResource: 'PayoutBatch: pb-4081',
      ipAddress: '103.212.144.18',
      status: 'SUCCESS',
      metadata: { amount: 42500, netCommission: 4250, destinationBank: 'HDFC Bank ****4412' },
    },
    {
      id: 'aud-1087',
      timestamp: new Date(Date.now() - 1000 * 60 * 600).toISOString(),
      actor: { name: 'Unknown Client', email: 'client.unverified@temp.com', role: 'Anonymous' },
      category: 'Security',
      action: 'UNAUTHORIZED_CROSS_TENANT_ACCESS',
      description: 'Blocked access attempt to confidential customer records across tenant barrier',
      targetResource: 'TenantContext: AccessControl',
      ipAddress: '49.36.120.91',
      status: 'FAILED',
      metadata: { attemptedRoute: '/api/v1/customers/crm-export', error: 'ForbiddenCrossTenant' },
    },
    {
      id: 'aud-1086',
      timestamp: new Date(Date.now() - 1000 * 60 * 1440).toISOString(),
      actor: { name: 'Aarav Singhania', email: 'provider@bookline.local', role: 'Owner' },
      category: 'Staff',
      action: 'STAFF_ROLE_UPDATED',
      description: 'Assigned "Manager" permission group to specialist Elena Rostova',
      targetResource: 'Staff: st-elena',
      ipAddress: '103.212.144.18',
      status: 'SUCCESS',
      metadata: { previousRole: 'Staff', newRole: 'Manager' },
    },
    {
      id: 'aud-1085',
      timestamp: new Date(Date.now() - 1000 * 60 * 2880).toISOString(),
      actor: { name: 'Aarav Singhania', email: 'provider@bookline.local', role: 'Owner' },
      category: 'Location',
      action: 'LOCATION_HOURS_UPDATED',
      description: 'Updated operating schedule for Bodakdev Flagship branch for upcoming holiday',
      targetResource: 'Location: loc-flagship',
      ipAddress: '103.212.144.18',
      status: 'SUCCESS',
      metadata: { holidayDate: '2026-10-15', openTime: '11:00 AM', closeTime: '06:00 PM' },
    },
  ]);

  const categories = ['All', 'Booking', 'Catalog', 'Financial', 'Security', 'Staff', 'Location'];
  const statuses = ['All', 'SUCCESS', 'WARNING', 'FAILED'];

  const filteredEvents = auditEvents.filter((ev) => {
    const matchesCategory = selectedCategory === 'All' || ev.category === selectedCategory;
    const matchesStatus = selectedStatus === 'All' || ev.status === selectedStatus;
    const matchesSearch =
      ev.description.toLowerCase().includes(searchQuery.toLowerCase()) ||
      ev.action.toLowerCase().includes(searchQuery.toLowerCase()) ||
      ev.actor.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      ev.targetResource.toLowerCase().includes(searchQuery.toLowerCase()) ||
      ev.ipAddress.includes(searchQuery);
    return matchesCategory && matchesStatus && matchesSearch;
  });

  const handleExportCsv = () => {
    const headers = 'ID,Timestamp,Actor,Role,Category,Action,Target,IP,Status,Description\n';
    const rows = filteredEvents
      .map(
        (e) =>
          `"${e.id}","${e.timestamp}","${e.actor.name}","${e.actor.role}","${e.category}","${e.action}","${e.targetResource}","${e.ipAddress}","${e.status}","${e.description.replace(/"/g, '""')}"`
      )
      .join('\n');
    const blob = new Blob([headers + rows], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `bookline-audit-log-${new Date().toISOString().split('T')[0]}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <span className="px-2.5 py-0.5 rounded-lg bg-[#E8546A]/10 border border-[#E8546A]/20 text-[#E8546A] text-[10px] uppercase font-bold tracking-wider">
              Compliance &amp; Security
            </span>
            <span className="text-xs text-[#7E88A8]">&bull; Section 44 Audit Trail</span>
          </div>
          <h1 className="font-heading text-2xl font-bold text-white tracking-tight">
            Security &amp; Operational Audit Log
          </h1>
          <p className="text-xs text-[#7E88A8]">
            Tamper-evident log of all system changes, financial events, and authorization activities
          </p>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={() => {
              // Simulated log reload
              const copy = [...auditEvents];
              setAuditEvents(copy);
            }}
            className="p-2.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-[#ECEFFE] transition-colors"
            title="Refresh logs"
          >
            <RefreshCw className="w-4 h-4" />
          </button>
          <button
            onClick={handleExportCsv}
            className="px-4 py-2.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-white text-xs font-semibold transition-colors flex items-center gap-2"
          >
            <Download className="w-4 h-4 text-[#34D399]" />
            <span>Export CSV</span>
          </button>
        </div>
      </div>

      {/* Audit Stats Banner */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="p-4 rounded-2xl bg-[#111520] border border-[#212638]">
          <span className="text-[10px] font-bold uppercase tracking-wider text-[#7E88A8] block">Total Events Logged</span>
          <span className="font-heading text-2xl font-bold text-white mt-1 block">{auditEvents.length}</span>
          <span className="text-[10px] text-[#34D399]">100% verified immutable</span>
        </div>
        <div className="p-4 rounded-2xl bg-[#111520] border border-[#212638]">
          <span className="text-[10px] font-bold uppercase tracking-wider text-[#7E88A8] block">Security Incidents</span>
          <span className="font-heading text-2xl font-bold text-red-400 mt-1 block">
            {auditEvents.filter((e) => e.status === 'FAILED').length}
          </span>
          <span className="text-[10px] text-[#7E88A8]">0 cross-tenant leaks</span>
        </div>
        <div className="p-4 rounded-2xl bg-[#111520] border border-[#212638]">
          <span className="text-[10px] font-bold uppercase tracking-wider text-[#7E88A8] block">Active Operators</span>
          <span className="font-heading text-2xl font-bold text-white mt-1 block">3</span>
          <span className="text-[10px] text-[#7E88A8]">Owner, Reception, System</span>
        </div>
        <div className="p-4 rounded-2xl bg-[#111520] border border-[#212638]">
          <span className="text-[10px] font-bold uppercase tracking-wider text-[#7E88A8] block">Retention Policy</span>
          <span className="font-heading text-2xl font-bold text-[#FBBF24] mt-1 block">365 Days</span>
          <span className="text-[10px] text-[#7E88A8]">Encrypted archive active</span>
        </div>
      </div>

      {/* Filter and Search Bar */}
      <div className="p-4 rounded-2xl bg-[#111520] border border-[#212638] flex flex-col md:flex-row items-center justify-between gap-3">
        {/* Search */}
        <div className="relative flex-1 w-full">
          <Search className="w-4 h-4 text-[#7E88A8] absolute left-3.5 top-1/2 -translate-y-1/2" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Search by action, description, user, IP, or resource..."
            className="w-full bg-[#181D2C] border border-[#212638] rounded-xl pl-10 pr-4 py-2 text-xs text-white placeholder-[#7E88A8] focus:border-[#E8546A] outline-none"
          />
        </div>

        {/* Category Filter */}
        <div className="flex items-center gap-2 w-full md:w-auto">
          <select
            value={selectedCategory}
            onChange={(e) => setSelectedCategory(e.target.value)}
            className="bg-[#181D2C] border border-[#212638] rounded-xl px-3 py-2 text-xs text-white outline-none cursor-pointer"
          >
            {categories.map((c) => (
              <option key={c} value={c}>
                Category: {c}
              </option>
            ))}
          </select>

          <select
            value={selectedStatus}
            onChange={(e) => setSelectedStatus(e.target.value)}
            className="bg-[#181D2C] border border-[#212638] rounded-xl px-3 py-2 text-xs text-white outline-none cursor-pointer"
          >
            {statuses.map((s) => (
              <option key={s} value={s}>
                Status: {s}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Audit Log Table */}
      <div className="bg-[#111520] border border-[#212638] rounded-2xl overflow-hidden shadow-xl">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-[#181D2C] border-b border-[#212638] text-[10px] uppercase font-bold text-[#7E88A8] tracking-wider">
              <tr>
                <th className="px-6 py-3.5">Timestamp (UTC)</th>
                <th className="px-6 py-3.5">Actor</th>
                <th className="px-6 py-3.5">Category</th>
                <th className="px-6 py-3.5">Event Action</th>
                <th className="px-6 py-3.5">Target Resource</th>
                <th className="px-6 py-3.5">IP Address</th>
                <th className="px-6 py-3.5">Status</th>
                <th className="px-6 py-3.5 text-right">Details</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#212638]">
              {filteredEvents.length === 0 ? (
                <tr>
                  <td colSpan={8} className="px-6 py-12 text-center text-[#7E88A8]">
                    No audit events match your filter criteria.
                  </td>
                </tr>
              ) : (
                filteredEvents.map((ev) => (
                  <tr key={ev.id} className="hover:bg-[#181D2C]/40 transition-colors">
                    <td className="px-6 py-4 whitespace-nowrap font-mono text-[11px] text-[#C3CAD6]">
                      {new Date(ev.timestamp).toLocaleString()}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div>
                        <p className="font-bold text-white">{ev.actor.name}</p>
                        <p className="text-[10px] text-[#7E88A8]">{ev.actor.role}</p>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span className="px-2 py-0.5 rounded-md bg-[#181D2C] border border-[#212638] text-[10px] font-semibold text-[#ECEFFE]">
                        {ev.category}
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap font-mono text-[11px] font-bold text-white">
                      {ev.action}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-[#7E88A8]">
                      {ev.targetResource}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap font-mono text-[11px] text-[#7E88A8]">
                      {ev.ipAddress}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      {ev.status === 'SUCCESS' && (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md bg-[#34D399]/15 text-[#34D399] font-bold text-[10px]">
                          <CheckCircle className="w-3 h-3" /> SUCCESS
                        </span>
                      )}
                      {ev.status === 'WARNING' && (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md bg-[#FBBF24]/15 text-[#FBBF24] font-bold text-[10px]">
                          <AlertTriangle className="w-3 h-3" /> WARNING
                        </span>
                      )}
                      {ev.status === 'FAILED' && (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md bg-red-500/15 text-red-400 font-bold text-[10px]">
                          <XCircle className="w-3 h-3" /> FAILED
                        </span>
                      )}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right">
                      <button
                        onClick={() => setSelectedEvent(ev)}
                        className="px-2.5 py-1 rounded-lg bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-xs font-semibold text-[#ECEFFE] transition-colors"
                      >
                        Inspect
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* JSON Metadata Inspector Modal */}
      {selectedEvent && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm animate-fadeIn">
          <div className="bg-[#111520] border border-[#212638] rounded-3xl w-full max-w-xl p-6 shadow-2xl space-y-4">
            <div className="flex items-center justify-between border-b border-[#212638] pb-3">
              <div className="flex items-center gap-2">
                <ShieldAlert className="w-5 h-5 text-[#E8546A]" />
                <h3 className="font-heading font-bold text-base text-white">
                  Audit Event #{selectedEvent.id}
                </h3>
              </div>
              <button
                onClick={() => setSelectedEvent(null)}
                className="text-[#7E88A8] hover:text-white"
              >
                &times;
              </button>
            </div>

            <div className="space-y-3 text-xs">
              <div>
                <span className="text-[#7E88A8] block text-[10px] uppercase font-bold">Action Description</span>
                <p className="text-white font-medium mt-0.5">{selectedEvent.description}</p>
              </div>

              <div className="grid grid-cols-2 gap-3 pt-2 border-t border-[#212638]">
                <div>
                  <span className="text-[#7E88A8] block text-[10px] uppercase font-bold">Actor</span>
                  <p className="text-white">{selectedEvent.actor.name} ({selectedEvent.actor.email})</p>
                </div>
                <div>
                  <span className="text-[#7E88A8] block text-[10px] uppercase font-bold">Role</span>
                  <p className="text-white">{selectedEvent.actor.role}</p>
                </div>
              </div>

              <div>
                <span className="text-[#7E88A8] block text-[10px] uppercase font-bold mb-1">Payload Metadata</span>
                <pre className="p-3 rounded-xl bg-[#0A0C13] border border-[#212638] font-mono text-[11px] text-[#34D399] overflow-x-auto">
                  {JSON.stringify(selectedEvent.metadata || {}, null, 2)}
                </pre>
              </div>
            </div>

            <div className="pt-2 text-right">
              <button
                onClick={() => setSelectedEvent(null)}
                className="px-4 py-2 rounded-xl bg-[#181D2C] hover:bg-[#212638] text-white text-xs font-semibold"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
