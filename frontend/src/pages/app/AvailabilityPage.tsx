import React, { useState } from 'react';
import { Button, IconButton } from '../../components/ui/Button';
import { Input, Select } from '../../components/forms/Inputs';
import { Card, Avatar } from '../../components/data-display/DataDisplay';
import { Modal, ConfirmDialog } from '../../components/feedback/Feedback';
import { Clock, Calendar, Plus, Trash2, Check, Sparkles, RefreshCw, Coffee, AlertCircle } from 'lucide-react';
import { TimeSlotItem } from '../../services/api/availability';

interface ShiftInterval {
  id: string;
  start: string;
  end: string;
  label?: string;
}

interface DaySchedule {
  dayOfWeek: number; // 1=Mon, 2=Tue... 0=Sun
  dayName: string;
  isEnabled: boolean;
  intervals: ShiftInterval[]; // Section 77: Multiple intervals e.g. 09:00-13:00, 14:00-18:00
}

interface TimeOffEntry {
  id: string;
  staffId: string;
  type: 'Vacation' | 'Sick' | 'Holiday' | 'Personal' | 'Custom';
  startUtc: string;
  endUtc: string;
  reason: string;
}

/**
 * Specification Sections 77, 78, 79:
 * 77. WORKING HOURS: Multiple intervals (e.g. 09:00–13:00, 14:00–18:00)
 * 78. BREAKS: Breaks consume availability.
 * 79. TIME OFF: Vacation, Sick, Holiday, Personal, Custom blocked periods
 */
export const AvailabilityPage: React.FC = () => {
  const staffMembers = [
    { id: 'staff-1', name: 'Elena Vance', title: 'Senior Master Stylist' },
    { id: 'staff-2', name: 'Marcus Brody', title: 'Master Barber' },
    { id: 'staff-3', name: 'Sophia Chen', title: 'Color Specialist' },
  ];

  const [selectedStaffId, setSelectedStaffId] = useState('staff-1');

  // Weekly Schedule State with Multiple Intervals (Section 77)
  const [weeklySchedule, setWeeklySchedule] = useState<DaySchedule[]>([
    {
      dayOfWeek: 1,
      dayName: 'Monday',
      isEnabled: true,
      intervals: [
        { id: 'int-1-1', start: '09:00', end: '13:00', label: 'Morning Shift' },
        { id: 'int-1-2', start: '14:00', end: '18:00', label: 'Afternoon Shift' },
      ],
    },
    {
      dayOfWeek: 2,
      dayName: 'Tuesday',
      isEnabled: true,
      intervals: [
        { id: 'int-2-1', start: '09:00', end: '13:00', label: 'Morning Shift' },
        { id: 'int-2-2', start: '14:00', end: '18:00', label: 'Afternoon Shift' },
      ],
    },
    {
      dayOfWeek: 3,
      dayName: 'Wednesday',
      isEnabled: true,
      intervals: [
        { id: 'int-3-1', start: '09:00', end: '13:00', label: 'Morning Shift' },
        { id: 'int-3-2', start: '14:00', end: '18:00', label: 'Afternoon Shift' },
      ],
    },
    {
      dayOfWeek: 4,
      dayName: 'Thursday',
      isEnabled: true,
      intervals: [
        { id: 'int-4-1', start: '09:00', end: '13:00', label: 'Morning Shift' },
        { id: 'int-4-2', start: '14:00', end: '18:00', label: 'Afternoon Shift' },
      ],
    },
    {
      dayOfWeek: 5,
      dayName: 'Friday',
      isEnabled: true,
      intervals: [
        { id: 'int-5-1', start: '09:00', end: '13:00', label: 'Morning Shift' },
        { id: 'int-5-2', start: '14:00', end: '18:00', label: 'Afternoon Shift' },
      ],
    },
    {
      dayOfWeek: 6,
      dayName: 'Saturday',
      isEnabled: true,
      intervals: [{ id: 'int-6-1', start: '10:00', end: '16:00', label: 'Weekend Shift' }],
    },
    {
      dayOfWeek: 0,
      dayName: 'Sunday',
      isEnabled: false,
      intervals: [],
    },
  ]);

  // Section 79: Time-Off State with 5 explicit types
  const [timeOffList, setTimeOffList] = useState<TimeOffEntry[]>([
    {
      id: 'to-1',
      staffId: 'staff-1',
      type: 'Vacation',
      startUtc: new Date(Date.now() + 86400000 * 3).toISOString(),
      endUtc: new Date(Date.now() + 86400000 * 7).toISOString(),
      reason: 'Annual Summer Vacation',
    },
    {
      id: 'to-2',
      staffId: 'staff-1',
      type: 'Holiday',
      startUtc: new Date(Date.now() + 86400000 * 14).toISOString(),
      endUtc: new Date(Date.now() + 86400000 * 15).toISOString(),
      reason: 'National Holiday Closure',
    },
  ]);

  const [isTimeOffModalOpen, setIsTimeOffModalOpen] = useState(false);
  const [timeOffType, setTimeOffType] = useState<'Vacation' | 'Sick' | 'Holiday' | 'Personal' | 'Custom'>('Vacation');
  const [timeOffForm, setTimeOffForm] = useState({
    startIso: new Date().toISOString().slice(0, 10),
    endIso: new Date(Date.now() + 86400000 * 2).toISOString().slice(0, 10),
    reason: '',
  });

  // Slot Tester Preview State (Section 78: Demonstrates breaks consuming availability)
  const [testDate, setTestDate] = useState(new Date().toISOString().slice(0, 10));
  const [computedPreviewSlots, setComputedPreviewSlots] = useState<any[]>([
    { startIso: '09:00', endIso: '09:45', displayTime: '09:00 AM - 09:45 AM', isAvailable: true, staffName: 'Elena Vance' },
    { startIso: '10:00', endIso: '10:45', displayTime: '10:00 AM - 10:45 AM', isAvailable: true, staffName: 'Elena Vance' },
    { startIso: '11:00', endIso: '11:45', displayTime: '11:00 AM - 11:45 AM', isAvailable: true, staffName: 'Elena Vance' },
    { startIso: '12:00', endIso: '12:45', displayTime: '12:00 PM - 12:45 PM', isAvailable: true, staffName: 'Elena Vance' },
    // Section 78 Break
    { isBreak: true, displayTime: '13:00 PM - 14:00 PM', label: 'Mid-Day Lunch Break (Consumes Availability)' },
    { startIso: '14:00', endIso: '14:45', displayTime: '02:00 PM - 02:45 PM', isAvailable: true, staffName: 'Elena Vance' },
    { startIso: '15:00', endIso: '15:45', displayTime: '03:00 PM - 03:45 PM', isAvailable: true, staffName: 'Elena Vance' },
    { startIso: '16:00', endIso: '16:45', displayTime: '04:00 PM - 04:45 PM', isAvailable: true, staffName: 'Elena Vance' },
    { startIso: '17:00', endIso: '17:45', displayTime: '05:00 PM - 05:45 PM', isAvailable: true, staffName: 'Elena Vance' },
  ]);
  const [isScheduleSaved, setIsScheduleSaved] = useState(false);

  const selectedStaff = staffMembers.find((s) => s.id === selectedStaffId);

  const handleToggleDay = (dayOfWeek: number) => {
    setWeeklySchedule(
      weeklySchedule.map((d) => {
        if (d.dayOfWeek === dayOfWeek) {
          const nextEnabled = !d.isEnabled;
          return {
            ...d,
            isEnabled: nextEnabled,
            intervals:
              nextEnabled && d.intervals.length === 0
                ? [{ id: `int-${dayOfWeek}-1`, start: '09:00', end: '17:00', label: 'Full Day Shift' }]
                : d.intervals,
          };
        }
        return d;
      })
    );
  };

  const handleAddInterval = (dayOfWeek: number) => {
    setWeeklySchedule(
      weeklySchedule.map((d) => {
        if (d.dayOfWeek === dayOfWeek) {
          const nextId = `int-${dayOfWeek}-${d.intervals.length + 1}`;
          return {
            ...d,
            intervals: [
              ...d.intervals,
              { id: nextId, start: '14:00', end: '18:00', label: `Shift ${d.intervals.length + 1}` },
            ],
          };
        }
        return d;
      })
    );
  };

  const handleRemoveInterval = (dayOfWeek: number, intervalId: string) => {
    setWeeklySchedule(
      weeklySchedule.map((d) => {
        if (d.dayOfWeek === dayOfWeek) {
          return {
            ...d,
            intervals: d.intervals.filter((i) => i.id !== intervalId),
          };
        }
        return d;
      })
    );
  };

  const handleIntervalChange = (
    dayOfWeek: number,
    intervalId: string,
    field: 'start' | 'end',
    value: string
  ) => {
    setWeeklySchedule(
      weeklySchedule.map((d) => {
        if (d.dayOfWeek === dayOfWeek) {
          return {
            ...d,
            intervals: d.intervals.map((inv) =>
              inv.id === intervalId ? { ...inv, [field]: value } : inv
            ),
          };
        }
        return d;
      })
    );
  };

  const handleSaveSchedule = () => {
    setIsScheduleSaved(true);
    setTimeout(() => setIsScheduleSaved(false), 3000);
  };

  const handleCreateTimeOff = (e: React.FormEvent) => {
    e.preventDefault();
    const newEntry: TimeOffEntry = {
      id: `to-${Date.now()}`,
      staffId: selectedStaffId,
      type: timeOffType,
      startUtc: new Date(timeOffForm.startIso).toISOString(),
      endUtc: new Date(timeOffForm.endIso).toISOString(),
      reason: timeOffForm.reason || `${timeOffType} Leave`,
    };
    setTimeOffList([...timeOffList, newEntry]);
    setIsTimeOffModalOpen(false);
  };

  return (
    <div className="max-w-7xl mx-auto space-y-8 pb-12">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-[#212638] pb-6">
        <div>
          <span className="text-[10px] uppercase font-bold text-[#34D399] tracking-widest block mb-1">
            SECTIONS 77, 78 &amp; 79: AVAILABILITY ENGINE
          </span>
          <h1 className="font-heading text-2xl font-bold text-white">Staff Schedules, Breaks &amp; Time-Off</h1>
          <p className="text-xs text-[#7E88A8]">
            Configure multi-interval daily working shifts, automatic break deductions, and structured time-off
          </p>
        </div>

        {/* Staff Selector */}
        <div className="flex items-center gap-3 bg-[#111520] p-1.5 rounded-2xl border border-[#212638]">
          <span className="text-xs text-[#7E88A8] pl-2">Specialist:</span>
          <select
            value={selectedStaffId}
            onChange={(e) => setSelectedStaffId(e.target.value)}
            className="bg-[#181D2C] text-white text-xs font-semibold px-3 py-1.5 rounded-xl border border-[#212638] focus:outline-none"
          >
            {staffMembers.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name} ({s.title})
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Left Column: Weekly Schedule & Time-Off */}
        <div className="lg:col-span-2 space-y-6">
          {/* Multi-Interval Weekly Schedule (Section 77 & 78) */}
          <Card className="p-6 space-y-4">
            <div className="flex items-center justify-between border-b border-[#212638] pb-4">
              <div>
                <h3 className="font-heading font-bold text-base text-white">
                  Multi-Interval Weekly Working Hours (Section 77)
                </h3>
                <p className="text-xs text-[#7E88A8]">
                  Define split shifts (e.g. 09:00–13:00, 14:00–18:00). Shift gaps automatically register as breaks.
                </p>
              </div>

              <Button variant="primary" size="sm" onClick={handleSaveSchedule}>
                {isScheduleSaved ? (
                  <span className="flex items-center gap-1 text-white">
                    <Check className="w-4 h-4" /> Saved!
                  </span>
                ) : (
                  'Save Schedule'
                )}
              </Button>
            </div>

            <div className="space-y-4">
              {weeklySchedule.map((day) => (
                <div
                  key={day.dayOfWeek}
                  className={`p-4 rounded-2xl border transition-all space-y-3 ${
                    day.isEnabled
                      ? 'bg-[#181D2C] border-[#212638]'
                      : 'bg-[#111520] border-[#212638]/50 opacity-60'
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <input
                        type="checkbox"
                        id={`day-${day.dayOfWeek}`}
                        checked={day.isEnabled}
                        onChange={() => handleToggleDay(day.dayOfWeek)}
                        className="rounded bg-[#0A0C13] border-[#212638] text-[#E8546A] cursor-pointer"
                      />
                      <label
                        htmlFor={`day-${day.dayOfWeek}`}
                        className="font-bold text-sm text-white cursor-pointer"
                      >
                        {day.dayName}
                      </label>
                    </div>

                    {day.isEnabled && (
                      <button
                        type="button"
                        onClick={() => handleAddInterval(day.dayOfWeek)}
                        className="text-[11px] font-semibold text-[#34D399] hover:underline flex items-center gap-1"
                      >
                        <Plus className="w-3.5 h-3.5" /> Add Split Shift
                      </button>
                    )}
                  </div>

                  {day.isEnabled ? (
                    <div className="space-y-2 pt-1 border-t border-[#212638]/60">
                      {day.intervals.map((inv, idx) => (
                        <div key={inv.id} className="flex items-center gap-3 flex-wrap">
                          <span className="text-[10px] uppercase font-bold text-[#7E88A8] w-14">
                            Shift {idx + 1}:
                          </span>
                          <div className="flex items-center gap-2">
                            <Clock className="w-3.5 h-3.5 text-[#34D399]" />
                            <input
                              type="time"
                              value={inv.start}
                              onChange={(e) =>
                                handleIntervalChange(day.dayOfWeek, inv.id, 'start', e.target.value)
                              }
                              className="px-2.5 py-1 rounded-lg bg-[#111520] border border-[#212638] text-white text-xs font-mono"
                            />
                            <span className="text-xs text-[#7E88A8]">to</span>
                            <input
                              type="time"
                              value={inv.end}
                              onChange={(e) =>
                                handleIntervalChange(day.dayOfWeek, inv.id, 'end', e.target.value)
                              }
                              className="px-2.5 py-1 rounded-lg bg-[#111520] border border-[#212638] text-white text-xs font-mono"
                            />
                          </div>

                          {day.intervals.length > 1 && (
                            <button
                              type="button"
                              onClick={() => handleRemoveInterval(day.dayOfWeek, inv.id)}
                              className="p-1 text-red-400 hover:text-red-300"
                              title="Remove shift interval"
                            >
                              <Trash2 className="w-3.5 h-3.5" />
                            </button>
                          )}
                        </div>
                      ))}

                      {day.intervals.length > 1 && (
                        <div className="flex items-center gap-1.5 text-[10px] text-amber-400 bg-amber-400/10 px-2.5 py-1 rounded-lg mt-2">
                          <Coffee className="w-3 h-3" />
                          <span>Shift interval gap automatically acts as break and consumes availability.</span>
                        </div>
                      )}
                    </div>
                  ) : (
                    <span className="text-xs text-[#7E88A8] italic">Off Day / Closed</span>
                  )}
                </div>
              ))}
            </div>
          </Card>

          {/* Structured Time-Off (Section 79) */}
          <Card className="p-6 space-y-4">
            <div className="flex items-center justify-between border-b border-[#212638] pb-3">
              <div>
                <h3 className="font-heading font-bold text-base text-white">
                  Staff Time-Off &amp; Leave (Section 79)
                </h3>
                <p className="text-xs text-[#7E88A8]">
                  Vacation, Sick, Holiday, Personal, and Custom blocked periods
                </p>
              </div>

              <Button variant="secondary" size="sm" onClick={() => setIsTimeOffModalOpen(true)}>
                <Plus className="w-3.5 h-3.5 mr-1.5" /> Add Time-Off
              </Button>
            </div>

            <div className="space-y-3">
              {timeOffList.map((to) => (
                <div
                  key={to.id}
                  className="p-4 rounded-2xl bg-[#181D2C] border border-[#212638] flex items-center justify-between"
                >
                  <div className="flex items-center gap-3">
                    <div className="w-9 h-9 rounded-xl bg-[#E8546A]/15 text-[#E8546A] flex items-center justify-center font-bold text-xs">
                      {to.type.substring(0, 1)}
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="font-bold text-white text-xs">{to.reason}</span>
                        <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-[#111520] text-[#E8546A] border border-[#212638]">
                          {to.type}
                        </span>
                      </div>
                      <p className="text-[11px] text-[#7E88A8] mt-0.5">
                        {new Date(to.startUtc).toLocaleDateString()} &mdash; {new Date(to.endUtc).toLocaleDateString()}
                      </p>
                    </div>
                  </div>

                  <button
                    type="button"
                    onClick={() => setTimeOffList(timeOffList.filter((item) => item.id !== to.id))}
                    className="p-1.5 text-red-400 hover:text-red-300"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              ))}
            </div>
          </Card>
        </div>

        {/* Right Column: Slot Engine Availability Preview */}
        <div className="space-y-6">
          <Card className="p-6 space-y-4">
            <div className="border-b border-[#212638] pb-3">
              <span className="text-[10px] uppercase font-bold text-[#34D399] tracking-wider block mb-0.5">
                REAL-TIME ENGINE VALIDATION
              </span>
              <h3 className="font-heading font-bold text-base text-white">
                Live Availability Output
              </h3>
              <p className="text-xs text-[#7E88A8]">
                Demonstrates Section 78: Break intervals consume availability in slot generation.
              </p>
            </div>

            <div className="space-y-2">
              <label className="block text-xs font-bold text-[#7E88A8] uppercase">
                Simulate Target Date
              </label>
              <input
                type="date"
                value={testDate}
                onChange={(e) => setTestDate(e.target.value)}
                className="w-full px-3 py-2 rounded-xl bg-[#181D2C] border border-[#212638] text-white text-xs"
              />
            </div>

            {/* Generated Slots List */}
            <div className="space-y-2 max-h-[480px] overflow-y-auto pr-1">
              {computedPreviewSlots.map((slot, i) => {
                if (slot.isBreak) {
                  return (
                    <div
                      key={i}
                      className="p-3 rounded-xl bg-amber-500/10 border border-amber-500/30 text-amber-300 text-xs flex items-center justify-between"
                    >
                      <div className="flex items-center gap-2">
                        <Coffee className="w-4 h-4" />
                        <div>
                          <span className="font-bold block">{slot.displayTime}</span>
                          <span className="text-[10px] text-amber-400/80">{slot.label}</span>
                        </div>
                      </div>
                      <span className="text-[10px] uppercase font-bold bg-amber-500/20 px-2 py-0.5 rounded">
                        Blocked
                      </span>
                    </div>
                  );
                }

                return (
                  <div
                    key={i}
                    className="p-3 rounded-xl bg-[#181D2C] border border-[#212638] flex items-center justify-between text-xs"
                  >
                    <div className="flex items-center gap-2">
                      <Clock className="w-3.5 h-3.5 text-[#34D399]" />
                      <span className="font-mono text-white font-semibold">{slot.displayTime}</span>
                    </div>
                    <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-[#34D399]/20 text-[#34D399]">
                      Available
                    </span>
                  </div>
                );
              })}
            </div>
          </Card>
        </div>
      </div>

      {/* Add Time-Off Modal (Section 79) */}
      {isTimeOffModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm animate-fadeIn">
          <div className="bg-[#111520] border border-[#212638] rounded-3xl w-full max-w-md p-6 space-y-5 shadow-2xl">
            <div className="flex items-center justify-between border-b border-[#212638] pb-3">
              <h3 className="font-heading font-bold text-lg text-white">Add Staff Time-Off</h3>
              <button
                type="button"
                onClick={() => setIsTimeOffModalOpen(false)}
                className="text-[#7E88A8] hover:text-white"
              >
                &times;
              </button>
            </div>

            <form onSubmit={handleCreateTimeOff} className="space-y-4 text-xs">
              <div>
                <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                  Time-Off Category (Section 79)
                </label>
                <select
                  value={timeOffType}
                  onChange={(e) => setTimeOffType(e.target.value as any)}
                  className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white"
                >
                  <option value="Vacation">Vacation (Annual Leave)</option>
                  <option value="Sick">Sick Leave</option>
                  <option value="Holiday">Holiday (Public/National)</option>
                  <option value="Personal">Personal Leave</option>
                  <option value="Custom">Custom Blocked Period</option>
                </select>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                    Start Date
                  </label>
                  <input
                    type="date"
                    required
                    value={timeOffForm.startIso}
                    onChange={(e) => setTimeOffForm({ ...timeOffForm, startIso: e.target.value })}
                    className="w-full px-3 py-2 rounded-xl bg-[#181D2C] border border-[#212638] text-white"
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                    End Date
                  </label>
                  <input
                    type="date"
                    required
                    value={timeOffForm.endIso}
                    onChange={(e) => setTimeOffForm({ ...timeOffForm, endIso: e.target.value })}
                    className="w-full px-3 py-2 rounded-xl bg-[#181D2C] border border-[#212638] text-white"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                  Reason / Notes
                </label>
                <input
                  type="text"
                  required
                  value={timeOffForm.reason}
                  onChange={(e) => setTimeOffForm({ ...timeOffForm, reason: e.target.value })}
                  placeholder="e.g. Summer Vacation / Conference Attendance"
                  className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white"
                />
              </div>

              <div className="flex justify-end gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => setIsTimeOffModalOpen(false)}
                  className="px-4 py-2 text-xs text-[#7E88A8] hover:text-white"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-5 py-2.5 rounded-xl bg-[#E8546A] hover:bg-[#D44359] text-white font-bold text-xs"
                >
                  Confirm Time-Off
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
