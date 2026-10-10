import React, { useState, useEffect } from 'react';
import {
  Clock,
  User,
  Calendar as CalendarIcon,
  Lock,
  AlertCircle,
  ChevronLeft,
  ChevronRight,
  Sparkles,
  CheckCircle,
} from 'lucide-react';
import { clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';
import { StatusBadge, AppointmentStatus, Avatar } from '../data-display/DataDisplay';

/* =========================================================================
 * 1. HOLD BANNER (Section 58, 109, 110)
 * ========================================================================= */
export interface HoldBannerProps {
  initialSeconds?: number;
  serviceName: string;
  slotTime: string;
  onExpire?: () => void;
  onProceed?: () => void;
  className?: string;
}

export const HoldBanner: React.FC<HoldBannerProps> = ({
  initialSeconds = 300, // 5 minute standard hold (Section 58)
  serviceName,
  slotTime,
  onExpire,
  onProceed,
  className = '',
}) => {
  const [secondsRemaining, setSecondsRemaining] = useState(initialSeconds);

  useEffect(() => {
    if (secondsRemaining <= 0) {
      onExpire?.();
      return;
    }
    const timer = setInterval(() => {
      setSecondsRemaining((prev) => {
        if (prev <= 1) {
          clearInterval(timer);
          onExpire?.();
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
    return () => clearInterval(timer);
  }, [secondsRemaining, onExpire]);

  const mins = Math.floor(secondsRemaining / 60);
  const secs = secondsRemaining % 60;
  const formattedTime = `${mins}:${secs.toString().padStart(2, '0')}`;
  const isUrgent = secondsRemaining < 60;

  if (secondsRemaining <= 0) {
    return (
      <div className={`p-4 rounded-2xl bg-red-500/10 border border-red-500/30 flex items-center justify-between gap-4 text-xs text-red-300 ${className}`}>
        <div className="flex items-center gap-2">
          <AlertCircle className="w-4 h-4 shrink-0 text-red-400" />
          <span>Slot reservation expired. Please select a fresh opening.</span>
        </div>
      </div>
    );
  }

  return (
    <div
      role="alert"
      className={twMerge(
        clsx(
          'p-3.5 sm:p-4 rounded-2xl border flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 text-xs transition-colors shadow-lg select-none',
          isUrgent
            ? 'bg-[#E8546A]/10 border-[#E8546A]/40 text-[#E8546A]'
            : 'bg-[#FBBF24]/10 border-[#FBBF24]/30 text-[#FBBF24]',
          className
        )
      )}
    >
      <div className="flex items-center gap-2.5">
        <Lock className="w-4 h-4 shrink-0 animate-pulse" />
        <div>
          <span className="font-semibold text-white">{serviceName}</span> at{' '}
          <strong className="text-white">{slotTime}</strong> is temporarily held for you.
        </div>
      </div>

      <div className="flex items-center gap-3 w-full sm:w-auto justify-between sm:justify-end">
        <div className="flex items-center gap-1 font-mono font-bold text-sm">
          <Clock className="w-3.5 h-3.5" />
          <span>{formattedTime}</span>
        </div>
        {onProceed && (
          <button
            type="button"
            onClick={onProceed}
            className="px-4 py-1.5 rounded-xl bg-[#E8546A] hover:bg-[#F06A7D] text-white text-xs font-bold transition-all shadow-md active:scale-95"
          >
            Complete Booking
          </button>
        )}
      </div>
    </div>
  );
};

/* =========================================================================
 * 2. DATE STRIP (Horizontal Date Picker Strip)
 * ========================================================================= */
export interface DateStripProps {
  dates: Array<{ date: string; dayName: string; dayNumber: string; isToday?: boolean }>;
  selectedDate: string;
  onSelectDate: (date: string) => void;
  className?: string;
}

export const DateStrip: React.FC<DateStripProps> = ({
  dates,
  selectedDate,
  onSelectDate,
  className = '',
}) => {
  return (
    <div className={twMerge('flex items-center gap-2 overflow-x-auto pb-2 select-none scrollbar-none', className)}>
      {dates.map((d) => {
        const isSelected = d.date === selectedDate;
        return (
          <button
            key={d.date}
            type="button"
            onClick={() => onSelectDate(d.date)}
            className={clsx(
              'flex flex-col items-center justify-center min-w-[64px] sm:min-w-[72px] py-2.5 px-2 rounded-2xl border text-center transition-all focus:outline-none focus:ring-2 focus:ring-[#E8546A]',
              isSelected
                ? 'bg-[#E8546A] border-[#E8546A] text-white shadow-lg shadow-[#E8546A]/25 scale-105 font-bold'
                : 'bg-[#111520] hover:bg-[#181D2C] border-[#212638] text-[#7E88A8] hover:text-[#ECEFFE]'
            )}
          >
            <span className="text-[10px] uppercase font-semibold tracking-wider">
              {d.isToday ? 'Today' : d.dayName}
            </span>
            <span className={`text-base font-bold font-mono mt-0.5 ${isSelected ? 'text-white' : 'text-[#ECEFFE]'}`}>
              {d.dayNumber}
            </span>
          </button>
        );
      })}
    </div>
  );
};

/* =========================================================================
 * 3. APPOINTMENT BLOCK
 * ========================================================================= */
export interface AppointmentBlockProps {
  id: string;
  customerName: string;
  serviceName: string;
  startTime: string;
  endTime: string;
  status: AppointmentStatus;
  price?: string;
  topOffsetPx?: number;
  heightPx?: number;
  onClick?: () => void;
}

export const AppointmentBlock: React.FC<AppointmentBlockProps> = ({
  customerName,
  serviceName,
  startTime,
  endTime,
  status,
  price,
  topOffsetPx,
  heightPx = 70,
  onClick,
}) => {
  const statusStyles: Record<AppointmentStatus, string> = {
    Free: 'bg-emerald-500/15 border-emerald-500/40 text-emerald-300',
    Held: 'bg-amber-500/15 border-amber-500/40 text-amber-300',
    Booked: 'bg-[#181D2C] border-[#212638] text-[#7E88A8]',
    Pending: 'bg-amber-500/15 border-amber-500/40 text-amber-300',
    Confirmed: 'bg-emerald-500/20 border-emerald-500/50 text-emerald-200',
    CheckedIn: 'bg-blue-500/20 border-blue-500/50 text-blue-200',
    Completed: 'bg-purple-500/20 border-purple-500/50 text-purple-200',
    Cancelled: 'bg-red-500/15 border-red-500/40 text-red-300 line-through',
    NoShow: 'bg-gray-500/15 border-gray-500/40 text-gray-400',
  };

  const styleObj: React.CSSProperties = {
    minHeight: `${Math.max(48, heightPx)}px`,
    ...(topOffsetPx !== undefined ? { position: 'absolute', top: `${topOffsetPx}px`, left: '4px', right: '4px' } : {}),
  };

  return (
    <div
      onClick={onClick}
      style={styleObj}
      className={twMerge(
        clsx(
          'p-2.5 rounded-xl border text-xs cursor-pointer transition-all hover:scale-[1.01] hover:shadow-lg flex flex-col justify-between overflow-hidden group select-none',
          statusStyles[status] || statusStyles.Confirmed
        )
      )}
    >
      <div className="flex items-center justify-between gap-1">
        <span className="font-mono font-bold text-[11px] truncate">
          {startTime} - {endTime}
        </span>
        {price && <span className="font-mono text-[10px] font-semibold">{price}</span>}
      </div>

      <div className="space-y-0.5">
        <h5 className="font-bold text-white truncate text-xs">{customerName}</h5>
        <p className="text-[10px] opacity-80 truncate">{serviceName}</p>
      </div>
    </div>
  );
};

/* =========================================================================
 * 4. STAFF COLUMN
 * ========================================================================= */
export interface StaffColumnProps {
  staffId: string;
  staffName: string;
  role?: string;
  appointments: AppointmentBlockProps[];
  onSelectSlot?: (time: string) => void;
  className?: string;
}

export const StaffColumn: React.FC<StaffColumnProps> = ({
  staffName,
  role = 'Specialist',
  appointments,
  onSelectSlot,
  className = '',
}) => {
  return (
    <div className={twMerge('flex-1 min-w-[200px] border-r border-[#212638] flex flex-col bg-[#111520]/50', className)}>
      {/* Column Header */}
      <div className="p-3 border-b border-[#212638] bg-[#151B27] flex items-center gap-2.5 sticky top-0 z-10">
        <Avatar name={staffName} size="sm" />
        <div className="truncate">
          <p className="text-xs font-bold text-white truncate">{staffName}</p>
          <span className="text-[10px] text-[#7E88A8] block truncate">{role}</span>
        </div>
      </div>

      {/* Appointments List / Grid */}
      <div className="p-2 space-y-2 flex-1 min-h-[300px]">
        {appointments.length === 0 ? (
          <div className="h-full flex items-center justify-center p-6 text-center text-xs text-[#7E88A8]">
            No appointments scheduled
          </div>
        ) : (
          appointments.map((apt) => <AppointmentBlock key={apt.id} {...apt} />)
        )}
      </div>
    </div>
  );
};

/* =========================================================================
 * 5. MASTER CALENDAR CONTAINER (Interactive Day/Week Multi-Staff Grid)
 * ========================================================================= */
export interface CalendarProps {
  title?: string;
  children: React.ReactNode;
  headerActions?: React.ReactNode;
  className?: string;
}

export const Calendar: React.FC<CalendarProps> = ({
  title,
  children,
  headerActions,
  className = '',
}) => {
  return (
    <div className={twMerge('rounded-2xl border border-[#212638] bg-[#111520] shadow-2xl overflow-hidden flex flex-col', className)}>
      {title && (
        <div className="px-6 py-4 border-b border-[#212638] bg-[#151B27] flex items-center justify-between">
          <h3 className="font-heading text-lg font-bold text-white">{title}</h3>
          {headerActions}
        </div>
      )}
      <div className="flex-1 overflow-x-auto flex">{children}</div>
    </div>
  );
};
