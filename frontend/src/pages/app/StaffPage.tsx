import React, { useState } from 'react';
import { Button, IconButton } from '../../components/ui/Button';
import { Input, SearchBox } from '../../components/forms/Inputs';
import { Card, Avatar } from '../../components/data-display/DataDisplay';
import { Modal, ConfirmDialog } from '../../components/feedback/Feedback';
import { UserPlus, Edit2, Archive, Mail, Phone, Clock, Scissors, Check, ShieldCheck } from 'lucide-react';
import { StaffItem } from '../../services/api/staff';

export const StaffPage: React.FC = () => {
  // Initial state for offline presentation & design system
  const [staffList, setStaffList] = useState<StaffItem[]>([
    {
      id: 'staff-1',
      tenantId: 't-1',
      name: 'Elena Vance',
      email: 'elena@bookline.com',
      phone: '+1 (555) 234-5678',
      title: 'Senior Master Stylist',
      bio: 'Over 10 years of experience in high-end editorial styling and coloring.',
      avatarUrl: '',
      timeZoneId: 'America/New_York',
      isActive: true,
      isArchived: false,
      assignedServiceIds: ['srv-1', 'srv-3'],
      workingHours: [],
      createdAtUtc: new Date().toISOString(),
    },
    {
      id: 'staff-2',
      tenantId: 't-1',
      name: 'Marcus Brody',
      email: 'marcus@bookline.com',
      phone: '+1 (555) 345-6789',
      title: 'Master Barber',
      bio: 'Precision fade specialist and traditional straight razor shave expert.',
      avatarUrl: '',
      timeZoneId: 'America/New_York',
      isActive: true,
      isArchived: false,
      assignedServiceIds: ['srv-2'],
      workingHours: [],
      createdAtUtc: new Date().toISOString(),
    },
    {
      id: 'staff-3',
      tenantId: 't-1',
      name: 'Sophia Chen',
      email: 'sophia@bookline.com',
      phone: '+1 (555) 456-7890',
      title: 'Color Specialist',
      bio: 'Expert in balayage techniques, corrective color, and organic hair treatments.',
      avatarUrl: '',
      timeZoneId: 'America/New_York',
      isActive: true,
      isArchived: false,
      assignedServiceIds: ['srv-1', 'srv-2', 'srv-3'],
      workingHours: [],
      createdAtUtc: new Date().toISOString(),
    },
  ]);

  const availableServices = [
    { id: 'srv-1', name: 'Signature Haircut & Style' },
    { id: 'srv-2', name: 'Express Men\'s Cut' },
    { id: 'srv-3', name: 'Full Balayage & Gloss Treatment' },
  ];

  const [searchQuery, setSearchQuery] = useState('');
  
  // Modals
  const [isStaffModalOpen, setIsStaffModalOpen] = useState(false);
  const [editingStaff, setEditingStaff] = useState<StaffItem | null>(null);
  const [isServiceAssignModalOpen, setIsServiceAssignModalOpen] = useState(false);
  const [assigningStaff, setAssigningStaff] = useState<StaffItem | null>(null);
  const [selectedServiceIds, setSelectedServiceIds] = useState<string[]>([]);
  const [archivingStaffId, setArchivingStaffId] = useState<string | null>(null);

  // Form State
  const [staffForm, setStaffForm] = useState({
    name: '',
    email: '',
    phone: '',
    title: 'Staff Member',
    bio: '',
    timeZoneId: 'America/New_York',
  });

  const filteredStaff = staffList.filter((s) => {
    const matchesSearch = s.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      s.email.toLowerCase().includes(searchQuery.toLowerCase()) ||
      (s.title && s.title.toLowerCase().includes(searchQuery.toLowerCase()));
    return matchesSearch && !s.isArchived;
  });

  const handleOpenCreateStaff = () => {
    setEditingStaff(null);
    setStaffForm({
      name: '',
      email: '',
      phone: '',
      title: 'Staff Member',
      bio: '',
      timeZoneId: 'America/New_York',
    });
    setIsStaffModalOpen(true);
  };

  const handleOpenEditStaff = (staff: StaffItem) => {
    setEditingStaff(staff);
    setStaffForm({
      name: staff.name,
      email: staff.email,
      phone: staff.phone || '',
      title: staff.title || 'Staff Member',
      bio: staff.bio || '',
      timeZoneId: staff.timeZoneId || 'America/New_York',
    });
    setIsStaffModalOpen(true);
  };

  const handleSaveStaff = (e: React.FormEvent) => {
    e.preventDefault();
    if (editingStaff) {
      setStaffList(staffList.map(s => s.id === editingStaff.id ? { ...s, ...staffForm } : s));
    } else {
      const newStaff: StaffItem = {
        id: `staff-${Date.now()}`,
        tenantId: 't-1',
        ...staffForm,
        isActive: true,
        isArchived: false,
        assignedServiceIds: [],
        workingHours: [],
        createdAtUtc: new Date().toISOString(),
      };
      setStaffList([...staffList, newStaff]);
    }
    setIsStaffModalOpen(false);
  };

  const handleOpenAssignServices = (staff: StaffItem) => {
    setAssigningStaff(staff);
    setSelectedServiceIds(staff.assignedServiceIds || []);
    setIsServiceAssignModalOpen(true);
  };

  const handleSaveServiceAssignments = () => {
    if (assigningStaff) {
      setStaffList(staffList.map(s => s.id === assigningStaff.id ? { ...s, assignedServiceIds: selectedServiceIds } : s));
    }
    setIsServiceAssignModalOpen(false);
  };

  const handleToggleServiceSelection = (srvId: string) => {
    if (selectedServiceIds.includes(srvId)) {
      setSelectedServiceIds(selectedServiceIds.filter(id => id !== srvId));
    } else {
      setSelectedServiceIds([...selectedServiceIds, srvId]);
    }
  };

  const handleConfirmArchive = () => {
    if (archivingStaffId) {
      setStaffList(staffList.map(s => s.id === archivingStaffId ? { ...s, isArchived: true } : s));
      setArchivingStaffId(null);
    }
  };

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="font-serif text-2xl sm:text-3xl text-[#ECEFFE] font-semibold tracking-tight">
            Team & Staff
          </h1>
          <p className="text-sm text-[#7E88A8] mt-1">
            Manage team members, roles, contact information, and service assignments.
          </p>
        </div>

        <Button variant="primary" onClick={handleOpenCreateStaff}>
          <UserPlus className="w-4 h-4 mr-2" />
          Add Staff Member
        </Button>
      </div>

      {/* Search Bar */}
      <Card className="p-4">
        <div className="max-w-md">
          <SearchBox
            placeholder="Search team members by name, email, or title..."
            value={searchQuery}
            onChange={(val) => setSearchQuery(val)}
          />
        </div>
      </Card>

      {/* Staff Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
        {filteredStaff.map((staff) => (
          <Card key={staff.id} className="flex flex-col justify-between hover:border-[#E8546A]/40 transition-all group">
            <div>
              {/* Header Profile Info */}
              <div className="flex items-start justify-between gap-3 mb-4">
                <div className="flex items-center gap-3">
                  <Avatar name={staff.name} size="md" />
                  <div>
                    <h3 className="text-base font-semibold text-[#ECEFFE] group-hover:text-[#E8546A] transition-colors">
                      {staff.name}
                    </h3>
                    <p className="text-xs font-medium text-[#E8546A]">
                      {staff.title || 'Staff Member'}
                    </p>
                  </div>
                </div>

                <span className={`px-2 py-0.5 text-[10px] font-semibold rounded-full ${staff.isActive ? 'bg-[#34D399]/10 text-[#34D399]' : 'bg-[#7E88A8]/10 text-[#7E88A8]'}`}>
                  {staff.isActive ? 'Active' : 'Inactive'}
                </span>
              </div>

              {/* Bio snippet */}
              <p className="text-xs text-[#7E88A8] line-clamp-2 mb-4">
                {staff.bio || 'No biography provided.'}
              </p>

              {/* Contact Information */}
              <div className="space-y-1.5 pt-3 border-t border-[#212638] text-xs text-[#7E88A8]">
                <div className="flex items-center gap-2">
                  <Mail className="w-3.5 h-3.5 text-[#ECEFFE]/60" />
                  <span className="truncate">{staff.email}</span>
                </div>
                {staff.phone && (
                  <div className="flex items-center gap-2">
                    <Phone className="w-3.5 h-3.5 text-[#ECEFFE]/60" />
                    <span>{staff.phone}</span>
                  </div>
                )}
                <div className="flex items-center gap-2">
                  <Clock className="w-3.5 h-3.5 text-[#ECEFFE]/60" />
                  <span>{staff.timeZoneId}</span>
                </div>
              </div>

              {/* Assigned Services Count */}
              <div className="mt-4 pt-3 border-t border-[#212638] flex items-center justify-between">
                <span className="text-xs text-[#7E88A8]">Services Provided</span>
                <span className="text-xs font-semibold text-[#ECEFFE] bg-[#181D2C] px-2.5 py-1 rounded-full border border-[#212638]">
                  {staff.assignedServiceIds?.length || 0} services
                </span>
              </div>
            </div>

            {/* Actions */}
            <div className="flex items-center justify-between pt-4 border-t border-[#212638] mt-4">
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
                  onClick={() => setArchivingStaffId(staff.id)}
                />
              </div>
            </div>
          </Card>
        ))}
      </div>

      {filteredStaff.length === 0 && (
        <Card className="p-12 text-center">
          <ShieldCheck className="w-10 h-10 text-[#7E88A8] mx-auto mb-3" />
          <h3 className="text-lg font-medium text-[#ECEFFE]">No team members found</h3>
          <p className="text-xs text-[#7E88A8] mt-1 max-w-sm mx-auto">
            Try searching for another name or title, or add your first team member.
          </p>
          <Button variant="primary" className="mt-4" onClick={handleOpenCreateStaff}>
            <UserPlus className="w-4 h-4 mr-2" />
            Add First Staff Member
          </Button>
        </Card>
      )}

      {/* Staff Modal */}
      <Modal
        isOpen={isStaffModalOpen}
        onClose={() => setIsStaffModalOpen(false)}
        title={editingStaff ? 'Edit Staff Member' : 'Add New Staff Member'}
      >
        <form onSubmit={handleSaveStaff} className="space-y-4">
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
              placeholder="elena@example.com"
              value={staffForm.email}
              onChange={(e) => setStaffForm({ ...staffForm, email: e.target.value })}
              required
            />
            <Input
              label="Phone Number"
              placeholder="+1 (555) 000-0000"
              value={staffForm.phone}
              onChange={(e) => setStaffForm({ ...staffForm, phone: e.target.value })}
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <Input
              label="Title / Role"
              placeholder="e.g. Master Stylist"
              value={staffForm.title}
              onChange={(e) => setStaffForm({ ...staffForm, title: e.target.value })}
            />
            <Input
              label="Timezone"
              placeholder="America/New_York"
              value={staffForm.timeZoneId}
              onChange={(e) => setStaffForm({ ...staffForm, timeZoneId: e.target.value })}
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-[#7E88A8] uppercase tracking-wider mb-1.5">
              Biography
            </label>
            <textarea
              className="w-full bg-[#181D2C] border border-[#212638] rounded-xl px-4 py-2.5 text-sm text-[#ECEFFE] focus:outline-none focus:border-[#E8546A] min-h-[80px]"
              placeholder="Short bio or background details..."
              value={staffForm.bio}
              onChange={(e) => setStaffForm({ ...staffForm, bio: e.target.value })}
            />
          </div>

          <div className="flex justify-end gap-3 pt-4 border-t border-[#212638]">
            <Button variant="ghost" type="button" onClick={() => setIsStaffModalOpen(false)}>
              Cancel
            </Button>
            <Button variant="primary" type="submit">
              {editingStaff ? 'Save Changes' : 'Add Staff Member'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Service Assignment Modal */}
      <Modal
        isOpen={isServiceAssignModalOpen}
        onClose={() => setIsServiceAssignModalOpen(false)}
        title={`Assign Services: ${assigningStaff?.name || ''}`}
      >
        <div className="space-y-4">
          <p className="text-xs text-[#7E88A8]">
            Select which services this staff member is qualified to perform for appointments:
          </p>

          <div className="space-y-2 max-h-60 overflow-y-auto pr-1">
            {availableServices.map((srv) => {
              const isSelected = selectedServiceIds.includes(srv.id);
              return (
                <div
                  key={srv.id}
                  onClick={() => handleToggleServiceSelection(srv.id)}
                  className={`flex items-center justify-between p-3 rounded-xl border cursor-pointer transition-all ${
                    isSelected
                      ? 'bg-[#E8546A]/10 border-[#E8546A] text-[#ECEFFE]'
                      : 'bg-[#181D2C] border-[#212638] text-[#7E88A8] hover:border-[#212638]/80'
                  }`}
                >
                  <span className="text-sm font-medium">{srv.name}</span>
                  {isSelected && <Check className="w-4 h-4 text-[#E8546A]" />}
                </div>
              );
            })}
          </div>

          <div className="flex justify-end gap-3 pt-4 border-t border-[#212638]">
            <Button variant="ghost" onClick={() => setIsServiceAssignModalOpen(false)}>
              Cancel
            </Button>
            <Button variant="primary" onClick={handleSaveServiceAssignments}>
              Save Service Assignments
            </Button>
          </div>
        </div>
      </Modal>

      {/* Archive Confirm Dialog */}
      <ConfirmDialog
        isOpen={!!archivingStaffId}
        onClose={() => setArchivingStaffId(null)}
        onConfirm={handleConfirmArchive}
        title="Archive Staff Member"
        message="Are you sure you want to archive this staff member? They will no longer be assignable to new appointments."
        confirmText="Archive Staff"
        isDanger={true}
      />
    </div>
  );
};
