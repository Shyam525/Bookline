import React from 'react';
import { Clock, User, Check, Lock } from 'lucide-react';
import { clsx } from 'clsx';
import { StatusBadge, AppointmentStatus } from '../data-display/DataDisplay';

export interface SlotProps {
  time: string;
  state: 'FREE' | 'SELECTED' | 'HELD' | 'BOOKED' | 'UNAVAILABLE';
  holdTimeRemaining?: string;
  onClick?: () => void;
}

export const Slot: React.FC<SlotProps> = ({ time, state, holdTimeRemaining, onClick }) => {
  const styles = {
    FREE: 'bg-[#111520] border-[#34D399]/40 text-[#ECEFFE] hover:border-[#34D399] hover:bg-[#34D399]/5 cursor-pointer',
    SELECTED: 'bg-[#E8546A]/10 border-[#E8546A] text-[#E8546A] ring-2 ring-[#E8546A]/30 font-semibold cursor-pointer',
    HELD: 'bg-[#FBBF24]/10 border-[#FBBF24]/40 text-[#FBBF24] cursor-not-allowed opacity-90',
    BOOKED: 'bg-[#181D2C] border-[#212638] text-[#64748B] cursor-not-allowed opacity-60',
    UNAVAILABLE: 'bg-[#0A0C13] border-[#212638]/50 text-[#7E88A8]/40 cursor-not-allowed opacity-40',
  };

  return (
    <button
      type="button"
      disabled={state === 'BOOKED' || state === 'UNAVAILABLE'}
      onClick={onClick}
      className={clsx(
        'flex flex-col items-center justify-center p-3 rounded-xl border text-sm font-mono transition-all duration-150 relative group',
        styles[state]
      )}
    >
      <div className="flex items-center gap-1.5 font-medium">
        {state === 'SELECTED' && <Check className="w-3.5 h-3.5" />}
        {state === 'HELD' && <Lock className="w-3.5 h-3.5 animate-pulse" />}
        <span>{time}</span>
      </div>
      {state === 'HELD' && holdTimeRemaining && (
        <span className="text-[10px] text-[#FBBF24] font-sans mt-0.5">{holdTimeRemaining}</span>
      )}
      {state === 'FREE' && (
        <span className="text-[10px] text-[#34D399] font-sans mt-0.5">Available</span>
      )}
      {state === 'BOOKED' && (
        <span className="text-[10px] text-[#64748B] font-sans mt-0.5">Booked</span>
      )}
    </button>
  );
};

export const SlotGrid: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  return <div className="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 gap-3">{children}</div>;
};

export interface AppointmentCardProps {
  customerName: string;
  serviceName: string;
  durationMinutes: number;
  staffName: string;
  timeRange: string;
  status: AppointmentStatus;
  price: string;
  onClick?: () => void;
}

export const AppointmentCard: React.FC<AppointmentCardProps> = ({
  customerName,
  serviceName,
  durationMinutes,
  staffName,
  timeRange,
  status,
  price,
  onClick,
}) => {
  return (
    <div
      onClick={onClick}
      className="bg-[#111520] border border-[#212638] hover:border-[#E8546A]/50 rounded-2xl p-5 space-y-4 cursor-pointer transition-all duration-150 hover:shadow-xl group"
    >
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Clock className="w-4 h-4 text-[#7E88A8]" />
          <span className="text-xs font-mono font-medium text-[#ECEFFE]">{timeRange}</span>
        </div>
        <StatusBadge status={status} />
      </div>

      <div>
        <h4 className="font-heading text-lg font-bold text-white group-hover:text-[#E8546A] transition-colors">
          {customerName}
        </h4>
        <p className="text-sm text-[#7E88A8]">{serviceName} • {durationMinutes} mins</p>
      </div>

      <div className="flex items-center justify-between pt-2 border-t border-[#212638] text-xs text-[#7E88A8]">
        <div className="flex items-center gap-1.5">
          <User className="w-3.5 h-3.5" />
          <span>{staffName}</span>
        </div>
        <span className="font-mono font-bold text-[#ECEFFE] text-sm">{price}</span>
      </div>
    </div>
  );
};

export const CalendarHeader: React.FC<{
  currentDateRange: string;
  onToday: () => void;
  onPrev: () => void;
  onNext: () => void;
  onNewAppointment?: () => void;
}> = ({ currentDateRange, onToday, onPrev, onNext, onNewAppointment }) => {
  return (
    <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 pb-6 border-b border-[#212638]">
      <div>
        <h2 className="font-heading text-2xl font-bold text-white">{currentDateRange}</h2>
        <p className="text-xs text-[#7E88A8]">Live operations calendar & staff availability schedule</p>
      </div>

      <div className="flex items-center gap-3">
        <div className="inline-flex rounded-xl border border-[#212638] bg-[#111520] p-1 gap-1">
          <button onClick={onPrev} className="px-3 py-1.5 text-xs font-medium text-[#7E88A8] hover:text-white rounded-lg hover:bg-[#181D2C]">
            &larr; Prev
          </button>
          <button onClick={onToday} className="px-3 py-1.5 text-xs font-semibold text-[#E8546A] rounded-lg bg-[#181D2C]">
            Today
          </button>
          <button onClick={onNext} className="px-3 py-1.5 text-xs font-medium text-[#7E88A8] hover:text-white rounded-lg hover:bg-[#181D2C]">
            Next &rarr;
          </button>
        </div>

        {onNewAppointment && (
          <button onClick={onNewAppointment} className="px-4 py-2 text-sm font-medium bg-[#E8546A] hover:bg-[#D44359] text-white rounded-xl shadow-lg shadow-[#E8546A]/20 transition-colors">
            + New Appointment
          </button>
        )}
      </div>
    </div>
  );
};

export const CalendarTimeAxis: React.FC<{ times: string[] }> = ({ times }) => {
  return (
    <div className="w-16 shrink-0 flex flex-col pt-12 text-xs font-mono text-[#7E88A8] border-r border-[#212638]">
      {times.map((t) => (
        <div key={t} className="h-16 flex items-start justify-end pr-3">
          {t}
        </div>
      ))}
    </div>
  );
};

export const WorkingHoursBlock: React.FC<{ hours: string }> = ({ hours }) => {
  return (
    <div className="bg-[#181D2C]/40 border border-dashed border-[#212638] rounded-xl p-3 text-center text-xs text-[#7E88A8]">
      <span>Working Hours: {hours}</span>
    </div>
  );
};
