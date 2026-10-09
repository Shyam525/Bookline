import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { Button, IconButton } from '../../components/ui/Button';
import { Input, SearchBox } from '../../components/forms/Inputs';
import { Card, Avatar } from '../../components/data-display/DataDisplay';
import { Modal, ConfirmDialog } from '../../components/feedback/Feedback';
import {
  UserPlus,
  Edit2,
  Archive,
  Mail,
  Phone,
  Clock,
  Scissors,
  Check,
  ShieldCheck,
  MapPin,
  Calendar,
  Coffee,
  ExternalLink,
} from 'lucide-react';

export interface StaffItemExtended {
  id: string;
  tenantId: string;
  name: string;
  email: string;
  phone?: string;
  title?: string;
  bio?: string;
  avatarUrl?: string;
  timeZoneId: string;
  locationName: string;
  isActive: boolean;
  isArchived: boolean;
  assignedServiceIds: string[];
  upcomingAppointmentsCount: number;
  timeOffDaysCount: number;
}

/**
 * Specification Section 76: STAFF
 * Staff includes:
 * profile, services, schedule, breaks, time off, location, appointments
 */
export const StaffPage: React.FC = () => {
  const [staffList, setStaffList] = useState<StaffItemExtended[]>([
    {
      id: 'staff-1',
      tenantId: 't-1',
      name: 'Elena Vance',
      email: 'elena@bookline.local',
      phone: '+91 98250 11001',
      title: 'Senior Master Stylist',
      bio: 'Over 10 years of experience in high-end editorial styling, corrective color, and therapeutic scalp care.',
      avatarUrl: '',
      timeZoneId: 'Asia/Kolkata',
      locationName: 'Bodakdev Flagship, Ahmedabad',
      isActive: true,
      isArchived: false,
      assignedServiceIds: ['srv-1', 'srv-3'],
      upcomingAppointmentsCount: 8,
      timeOffDaysCount: 4,
    },
    {
      id: 'staff-2',
      tenantId: 't-1',
      name: 'Marcus Brody',
      email: 'marcus@bookline.local',
      phone: '+91 98250 11002',
      title: 'Master Barber & Grooming Specialist',
      bio: 'Precision fade specialist, traditional hot towel straight razor shaves, and beard architecture.',
      avatarUrl: '',
      timeZoneId: 'Asia/Kolkata',
      locationName: 'Bandra West, Mumbai',
      isActive: true,
      isArchived: false,
      assignedServiceIds: ['srv-2'],
      upcomingAppointmentsCount: 5,
      timeOffDaysCount: 0,
    },
    {
      id: 'staff-3',
      tenantId: 't-1',
      name: 'Sophia Chen',
      email: 'sophia@bookline.local',
      phone: '+91 98250 11003',
      title: 'Lead Color & Balayage Director',
      bio: 'Expert in balayage techniques, corrective color formulas, and organic keratin restorative treatments.',
      avatarUrl: '',
      timeZoneId: 'Asia/Kolkata',
      locationName: 'Bodakdev Flagship, Ahmedabad',
      isActive: true,
      isArchived: false,
      assignedServiceIds: ['srv-1', 'srv-2', 'srv-3'],
      upcomingAppointmentsCount: 11,
      timeOffDaysCount: 2,
    },
  ]);

  const availableServices = [
    { id: 'srv-1', name: 'Signature Haircut & Style' },
    { id: 'srv-2', name: "Express Men's Cut" },
    { id: 'srv-3', name: 'Full Balayage & Gloss Treatment' },
  ];

  const availableLocations = [
    'Bodakdev Flagship, Ahmedabad',
    'Satellite Executive Suite, Ahmedabad',
    'Bandra West, Mumbai',
  ];

  const [searchQuery, setSearchQuery] = useState('');

  // Tabs & Team Management State (Section 95: Owner, Admin, Manager, Receptionist, Staff, Viewer)
  const [activeTab, setActiveTab] = useState<'roster' | 'team'>('roster');
  const [teamMembers, setTeamMembers] = useState([
    { id: 'tm-1', name: 'Aarav Singhania', email: 'provider@bookline.local', role: 'Owner', joinedAt: '2026-04-10' },
    { id: 'tm-2', name: 'Elena Vance', email: 'elena@bookline.local', role: 'Admin', joinedAt: '2026-05-12' },
    { id: 'tm-3', name: 'Marcus Brody', email: 'marcus@bookline.local', role: 'Manager', joinedAt: '2026-06-01' },
    { id: 'tm-4', name: 'Kavita Patel', email: 'reception@bookline.local', role: 'Receptionist', joinedAt: '2026-07-15' },
    { id: 'tm-5', name: 'Sophia Chen', email: 'sophia@bookline.local', role: 'Staff', joinedAt: '2026-08-01' },
    { id: 'tm-6', name: 'Rahul Joshi', email: 'auditor@bookline.local', role: 'Viewer', joinedAt: '2026-09-10' },
  ]);
  const [isInviteModalOpen, setIsInviteModalOpen] = useState(false);
  const [inviteForm, setInviteForm] = useState({ firstName: '', lastName: '', email: '', role: 'Staff' });

  // Modals
  const [isStaffModalOpen, setIsStaffModalOpen] = useState(false);
  const [editingStaff, setEditingStaff] = useState<StaffItemExtended | null>(null);
  const [isServiceAssignModalOpen, setIsServiceAssignModalOpen] = useState(false);
  const [assigningStaff, setAssigningStaff] = useState<StaffItemExtended | null>(null);
  const [selectedServiceIds, setSelectedServiceIds] = useState<string[]>([]);
  const [archivingStaffId, setArchivingStaffId] = useState<string | null>(null);

  // Form State
  const [staffForm, setStaffForm] = useState({
    name: '',
    email: '',
    phone: '',
    title: 'Staff Member',
    bio: '',
    locationName: 'Bodakdev Flagship, Ahmedabad',
    timeZoneId: 'Asia/Kolkata',
  });

  const filteredStaff = staffList.filter((s) => {
    const matchesSearch =
      s.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      s.email.toLowerCase().includes(searchQuery.toLowerCase()) ||
      (s.title && s.title.toLowerCase().includes(searchQuery.toLowerCase())) ||
      s.locationName.toLowerCase().includes(searchQuery.toLowerCase());
    return matchesSearch && !s.isArchived;
  });

  const handleOpenCreateStaff = () => {
    setEditingStaff(null);
    setStaffForm({
      name: '',
      email: '',
      phone: '',
      title: 'Specialist Stylist',
      bio: '',
      locationName: 'Bodakdev Flagship, Ahmedabad',
      timeZoneId: 'Asia/Kolkata',
    });
    setIsStaffModalOpen(true);
  };

  const handleOpenEditStaff = (staff: StaffItemExtended) => {
    setEditingStaff(staff);
    setStaffForm({
      name: staff.name,
      email: staff.email,
      phone: staff.phone || '',
      title: staff.title || 'Staff Member',
      bio: staff.bio || '',
      locationName: staff.locationName,
      timeZoneId: staff.timeZoneId || 'Asia/Kolkata',
    });
    setIsStaffModalOpen(true);
  };

  const handleSaveStaff = (e: React.FormEvent) => {
    e.preventDefault();
    if (editingStaff) {
      setStaffList(
        staffList.map((s) => (s.id === editingStaff.id ? { ...s, ...staffForm } : s))
      );
    } else {
      const newStaff: StaffItemExtended = {
        id: `staff-${Date.now()}`,
        tenantId: 't-1',
        ...staffForm,
        isActive: true,
        isArchived: false,
        assignedServiceIds: [],
        upcomingAppointmentsCount: 0,
        timeOffDaysCount: 0,
      };
      setStaffList([...staffList, newStaff]);
    }
    setIsStaffModalOpen(false);
  };

  const handleOpenAssignServices = (staff: StaffItemExtended) => {
    setAssigningStaff(staff);
    setSelectedServiceIds(staff.assignedServiceIds || []);
    setIsServiceAssignModalOpen(true);
  };

  const handleSaveServiceAssignments = () => {
    if (assigningStaff) {
      setStaffList(
        staffList.map((s) =>
          s.id === assigningStaff.id ? { ...s, assignedServiceIds: selectedServiceIds } : s
        )
      );
    }
    setIsServiceAssignModalOpen(false);
  };

  const handleToggleServiceSelection = (srvId: string) => {
    if (selectedServiceIds.includes(srvId)) {
      setSelectedServiceIds(selectedServiceIds.filter((id) => id !== srvId));
    } else {
      setSelectedServiceIds([...selectedServiceIds, srvId]);
    }
  };

  return (
    <div className="space-y-6 pb-12">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-[#212638] pb-6">
        <div>
          <span className="text-[10px] uppercase font-bold text-[#34D399] tracking-widest block mb-1">
            SECTION 76: STAFF MANAGEMENT MATRIX
          </span>
          <h1 className="font-heading text-2xl font-bold text-white">
            Staff, Specialists &amp; Operating Roster
          </h1>
          <p className="text-xs text-[#7E88A8]">
            Manage staff profiles, assigned services, schedules, breaks, time-off, branch locations, and calendar bookings
          </p>
        </div>

        <Button variant="primary" onClick={handleOpenCreateStaff}>
          <UserPlus className="w-4 h-4 mr-2" />
          Add Staff Member
        </Button>
      </div>

      {/* Tab Switcher: Section 76 Specialists vs Section 95 Provider Team */}
      <div className="flex border-b border-[#212638] gap-6">
        <button
          onClick={() => setActiveTab('roster')}
          className={`pb-3 text-sm font-semibold transition-colors border-b-2 flex items-center gap-2 ${
            activeTab === 'roster'
              ? 'border-[#E8546A] text-white'
              : 'border-transparent text-[#7E88A8] hover:text-white'
          }`}
        >
          <Scissors className="w-4 h-4 text-[#E8546A]" />
          Specialists Roster (Section 76)
          <span className="text-[10px] bg-[#181D2C] px-2 py-0.5 rounded-full border border-[#212638] text-[#ECEFFE]">
            {filteredStaff.length}
          </span>
        </button>
        <button
          onClick={() => setActiveTab('team')}
          className={`pb-3 text-sm font-semibold transition-colors border-b-2 flex items-center gap-2 ${
            activeTab === 'team'
              ? 'border-[#34D399] text-white'
              : 'border-transparent text-[#7E88A8] hover:text-white'
          }`}
        >
          <ShieldCheck className="w-4 h-4 text-[#34D399]" />
          Provider Team &amp; Access Control (Section 95)
          <span className="text-[10px] bg-[#34D399]/20 text-[#34D399] px-2 py-0.5 rounded-full border border-[#34D399]/30">
            {teamMembers.length}
          </span>
        </button>
      </div>

      {activeTab === 'roster' ? (
        <>
          {/* Search Bar */}
          <Card className="p-4">
            <div className="max-w-md">
              <SearchBox
                placeholder="Search by name, email, specialty, or branch location..."
                value={searchQuery}
                onChange={(val) => setSearchQuery(val)}
              />
            </div>
          </Card>

      {/* Staff Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {filteredStaff.map((staff) => (
          <Card
            key={staff.id}
            className="flex flex-col justify-between hover:border-[#E8546A]/40 transition-all p-6 space-y-4 group bg-[#111520] border-[#212638]"
          >
            <div className="space-y-3">
              {/* Profile Header */}
              <div className="flex items-start justify-between gap-3">
                <div className="flex items-center gap-3">
                  <Avatar name={staff.name} size="md" />
                  <div>
                    <h3 className="font-heading font-bold text-base text-white group-hover:text-[#E8546A] transition-colors">
                      {staff.name}
                    </h3>
                    <p className="text-xs font-semibold text-[#E8546A]">
                      {staff.title || 'Specialist'}
                    </p>
                  </div>
                </div>

                <span
                  className={`px-2 py-0.5 text-[10px] font-bold rounded-full ${
                    staff.isActive
                      ? 'bg-[#34D399]/15 text-[#34D399] border border-[#34D399]/30'
                      : 'bg-[#7E88A8]/15 text-[#7E88A8]'
                  }`}
                >
                  {staff.isActive ? 'Active' : 'Inactive'}
                </span>
              </div>

              {/* Location Badge (Section 76 Location) */}
              <div className="flex items-center gap-1.5 text-xs text-[#ECEFFE] bg-[#181D2C] px-3 py-1.5 rounded-xl border border-[#212638]">
                <MapPin className="w-3.5 h-3.5 text-[#E8546A]" />
                <span className="truncate">{staff.locationName}</span>
              </div>

              {/* Bio Snippet */}
              <p className="text-xs text-[#7E88A8] line-clamp-2 leading-relaxed">
                {staff.bio || 'Professional team member on the Bookline provider roster.'}
              </p>

              {/* Contact Information */}
              <div className="space-y-1.5 pt-3 border-t border-[#212638] text-xs text-[#7E88A8]">
                <div className="flex items-center gap-2">
                  <Mail className="w-3.5 h-3.5 text-[#7E88A8]" />
                  <span className="truncate">{staff.email}</span>
                </div>
                {staff.phone && (
                  <div className="flex items-center gap-2">
                    <Phone className="w-3.5 h-3.5 text-[#7E88A8]" />
                    <span>{staff.phone}</span>
                  </div>
                )}
                <div className="flex items-center gap-2">
                  <Clock className="w-3.5 h-3.5 text-[#34D399]" />
                  <span>Timezone: {staff.timeZoneId}</span>
                </div>
              </div>

              {/* Section 76 Metrics: Schedule, Breaks, Time Off, Appointments */}
              <div className="grid grid-cols-3 gap-2 pt-3 border-t border-[#212638] text-center">
                <Link
                  to="/provider/calendar"
                  className="p-2 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] transition-colors"
                  title="View Scheduled Appointments"
                >
                  <Calendar className="w-3.5 h-3.5 text-[#34D399] mx-auto mb-1" />
                  <span className="text-[10px] text-[#7E88A8] block">Bookings</span>
                  <span className="text-xs font-bold text-white">
                    {staff.upcomingAppointmentsCount}
                  </span>
                </Link>

                <Link
                  to="/provider/availability"
                  className="p-2 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] transition-colors"
                  title="View Shift Schedule & Breaks"
                >
                  <Coffee className="w-3.5 h-3.5 text-amber-400 mx-auto mb-1" />
                  <span className="text-[10px] text-[#7E88A8] block">Shifts</span>
                  <span className="text-xs font-bold text-white">Multi-Shift</span>
                </Link>

                <Link
                  to="/provider/availability"
                  className="p-2 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] transition-colors"
                  title="View Scheduled Time-Off"
                >
                  <ShieldCheck className="w-3.5 h-3.5 text-[#E8546A] mx-auto mb-1" />
                  <span className="text-[10px] text-[#7E88A8] block">Time-Off</span>
                  <span className="text-xs font-bold text-white">
                    {staff.timeOffDaysCount} days
                  </span>
                </Link>
              </div>

              {/* Assigned Services Count */}
              <div className="pt-2 flex items-center justify-between text-xs">
                <span className="text-[#7E88A8]">Assigned Services</span>
                <span className="font-semibold text-white bg-[#181D2C] px-2.5 py-0.5 rounded-full border border-[#212638]">
                  {staff.assignedServiceIds?.length || 0} services
                </span>
              </div>
            </div>

            {/* Actions */}
            <div className="flex items-center justify-between pt-3 border-t border-[#212638]">
              <Button variant="outline" size="sm" onClick={() => handleOpenAssignServices(staff)}>
                <Scissors className="w-3.5 h-3.5 mr-1.5 text-[#E8546A]" />
                Assign Services
              </Button>

              <div className="flex items-center gap-1">
                <IconButton
                  icon={<Edit2 className="w-4 h-4" />}
                  variant="ghost"
                  size="sm"
                  title="Edit Staff Member"
                  onClick={() => handleOpenEditStaff(staff)}
                />
                <IconButton
                  icon={<Archive className="w-4 h-4 text-[#E8546A]" />}
                  variant="ghost"
                  size="sm"
                  title="Archive Staff Member"
                  onClick={() =>
                    setStaffList(staffList.map((s) => (s.id === staff.id ? { ...s, isArchived: true } : s)))
                  }
                />
              </div>
            </div>
          </Card>
        ))}
      </div>
    </>
  ) : (
    /* Section 95: Provider Team Management Tab */
    <div className="space-y-6">
      <Card className="p-6 bg-[#111520] border-[#212638]">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 mb-1">
              <ShieldCheck className="w-5 h-5 text-[#34D399]" />
              <h2 className="text-lg font-bold text-white font-heading">
                Provider Team &amp; Access Control
              </h2>
            </div>
            <p className="text-xs text-[#7E88A8]">
              Enforce role-based access control across your organization. Roles: <strong className="text-white">Owner</strong>, <strong className="text-white">Admin</strong>, <strong className="text-white">Manager</strong>, <strong className="text-white">Receptionist</strong>, <strong className="text-white">Staff</strong>, and <strong className="text-white">Viewer</strong>. All permissions are strictly server-enforced.
            </p>
          </div>
          <Button variant="primary" onClick={() => setIsInviteModalOpen(true)}>
            <UserPlus className="w-4 h-4 mr-2" />
            Invite Member
          </Button>
        </div>
      </Card>

      {/* Team Members Table */}
      <Card className="overflow-hidden border-[#212638] bg-[#111520]">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-[#181D2C] border-b border-[#212638] text-[#7E88A8] uppercase tracking-wider font-semibold">
              <tr>
                <th className="py-3 px-4">Member</th>
                <th className="py-3 px-4">Email</th>
                <th className="py-3 px-4">Assigned Role</th>
                <th className="py-3 px-4">Joined</th>
                <th className="py-3 px-4 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#212638] text-white">
              {teamMembers.map((member) => {
                const getRoleBadge = (role: string) => {
                  switch (role) {
                    case 'Owner':
                      return 'bg-purple-500/15 text-purple-400 border border-purple-500/30';
                    case 'Admin':
                      return 'bg-[#E8546A]/15 text-[#E8546A] border border-[#E8546A]/30';
                    case 'Manager':
                      return 'bg-blue-500/15 text-blue-400 border border-blue-500/30';
                    case 'Receptionist':
                      return 'bg-amber-500/15 text-amber-400 border border-amber-500/30';
                    case 'Staff':
                      return 'bg-[#34D399]/15 text-[#34D399] border border-[#34D399]/30';
                    case 'Viewer':
                    default:
                      return 'bg-[#7E88A8]/15 text-[#7E88A8] border border-[#7E88A8]/30';
                  }
                };

                return (
                  <tr key={member.id} className="hover:bg-[#181D2C]/50 transition-colors">
                    <td className="py-3.5 px-4">
                      <div className="flex items-center gap-3">
                        <Avatar name={member.name} size="sm" />
                        <div>
                          <p className="font-semibold text-white">{member.name}</p>
                          <span className="text-[10px] text-[#7E88A8]">ID: {member.id}</span>
                        </div>
                      </div>
                    </td>
                    <td className="py-3.5 px-4 text-[#ECEFFE] font-mono">
                      {member.email}
                    </td>
                    <td className="py-3.5 px-4">
                      <div className="flex items-center gap-2">
                        <span className={`px-2 py-0.5 text-[10px] font-bold rounded-full ${getRoleBadge(member.role)}`}>
                          {member.role}
                        </span>
                        <select
                          value={member.role}
                          disabled={member.role === 'Owner'}
                          onChange={(e) => {
                            const newRole = e.target.value;
                            setTeamMembers(teamMembers.map((m) => m.id === member.id ? { ...m, role: newRole } : m));
                          }}
                          className="text-[11px] bg-[#181D2C] border border-[#212638] rounded-lg px-2 py-1 text-white disabled:opacity-50 disabled:cursor-not-allowed focus:outline-none focus:border-[#E8546A]"
                        >
                          <option value="Owner" disabled>Owner (Fixed)</option>
                          <option value="Admin">Admin</option>
                          <option value="Manager">Manager</option>
                          <option value="Receptionist">Receptionist</option>
                          <option value="Staff">Staff</option>
                          <option value="Viewer">Viewer</option>
                        </select>
                      </div>
                    </td>
                    <td className="py-3.5 px-4 text-[#7E88A8]">
                      {member.joinedAt}
                    </td>
                    <td className="py-3.5 px-4 text-right">
                      {member.role !== 'Owner' ? (
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => setTeamMembers(teamMembers.filter((m) => m.id !== member.id))}
                          className="text-[#E8546A] hover:bg-[#E8546A]/10 text-xs px-2 py-1"
                        >
                          Revoke Access
                        </Button>
                      ) : (
                        <span className="text-[10px] text-[#7E88A8] italic">Primary Owner</span>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  )}

      {/* Invite Team Member Modal (Section 95) */}
      <Modal
        isOpen={isInviteModalOpen}
        onClose={() => setIsInviteModalOpen(false)}
        title="Invite Provider Team Member"
      >
        <form
          onSubmit={(e) => {
            e.preventDefault();
            const fullName = `${inviteForm.firstName} ${inviteForm.lastName}`.trim() || inviteForm.email.split('@')[0];
            setTeamMembers([
              ...teamMembers,
              {
                id: `tm-${Date.now()}`,
                name: fullName,
                email: inviteForm.email,
                role: inviteForm.role,
                joinedAt: new Date().toISOString().split('T')[0],
              },
            ]);
            setIsInviteModalOpen(false);
            setInviteForm({ firstName: '', lastName: '', email: '', role: 'Staff' });
          }}
          className="space-y-4 text-xs"
        >
          <div className="grid grid-cols-2 gap-3">
            <Input
              label="First Name"
              placeholder="e.g. Priyanshu"
              value={inviteForm.firstName}
              onChange={(e) => setInviteForm({ ...inviteForm, firstName: e.target.value })}
              required
            />
            <Input
              label="Last Name"
              placeholder="e.g. Sharma"
              value={inviteForm.lastName}
              onChange={(e) => setInviteForm({ ...inviteForm, lastName: e.target.value })}
              required
            />
          </div>

          <Input
            label="Email Address"
            type="email"
            placeholder="colleague@domain.com"
            value={inviteForm.email}
            onChange={(e) => setInviteForm({ ...inviteForm, email: e.target.value })}
            required
          />

          <div>
            <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
              Select Role (Server-Enforced)
            </label>
            <select
              value={inviteForm.role}
              onChange={(e) => setInviteForm({ ...inviteForm, role: e.target.value })}
              className="w-full px-3 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
            >
              <option value="Admin">Admin - Full administrative access to operations and staff</option>
              <option value="Manager">Manager - Shift and appointment scheduling authority</option>
              <option value="Receptionist">Receptionist - Booking, appointments &amp; check-in only</option>
              <option value="Staff">Staff - Personal roster, appointments &amp; availability</option>
              <option value="Viewer">Viewer - Read-only access to schedules and dashboard</option>
            </select>
            <p className="text-[11px] text-[#7E88A8] mt-1.5">
              Permissions are strictly validated on the server for every endpoint.
            </p>
          </div>

          <div className="flex justify-end gap-3 pt-3 border-t border-[#212638]">
            <Button variant="ghost" onClick={() => setIsInviteModalOpen(false)}>
              Cancel
            </Button>
            <Button variant="primary" type="submit">
              Send Invitation
            </Button>
          </div>
        </form>
      </Modal>

      {/* Staff Modal */}
      <Modal
        isOpen={isStaffModalOpen}
        onClose={() => setIsStaffModalOpen(false)}
        title={editingStaff ? 'Edit Staff Specialist' : 'Add New Staff Specialist'}
      >
        <form onSubmit={handleSaveStaff} className="space-y-4 text-xs">
          <Input
            label="Full Name"
            placeholder="e.g. Elena Vance"
            value={staffForm.name}
            onChange={(e) => setStaffForm({ ...staffForm, name: e.target.value })}
            required
          />

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <Input
              label="Email Address"
              type="email"
              value={staffForm.email}
              onChange={(e) => setStaffForm({ ...staffForm, email: e.target.value })}
              required
            />
            <Input
              label="Phone Number"
              value={staffForm.phone}
              onChange={(e) => setStaffForm({ ...staffForm, phone: e.target.value })}
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <Input
              label="Professional Title"
              value={staffForm.title}
              onChange={(e) => setStaffForm({ ...staffForm, title: e.target.value })}
              required
            />
            <div>
              <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                Assigned Branch Location
              </label>
              <select
                value={staffForm.locationName}
                onChange={(e) => setStaffForm({ ...staffForm, locationName: e.target.value })}
                className="w-full px-3 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white"
              >
                {availableLocations.map((loc) => (
                  <option key={loc} value={loc}>
                    {loc}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div>
            <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
              Specialist Bio &amp; Credentials
            </label>
            <textarea
              rows={3}
              value={staffForm.bio}
              onChange={(e) => setStaffForm({ ...staffForm, bio: e.target.value })}
              className="w-full px-3 py-2 rounded-xl bg-[#181D2C] border border-[#212638] text-white"
            />
          </div>

          <div className="flex justify-end gap-3 pt-2 border-t border-[#212638]">
            <Button variant="ghost" onClick={() => setIsStaffModalOpen(false)}>
              Cancel
            </Button>
            <Button variant="primary" type="submit">
              Save Specialist
            </Button>
          </div>
        </form>
      </Modal>

      {/* Service Assignment Modal */}
      <Modal
        isOpen={isServiceAssignModalOpen}
        onClose={() => setIsServiceAssignModalOpen(false)}
        title={`Assign Services: ${assigningStaff?.name}`}
      >
        <div className="space-y-4 text-xs">
          <p className="text-[#7E88A8]">
            Select the services that this specialist is certified to perform:
          </p>
          <div className="space-y-2">
            {availableServices.map((srv) => {
              const isSelected = selectedServiceIds.includes(srv.id);
              return (
                <div
                  key={srv.id}
                  onClick={() => handleToggleServiceSelection(srv.id)}
                  className={`p-3 rounded-xl border flex items-center justify-between cursor-pointer transition-all ${
                    isSelected
                      ? 'bg-[#181D2C] border-[#34D399] text-white'
                      : 'bg-[#111520] border-[#212638] text-[#7E88A8]'
                  }`}
                >
                  <span className="font-semibold">{srv.name}</span>
                  {isSelected && <Check className="w-4 h-4 text-[#34D399]" />}
                </div>
              );
            })}
          </div>

          <div className="flex justify-end gap-3 pt-3 border-t border-[#212638]">
            <Button variant="ghost" onClick={() => setIsServiceAssignModalOpen(false)}>
              Cancel
            </Button>
            <Button variant="primary" onClick={handleSaveServiceAssignments}>
              Confirm Assignments
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
};
