import React, { useState } from 'react';
import { Button, IconButton } from '../../components/ui/Button';
import { Input, Select } from '../../components/forms/Inputs';
import { Card, Avatar } from '../../components/data-display/DataDisplay';
import { Modal, ConfirmDialog } from '../../components/feedback/Feedback';
import { Clock, Calendar, Plus, Trash2, Check, Sparkles, RefreshCw } from 'lucide-react';
import { TimeOffItem, TimeSlotItem } from '../../services/api/availability';

interface DaySchedule {
  dayOfWeek: number; // 1=Mon, 2=Tue... 0=Sun
  dayName: string;
  isEnabled: boolean;
  startTime: string;
  endTime: string;
}

export const AvailabilityPage: React.FC = () => {
  const staffMembers = [
    { id: 'staff-1', name: 'Elena Vance', title: 'Senior Master Stylist' },
    { id: 'staff-2', name: 'Marcus Brody', title: 'Master Barber' },
    { id: 'staff-3', name: 'Sophia Chen', title: 'Color Specialist' },
  ];

  const [selectedStaffId, setSelectedStaffId] = useState('staff-1');

  // Weekly Schedule State
  const [weeklySchedule, setWeeklySchedule] = useState<DaySchedule[]>([
    { dayOfWeek: 1, dayName: 'Monday', isEnabled: true, startTime: '09:00', endTime: '17:00' },
    { dayOfWeek: 2, dayName: 'Tuesday', isEnabled: true, startTime: '09:00', endTime: '17:00' },
    { dayOfWeek: 3, dayName: 'Wednesday', isEnabled: true, startTime: '09:00', endTime: '17:00' },
    { dayOfWeek: 4, dayName: 'Thursday', isEnabled: true, startTime: '09:00', endTime: '17:00' },
    { dayOfWeek: 5, dayName: 'Friday', isEnabled: true, startTime: '09:00', endTime: '18:00' },
    { dayOfWeek: 6, dayName: 'Saturday', isEnabled: true, startTime: '10:00', endTime: '16:00' },
    { dayOfWeek: 0, dayName: 'Sunday', isEnabled: false, startTime: '09:00', endTime: '17:00' },
  ]);

  // Time-Off State
  const [timeOffList, setTimeOffList] = useState<TimeOffItem[]>([
    {
      id: 'to-1',
      staffId: 'staff-1',
      startUtc: new Date(Date.now() + 86400000 * 3).toISOString(),
      endUtc: new Date(Date.now() + 86400000 * 7).toISOString(),
      reason: 'Annual Summer Vacation',
    },
  ]);

  const [isTimeOffModalOpen, setIsTimeOffModalOpen] = useState(false);
  const [deletingTimeOffId, setDeletingTimeOffId] = useState<string | null>(null);
  const [timeOffForm, setTimeOffForm] = useState({
    startIso: new Date().toISOString().slice(0, 10),
    endIso: new Date(Date.now() + 86400000 * 2).toISOString().slice(0, 10),
    reason: '',
  });

  // Slot Tester Preview State
  const [testDate, setTestDate] = useState(new Date().toISOString().slice(0, 10));
  const [testServiceDuration, setTestServiceDuration] = useState(45);
  const [testBufferMinutes, setTestBufferMinutes] = useState(15);
  const [computedPreviewSlots, setComputedPreviewSlots] = useState<TimeSlotItem[]>([
    { startIso: '09:00', endIso: '09:45', displayTime: '09:00 AM - 09:45 AM', isAvailable: true, staffName: 'Elena Vance' },
    { startIso: '10:00', endIso: '10:45', displayTime: '10:00 AM - 10:45 AM', isAvailable: true, staffName: 'Elena Vance' },
    { startIso: '11:00', endIso: '11:45', displayTime: '11:00 AM - 11:45 AM', isAvailable: true, staffName: 'Elena Vance' },
    { startIso: '13:00', endIso: '13:45', displayTime: '01:00 PM - 01:45 PM', isAvailable: true, staffName: 'Elena Vance' },
    { startIso: '14:00', endIso: '14:45', displayTime: '02:00 PM - 02:45 PM', isAvailable: true, staffName: 'Elena Vance' },
    { startIso: '15:00', endIso: '15:45', displayTime: '03:00 PM - 03:45 PM', isAvailable: true, staffName: 'Elena Vance' },
  ]);
  const [isScheduleSaved, setIsScheduleSaved] = useState(false);

  const selectedStaff = staffMembers.find((s) => s.id === selectedStaffId);

  const handleToggleDay = (dayOfWeek: number) => {
    setWeeklySchedule(
      weeklySchedule.map((d) => (d.dayOfWeek === dayOfWeek ? { ...d, isEnabled: !d.isEnabled } : d))
    );
  };

  const handleTimeChange = (dayOfWeek: number, field: 'startTime' | 'endTime', value: string) => {
    setWeeklySchedule(
      weeklySchedule.map((d) => (d.dayOfWeek === dayOfWeek ? { ...d, [field]: value } : d))
    );
  };

  const handleSaveSchedule = () => {
    setIsScheduleSaved(true);
    setTimeout(() => setIsScheduleSaved(false), 3000);
  };

  const handleCreateTimeOff = (e: React.FormEvent) => {
    e.preventDefault();
    const newEntry: TimeOffItem = {
      id: `to-${Date.now()}`,
      staffId: selectedStaffId,
      startUtc: new Date(timeOffForm.startIso).toISOString(),
      endUtc: new Date(timeOffForm.endIso).toISOString(),
      reason: timeOffForm.reason || 'General Time-Off',
    };
    setTimeOffList([...timeOffList, newEntry]);
    setIsTimeOffModalOpen(false);
  };

  const handleConfirmDeleteTimeOff = () => {
    if (deletingTimeOffId) {
      setTimeOffList(timeOffList.filter((to) => to.id !== deletingTimeOffId));
      setDeletingTimeOffId(null);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="font-serif text-2xl sm:text-3xl text-[#ECEFFE] font-semibold tracking-tight">
            Availability & Schedule Engine
          </h1>
          <p className="text-sm text-[#7E88A8] mt-1">
            Configure working windows, time-off exceptions, and compute realtime appointment slots.
          </p>
        </div>

        {/* Staff Switcher */}
        <div className="w-full sm:w-72">
          <Select
            label="Select Staff Member"
            value={selectedStaffId}
            onChange={(e) => setSelectedStaffId(e.target.value)}
            options={staffMembers.map((s) => ({ value: s.id, label: `${s.name} (${s.title})` }))}
          />
        </div>
      </div>

      {/* Main Grid Layout */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left 2 Cols: Weekly Working Hours */}
        <div className="lg:col-span-2 space-y-6">
          <Card className="p-6">
            <div className="flex items-center justify-between mb-6">
              <div className="flex items-center gap-3">
                <Avatar name={selectedStaff?.name || 'Staff'} size="md" />
                <div>
                  <h2 className="text-lg font-semibold text-[#ECEFFE]">
                    Weekly Working Hours — {selectedStaff?.name}
                  </h2>
                  <p className="text-xs text-[#7E88A8]">
                    Set recurring availability windows per day.
                  </p>
                </div>
              </div>

              <Button variant="primary" onClick={handleSaveSchedule}>
                {isScheduleSaved ? (
                  <span className="flex items-center gap-1 text-white">
                    <Check className="w-4 h-4" /> Saved!
                  </span>
                ) : (
                  'Save Schedule'
                )}
              </Button>
            </div>

            <div className="space-y-3">
              {weeklySchedule.map((day) => (
                <div
                  key={day.dayOfWeek}
                  className={`flex flex-col sm:flex-row sm:items-center justify-between p-3.5 rounded-xl border transition-all ${
                    day.isEnabled
                      ? 'bg-[#181D2C] border-[#212638]'
                      : 'bg-[#0A0C13] border-[#212638]/60 opacity-60'
                  }`}
                >
                  <div className="flex items-center gap-3 mb-2 sm:mb-0">
                    <input
                      type="checkbox"
                      id={`day-${day.dayOfWeek}`}
                      checked={day.isEnabled}
                      onChange={() => handleToggleDay(day.dayOfWeek)}
                      className="rounded bg-[#0A0C13] border-[#212638] text-[#E8546A] focus:ring-0 cursor-pointer"
                    />
                    <label
                      htmlFor={`day-${day.dayOfWeek}`}
                      className="text-sm font-semibold text-[#ECEFFE] cursor-pointer w-28"
                    >
                      {day.dayName}
                    </label>
                  </div>

                  {day.isEnabled ? (
                    <div className="flex items-center gap-3">
                      <div className="flex items-center gap-2">
                        <Clock className="w-3.5 h-3.5 text-[#7E88A8]" />
                        <Input
                          type="time"
                          value={day.startTime}
                          onChange={(e) => handleTimeChange(day.dayOfWeek, 'startTime', e.target.value)}
                          className="py-1 px-2.5 text-xs w-32"
                        />
                      </div>
                      <span className="text-xs text-[#7E88A8]">to</span>
                      <Input
                        type="time"
                        value={day.endTime}
                        onChange={(e) => handleTimeChange(day.dayOfWeek, 'endTime', e.target.value)}
                        className="py-1 px-2.5 text-xs w-32"
                      />
                    </div>
                  ) : (
                    <span className="text-xs text-[#7E88A8] italic font-medium sm:pr-4">
                      Unavailable / Off Day
                    </span>
                  )}
                </div>
              ))}
            </div>
          </Card>

          {/* Time Off Section */}
          <Card className="p-6">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h3 className="text-lg font-semibold text-[#ECEFFE]">Time-Off & Vacations</h3>
                <p className="text-xs text-[#7E88A8] mt-0.5">
                  Scheduled leave overrides weekly working hours automatically.
                </p>
              </div>

              <Button variant="secondary" size="sm" onClick={() => setIsTimeOffModalOpen(true)}>
                <Plus className="w-4 h-4 mr-1.5" />
                Add Time-Off
              </Button>
            </div>

            <div className="space-y-2.5">
              {timeOffList.map((to) => (
                <div
                  key={to.id}
                  className="flex items-center justify-between p-3 bg-[#181D2C] border border-[#212638] rounded-xl text-xs"
                >
                  <div className="flex items-center gap-3">
                    <Calendar className="w-4 h-4 text-[#E8546A]" />
                    <div>
                      <div className="font-semibold text-[#ECEFFE]">{to.reason || 'Time Off'}</div>
                      <div className="text-[#7E88A8] text-[11px] mt-0.5">
                        {new Date(to.startUtc).toLocaleDateString()} — {new Date(to.endUtc).toLocaleDateString()}
                      </div>
                    </div>
                  </div>

                  <IconButton
                    icon={<Trash2 className="w-4 h-4 text-[#E8546A]" />}
                    variant="ghost"
                    size="sm"
                    title="Remove Time-off"
                    onClick={() => setDeletingTimeOffId(to.id)}
                  />
                </div>
              ))}

              {timeOffList.length === 0 && (
                <p className="text-xs text-[#7E88A8] text-center py-6">
                  No time-off or vacation records added for this staff member.
                </p>
              )}
            </div>
          </Card>
        </div>

        {/* Right Col: Live Availability Tester Widget */}
        <div className="space-y-6">
          <Card className="p-6">
            <div className="flex items-center gap-2 mb-4 border-b border-[#212638] pb-3">
              <Sparkles className="w-5 h-5 text-[#E8546A]" />
              <h3 className="text-base font-semibold text-[#ECEFFE]">Live Slot Engine Tester</h3>
            </div>

            <p className="text-xs text-[#7E88A8] mb-4">
              Preview real-time slot generation computed by Bookline's ISlotEngine.
            </p>

            <div className="space-y-3 mb-5">
              <Input
                label="Date"
                type="date"
                value={testDate}
                onChange={(e) => setTestDate(e.target.value)}
              />

              <div className="grid grid-cols-2 gap-2">
                <Input
                  label="Service (mins)"
                  type="number"
                  value={testServiceDuration}
                  onChange={(e) => setTestServiceDuration(Number(e.target.value))}
                />
                <Input
                  label="Buffers (mins)"
                  type="number"
                  value={testBufferMinutes}
                  onChange={(e) => setTestBufferMinutes(Number(e.target.value))}
                />
              </div>

              <Button variant="secondary" className="w-full mt-2" size="sm" onClick={() => { setComputedPreviewSlots([...computedPreviewSlots]); }}>
                <RefreshCw className="w-3.5 h-3.5 mr-1.5" /> Re-calculate Slots
              </Button>
            </div>

            {/* Slot Preview Output */}
            <div>
              <div className="text-xs font-semibold text-[#7E88A8] uppercase tracking-wider mb-2">
                Computed Open Slots ({computedPreviewSlots.length})
              </div>

              <div className="grid grid-cols-1 gap-2 max-h-64 overflow-y-auto pr-1">
                {computedPreviewSlots.map((slot, index) => (
                  <div
                    key={index}
                    className="flex items-center justify-between px-3 py-2 bg-[#181D2C] border border-[#212638] rounded-lg text-xs"
                  >
                    <span className="font-medium text-[#ECEFFE]">{slot.displayTime}</span>
                    <span className="px-2 py-0.5 text-[10px] font-semibold bg-[#34D399]/10 text-[#34D399] rounded-full">
                      Available
                    </span>
                  </div>
                ))}
              </div>
            </div>
          </Card>
        </div>
      </div>

      {/* Time-Off Modal */}
      <Modal
        isOpen={isTimeOffModalOpen}
        onClose={() => setIsTimeOffModalOpen(false)}
        title={`Add Time-Off: ${selectedStaff?.name || ''}`}
      >
        <form onSubmit={handleCreateTimeOff} className="space-y-4">
          <Input
            label="Reason / Note"
            placeholder="e.g. Annual Summer Vacation"
            value={timeOffForm.reason}
            onChange={(e) => setTimeOffForm({ ...timeOffForm, reason: e.target.value })}
            required
          />

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <Input
              label="Start Date"
              type="date"
              value={timeOffForm.startIso}
              onChange={(e) => setTimeOffForm({ ...timeOffForm, startIso: e.target.value })}
              required
            />
            <Input
              label="End Date"
              type="date"
              value={timeOffForm.endIso}
              onChange={(e) => setTimeOffForm({ ...timeOffForm, endIso: e.target.value })}
              required
            />
          </div>

          <div className="flex justify-end gap-3 pt-4 border-t border-[#212638]">
            <Button variant="ghost" type="button" onClick={() => setIsTimeOffModalOpen(false)}>
              Cancel
            </Button>
            <Button variant="primary" type="submit">
              Save Time-Off
            </Button>
          </div>
        </form>
      </Modal>

      {/* Delete Time-Off Confirm Dialog */}
      <ConfirmDialog
        isOpen={!!deletingTimeOffId}
        onClose={() => setDeletingTimeOffId(null)}
        onConfirm={handleConfirmDeleteTimeOff}
        title="Remove Time-Off Entry"
        message="Are you sure you want to remove this time-off entry? Open slots will be restored for this date interval."
        confirmText="Remove Entry"
        isDanger={true}
      />
    </div>
  );
};
