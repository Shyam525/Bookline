import React, { useState } from 'react';
import { useAuth } from '../../app/providers/AuthProvider';
import {
  Users,
  ShieldCheck,
  Plus,
  Mail,
  UserCheck,
  CheckCircle,
  X,
} from 'lucide-react';

interface TeamMember {
  id: string;
  name: string;
  email: string;
  role: 'Owner' | 'Admin' | 'Manager' | 'Specialist' | 'Receptionist';
  status: 'Active' | 'Invited';
}

export const ProviderTeamPage: React.FC = () => {
  const { activeBusiness } = useAuth();
  const [members, setMembers] = useState<TeamMember[]>([
    {
      id: 'tm-1',
      name: 'Dr. Kabir Varma',
      email: 'provider@bookline.local',
      role: 'Owner',
      status: 'Active',
    },
    {
      id: 'tm-2',
      name: 'Elena Vance',
      email: 'elena.v@example.com',
      role: 'Specialist',
      status: 'Active',
    },
    {
      id: 'tm-3',
      name: 'Marcus Brody',
      email: 'marcus.b@example.com',
      role: 'Specialist',
      status: 'Active',
    },
    {
      id: 'tm-4',
      name: 'Sophia Chen',
      email: 'sophia.c@example.com',
      role: 'Specialist',
      status: 'Active',
    },
    {
      id: 'tm-5',
      name: 'Ritu Shah',
      email: 'reception@example.com',
      role: 'Receptionist',
      status: 'Active',
    },
  ]);

  const [isInviteOpen, setIsInviteOpen] = useState(false);
  const [inviteName, setInviteName] = useState('');
  const [inviteEmail, setInviteEmail] = useState('');
  const [inviteRole, setInviteRole] = useState<TeamMember['role']>('Specialist');

  const handleInviteSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setMembers((prev) => [
      ...prev,
      {
        id: `tm-${Date.now()}`,
        name: inviteName,
        email: inviteEmail,
        role: inviteRole,
        status: 'Invited',
      },
    ]);
    setIsInviteOpen(false);
    setInviteName('');
    setInviteEmail('');
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="font-heading text-2xl font-bold text-white">Team &amp; Access Controls</h1>
          <p className="text-xs text-[#7E88A8]">
            Manage staff members, roles, and granular tenant permissions for{' '}
            <strong className="text-white">{activeBusiness?.name || 'Aura Wellness'}</strong>
          </p>
        </div>

        <button
          onClick={() => setIsInviteOpen(true)}
          className="px-4 py-2.5 rounded-xl bg-[#E8546A] hover:bg-[#D44359] text-white text-xs font-bold transition-all shadow-md flex items-center gap-2 self-start sm:self-auto"
        >
          <Plus className="w-4 h-4" />
          <span>Invite Team Member</span>
        </button>
      </div>

      {/* RBAC Reference Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-4 gap-4">
        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-4 space-y-1">
          <p className="font-bold text-white text-xs">Owner &bull; Admin</p>
          <p className="text-[11px] text-[#7E88A8]">Full billing, settings, catalog &amp; financial control</p>
        </div>
        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-4 space-y-1">
          <p className="font-bold text-white text-xs">Manager</p>
          <p className="text-[11px] text-[#7E88A8]">Schedules, staff rosters &amp; order fulfillment</p>
        </div>
        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-4 space-y-1">
          <p className="font-bold text-white text-xs">Specialist</p>
          <p className="text-[11px] text-[#7E88A8]">Individual calendar, services assigned &amp; appointments</p>
        </div>
        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-4 space-y-1">
          <p className="font-bold text-white text-xs">Receptionist</p>
          <p className="text-[11px] text-[#7E88A8]">Client check-in, walk-in bookings &amp; roster lookup</p>
        </div>
      </div>

      {/* Team Roster Table */}
      <div className="bg-[#111520] border border-[#212638] rounded-2xl overflow-hidden shadow-xl">
        <table className="w-full text-left text-xs text-[#ECEFFE]">
          <thead className="bg-[#181D2C] text-[#7E88A8] uppercase tracking-wider font-semibold border-b border-[#212638]">
            <tr>
              <th className="py-3 px-4">Member Name</th>
              <th className="py-3 px-4">Email</th>
              <th className="py-3 px-4">RBAC Role</th>
              <th className="py-3 px-4">Status</th>
              <th className="py-3 px-4 text-right">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-[#212638]">
            {members.map((m) => (
              <tr key={m.id} className="hover:bg-[#181D2C]/40 transition-colors">
                <td className="py-3.5 px-4">
                  <div className="flex items-center gap-3">
                    <div className="w-8 h-8 rounded-xl bg-[#181D2C] border border-[#212638] text-white flex items-center justify-center font-bold text-xs">
                      {m.name.substring(0, 1)}
                    </div>
                    <span className="font-semibold text-white">{m.name}</span>
                  </div>
                </td>
                <td className="py-3.5 px-4 font-mono text-[11px] text-[#7E88A8]">{m.email}</td>
                <td className="py-3.5 px-4">
                  <span className="px-2.5 py-0.5 rounded-lg bg-[#181D2C] border border-[#212638] text-white font-medium text-[11px]">
                    {m.role}
                  </span>
                </td>
                <td className="py-3.5 px-4">
                  <span
                    className={`inline-flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-bold ${
                      m.status === 'Active'
                        ? 'bg-[#34D399]/15 text-[#34D399]'
                        : 'bg-[#FBBF24]/15 text-[#FBBF24]'
                    }`}
                  >
                    {m.status}
                  </span>
                </td>
                <td className="py-3.5 px-4 text-right">
                  <span className="text-[11px] text-[#7E88A8]">Manage Access</span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Invite Modal */}
      {isInviteOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm animate-fadeIn">
          <div className="bg-[#111520] border border-[#212638] rounded-3xl w-full max-w-md p-6 space-y-6 shadow-2xl">
            <div className="flex items-center justify-between border-b border-[#212638] pb-3">
              <h3 className="font-heading font-bold text-lg text-white">Invite Team Member</h3>
              <button
                onClick={() => setIsInviteOpen(false)}
                className="p-1.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] text-[#7E88A8] hover:text-white"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            <form onSubmit={handleInviteSubmit} className="space-y-4 text-xs">
              <div>
                <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                  Full Name
                </label>
                <input
                  type="text"
                  required
                  value={inviteName}
                  onChange={(e) => setInviteName(e.target.value)}
                  placeholder="e.g. John Doe"
                  className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                />
              </div>

              <div>
                <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                  Email Address
                </label>
                <input
                  type="email"
                  required
                  value={inviteEmail}
                  onChange={(e) => setInviteEmail(e.target.value)}
                  placeholder="john@example.com"
                  className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                />
              </div>

              <div>
                <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                  Role &amp; Permissions
                </label>
                <select
                  value={inviteRole}
                  onChange={(e) => setInviteRole(e.target.value as any)}
                  className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                >
                  <option value="Admin">Admin</option>
                  <option value="Manager">Manager</option>
                  <option value="Specialist">Specialist</option>
                  <option value="Receptionist">Receptionist</option>
                </select>
              </div>

              <div className="pt-2 flex justify-end gap-3">
                <button
                  type="button"
                  onClick={() => setIsInviteOpen(false)}
                  className="px-4 py-2 text-xs text-[#7E88A8] hover:text-white"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-5 py-2.5 rounded-xl bg-[#E8546A] hover:bg-[#D44359] text-white font-bold text-xs shadow-lg"
                >
                  Send Invitation
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
