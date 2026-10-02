import React from 'react';
import { Loader2 } from 'lucide-react';
import { clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger';
  size?: 'sm' | 'md' | 'lg';
  isLoading?: boolean;
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
}

export const Button: React.FC<ButtonProps> = ({
  children,
  variant = 'primary',
  size = 'md',
  isLoading = false,
  leftIcon,
  rightIcon,
  className,
  disabled,
  ...props
}) => {
  const baseStyles = 'inline-flex items-center justify-center font-medium transition-colors rounded-xl focus:outline-none focus:ring-2 focus:ring-[#E8546A] focus:ring-offset-2 focus:ring-offset-[#0A0C13] disabled:opacity-50 disabled:cursor-not-allowed select-none';

  const variants = {
    primary: 'bg-[#E8546A] hover:bg-[#D44359] text-white shadow-lg shadow-[#E8546A]/20',
    secondary: 'bg-[#181D2C] hover:bg-[#212638] text-[#ECEFFE] border border-[#212638]',
    outline: 'border border-[#212638] hover:bg-[#181D2C] text-[#ECEFFE]',
    ghost: 'hover:bg-[#181D2C] text-[#7E88A8] hover:text-[#ECEFFE]',
    danger: 'bg-red-600 hover:bg-red-700 text-white shadow-lg shadow-red-600/20',
  };

  const sizes = {
    sm: 'text-xs px-3 py-1.5 gap-1.5',
    md: 'text-sm px-4 py-2.5 gap-2',
    lg: 'text-base px-6 py-3 gap-2.5',
  };

  return (
    <button
      className={twMerge(clsx(baseStyles, variants[variant], sizes[size], className))}
      disabled={disabled || isLoading}
      {...props}
    >
      {isLoading ? (
        <Loader2 className="w-4 h-4 animate-spin text-current" />
      ) : (
        leftIcon
      )}
      <span>{children}</span>
      {!isLoading && rightIcon}
    </button>
  );
};

export interface IconButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  icon: React.ReactNode;
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger';
  size?: 'sm' | 'md' | 'lg';
  isLoading?: boolean;
}

export const IconButton: React.FC<IconButtonProps> = ({
  icon,
  variant = 'ghost',
  size = 'md',
  isLoading = false,
  className,
  disabled,
  ...props
}) => {
  const sizes = {
    sm: 'p-1.5 text-xs',
    md: 'p-2 text-sm',
    lg: 'p-3 text-base',
  };

  return (
    <button
      className={twMerge(
        clsx(
          'inline-flex items-center justify-center rounded-xl transition-colors focus:outline-none focus:ring-2 focus:ring-[#E8546A] disabled:opacity-50 disabled:cursor-not-allowed',
          variant === 'primary' && 'bg-[#E8546A] hover:bg-[#D44359] text-white',
          variant === 'secondary' && 'bg-[#181D2C] hover:bg-[#212638] text-[#ECEFFE] border border-[#212638]',
          variant === 'outline' && 'border border-[#212638] hover:bg-[#181D2C] text-[#ECEFFE]',
          variant === 'ghost' && 'hover:bg-[#181D2C] text-[#7E88A8] hover:text-[#ECEFFE]',
          variant === 'danger' && 'bg-red-600 hover:bg-red-700 text-white',
          sizes[size],
          className
        )
      )}
      disabled={disabled || isLoading}
      {...props}
    >
      {isLoading ? <Loader2 className="w-4 h-4 animate-spin text-current" /> : icon}
    </button>
  );
};

export const ButtonGroup: React.FC<{ children: React.ReactNode; className?: string }> = ({ children, className }) => {
  return (
    <div className={twMerge('inline-flex rounded-xl shadow-sm border border-[#212638] p-1 bg-[#111520] gap-1', className)}>
      {children}
    </div>
  );
};
