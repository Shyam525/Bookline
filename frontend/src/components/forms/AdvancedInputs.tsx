import React, { useState, useRef, useEffect } from 'react';
import {
  Search,
  Calendar as CalendarIcon,
  Clock,
  ChevronDown,
  X,
  Check,
  DollarSign,
  Phone,
  AlertCircle,
  Command,
} from 'lucide-react';
import { clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

/* =========================================================================
 * 1. SEARCH INPUT (Section 109 & 110)
 * ========================================================================= */
export interface SearchInputProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'onChange'> {
  value: string;
  onChange: (value: string) => void;
  onClear?: () => void;
  shortcutBadge?: string;
  loading?: boolean;
}

export const SearchInput = React.forwardRef<HTMLInputElement, SearchInputProps>(
  ({ value, onChange, onClear, shortcutBadge, loading = false, placeholder = 'Search...', className, disabled, ...props }, ref) => {
    return (
      <div className="relative w-full">
        <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-[#7E88A8] pointer-events-none" />
        <input
          ref={ref}
          type="text"
          value={value}
          disabled={disabled}
          onChange={(e) => onChange(e.target.value)}
          placeholder={placeholder}
          className={twMerge(
            clsx(
              'w-full bg-[#181D2C] border border-[#212638] text-sm text-[#ECEFFE] rounded-xl pl-10 pr-10 py-2.5 transition-colors placeholder:text-[#7E88A8]/60 focus:outline-none focus:border-[#E8546A] focus:ring-1 focus:ring-[#E8546A] disabled:opacity-50 disabled:cursor-not-allowed',
              className
            )
          )}
          {...props}
        />
        <div className="absolute right-3 top-1/2 -translate-y-1/2 flex items-center gap-1.5">
          {loading && (
            <div className="w-3.5 h-3.5 border-2 border-[#E8546A] border-t-transparent rounded-full animate-spin" />
          )}
          {value && !disabled && (
            <button
              type="button"
              onClick={() => {
                onChange('');
                onClear?.();
              }}
              className="p-0.5 text-[#7E88A8] hover:text-white transition-colors"
              aria-label="Clear search"
            >
              <X className="w-3.5 h-3.5" />
            </button>
          )}
          {shortcutBadge && !value && (
            <kbd className="hidden sm:inline-flex items-center px-1.5 py-0.5 text-[10px] font-mono bg-[#111520] border border-[#212638] rounded text-[#7E88A8]">
              {shortcutBadge}
            </kbd>
          )}
        </div>
      </div>
    );
  }
);
SearchInput.displayName = 'SearchInput';

/* =========================================================================
 * 2. MONEY INPUT (Section 109 & 110)
 * ========================================================================= */
export interface MoneyInputProps {
  label?: string;
  value: number | string;
  onChange: (value: number) => void;
  currency?: string;
  error?: string;
  helperText?: string;
  disabled?: boolean;
  min?: number;
  max?: number;
  step?: number;
  placeholder?: string;
}

export const MoneyInput: React.FC<MoneyInputProps> = ({
  label,
  value,
  onChange,
  currency = '₹',
  error,
  helperText,
  disabled = false,
  min = 0,
  max,
  step = 1,
  placeholder = '0.00',
}) => {
  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const raw = e.target.value;
    if (raw === '') {
      onChange(0);
      return;
    }
    const parsed = parseFloat(raw);
    if (!isNaN(parsed)) {
      onChange(parsed);
    }
  };

  return (
    <div className="space-y-1.5 w-full">
      {label && (
        <label className="block text-xs font-semibold text-[#7E88A8] uppercase tracking-wider">
          {label}
        </label>
      )}
      <div className="relative flex items-center">
        <span className="absolute left-3.5 text-[#7E88A8] font-bold text-sm pointer-events-none select-none">
          {currency}
        </span>
        <input
          type="number"
          value={value === 0 ? '' : value}
          onChange={handleChange}
          disabled={disabled}
          min={min}
          max={max}
          step={step}
          placeholder={placeholder}
          className={twMerge(
            clsx(
              'w-full bg-[#181D2C] border text-sm text-[#ECEFFE] font-mono rounded-xl pl-9 pr-4 py-2.5 transition-colors placeholder:text-[#7E88A8]/60 focus:outline-none disabled:opacity-50 disabled:cursor-not-allowed',
              error
                ? 'border-red-500 focus:border-red-500 focus:ring-1 focus:ring-red-500'
                : 'border-[#212638] focus:border-[#E8546A] focus:ring-1 focus:ring-[#E8546A]'
            )
          )}
        />
      </div>
      {error ? (
        <p className="text-xs text-red-500 font-medium">{error}</p>
      ) : helperText ? (
        <p className="text-xs text-[#7E88A8]">{helperText}</p>
      ) : null}
    </div>
  );
};

/* =========================================================================
 * 3. PHONE INPUT (Section 109 & 110)
 * ========================================================================= */
export interface PhoneInputProps {
  label?: string;
  value: string;
  onChange: (value: string) => void;
  countryCode?: string;
  onCountryCodeChange?: (code: string) => void;
  error?: string;
  disabled?: boolean;
}

export const PhoneInput: React.FC<PhoneInputProps> = ({
  label,
  value,
  onChange,
  countryCode = '+91',
  onCountryCodeChange,
  error,
  disabled = false,
}) => {
  const countryCodes = ['+91', '+1', '+44', '+971', '+61', '+65'];

  return (
    <div className="space-y-1.5 w-full">
      {label && (
        <label className="block text-xs font-semibold text-[#7E88A8] uppercase tracking-wider">
          {label}
        </label>
      )}
      <div className="relative flex items-center rounded-xl overflow-hidden border border-[#212638] focus-within:border-[#E8546A] transition-colors">
        <select
          value={countryCode}
          disabled={disabled}
          onChange={(e) => onCountryCodeChange?.(e.target.value)}
          aria-label="Country Calling Code"
          className="bg-[#111520] text-xs font-mono font-semibold text-[#ECEFFE] px-3 py-2.5 border-r border-[#212638] focus:outline-none disabled:opacity-50 cursor-pointer"
        >
          {countryCodes.map((c) => (
            <option key={c} value={c} className="bg-[#111520] text-white">
              {c}
            </option>
          ))}
        </select>
        <div className="relative flex-1 flex items-center">
          <Phone className="absolute left-3 w-4 h-4 text-[#7E88A8] pointer-events-none" />
          <input
            type="tel"
            value={value}
            disabled={disabled}
            onChange={(e) => onChange(e.target.value)}
            placeholder="98765 43210"
            className="w-full bg-[#181D2C] text-sm text-[#ECEFFE] pl-9 pr-4 py-2.5 focus:outline-none disabled:opacity-50"
          />
        </div>
      </div>
      {error && <p className="text-xs text-red-500 font-medium">{error}</p>}
    </div>
  );
};

/* =========================================================================
 * 4. COMBOBOX (SEARCHABLE DROPDOWN)
 * ========================================================================= */
export interface ComboboxOption {
  value: string;
  label: string;
  description?: string;
}

export interface ComboboxProps {
  label?: string;
  options: ComboboxOption[];
  value: string;
  onChange: (val: string) => void;
  placeholder?: string;
  error?: string;
  disabled?: boolean;
}

export const Combobox: React.FC<ComboboxProps> = ({
  label,
  options,
  value,
  onChange,
  placeholder = 'Select option...',
  error,
  disabled = false,
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const [search, setSearch] = useState('');
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleOutsideClick = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleOutsideClick);
    return () => document.removeEventListener('mousedown', handleOutsideClick);
  }, []);

  const selectedOption = options.find((o) => o.value === value);
  const filtered = options.filter((o) =>
    o.label.toLowerCase().includes(search.toLowerCase()) ||
    (o.description && o.description.toLowerCase().includes(search.toLowerCase()))
  );

  return (
    <div ref={containerRef} className="space-y-1.5 w-full relative">
      {label && (
        <label className="block text-xs font-semibold text-[#7E88A8] uppercase tracking-wider">
          {label}
        </label>
      )}
      <button
        type="button"
        disabled={disabled}
        onClick={() => !disabled && setIsOpen(!isOpen)}
        aria-haspopup="listbox"
        aria-expanded={isOpen}
        className={twMerge(
          clsx(
            'w-full bg-[#181D2C] border text-sm text-[#ECEFFE] rounded-xl px-4 py-2.5 flex items-center justify-between text-left transition-colors focus:outline-none disabled:opacity-50 disabled:cursor-not-allowed',
            error ? 'border-red-500' : 'border-[#212638] focus:border-[#E8546A]'
          )
        )}
      >
        <span className={selectedOption ? 'text-white' : 'text-[#7E88A8]'}>
          {selectedOption ? selectedOption.label : placeholder}
        </span>
        <ChevronDown className="w-4 h-4 text-[#7E88A8] shrink-0" />
      </button>

      {isOpen && (
        <div
          role="listbox"
          className="absolute left-0 mt-2 w-full bg-[#111520] border border-[#212638] rounded-2xl shadow-2xl z-50 p-2 space-y-1 animate-fadeIn max-h-60 overflow-y-auto"
        >
          <div className="relative mb-2">
            <Search className="w-3.5 h-3.5 text-[#7E88A8] absolute left-2.5 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Filter options..."
              className="w-full bg-[#181D2C] border border-[#212638] rounded-xl pl-8 pr-3 py-1.5 text-xs text-white placeholder-[#7E88A8] focus:outline-none focus:border-[#E8546A]"
            />
          </div>

          {filtered.length === 0 ? (
            <div className="p-3 text-center text-xs text-[#7E88A8]">No matching options</div>
          ) : (
            filtered.map((opt) => (
              <button
                key={opt.value}
                type="button"
                role="option"
                aria-selected={opt.value === value}
                onClick={() => {
                  onChange(opt.value);
                  setIsOpen(false);
                  setSearch('');
                }}
                className={`w-full flex items-center justify-between px-3 py-2 rounded-xl text-xs text-left transition-colors ${
                  opt.value === value
                    ? 'bg-[#181D2C] text-[#E8546A] font-semibold'
                    : 'text-[#ECEFFE] hover:bg-[#181D2C]/60'
                }`}
              >
                <div>
                  <div>{opt.label}</div>
                  {opt.description && (
                    <div className="text-[10px] text-[#7E88A8]">{opt.description}</div>
                  )}
                </div>
                {opt.value === value && <Check className="w-3.5 h-3.5 text-[#E8546A] shrink-0" />}
              </button>
            ))
          )}
        </div>
      )}
      {error && <p className="text-xs text-red-500 font-medium">{error}</p>}
    </div>
  );
};

/* =========================================================================
 * 5. DATE PICKER
 * ========================================================================= */
export interface DatePickerProps {
  label?: string;
  value: string; // YYYY-MM-DD
  onChange: (date: string) => void;
  minDate?: string;
  maxDate?: string;
  error?: string;
  disabled?: boolean;
}

export const DatePicker: React.FC<DatePickerProps> = ({
  label,
  value,
  onChange,
  minDate,
  maxDate,
  error,
  disabled = false,
}) => {
  return (
    <div className="space-y-1.5 w-full">
      {label && (
        <label className="block text-xs font-semibold text-[#7E88A8] uppercase tracking-wider">
          {label}
        </label>
      )}
      <div className="relative flex items-center">
        <CalendarIcon className="absolute left-3.5 text-[#7E88A8] w-4 h-4 pointer-events-none" />
        <input
          type="date"
          value={value}
          min={minDate}
          max={maxDate}
          disabled={disabled}
          onChange={(e) => onChange(e.target.value)}
          className={twMerge(
            clsx(
              'w-full bg-[#181D2C] border text-sm text-[#ECEFFE] rounded-xl pl-10 pr-4 py-2.5 transition-colors focus:outline-none disabled:opacity-50 cursor-pointer',
              error ? 'border-red-500' : 'border-[#212638] focus:border-[#E8546A]'
            )
          )}
        />
      </div>
      {error && <p className="text-xs text-red-500 font-medium">{error}</p>}
    </div>
  );
};

/* =========================================================================
 * 6. TIME PICKER
 * ========================================================================= */
export interface TimePickerProps {
  label?: string;
  value: string; // HH:mm
  onChange: (time: string) => void;
  stepMinutes?: number;
  startHour?: number;
  endHour?: number;
  error?: string;
  disabled?: boolean;
}

export const TimePicker: React.FC<TimePickerProps> = ({
  label,
  value,
  onChange,
  stepMinutes = 30,
  startHour = 8,
  endHour = 20,
  error,
  disabled = false,
}) => {
  // Generate slot increments
  const timeSlots: string[] = [];
  for (let h = startHour; h <= endHour; h++) {
    for (let m = 0; m < 60; m += stepMinutes) {
      const hh = h.toString().padStart(2, '0');
      const mm = m.toString().padStart(2, '0');
      timeSlots.push(`${hh}:${mm}`);
    }
  }

  return (
    <div className="space-y-1.5 w-full">
      {label && (
        <label className="block text-xs font-semibold text-[#7E88A8] uppercase tracking-wider">
          {label}
        </label>
      )}
      <div className="relative flex items-center">
        <Clock className="absolute left-3.5 text-[#7E88A8] w-4 h-4 pointer-events-none" />
        <select
          value={value}
          disabled={disabled}
          onChange={(e) => onChange(e.target.value)}
          className={twMerge(
            clsx(
              'w-full bg-[#181D2C] border text-sm text-[#ECEFFE] font-mono rounded-xl pl-10 pr-4 py-2.5 transition-colors focus:outline-none disabled:opacity-50 cursor-pointer',
              error ? 'border-red-500' : 'border-[#212638] focus:border-[#E8546A]'
            )
          )}
        >
          <option value="" disabled>Select Time</option>
          {timeSlots.map((slot) => {
            const [hStr, mStr] = slot.split(':');
            const hNum = parseInt(hStr, 10);
            const ampm = hNum >= 12 ? 'PM' : 'AM';
            const displayH = hNum % 12 || 12;
            const displayTime = `${displayH}:${mStr} ${ampm}`;
            return (
              <option key={slot} value={slot} className="bg-[#111520] text-white">
                {displayTime} ({slot})
              </option>
            );
          })}
        </select>
      </div>
      {error && <p className="text-xs text-red-500 font-medium">{error}</p>}
    </div>
  );
};
