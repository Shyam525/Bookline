import React from 'react';
import {
  formatInTimeZone,
  formatTimeInZone,
  formatDateInZone,
  getTimeZoneAbbreviation,
  getUserTimeZone,
  isValidIanaTimeZone,
} from '../../utils/timezones';
import { Clock, Globe } from 'lucide-react';

export interface ZonedTimeBadgeProps {
  /** Canonical UTC instant (ISO string or Date) */
  instant: string | Date | null | undefined;
  /** Primary location/provider IANA timezone */
  providerTimeZone: string;
  /** Customer/viewer IANA timezone (defaults to detected browser zone) */
  userTimeZone?: string;
  /** Whether to display both provider and viewer times if timezones differ */
  showBoth?: boolean;
  /** Whether to show the calendar date alongside time */
  showDate?: boolean;
  /** Optional custom CSS classes */
  className?: string;
}

/**
 * ZonedTimeBadge component for multi-timezone appointment display.
 * Conforms to Section 87: NodaTime and Clock Correctness.
 * 
 * Prevents machine-local time confusion by explicitly displaying
 * the appointment time in the location timezone, with optional
 * dual-timezone badges for remote customers.
 */
export const ZonedTimeBadge: React.FC<ZonedTimeBadgeProps> = ({
  instant,
  providerTimeZone,
  userTimeZone,
  showBoth = true,
  showDate = true,
  className = '',
}) => {
  if (!instant) {
    return <span className={`text-slate-400 text-xs ${className}`}>—</span>;
  }

  const locationZone = isValidIanaTimeZone(providerTimeZone) ? providerTimeZone : 'UTC';
  const customerZone = userTimeZone || getUserTimeZone();
  const isDifferentZone = locationZone !== customerZone && isValidIanaTimeZone(customerZone);

  const locationTimeStr = formatTimeInZone(instant, locationZone);
  const locationDateStr = showDate ? formatDateInZone(instant, locationZone) : null;
  const locationAbbr = getTimeZoneAbbreviation(locationZone);

  const customerTimeStr = isDifferentZone ? formatTimeInZone(instant, customerZone) : null;
  const customerAbbr = isDifferentZone ? getTimeZoneAbbreviation(customerZone) : null;

  return (
    <div className={`inline-flex flex-wrap items-center gap-1.5 text-xs ${className}`}>
      {/* Primary Provider Location Time */}
      <span
        className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md bg-slate-800/80 border border-slate-700/60 text-slate-200 font-medium shadow-sm"
        title={`Location Time (${locationZone})`}
      >
        <Clock className="w-3.5 h-3.5 text-indigo-400 shrink-0" />
        {locationDateStr && <span className="text-slate-300 font-normal">{locationDateStr},</span>}
        <span className="font-semibold text-white">{locationTimeStr}</span>
        <span className="text-[10px] text-indigo-300/80 uppercase font-mono tracking-wider ml-0.5">
          {locationAbbr}
        </span>
      </span>

      {/* Optional Dual Badge if Customer is in a Different Timezone */}
      {showBoth && isDifferentZone && customerTimeStr && (
        <span
          className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md bg-indigo-950/40 border border-indigo-800/40 text-indigo-200 text-[11px]"
          title={`Your Local Time (${customerZone})`}
        >
          <Globe className="w-3 h-3 text-indigo-400 shrink-0" />
          <span>Your time:</span>
          <span className="font-semibold text-white">{customerTimeStr}</span>
          <span className="text-[9px] text-indigo-300 font-mono uppercase">{customerAbbr}</span>
        </span>
      )}
    </div>
  );
};
