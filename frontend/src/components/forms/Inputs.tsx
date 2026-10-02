import React from 'react';
import { Search } from 'lucide-react';
import { clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  helperText?: string;
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
}

export const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, helperText, leftIcon, rightIcon, className, id, disabled, ...props }, ref) => {
    const inputId = id || (label ? label.toLowerCase().replace(/\s+/g, '-') : undefined);

    return (
      <div className="space-y-1.5 w-full">
        {label && (
          <label htmlFor={inputId} className="block text-xs font-semibold text-[#7E88A8] uppercase tracking-wider">
            {label}
          </label>
        )}
        <div className="relative flex items-center">
          {leftIcon && <div className="absolute left-3.5 text-[#7E88A8] pointer-events-none">{leftIcon}</div>}
          <input
            ref={ref}
            id={inputId}
            disabled={disabled}
            className={twMerge(
              clsx(
                'w-full bg-[#181D2C] border text-sm text-[#ECEFFE] rounded-xl px-4 py-2.5 transition-colors placeholder:text-[#7E88A8]/60 focus:outline-none disabled:opacity-50 disabled:cursor-not-allowed',
                error
                  ? 'border-red-500 focus:border-red-500 focus:ring-1 focus:ring-red-500'
                  : 'border-[#212638] focus:border-[#E8546A] focus:ring-1 focus:ring-[#E8546A]',
                leftIcon && 'pl-10',
                rightIcon && 'pr-10',
                className
              )
            )}
            {...props}
          />
          {rightIcon && <div className="absolute right-3.5 text-[#7E88A8] pointer-events-none">{rightIcon}</div>}
        </div>
        {error ? (
          <p className="text-xs text-red-500 font-medium">{error}</p>
        ) : helperText ? (
          <p className="text-xs text-[#7E88A8]">{helperText}</p>
        ) : null}
      </div>
    );
  }
);
Input.displayName = 'Input';

export interface TextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: string;
  error?: string;
  helperText?: string;
}

export const Textarea = React.forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ label, error, helperText, className, id, disabled, ...props }, ref) => {
    const inputId = id || (label ? label.toLowerCase().replace(/\s+/g, '-') : undefined);

    return (
      <div className="space-y-1.5 w-full">
        {label && (
          <label htmlFor={inputId} className="block text-xs font-semibold text-[#7E88A8] uppercase tracking-wider">
            {label}
          </label>
        )}
        <textarea
          ref={ref}
          id={inputId}
          disabled={disabled}
          className={twMerge(
            clsx(
              'w-full bg-[#181D2C] border text-sm text-[#ECEFFE] rounded-xl px-4 py-2.5 transition-colors placeholder:text-[#7E88A8]/60 focus:outline-none disabled:opacity-50 disabled:cursor-not-allowed min-h-[100px]',
              error
                ? 'border-red-500 focus:border-red-500'
                : 'border-[#212638] focus:border-[#E8546A]',
              className
            )
          )}
          {...props}
        />
        {error ? (
          <p className="text-xs text-red-500 font-medium">{error}</p>
        ) : helperText ? (
          <p className="text-xs text-[#7E88A8]">{helperText}</p>
        ) : null}
      </div>
    );
  }
);
Textarea.displayName = 'Textarea';

export interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  label?: string;
  error?: string;
  options: Array<{ value: string; label: string }>;
}

export const Select = React.forwardRef<HTMLSelectElement, SelectProps>(
  ({ label, error, options, className, id, disabled, ...props }, ref) => {
    const inputId = id || (label ? label.toLowerCase().replace(/\s+/g, '-') : undefined);

    return (
      <div className="space-y-1.5 w-full">
        {label && (
          <label htmlFor={inputId} className="block text-xs font-semibold text-[#7E88A8] uppercase tracking-wider">
            {label}
          </label>
        )}
        <select
          ref={ref}
          id={inputId}
          disabled={disabled}
          className={twMerge(
            clsx(
              'w-full bg-[#181D2C] border text-sm text-[#ECEFFE] rounded-xl px-4 py-2.5 transition-colors focus:outline-none disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer',
              error ? 'border-red-500' : 'border-[#212638] focus:border-[#E8546A]',
              className
            )
          )}
          {...props}
        >
          {options.map((opt) => (
            <option key={opt.value} value={opt.value} className="bg-[#111520] text-[#ECEFFE]">
              {opt.label}
            </option>
          ))}
        </select>
        {error && <p className="text-xs text-red-500 font-medium">{error}</p>}
      </div>
    );
  }
);
Select.displayName = 'Select';

export const SearchBox: React.FC<{ value?: string; onChange?: (val: string) => void; placeholder?: string }> = ({
  value,
  onChange,
  placeholder = 'Search appointments, customers, staff...',
}) => {
  return (
    <div className="relative w-full max-w-md">
      <Search className="absolute left-3.5 top-3 w-4 h-4 text-[#7E88A8]" />
      <input
        type="text"
        value={value}
        onChange={(e) => onChange?.(e.target.value)}
        placeholder={placeholder}
        className="w-full bg-[#181D2C] border border-[#212638] text-sm text-[#ECEFFE] rounded-xl pl-10 pr-4 py-2 focus:outline-none focus:border-[#E8546A] placeholder:text-[#7E88A8]"
      />
    </div>
  );
};

export const Switch: React.FC<{ checked: boolean; onChange: (checked: boolean) => void; label?: string; disabled?: boolean }> = ({
  checked,
  onChange,
  label,
  disabled = false,
}) => {
  return (
    <label className={clsx("inline-flex items-center gap-3 select-none cursor-pointer", disabled && "opacity-50 cursor-not-allowed")}>
      <button
        type="button"
        role="switch"
        aria-checked={checked}
        disabled={disabled}
        onClick={() => !disabled && onChange(!checked)}
        className={clsx(
          "w-11 h-6 rounded-full transition-colors p-0.5 focus:outline-none focus:ring-2 focus:ring-[#E8546A]",
          checked ? "bg-[#E8546A]" : "bg-[#212638]"
        )}
      >
        <div
          className={clsx(
            "w-5 h-5 rounded-full bg-white transition-transform transform shadow-md",
            checked ? "translate-x-5" : "translate-x-0"
          )}
        />
      </button>
      {label && <span className="text-sm font-medium text-[#ECEFFE]">{label}</span>}
    </label>
  );
};
