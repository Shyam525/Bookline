import React from 'react';

/**
 * Centralized Time & Date Utilities for Bookline
 * Conforms to Specification:
 * - Section 57: FRONTEND TIME UTILITIES
 * - Section 58: INVALID DATE DEFENSE
 * 
 * Rules:
 * - NEVER render: "Invalid Date", "undefined", "null", "NaN", raw timestamp
 * - If temporal data is invalid, render graceful defense:
 *   "Unable to load schedule." [Retry]
 * - BACKEND decides availability, FRONTEND requests availability.
 * - No component should independently parse date strings.
 */

/**
 * Checks whether an input represents a valid temporal value.
 */
export function isValidTemporal(input: unknown): boolean {
  if (input === null || input === undefined) return false;
  if (typeof input === 'string') {
    const trimmed = input.trim().toLowerCase();
    if (
      trimmed === '' ||
      trimmed === 'undefined' ||
      trimmed === 'null' ||
      trimmed === 'nan' ||
      trimmed === 'invalid date'
    ) {
      return false;
    }
  }
  if (typeof input === 'number' && isNaN(input)) return false;

  const d = parseDateSafe(input as any);
  return d !== null && !isNaN(d.getTime());
}

/**
 * Safely parses any date representation into a valid Date object.
 * Returns null if the date is invalid (guarding against NaN, undefined, "null", etc.).
 */
export function parseDateSafe(input: string | Date | number | null | undefined): Date | null {
  if (input === null || input === undefined) return null;

  if (input instanceof Date) {
    return isNaN(input.getTime()) ? null : input;
  }

  if (typeof input === 'number') {
    if (isNaN(input)) return null;
    const d = new Date(input);
    return isNaN(d.getTime()) ? null : d;
  }

  if (typeof input === 'string') {
    const trimmed = input.trim();
    const lower = trimmed.toLowerCase();
    if (
      lower === '' ||
      lower === 'undefined' ||
      lower === 'null' ||
      lower === 'nan' ||
      lower === 'invalid date'
    ) {
      return null;
    }

    // Time-only string "HH:mm" or "HH:mm:ss"
    if (/^\d{1,2}:\d{2}(:\d{2})?$/.test(trimmed)) {
      const [h, m] = trimmed.split(':').map(Number);
      if (isNaN(h) || isNaN(m) || h < 0 || h > 23 || m < 0 || m > 59) return null;
      const d = new Date();
      d.setHours(h, m, 0, 0);
      return d;
    }

    const d = new Date(trimmed);
    return isNaN(d.getTime()) ? null : d;
  }

  return null;
}

/**
 * Formats a date into business format:
 * - 'full': "Tuesday, 14 October 2026"
 * - 'short': "14 Oct 2026"
 * - 'today-prefix': "Today, 14 October" or "14 October"
 * 
 * Section 58 Defense: Never returns 'Invalid Date', 'undefined', 'null', 'NaN', raw timestamp.
 */
export function formatBusinessDate(
  input: string | Date | number | null | undefined,
  mode: 'full' | 'short' | 'today-prefix' | 'month-day' = 'today-prefix',
  timezone?: string
): string {
  const d = parseDateSafe(input);
  if (!d) return 'Schedule date pending';

  try {
    const now = new Date();
    const isToday =
      d.getFullYear() === now.getFullYear() &&
      d.getMonth() === now.getMonth() &&
      d.getDate() === now.getDate();

    const isTomorrow =
      d.getFullYear() === now.getFullYear() &&
      d.getMonth() === now.getMonth() &&
      d.getDate() === now.getDate() + 1;

    if (mode === 'today-prefix') {
      const dayAndMonth = d.toLocaleDateString('en-US', {
        day: 'numeric',
        month: 'long',
        timeZone: timezone,
      });
      if (isToday) return `Today, ${dayAndMonth}`;
      if (isTomorrow) return `Tomorrow, ${dayAndMonth}`;
      return d.toLocaleDateString('en-US', {
        weekday: 'short',
        day: 'numeric',
        month: 'short',
        timeZone: timezone,
      });
    }

    if (mode === 'month-day') {
      return d.toLocaleDateString('en-US', {
        day: 'numeric',
        month: 'short',
        timeZone: timezone,
      });
    }

    if (mode === 'full') {
      return d.toLocaleDateString('en-US', {
        weekday: 'long',
        day: 'numeric',
        month: 'long',
        year: 'numeric',
        timeZone: timezone,
      });
    }

    // 'short'
    return d.toLocaleDateString('en-US', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
      timeZone: timezone,
    });
  } catch {
    return 'Schedule date pending';
  }
}

/**
 * Formats a time into standard 12-hour business format: "10:30 AM", "02:15 PM".
 * Section 58 Defense: Never returns 'Invalid Date', 'undefined', 'null', 'NaN'.
 */
export function formatBusinessTime(
  input: string | Date | null | undefined,
  timezone?: string
): string {
  if (typeof input === 'string') {
    const trimmed = input.trim();
    if (/^\d{1,2}:\d{2}\s*(AM|PM)$/i.test(trimmed)) {
      return trimmed.toUpperCase();
    }
  }

  const d = parseDateSafe(input);
  if (!d) return '--:--';

  try {
    return d.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
      timeZone: timezone,
    });
  } catch {
    return '--:--';
  }
}

/**
 * Formats an ISO instant into clean business display: "14 Oct 2026, 10:30 AM".
 * Section 58 Defense: Never returns 'Invalid Date', 'undefined', 'null', 'NaN', or raw timestamp.
 */
export function formatInstant(
  instantIso: string | Date | null | undefined,
  timezone?: string
): string {
  const d = parseDateSafe(instantIso);
  if (!d) return 'Schedule time pending';

  try {
    const datePart = d.toLocaleDateString('en-US', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
      timeZone: timezone,
    });

    const timePart = d.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
      timeZone: timezone,
    });

    return `${datePart}, ${timePart}`;
  } catch {
    return 'Schedule time pending';
  }
}

/**
 * Formats appointment start and end into a clean time range: "10:00 AM – 10:45 AM"
 * Section 58 Defense: Never returns 'Invalid Date'.
 */
export function formatAppointmentRange(
  startIso: string | Date | null | undefined,
  endIso: string | Date | null | undefined,
  timezone?: string
): string {
  const start = parseDateSafe(startIso);
  const end = parseDateSafe(endIso);

  if (!start || !end) return 'Schedule window pending';

  try {
    const startTime = formatBusinessTime(start, timezone);
    const endTime = formatBusinessTime(end, timezone);

    // If different calendar days, include date for both
    if (start.toDateString() !== end.toDateString()) {
      const startDate = formatBusinessDate(start, 'short', timezone);
      const endDate = formatBusinessDate(end, 'short', timezone);
      return `${startDate} ${startTime} – ${endDate} ${endTime}`;
    }

    return `${startTime} – ${endTime}`;
  } catch {
    return 'Schedule window pending';
  }
}

/**
 * Formats duration in minutes into clean readable text:
 * - 45 -> "45 mins"
 * - 60 -> "1 hr"
 * - 75 -> "1 hr 15 mins"
 * - 120 -> "2 hrs"
 */
export function formatDuration(minutes: number | null | undefined): string {
  if (!minutes || isNaN(minutes) || minutes <= 0) return '0 mins';
  const hrs = Math.floor(minutes / 60);
  const remainingMins = minutes % 60;

  if (hrs === 0) {
    return `${remainingMins} mins`;
  }

  const hrText = hrs === 1 ? '1 hr' : `${hrs} hrs`;
  if (remainingMins === 0) {
    return hrText;
  }

  return `${hrText} ${remainingMins} mins`;
}

/**
 * Formats a timezone identifier into a user-friendly readable label.
 * E.g., "Asia/Kolkata" -> "Asia/Kolkata (IST · GMT+5:30)"
 */
export function formatTimezone(timezone: string = 'UTC'): string {
  try {
    const d = new Date();
    const shortFormatter = new Intl.DateTimeFormat('en-US', {
      timeZone: timezone,
      timeZoneName: 'short',
    });
    const shortParts = shortFormatter.formatToParts(d);
    const shortName = shortParts.find((p) => p.type === 'timeZoneName')?.value || timezone;

    const offsetFormatter = new Intl.DateTimeFormat('en-US', {
      timeZone: timezone,
      timeZoneName: 'longOffset',
    });
    const offsetParts = offsetFormatter.formatToParts(d);
    const offsetName = offsetParts.find((p) => p.type === 'timeZoneName')?.value || '';

    if (offsetName && shortName !== offsetName) {
      return `${timezone} (${shortName} · ${offsetName})`;
    }
    return `${timezone} (${shortName})`;
  } catch {
    return timezone;
  }
}

export { ScheduleErrorDefense } from '../components/common/ScheduleErrorDefense';
export type { ScheduleErrorDefenseProps } from '../components/common/ScheduleErrorDefense';
