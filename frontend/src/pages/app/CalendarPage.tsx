import React, { useState } from 'react';
import { Button, IconButton } from '../../components/ui/Button';
import { Input, Select } from '../../components/forms/Inputs';
import { Card, Avatar } from '../../components/data-display/DataDisplay';
import { Modal, Drawer, ConfirmDialog } from '../../components/feedback/Feedback';
import { Calendar as CalendarIcon, ChevronLeft, ChevronRight, Plus, Clock, User, Scissors, CheckCircle, XCircle } from 'lucide-react';
import { CalendarBookingItem } from '../../services/api/bookings';

export const CalendarPage: React.FC = () => {
  const staffMembers = [
    { id: 'all', name: 'All Team Members' },
    { id: 'staff-1', name: 'Elena Vance' },
    { id: 'staff-2', name: 'Marcus Brody' },
    { id: 'staff-3', name: 'Sophia Chen' },
  ];

  const servicesList = [
    { id: 'srv-1', name: 'Signature Haircut & Style', duration: 45, price: 85.00 },
    { id: 'srv-2', name: 'Express Men\'s Cut', duration: 30, price: 45.00 },
    { id: 'srv-3', name: 'Full Balayage & Gloss Treatment', duration: 120, price: 220.00 },
  ];

  const customersList = [
    { id: 'cust-1', name: 'Samantha Reed', email: 'samantha.reed@example.com' },
    { id: 'cust-2', name: 'Alexander Wright', email: 'alex.wright@example.com' },
    { id: 'cust-3', name: 'Olivia Taylor', email: 'olivia.taylor@example.com' },
  ];

  const [viewMode, setViewMode] = useState<'day' | 'week' | 'month'>('day');
  const [selectedStaffId, setSelectedStaffId] = useState('all');
  const [currentDate, setCurrentDate] = useState(new Date());

  // Calendar Appointments State
  const [bookings, setBookings] = useState<CalendarBookingItem[]>([
    {
      id: 'bk-1',
      staffId: 'staff-1',
      staffName: 'Elena Vance',
      serviceId: 'srv-1',
      serviceName: 'Signature Haircut & Style',
      customerId: 'cust-1',
      customerName: 'Samantha Reed',
      startUtc: new Date(new Date().setHours(9, 0, 0, 0)).toISOString(),
      endUtc: new Date(new Date().setHours(9, 45, 0, 0)).toISOString(),
      status: 'Confirmed',
    },
    {
      id: 'bk-2',
      staffId: 'staff-2',
      staffName: 'Marcus Brody',
      serviceId: 'srv-2',
      serviceName: 'Express Men\'s Cut',
      customerId: 'cust-2',
      customerName: 'Alexander Wright',
      startUtc: new Date(new Date().setHours(11, 0, 0, 0)).toISOString(),
      endUtc: new Date(new Date().setHours(11, 30, 0, 0)).toISOString(),
      status: 'Pending',
    },
    {
      id: 'bk-3',
      staffId: 'staff-3',
      staffName: 'Sophia Chen',
      serviceId: 'srv-3',
      serviceName: 'Full Balayage & Gloss Treatment',
      customerId: 'cust-3',
      customerName: 'Olivia Taylor',
      startUtc: new Date(new Date().setHours(14, 0, 0, 0)).toISOString(),
      endUtc: new Date(new Date().setHours(16, 0, 0, 0)).toISOString(),
      status: 'Confirmed',
    },
  ]);

  // Modals & Drawers
  const [isBookingModalOpen, setIsBookingModalOpen] = useState(false);
  const [selectedBooking, setSelectedBooking] = useState<CalendarBookingItem | null>(null);
  const [cancellingBookingId, setCancellingBookingId] = useState<string | null>(null);

  // Form State
  const [bookingForm, setBookingForm] = useState({
    staffId: 'staff-1',
    serviceId: 'srv-1',
    customerId: 'cust-1',
    time: '10:00',
  });

  const filteredBookings = bookings.filter(
    (b) => selectedStaffId === 'all' || b.staffId === selectedStaffId
  );

  const handlePrevDate = () => {
    const newDate = new Date(currentDate);
    newDate.setDate(newDate.getDate() - 1);
    setCurrentDate(newDate);
  };

  const handleNextDate = () => {
    const newDate = new Date(currentDate);
    newDate.setDate(newDate.getDate() + 1);
    setCurrentDate(newDate);
  };

  const handleCreateBooking = (e: React.FormEvent) => {
    e.preventDefault();
    const staff = staffMembers.find((s) => s.id === bookingForm.staffId);
    const service = servicesList.find((s) => s.id === bookingForm.serviceId);
    const customer = customersList.find((c) => c.id === bookingForm.customerId);

    const [hours, mins] = bookingForm.time.split(':').map(Number);
    const start = new Date(currentDate);
    start.setHours(hours, mins, 0, 0);

    const duration = service?.duration || 45;
    const end = new Date(start.getTime() + duration * 60000);

    const newBooking: CalendarBookingItem = {
      id: `bk-${Date.now()}`,
      staffId: bookingForm.staffId,
      staffName: staff?.name || 'Staff Member',
      serviceId: bookingForm.serviceId,
      serviceName: service?.name || 'Service',
      customerId: bookingForm.customerId,
      customerName: customer?.name || 'Customer',
      startUtc: start.toISOString(),
      endUtc: end.toISOString(),
      status: 'Confirmed',
    };

    setBookings([...bookings, newBooking]);
    setIsBookingModalOpen(false);
  };

  const handleConfirmStatus = (id: string) => {
    setBookings(
      bookings.map((b) => (b.id === id ? { ...b, status: 'Confirmed' } : b))
    );
    if (selectedBooking && selectedBooking.id === id) {
      setSelectedBooking({ ...selectedBooking, status: 'Confirmed' });
    }
  };

  const handleCompleteStatus = (id: string) => {
    setBookings(
      bookings.map((b) => (b.id === id ? { ...b, status: 'Completed' } : b))
    );
    if (selectedBooking && selectedBooking.id === id) {
      setSelectedBooking({ ...selectedBooking, status: 'Completed' });
    }
  };

  const handleConfirmCancel = () => {
    if (cancellingBookingId) {
      setBookings(
        bookings.map((b) =>
          b.id === cancellingBookingId ? { ...b, status: 'Cancelled' } : b
        )
      );
      if (selectedBooking && selectedBooking.id === cancellingBookingId) {
        setSelectedBooking({ ...selectedBooking, status: 'Cancelled' });
      }
      setCancellingBookingId(null);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Confirmed':
        return 'bg-[#34D399]/10 border-[#34D399] text-[#34D399]';
      case 'Pending':
        return 'bg-[#FBBF24]/10 border-[#FBBF24] text-[#FBBF24]';
      case 'Completed':
        return 'bg-blue-500/10 border-blue-500 text-blue-400';
      case 'Cancelled':
      case 'NoShow':
        return 'bg-red-500/10 border-red-500 text-red-400';
      default:
        return 'bg-[#7E88A8]/10 border-[#7E88A8] text-[#7E88A8]';
    }
  };

  // Time Grid Hours (08:00 - 18:00)
  const timeSlots = Array.from({ length: 11 }, (_, i) => i + 8);

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="font-serif text-2xl sm:text-3xl text-[#ECEFFE] font-semibold tracking-tight">
            Calendar & Appointments
          </h1>
          <p className="text-sm text-[#7E88A8] mt-1">
            Realtime multi-staff appointment schedule workspace.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <Button variant="primary" onClick={() => setIsBookingModalOpen(true)}>
            <Plus className="w-4 h-4 mr-2" />
            New Appointment
          </Button>
        </div>
      </div>

      {/* Control Bar */}
      <Card className="p-4">
        <div className="flex flex-col md:flex-row items-center justify-between gap-4">
          {/* Date Navigator */}
          <div className="flex items-center gap-3">
            <IconButton
              icon={<ChevronLeft className="w-4 h-4" />}
              variant="outline"
              size="sm"
              title="Previous Date"
              onClick={handlePrevDate}
            />
            <div className="flex items-center gap-2 font-semibold text-[#ECEFFE] text-base min-w-[200px] justify-center">
              <CalendarIcon className="w-4 h-4 text-[#E8546A]" />
              {currentDate.toLocaleDateString('en-US', {
                weekday: 'short',
                month: 'short',
                day: 'numeric',
                year: 'numeric',
              })}
            </div>
            <IconButton
              icon={<ChevronRight className="w-4 h-4" />}
              variant="outline"
              size="sm"
              title="Next Date"
              onClick={handleNextDate}
            />
            <Button variant="ghost" size="sm" onClick={() => setCurrentDate(new Date())}>
              Today
            </Button>
          </div>

          {/* View Mode & Staff Filter */}
          <div className="flex items-center gap-3 w-full md:w-auto justify-between md:justify-end">
            <div className="w-56">
              <Select
                value={selectedStaffId}
                onChange={(e) => setSelectedStaffId(e.target.value)}
                options={staffMembers.map((s) => ({ value: s.id, label: s.name }))}
              />
            </div>

            <div className="flex items-center bg-[#181D2C] p-1 rounded-xl border border-[#212638]">
              {(['day', 'week', 'month'] as const).map((mode) => (
                <button
                  key={mode}
                  onClick={() => setViewMode(mode)}
                  className={`px-3 py-1.5 rounded-lg text-xs font-semibold capitalize transition-all ${
                    viewMode === mode
                      ? 'bg-[#E8546A] text-white shadow-md'
                      : 'text-[#7E88A8] hover:text-[#ECEFFE]'
                  }`}
                >
                  {mode}
                </button>
              ))}
            </div>
          </div>
        </div>
      </Card>

      {/* Calendar Day Workspace */}
      <Card className="p-6 overflow-x-auto">
        <div className="min-w-[700px]">
          {timeSlots.map((hour) => {
            const formattedHour = `${hour.toString().padStart(2, '0')}:00`;
            const matchingBookings = filteredBookings.filter((b) => {
              const startHour = new Date(b.startUtc).getHours();
              return startHour === hour;
            });

            return (
              <div
                key={hour}
                className="flex items-start border-b border-[#212638]/60 py-3 min-h-[72px] transition-colors hover:bg-[#181D2C]/30 group"
              >
                {/* Time Label */}
                <div className="w-20 text-xs font-semibold text-[#7E88A8] pt-1">
                  {hour > 12 ? `${hour - 12}:00 PM` : hour === 12 ? '12:00 PM' : `${hour}:00 AM`}
                </div>

                {/* Slots Grid Content */}
                <div className="flex-1 grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-3">
                  {matchingBookings.map((bk) => (
                    <div
                      key={bk.id}
                      onClick={() => setSelectedBooking(bk)}
                      className={`p-3 rounded-xl border cursor-pointer transition-all hover:scale-[1.01] shadow-lg ${getStatusColor(
                        bk.status
                      )}`}
                    >
                      <div className="flex items-center justify-between gap-2">
                        <span className="font-semibold text-xs text-[#ECEFFE] truncate">
                          {bk.customerName}
                        </span>
                        <span className="text-[10px] font-bold uppercase tracking-wider px-1.5 py-0.5 rounded bg-black/30">
                          {bk.status}
                        </span>
                      </div>

                      <div className="text-xs text-[#ECEFFE]/90 mt-1 font-medium truncate">
                        {bk.serviceName}
                      </div>

                      <div className="flex items-center justify-between text-[11px] text-[#7E88A8] mt-2 pt-1 border-t border-white/10">
                        <span className="flex items-center gap-1">
                          <User className="w-3 h-3 text-[#E8546A]" /> {bk.staffName}
                        </span>
                        <span className="flex items-center gap-1">
                          <Clock className="w-3 h-3" />
                          {new Date(bk.startUtc).toLocaleTimeString([], {
                            hour: '2-digit',
                            minute: '2-digit',
                          })}
                        </span>
                      </div>
                    </div>
                  ))}

                  {matchingBookings.length === 0 && (
                    <div
                      onClick={() => {
                        setBookingForm({
                          ...bookingForm,
                          time: formattedHour,
                        });
                        setIsBookingModalOpen(true);
                      }}
                      className="border border-dashed border-[#212638] rounded-xl p-3 flex items-center justify-center text-xs text-[#7E88A8]/60 cursor-pointer opacity-0 group-hover:opacity-100 hover:border-[#E8546A] hover:text-[#E8546A] transition-all"
                    >
                      + Book at {formattedHour}
                    </div>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      </Card>

      {/* Appointment Details Drawer */}
      <Drawer
        isOpen={!!selectedBooking}
        onClose={() => setSelectedBooking(null)}
        title="Appointment Details"
      >
        {selectedBooking && (
          <div className="space-y-6">
            <div className="flex items-center justify-between border-b border-[#212638] pb-4">
              <div>
                <span className={`px-2.5 py-1 rounded-full text-xs font-bold uppercase tracking-wider ${getStatusColor(selectedBooking.status)}`}>
                  {selectedBooking.status}
                </span>
              </div>
              <span className="text-xs text-[#7E88A8]">ID: {selectedBooking.id}</span>
            </div>

            {/* Customer Section */}
            <div className="bg-[#181D2C] p-4 rounded-xl border border-[#212638] space-y-2">
              <div className="text-xs font-semibold text-[#7E88A8] uppercase tracking-wider">
                Client Information
              </div>
              <div className="flex items-center gap-3">
                <Avatar name={selectedBooking.customerName} size="md" />
                <div>
                  <div className="text-base font-bold text-[#ECEFFE]">
                    {selectedBooking.customerName}
                  </div>
                  <div className="text-xs text-[#7E88A8]">Registered Client</div>
                </div>
              </div>
            </div>

            {/* Service & Staff Info */}
            <div className="space-y-3">
              <div className="flex items-center justify-between text-sm">
                <span className="text-[#7E88A8] flex items-center gap-2">
                  <Scissors className="w-4 h-4 text-[#E8546A]" /> Service
                </span>
                <span className="font-semibold text-[#ECEFFE]">
                  {selectedBooking.serviceName}
                </span>
              </div>

              <div className="flex items-center justify-between text-sm">
                <span className="text-[#7E88A8] flex items-center gap-2">
                  <User className="w-4 h-4 text-[#34D399]" /> Assigned Staff
                </span>
                <span className="font-semibold text-[#ECEFFE]">
                  {selectedBooking.staffName}
                </span>
              </div>

              <div className="flex items-center justify-between text-sm">
                <span className="text-[#7E88A8] flex items-center gap-2">
                  <Clock className="w-4 h-4 text-[#FBBF24]" /> Start Time
                </span>
                <span className="font-semibold text-[#ECEFFE]">
                  {new Date(selectedBooking.startUtc).toLocaleString()}
                </span>
              </div>
            </div>

            {/* Actions */}
            <div className="pt-4 border-t border-[#212638] space-y-2">
              {selectedBooking.status === 'Pending' && (
                <Button
                  variant="primary"
                  className="w-full"
                  onClick={() => handleConfirmStatus(selectedBooking.id)}
                >
                  <CheckCircle className="w-4 h-4 mr-2" /> Confirm Appointment
                </Button>
              )}

              {selectedBooking.status === 'Confirmed' && (
                <Button
                  variant="secondary"
                  className="w-full text-[#34D399]"
                  onClick={() => handleCompleteStatus(selectedBooking.id)}
                >
                  <CheckCircle className="w-4 h-4 mr-2" /> Mark as Completed
                </Button>
              )}

              {selectedBooking.status !== 'Cancelled' && (
                <Button
                  variant="danger"
                  className="w-full"
                  onClick={() => setCancellingBookingId(selectedBooking.id)}
                >
                  <XCircle className="w-4 h-4 mr-2" /> Cancel Appointment
                </Button>
              )}
            </div>
          </div>
        )}
      </Drawer>

      {/* New Booking Modal */}
      <Modal
        isOpen={isBookingModalOpen}
        onClose={() => setIsBookingModalOpen(false)}
        title="Schedule New Appointment"
      >
        <form onSubmit={handleCreateBooking} className="space-y-4">
          <Select
            label="Client"
            value={bookingForm.customerId}
            onChange={(e) => setBookingForm({ ...bookingForm, customerId: e.target.value })}
            options={customersList.map((c) => ({ value: c.id, label: `${c.name} (${c.email})` }))}
            required
          />

          <Select
            label="Service"
            value={bookingForm.serviceId}
            onChange={(e) => setBookingForm({ ...bookingForm, serviceId: e.target.value })}
            options={servicesList.map((s) => ({ value: s.id, label: `${s.name} (${s.duration} mins - $${s.price})` }))}
            required
          />

          <Select
            label="Assigned Staff Member"
            value={bookingForm.staffId}
            onChange={(e) => setBookingForm({ ...bookingForm, staffId: e.target.value })}
            options={staffMembers.filter(s => s.id !== 'all').map((s) => ({ value: s.id, label: s.name }))}
            required
          />

          <Input
            label="Start Time"
            type="time"
            value={bookingForm.time}
            onChange={(e) => setBookingForm({ ...bookingForm, time: e.target.value })}
            required
          />

          <div className="flex justify-end gap-3 pt-4 border-t border-[#212638]">
            <Button variant="ghost" type="button" onClick={() => setIsBookingModalOpen(false)}>
              Cancel
            </Button>
            <Button variant="primary" type="submit">
              Confirm & Book
            </Button>
          </div>
        </form>
      </Modal>

      {/* Cancel Confirm Dialog */}
      <ConfirmDialog
        isOpen={!!cancellingBookingId}
        onClose={() => setCancellingBookingId(null)}
        onConfirm={handleConfirmCancel}
        title="Cancel Appointment"
        message="Are you sure you want to cancel this appointment? The reserved time slot will be reopened on the calendar."
        confirmText="Cancel Appointment"
        isDanger={true}
      />
    </div>
  );
};
