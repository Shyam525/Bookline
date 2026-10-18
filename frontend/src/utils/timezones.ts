/**
 * Centralized Timezone Utilities for Bookline
 * Conforms to Section 87: NodaTime and Clock Correctness
 * 
 * Rules:
 * - Validate IANA timezone identifiers via Intl runtime API.
 * - Format temporal instants in provider or customer local timezones without machine-local drift.
 * - Detect user timezones safely.
/**
 * Safely parses any date representation into a valid Date object.
 * Returns null if the date is invalid (guarding against NaN, undefined, 'null', etc.).
 */
export function parseDateSafe(input: string | Date | number | null | undefined): Date | null {
  if (input === null || input === undefined) return null;
  if (input instanceof Date) return isNaN(input.getTime()) ? null : input;
  if (typeof input === 'number') {
    if (isNaN(input)) return null;
    const d = new Date(input);
    return isNaN(d.getTime()) ? null : d;
  }
  if (typeof input === 'string') {
    const trimmed = input.trim();
    if (!trimmed) return null;
    const lower = trimmed.toLowerCase();
    if (lower === 'null' || lower === 'undefined' || lower === 'nan' || lower === 'invalid date') return null;
    const d = new Date(trimmed);
    return isNaN(d.getTime()) ? null : d;
  }
  return null;
}

/**
 * Validates whether a given string is a valid, recognized IANA timezone identifier.
 */
export function isValidIanaTimeZone(timeZone: string | null | undefined): boolean {
  if (!timeZone || typeof timeZone !== 'string') return false;
  const trimmed = timeZone.trim();
  if (!trimmed) return false;

  try {
    Intl.DateTimeFormat(undefined, { timeZone: trimmed });
    return true;
  } catch {
    return false;
  }
}

/**
 * Safely resolves the user's browser timezone.
 * Falls back to 'UTC' if detection fails or is unavailable.
 */
export function getUserTimeZone(): string {
  try {
    const resolved = Intl.DateTimeFormat().resolvedOptions().timeZone;
    if (resolved && isValidIanaTimeZone(resolved)) {
      return resolved;
    }
  } catch {
    // Fallback
  }
  return 'UTC';
}

/**
 * Formats an instant or date safely within a specific IANA timezone.
 * If input or timezone is invalid, returns a safe fallback message without throwing.
 */
export function formatInTimeZone(
  dateInput: string | Date | number | null | undefined,
  timeZone: string,
  options: Intl.DateTimeFormatOptions = {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hour12: true,
  }
): string {
  const date = parseDateSafe(dateInput);
  if (!date) return '—';

  const validZone = isValidIanaTimeZone(timeZone) ? timeZone : 'UTC';

  try {
    const formatter = new Intl.DateTimeFormat('en-US', {
      ...options,
      timeZone: validZone,
    });
    return formatter.format(date);
  } catch {
    return date.toISOString();
  }
}

/**
 * Formats time only (e.g. "09:30 AM") within a given IANA timezone.
 */
export function formatTimeInZone(
  dateInput: string | Date | number | null | undefined,
  timeZone: string,
  hour12 = true
): string {
  return formatInTimeZone(dateInput, timeZone, {
    hour: 'numeric',
    minute: '2-digit',
    hour12,
  });
}

/**
 * Formats date only (e.g. "Mon, Oct 19, 2026") within a given IANA timezone.
 */
export function formatDateInZone(
  dateInput: string | Date | number | null | undefined,
  timeZone: string
): string {
  return formatInTimeZone(dateInput, timeZone, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
}

/**
 * Returns formatted offset string like "UTC+05:30" or "UTC-04:00" for a given timezone.
 */
export function getTimeZoneOffsetString(timeZone: string, date: Date = new Date()): string {
  if (!isValidIanaTimeZone(timeZone)) return 'UTC';

  try {
    const formatter = new Intl.DateTimeFormat('en-US', {
      timeZone,
      timeZoneName: 'longOffset',
    });
    const parts = formatter.formatToParts(date);
    const tzPart = parts.find((p) => p.type === 'timeZoneName');
    if (tzPart && tzPart.value) {
      return tzPart.value.replace('GMT', 'UTC');
    }
  } catch {
    // Fallback if longOffset unsupported
  }

  return 'UTC';
}

/**
 * Returns short abbreviation like "EST", "EDT", "IST", "UTC" for a given timezone.
 */
export function getTimeZoneAbbreviation(timeZone: string, date: Date = new Date()): string {
  if (!isValidIanaTimeZone(timeZone)) return 'UTC';

  try {
    const formatter = new Intl.DateTimeFormat('en-US', {
      timeZone,
      timeZoneName: 'short',
    });
    const parts = formatter.formatToParts(date);
    const tzPart = parts.find((p) => p.type === 'timeZoneName');
    return tzPart?.value || timeZone;
  } catch {
    return timeZone;
  }
}

/**
 * Checks if two temporal points fall on the exact same calendar day when viewed in a specific timezone.
 */
export function isSameDayInTimeZone(
  dateAInput: string | Date | number | null | undefined,
  dateBInput: string | Date | number | null | undefined,
  timeZone: string
): boolean {
  const a = parseDateSafe(dateAInput);
  const b = parseDateSafe(dateBInput);
  if (!a || !b) return false;

  const validZone = isValidIanaTimeZone(timeZone) ? timeZone : 'UTC';
  const opt: Intl.DateTimeFormatOptions = {
    year: 'numeric',
    month: 'numeric',
    day: 'numeric',
    timeZone: validZone,
  };

  const fmtA = new Intl.DateTimeFormat('en-US', opt).format(a);
  const fmtB = new Intl.DateTimeFormat('en-US', opt).format(b);

  return fmtA === fmtB;
}

/**
 * Standard list of common IANA timezones for selectors and dropdowns.
 */
export interface TimeZoneOption {
  value: string;
  label: string;
  region: string;
}

export const COMMON_IANA_TIMEZONES: TimeZoneOption[] = [
  { value: 'UTC', label: 'Coordinated Universal Time (UTC)', region: 'Global' },
  { value: 'America/New_York', label: 'Eastern Time (US & Canada)', region: 'Americas' },
  { value: 'America/Chicago', label: 'Central Time (US & Canada)', region: 'Americas' },
  { value: 'America/Denver', label: 'Mountain Time (US & Canada)', region: 'Americas' },
  { value: 'America/Los_Angeles', label: 'Pacific Time (US & Canada)', region: 'Americas' },
  { value: 'America/Anchorage', label: 'Alaska Time', region: 'Americas' },
  { value: 'Pacific/Honolulu', label: 'Hawaii Time', region: 'Americas' },
  { value: 'America/Toronto', label: 'Eastern Time (Toronto)', region: 'Americas' },
  { value: 'America/Sao_Paulo', label: 'Brasilia Time (Sao Paulo)', region: 'Americas' },
  { value: 'Europe/London', label: 'Greenwich Mean / British Summer Time (London)', region: 'Europe' },
  { value: 'Europe/Paris', label: 'Central European Time (Paris, Berlin, Rome)', region: 'Europe' },
  { value: 'Europe/Athens', label: 'Eastern European Time (Athens, Helsinki)', region: 'Europe' },
  { value: 'Asia/Dubai', label: 'Gulf Standard Time (Dubai)', region: 'Middle East' },
  { value: 'Asia/Kolkata', label: 'India Standard Time (Kolkata, Mumbai)', region: 'Asia' },
  { value: 'Asia/Singapore', label: 'Singapore Standard Time', region: 'Asia' },
  { value: 'Asia/Tokyo', label: 'Japan Standard Time (Tokyo)', region: 'Asia' },
  { value: 'Australia/Sydney', label: 'Australian Eastern Time (Sydney, Melbourne)', region: 'Oceania' },
  { value: 'Pacific/Auckland', label: 'New Zealand Time (Auckland)', region: 'Oceania' },
];
