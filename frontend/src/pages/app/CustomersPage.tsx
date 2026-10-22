import React, { useState, useEffect } from 'react';
import { useAuth } from '../../app/providers/AuthProvider';
import { Button, IconButton } from '../../components/ui/Button';
import { Input, SearchBox } from '../../components/forms/Inputs';
import { Card, Avatar } from '../../components/data-display/DataDisplay';
import { Modal, Drawer, ConfirmDialog, Skeleton, EmptyState, ErrorState } from '../../components/feedback/Feedback';
import { UserPlus, Edit2, Archive, Eye, Mail, Phone, Calendar, Users, FileText, RefreshCw } from 'lucide-react';
import { CustomerItem, customersApi } from '../../services/api/customers';
import { useDebounce } from '../../hooks/useDebounce';

export const CustomersPage: React.FC = () => {
  const { token } = useAuth();
  // Initial state for design system & offline presentation
  const [customers, setCustomers] = useState<CustomerItem[]>([
    {
      id: 'cust-1',
      tenantId: 't-1',
      firstName: 'Samantha',
      lastName: 'Reed',
      fullName: 'Samantha Reed',
      email: 'samantha.reed@example.com',
      phone: '+1 (555) 987-6543',
      notes: 'Prefers afternoon appointments. Sensitive scalp, uses sulfate-free shampoo.',
      avatarUrl: '',
      totalBookingsCount: 12,
      totalSpentAmount: 1140.00,
      isArchived: false,
      createdAtUtc: new Date(Date.now() - 86400000 * 90).toISOString(),
    },
    {
      id: 'cust-2',
      tenantId: 't-1',
      firstName: 'Alexander',
      lastName: 'Wright',
      fullName: 'Alexander Wright',
      email: 'alex.wright@example.com',
      phone: '+1 (555) 876-5432',
      notes: 'Regular every 3 weeks for express fade and beard trim.',
      avatarUrl: '',
      totalBookingsCount: 8,
      totalSpentAmount: 360.00,
      isArchived: false,
      createdAtUtc: new Date(Date.now() - 86400000 * 60).toISOString(),
    },
    {
      id: 'cust-3',
      tenantId: 't-1',
      firstName: 'Olivia',
      lastName: 'Taylor',
      fullName: 'Olivia Taylor',
      email: 'olivia.taylor@example.com',
      phone: '+1 (555) 765-4321',
      notes: 'Loves full balayage treatments. Books with Sophia Chen.',
      avatarUrl: '',
      totalBookingsCount: 5,
      totalSpentAmount: 1100.00,
      isArchived: false,
      createdAtUtc: new Date(Date.now() - 86400000 * 30).toISOString(),
    },
  ]);

  const [searchQuery, setSearchQuery] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const debouncedSearch = useDebounce(searchQuery, 300);

  const fetchCustomers = async () => {
    setIsLoading(true);
    setErrorMsg(null);
    try {
      if (token) {
        const res = await customersApi.getCustomers(token, debouncedSearch);
        const items = res?.items ?? (Array.isArray(res) ? res : []);
        if (items && items.length > 0) {
          setCustomers(items);
        }
      }
    } catch (err: any) {
      console.warn('Backend customers fetch failed, using fallback directory:', err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchCustomers();
  }, [token, debouncedSearch]);
  
  // Modals & Drawers
  const [isCustomerModalOpen, setIsCustomerModalOpen] = useState(false);
  const [editingCustomer, setEditingCustomer] = useState<CustomerItem | null>(null);
  const [viewingCustomer, setViewingCustomer] = useState<CustomerItem | null>(null);
  const [archivingCustomerId, setArchivingCustomerId] = useState<string | null>(null);

  // Form State
  const [customerForm, setCustomerForm] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    notes: '',
  });

  const filteredCustomers = customers.filter((c) => {
    const matchesSearch = c.fullName.toLowerCase().includes(debouncedSearch.toLowerCase()) ||
      c.email.toLowerCase().includes(debouncedSearch.toLowerCase()) ||
      c.phone.toLowerCase().includes(debouncedSearch.toLowerCase());
    return matchesSearch && !c.isArchived;
  });

  const handleOpenCreateCustomer = () => {
    setEditingCustomer(null);
    setCustomerForm({
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      notes: '',
    });
    setIsCustomerModalOpen(true);
  };

  const handleOpenEditCustomer = (customer: CustomerItem) => {
    setEditingCustomer(customer);
    setCustomerForm({
      firstName: customer.firstName,
      lastName: customer.lastName,
      email: customer.email,
      phone: customer.phone || '',
      notes: customer.notes || '',
    });
    setIsCustomerModalOpen(true);
  };

  const handleSaveCustomer = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editingCustomer) {
        if (token) {
          await customersApi.updateCustomer(token, editingCustomer.id, customerForm);
        }
        setCustomers(customers.map(c => c.id === editingCustomer.id ? {
          ...c,
          ...customerForm,
          fullName: `${customerForm.firstName} ${customerForm.lastName}`.trim(),
        } : c));
      } else {
        let created: CustomerItem | null = null;
        if (token) {
          try {
            created = await customersApi.createCustomer(token, customerForm);
          } catch (err) {
            console.warn('Backend createCustomer failed, using local state:', err);
          }
        }
        const newCustomer: CustomerItem = created || {
          id: `cust-${Date.now()}`,
          tenantId: 't-1',
          ...customerForm,
          fullName: `${customerForm.firstName} ${customerForm.lastName}`.trim(),
          totalBookingsCount: 0,
          totalSpentAmount: 0.00,
          isArchived: false,
          createdAtUtc: new Date().toISOString(),
        };
        setCustomers([...customers, newCustomer]);
      }
      setIsCustomerModalOpen(false);
    } catch (err: any) {
      setErrorMsg(err.message || 'Failed to save customer');
    }
  };

  const handleConfirmArchive = async () => {
    if (archivingCustomerId) {
      try {
        if (token) {
          await customersApi.archiveCustomer(token, archivingCustomerId);
        }
        setCustomers(customers.map(c => c.id === archivingCustomerId ? { ...c, isArchived: true } : c));
      } catch (err: any) {
        setErrorMsg(err.message || 'Failed to archive customer');
      } finally {
        setArchivingCustomerId(null);
      }
    }
  };

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="font-serif text-2xl sm:text-3xl text-[#ECEFFE] font-semibold tracking-tight">
            Customer Directory & CRM
          </h1>
          <p className="text-sm text-[#7E88A8] mt-1">
            Manage client profiles, booking history, lifetime spend, and internal preferences.
          </p>
        </div>

        <Button variant="primary" onClick={handleOpenCreateCustomer}>
          <UserPlus className="w-4 h-4 mr-2" />
          Add New Client
        </Button>
      </div>

      {/* Search Bar */}
      <Card className="p-4">
        <div className="max-w-md">
          <SearchBox
            placeholder="Search clients by name, email, or phone number..."
            value={searchQuery}
            onChange={(val) => setSearchQuery(val)}
          />
        </div>
      </Card>

      {/* Customer Content */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          <Skeleton className="h-64 rounded-2xl" />
          <Skeleton className="h-64 rounded-2xl" />
          <Skeleton className="h-64 rounded-2xl" />
        </div>
      ) : errorMsg ? (
        <ErrorState
          title="Unable to load customers"
          message={errorMsg}
          onRetry={fetchCustomers}
        />
      ) : filteredCustomers.length === 0 ? (
        <EmptyState
          title={debouncedSearch ? "No clients found." : "No clients yet."}
          description={
            debouncedSearch
              ? `No client profiles match "${debouncedSearch}". Try a different name, email, or phone.`
              : "Start building your client directory to track lifetime appointments and personal formulas."
          }
          action={
            <Button variant="primary" onClick={handleOpenCreateCustomer}>
              <UserPlus className="w-4 h-4 mr-2" />
              Add First Client
            </Button>
          }
        />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {filteredCustomers.map((cust) => (
            <Card key={cust.id} className="flex flex-col justify-between hover:border-[#E8546A]/40 transition-all group">
              <div>
                {/* Header Info */}
                <div className="flex items-start justify-between gap-3 mb-4">
                  <div className="flex items-center gap-3">
                    <Avatar name={cust.fullName} size="md" />
                    <div>
                      <h3 className="text-base font-semibold text-[#ECEFFE] group-hover:text-[#E8546A] transition-colors">
                        {cust.fullName}
                      </h3>
                      <div className="flex items-center gap-1.5 text-xs text-[#7E88A8] mt-0.5">
                        <Mail className="w-3 h-3 text-[#ECEFFE]/60" />
                        <span className="truncate">{cust.email}</span>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Phone & Date Info */}
                <div className="space-y-1.5 pt-3 border-t border-[#212638] text-xs text-[#7E88A8]">
                  {cust.phone && (
                    <div className="flex items-center gap-2">
                      <Phone className="w-3.5 h-3.5 text-[#ECEFFE]/60" />
                      <span>{cust.phone}</span>
                    </div>
                  )}
                  <div className="flex items-center gap-2">
                    <Calendar className="w-3.5 h-3.5 text-[#ECEFFE]/60" />
                    <span>Client since {new Date(cust.createdAtUtc).toLocaleDateString()}</span>
                  </div>
                </div>

                {/* Stats Bar */}
                <div className="grid grid-cols-2 gap-2 my-4 pt-3 border-t border-[#212638]">
                  <div className="bg-[#181D2C] p-2.5 rounded-xl border border-[#212638]">
                    <div className="text-[10px] uppercase font-semibold tracking-wider text-[#7E88A8]">
                      Total Bookings
                    </div>
                    <div className="text-sm font-bold text-[#ECEFFE] mt-0.5">
                      {cust.totalBookingsCount} appointments
                    </div>
                  </div>

                  <div className="bg-[#181D2C] p-2.5 rounded-xl border border-[#212638]">
                    <div className="text-[10px] uppercase font-semibold tracking-wider text-[#7E88A8]">
                      Lifetime Value
                    </div>
                    <div className="text-sm font-bold text-[#34D399] mt-0.5">
                      ${cust.totalSpentAmount.toFixed(2)}
                    </div>
                  </div>
                </div>

                {/* Notes Snippet */}
                {cust.notes && (
                  <div className="bg-[#181D2C]/60 p-2.5 rounded-xl border border-[#212638] text-xs text-[#7E88A8] line-clamp-2">
                    <span className="font-semibold text-[#ECEFFE] mr-1">Notes:</span>
                    {cust.notes}
                  </div>
                )}
              </div>

              {/* Actions */}
              <div className="flex items-center justify-between pt-4 border-t border-[#212638] mt-4">
                <Button variant="outline" size="sm" onClick={() => setViewingCustomer(cust)}>
                  <Eye className="w-3.5 h-3.5 mr-1.5 text-[#E8546A]" />
                  View Details
                </Button>

                <div className="flex items-center gap-1">
                  <IconButton
                    icon={<Edit2 className="w-4 h-4" />}
                    variant="ghost"
                    size="sm"
                    title="Edit Customer"
                    onClick={() => handleOpenEditCustomer(cust)}
                  />
                  <IconButton
                    icon={<Archive className="w-4 h-4 text-[#E8546A]" />}
                    variant="ghost"
                    size="sm"
                    title="Archive Customer"
                    onClick={() => setArchivingCustomerId(cust.id)}
                  />
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}

      {/* Customer Create/Edit Modal */}
      <Modal
        isOpen={isCustomerModalOpen}
        onClose={() => setIsCustomerModalOpen(false)}
        title={editingCustomer ? 'Edit Client Profile' : 'Add New Client'}
      >
        <form onSubmit={handleSaveCustomer} className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <Input
              label="First Name"
              placeholder="e.g. Samantha"
              value={customerForm.firstName}
              onChange={(e) => setCustomerForm({ ...customerForm, firstName: e.target.value })}
              required
            />
            <Input
              label="Last Name"
              placeholder="e.g. Reed"
              value={customerForm.lastName}
              onChange={(e) => setCustomerForm({ ...customerForm, lastName: e.target.value })}
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <Input
              label="Email Address"
              type="email"
              placeholder="samantha@example.com"
              value={customerForm.email}
              onChange={(e) => setCustomerForm({ ...customerForm, email: e.target.value })}
              required
            />
            <Input
              label="Phone Number"
              placeholder="+1 (555) 000-0000"
              value={customerForm.phone}
              onChange={(e) => setCustomerForm({ ...customerForm, phone: e.target.value })}
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-[#7E88A8] uppercase tracking-wider mb-1.5">
              Internal Client Notes & Preferences
            </label>
            <textarea
              className="w-full bg-[#181D2C] border border-[#212638] rounded-xl px-4 py-2.5 text-sm text-[#ECEFFE] focus:outline-none focus:border-[#E8546A] min-h-[80px]"
              placeholder="Internal staff notes, allergies, preferences..."
              value={customerForm.notes}
              onChange={(e) => setCustomerForm({ ...customerForm, notes: e.target.value })}
            />
          </div>

          <div className="flex justify-end gap-3 pt-4 border-t border-[#212638]">
            <Button variant="ghost" type="button" onClick={() => setIsCustomerModalOpen(false)}>
              Cancel
            </Button>
            <Button variant="primary" type="submit">
              {editingCustomer ? 'Save Changes' : 'Create Client'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Customer Details Drawer */}
      <Drawer
        isOpen={!!viewingCustomer}
        onClose={() => setViewingCustomer(null)}
        title={viewingCustomer?.fullName || 'Client Profile'}
      >
        {viewingCustomer && (
          <div className="space-y-6">
            <div className="flex items-center gap-4 border-b border-[#212638] pb-5">
              <Avatar name={viewingCustomer.fullName} size="lg" />
              <div>
                <h2 className="text-xl font-bold text-[#ECEFFE]">{viewingCustomer.fullName}</h2>
                <div className="text-xs text-[#7E88A8] mt-1 flex items-center gap-1.5">
                  <Mail className="w-3.5 h-3.5 text-[#E8546A]" /> {viewingCustomer.email}
                </div>
                {viewingCustomer.phone && (
                  <div className="text-xs text-[#7E88A8] mt-1 flex items-center gap-1.5">
                    <Phone className="w-3.5 h-3.5 text-[#34D399]" /> {viewingCustomer.phone}
                  </div>
                )}
              </div>
            </div>

            {/* Lifetime Stats */}
            <div className="grid grid-cols-2 gap-3">
              <div className="bg-[#181D2C] p-3.5 rounded-xl border border-[#212638]">
                <div className="text-xs font-medium text-[#7E88A8]">Total Bookings</div>
                <div className="text-xl font-bold text-[#ECEFFE] mt-1">
                  {viewingCustomer.totalBookingsCount}
                </div>
              </div>
              <div className="bg-[#181D2C] p-3.5 rounded-xl border border-[#212638]">
                <div className="text-xs font-medium text-[#7E88A8]">Total Value</div>
                <div className="text-xl font-bold text-[#34D399] mt-1">
                  ${viewingCustomer.totalSpentAmount.toFixed(2)}
                </div>
              </div>
            </div>

            {/* Client Notes */}
            <div>
              <div className="flex items-center gap-2 text-xs font-semibold text-[#7E88A8] uppercase tracking-wider mb-2">
                <FileText className="w-3.5 h-3.5 text-[#E8546A]" /> Client Preferences & Notes
              </div>
              <div className="bg-[#181D2C] p-4 rounded-xl border border-[#212638] text-sm text-[#ECEFFE]">
                {viewingCustomer.notes || 'No notes specified.'}
              </div>
            </div>

            <div className="pt-4 border-t border-[#212638] flex justify-end gap-3">
              <Button
                variant="secondary"
                size="sm"
                onClick={() => {
                  const cust = viewingCustomer;
                  setViewingCustomer(null);
                  handleOpenEditCustomer(cust);
                }}
              >
                <Edit2 className="w-3.5 h-3.5 mr-1.5" /> Edit Client
              </Button>
            </div>
          </div>
        )}
      </Drawer>

      {/* Archive Confirm Dialog */}
      <ConfirmDialog
        isOpen={!!archivingCustomerId}
        onClose={() => setArchivingCustomerId(null)}
        onConfirm={handleConfirmArchive}
        title="Archive Client Profile"
        message="Are you sure you want to archive this client profile? Their record will be hidden from active searches."
        confirmText="Archive Client"
        isDanger={true}
      />
    </div>
  );
};
