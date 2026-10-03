import React, { useState } from 'react';
import { Button, IconButton } from '../../components/ui/Button';
import { Input, Select, SearchBox } from '../../components/forms/Inputs';
import { Card, StatusBadge } from '../../components/data-display/DataDisplay';
import { Modal, Drawer, ConfirmDialog } from '../../components/feedback/Feedback';
import { MapPin, Plus, Edit2, Archive, Globe, Phone, DollarSign, Building } from 'lucide-react';
import { LocationItem } from '../../services/api/locations';

export const LocationsPage: React.FC = () => {
  const [locations, setLocations] = useState<LocationItem[]>([
    {
      id: '1',
      tenantId: 'tenant-1',
      name: 'Main Flagship Branch',
      address: '123 Main St, Suite 100',
      phone: '+1 (555) 019-2831',
      timezone: 'Asia/Kolkata',
      currency: 'USD',
      isActive: true,
      isArchived: false,
      createdAtUtc: new Date().toISOString(),
    },
    {
      id: '2',
      tenantId: 'tenant-1',
      name: 'Westside Studio',
      address: '456 West Ave, Bay Area',
      phone: '+1 (555) 482-9102',
      timezone: 'America/New_York',
      currency: 'USD',
      isActive: true,
      isArchived: false,
      createdAtUtc: new Date().toISOString(),
    },
  ]);

  const [searchQuery, setSearchQuery] = useState('');
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [editingLocation, setEditingLocation] = useState<LocationItem | null>(null);
  const [selectedLocation, setSelectedLocation] = useState<LocationItem | null>(null);
  const [archivingLocation, setArchivingLocation] = useState<LocationItem | null>(null);

  const [form, setForm] = useState({
    name: '',
    address: '',
    phone: '',
    timezone: 'Asia/Kolkata',
    currency: 'USD',
  });

  const handleCreate = (e: React.FormEvent) => {
    e.preventDefault();
    const newLoc: LocationItem = {
      id: Guid(),
      tenantId: 'tenant-1',
      name: form.name,
      address: form.address,
      phone: form.phone,
      timezone: form.timezone,
      currency: form.currency,
      isActive: true,
      isArchived: false,
      createdAtUtc: new Date().toISOString(),
    };
    setLocations([...locations, newLoc]);
    setIsCreateModalOpen(false);
    setForm({ name: '', address: '', phone: '', timezone: 'Asia/Kolkata', currency: 'USD' });
  };

  const handleUpdate = (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingLocation) return;
    setLocations(locations.map(l => l.id === editingLocation.id ? {
      ...l,
      name: form.name,
      address: form.address,
      phone: form.phone,
      timezone: form.timezone,
      currency: form.currency,
    } : l));
    setEditingLocation(null);
  };

  const handleArchive = () => {
    if (!archivingLocation) return;
    setLocations(locations.map(l => l.id === archivingLocation.id ? { ...l, isArchived: true, isActive: false } : l));
    setArchivingLocation(null);
  };

  const Guid = () => Math.random().toString(36).substring(2, 9);

  const filtered = locations.filter(l =>
    !l.isArchived &&
    (l.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
     l.address.toLowerCase().includes(searchQuery.toLowerCase()))
  );

  return (
    <div className="space-y-8 max-w-6xl mx-auto pb-20">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 border-b border-[#212638] pb-6">
        <div>
          <h1 className="font-heading text-3xl font-extrabold text-white">Locations</h1>
          <p className="text-sm text-[#7E88A8]">Manage operational branches, addresses, and regional timezones</p>
        </div>
        <Button variant="primary" leftIcon={<Plus className="w-4 h-4" />} onClick={() => {
          setForm({ name: '', address: '', phone: '', timezone: 'Asia/Kolkata', currency: 'USD' });
          setIsCreateModalOpen(true);
        }}>
          Add New Location
        </Button>
      </div>

      {/* Filter Bar */}
      <div className="flex items-center justify-between gap-4">
        <SearchBox value={searchQuery} onChange={setSearchQuery} placeholder="Search location name or address..." />
        <span className="text-xs font-mono text-[#7E88A8]">{filtered.length} Locations active</span>
      </div>

      {/* Locations Data Table */}
      <Card className="p-0 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="bg-[#181D2C]/60 text-[#7E88A8] text-xs font-semibold uppercase tracking-wider border-b border-[#212638]">
              <tr>
                <th className="p-4">Location Name</th>
                <th className="p-4">Address</th>
                <th className="p-4">Phone</th>
                <th className="p-4">Timezone</th>
                <th className="p-4">Currency</th>
                <th className="p-4">Status</th>
                <th className="p-4 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#212638]">
              {filtered.map((loc) => (
                <tr key={loc.id} className="hover:bg-[#181D2C]/30 transition-colors">
                  <td className="p-4 font-semibold text-white">
                    <button
                      onClick={() => setSelectedLocation(loc)}
                      className="hover:text-[#E8546A] transition-colors flex items-center gap-2"
                    >
                      <Building className="w-4 h-4 text-[#E8546A]" />
                      {loc.name}
                    </button>
                  </td>
                  <td className="p-4 text-[#7E88A8]">{loc.address}</td>
                  <td className="p-4 text-[#7E88A8] font-mono text-xs">{loc.phone}</td>
                  <td className="p-4 font-mono text-xs text-[#34D399]">{loc.timezone}</td>
                  <td className="p-4 font-mono text-xs text-[#ECEFFE]">{loc.currency}</td>
                  <td className="p-4">
                    <StatusBadge status={loc.isActive ? 'Confirmed' : 'Cancelled'} />
                  </td>
                  <td className="p-4 text-right space-x-2">
                    <IconButton
                      icon={<Edit2 className="w-3.5 h-3.5" />}
                      variant="ghost"
                      onClick={() => {
                        setEditingLocation(loc);
                        setForm({
                          name: loc.name,
                          address: loc.address,
                          phone: loc.phone,
                          timezone: loc.timezone,
                          currency: loc.currency,
                        });
                      }}
                    />
                    <IconButton
                      icon={<Archive className="w-3.5 h-3.5" />}
                      variant="ghost"
                      onClick={() => setArchivingLocation(loc)}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>

      {/* Create Location Modal */}
      <Modal isOpen={isCreateModalOpen} onClose={() => setIsCreateModalOpen(false)} title="Create New Location">
        <form onSubmit={handleCreate} className="space-y-4">
          <Input
            label="Location Name"
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
            placeholder="e.g. Downtown Branch"
            required
          />
          <Input
            label="Street Address"
            value={form.address}
            onChange={(e) => setForm({ ...form, address: e.target.value })}
            placeholder="e.g. 789 Broadway, 5th Floor"
            required
          />
          <Input
            label="Phone Number"
            value={form.phone}
            onChange={(e) => setForm({ ...form, phone: e.target.value })}
            placeholder="+1 (555) 000-0000"
            required
          />
          <Select
            label="Timezone"
            value={form.timezone}
            onChange={(e) => setForm({ ...form, timezone: e.target.value })}
            options={[
              { value: 'Asia/Kolkata', label: 'Asia/Kolkata (IST +05:30)' },
              { value: 'America/New_York', label: 'America/New_York (EST -05:00)' },
              { value: 'Europe/London', label: 'Europe/London (GMT +00:00)' },
            ]}
          />
          <Select
            label="Currency"
            value={form.currency}
            onChange={(e) => setForm({ ...form, currency: e.target.value })}
            options={[
              { value: 'USD', label: 'USD ($)' },
              { value: 'EUR', label: 'EUR (€)' },
              { value: 'GBP', label: 'GBP (£)' },
              { value: 'INR', label: 'INR (₹)' },
            ]}
          />
          <div className="flex justify-end gap-3 pt-4 border-t border-[#212638]">
            <Button type="button" variant="secondary" onClick={() => setIsCreateModalOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" variant="primary">
              Save Location
            </Button>
          </div>
        </form>
      </Modal>

      {/* Edit Location Modal */}
      <Modal isOpen={!!editingLocation} onClose={() => setEditingLocation(null)} title="Edit Location">
        <form onSubmit={handleUpdate} className="space-y-4">
          <Input
            label="Location Name"
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
            required
          />
          <Input
            label="Street Address"
            value={form.address}
            onChange={(e) => setForm({ ...form, address: e.target.value })}
            required
          />
          <Input
            label="Phone Number"
            value={form.phone}
            onChange={(e) => setForm({ ...form, phone: e.target.value })}
            required
          />
          <Select
            label="Timezone"
            value={form.timezone}
            onChange={(e) => setForm({ ...form, timezone: e.target.value })}
            options={[
              { value: 'Asia/Kolkata', label: 'Asia/Kolkata' },
              { value: 'America/New_York', label: 'America/New_York' },
              { value: 'Europe/London', label: 'Europe/London' },
            ]}
          />
          <Select
            label="Currency"
            value={form.currency}
            onChange={(e) => setForm({ ...form, currency: e.target.value })}
            options={[
              { value: 'USD', label: 'USD ($)' },
              { value: 'EUR', label: 'EUR (€)' },
              { value: 'INR', label: 'INR (₹)' },
            ]}
          />
          <div className="flex justify-end gap-3 pt-4 border-t border-[#212638]">
            <Button type="button" variant="secondary" onClick={() => setEditingLocation(null)}>
              Cancel
            </Button>
            <Button type="submit" variant="primary">
              Update Location
            </Button>
          </div>
        </form>
      </Modal>

      {/* Location Detail Drawer */}
      <Drawer isOpen={!!selectedLocation} onClose={() => setSelectedLocation(null)} title="Location Overview">
        {selectedLocation && (
          <div className="space-y-6">
            <div>
              <h3 className="font-heading text-xl font-bold text-white">{selectedLocation.name}</h3>
              <p className="text-xs text-[#7E88A8]">ID: {selectedLocation.id}</p>
            </div>
            <div className="space-y-3 bg-[#181D2C] p-4 rounded-xl text-xs text-[#7E88A8]">
              <p className="flex items-center gap-2"><MapPin className="w-4 h-4 text-[#E8546A]" /> {selectedLocation.address}</p>
              <p className="flex items-center gap-2"><Phone className="w-4 h-4 text-[#34D399]" /> {selectedLocation.phone}</p>
              <p className="flex items-center gap-2"><Globe className="w-4 h-4 text-[#FBBF24]" /> {selectedLocation.timezone}</p>
              <p className="flex items-center gap-2"><DollarSign className="w-4 h-4 text-[#ECEFFE]" /> {selectedLocation.currency}</p>
            </div>
          </div>
        )}
      </Drawer>

      {/* Confirm Archive Dialog */}
      <ConfirmDialog
        isOpen={!!archivingLocation}
        onClose={() => setArchivingLocation(null)}
        onConfirm={handleArchive}
        title="Archive Location?"
        message={`Are you sure you want to archive "${archivingLocation?.name}"? New appointments will no longer be allowed at this branch.`}
        confirmText="Archive Branch"
        isDanger
      />
    </div>
  );
};
