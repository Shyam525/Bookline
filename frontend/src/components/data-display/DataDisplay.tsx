import React from 'react';
import { TrendingUp, TrendingDown, AlertTriangle, Inbox, HelpCircle } from 'lucide-react';
import { clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

export const Card: React.FC<{ children: React.ReactNode; className?: string }> = ({ children, className }) => {
  return (
    <div className={twMerge('bg-[#111520] border border-[#212638] rounded-2xl p-6 shadow-xl', className)}>
      {children}
    </div>
  );
};

export interface MetricCardProps {
  label: string;
  metric: string | number;
  comparison?: string;
  period?: string;
  trend?: 'up' | 'down' | 'neutral';
  icon?: React.ReactNode;
  tooltip?: string;
  isLoading?: boolean;
}

export const MetricCard: React.FC<MetricCardProps> = ({
  label,
  metric,
  comparison,
  period,
  trend = 'up',
  icon,
  tooltip,
  isLoading = false,
}) => {
  if (isLoading) {
    return (
      <Card className="animate-pulse space-y-3">
        <div className="h-3 w-24 bg-[#181D2C] rounded" />
        <div className="h-8 w-16 bg-[#181D2C] rounded" />
        <div className="h-3 w-32 bg-[#181D2C] rounded" />
      </Card>
    );
  }

  return (
    <Card className="space-y-3 relative group">
      <div className="flex items-center justify-between text-[#7E88A8]">
        <div className="flex items-center gap-1.5">
          <span className="text-xs font-semibold uppercase tracking-wider">{label}</span>
          {tooltip && (
            <div className="relative cursor-help" title={tooltip}>
              <HelpCircle className="w-3.5 h-3.5 text-[#7E88A8]/60 hover:text-[#ECEFFE] transition-colors" />
            </div>
          )}
        </div>
        {icon && <div className="text-[#E8546A]">{icon}</div>}
      </div>
      <p className="font-heading text-3xl font-bold text-[#ECEFFE] tracking-tight">{metric}</p>
      {(comparison || period) && (
        <div className="flex flex-wrap items-center gap-1.5 text-xs font-medium">
          {comparison && (
            <>
              {trend === 'up' && (
                <span className="text-[#34D399] flex items-center gap-1">
                  <TrendingUp className="w-3.5 h-3.5" /> {comparison}
                </span>
              )}
              {trend === 'down' && (
                <span className="text-red-400 flex items-center gap-1">
                  <TrendingDown className="w-3.5 h-3.5" /> {comparison}
                </span>
              )}
              {trend === 'neutral' && <span className="text-[#7E88A8]">{comparison}</span>}
            </>
          )}
          {period && (
            <span className="text-[#7E88A8]/70 text-[11px] font-normal">
              · {period}
            </span>
          )}
        </div>
      )}
    </Card>
  );
};

export const Badge: React.FC<{
  children: React.ReactNode;
  variant?: 'primary' | 'success' | 'warning' | 'neutral' | 'danger';
  className?: string;
}> = ({ children, variant = 'neutral', className }) => {
  const variants = {
    primary: 'bg-[#E8546A]/10 text-[#E8546A] border-[#E8546A]/20',
    success: 'bg-[#34D399]/10 text-[#34D399] border-[#34D399]/20',
    warning: 'bg-[#FBBF24]/10 text-[#FBBF24] border-[#FBBF24]/20',
    neutral: 'bg-[#181D2C] text-[#7E88A8] border-[#212638]',
    danger: 'bg-red-500/10 text-red-400 border-red-500/20',
  };

  return (
    <span className={twMerge(clsx('inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-mono font-medium border', variants[variant], className))}>
      {children}
    </span>
  );
};

export type AppointmentStatus = 'Free' | 'Held' | 'Booked' | 'Pending' | 'Confirmed' | 'CheckedIn' | 'Completed' | 'Cancelled' | 'NoShow';

export const StatusBadge: React.FC<{ status: AppointmentStatus }> = ({ status }) => {
  const config: Record<AppointmentStatus, { label: string; colorClass: string }> = {
    Free: { label: 'Free Slot', colorClass: 'bg-[#34D399]/10 text-[#34D399] border-[#34D399]/30' },
    Held: { label: 'Held (5m)', colorClass: 'bg-[#FBBF24]/10 text-[#FBBF24] border-[#FBBF24]/30' },
    Booked: { label: 'Booked', colorClass: 'bg-[#64748B]/10 text-[#64748B] border-[#64748B]/30' },
    Pending: { label: 'Pending', colorClass: 'bg-[#FBBF24]/10 text-[#FBBF24] border-[#FBBF24]/30' },
    Confirmed: { label: 'Confirmed', colorClass: 'bg-[#34D399]/10 text-[#34D399] border-[#34D399]/30' },
    CheckedIn: { label: 'Checked In', colorClass: 'bg-[#3B82F6]/10 text-[#3B82F6] border-[#3B82F6]/30' },
    Completed: { label: 'Completed', colorClass: 'bg-purple-500/10 text-purple-400 border-purple-500/30' },
    Cancelled: { label: 'Cancelled', colorClass: 'bg-red-500/10 text-red-400 border-red-500/30' },
    NoShow: { label: 'No Show', colorClass: 'bg-gray-500/10 text-gray-400 border-gray-500/30' },
  };

  const current = config[status] || { label: status, colorClass: 'bg-[#181D2C] text-[#7E88A8] border-[#212638]' };

  return (
    <span className={clsx('inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-xs font-medium border', current.colorClass)}>
      <span className="w-1.5 h-1.5 rounded-full bg-current" />
      {current.label}
    </span>
  );
};

export const Avatar: React.FC<{ name: string; size?: 'sm' | 'md' | 'lg' }> = ({ name, size = 'md' }) => {
  const initials = name
    .split(' ')
    .map((n) => n[0])
    .join('')
    .substring(0, 2)
    .toUpperCase();

  const sizes = {
    sm: 'w-7 h-7 text-xs',
    md: 'w-9 h-9 text-sm',
    lg: 'w-12 h-12 text-base',
  };

  return (
    <div className={clsx('rounded-full bg-[#181D2C] border border-[#212638] flex items-center justify-center font-semibold text-[#ECEFFE] select-none', sizes[size])}>
      {initials}
    </div>
  );
};

export const Skeleton: React.FC<{ className?: string }> = ({ className }) => {
  return <div className={twMerge('bg-[#181D2C] animate-pulse rounded-lg', className)} />;
};

export const EmptyState: React.FC<{ title: string; description: string; action?: React.ReactNode }> = ({
  title,
  description,
  action,
}) => {
  return (
    <div className="flex flex-col items-center justify-center text-center p-12 bg-[#111520] border border-[#212638] rounded-2xl space-y-4">
      <div className="w-12 h-12 rounded-full bg-[#181D2C] flex items-center justify-center text-[#7E88A8]">
        <Inbox className="w-6 h-6" />
      </div>
      <div className="space-y-1 max-w-sm">
        <h4 className="font-heading text-lg font-semibold text-[#ECEFFE]">{title}</h4>
        <p className="text-sm text-[#7E88A8]">{description}</p>
      </div>
      {action && <div className="pt-2">{action}</div>}
    </div>
  );
};

export const ErrorState: React.FC<{ title?: string; message: string; onRetry?: () => void }> = ({
  title = 'Something went wrong',
  message,
  onRetry,
}) => {
  return (
    <div className="flex flex-col items-center justify-center text-center p-8 bg-red-500/5 border border-red-500/20 rounded-2xl space-y-4">
      <div className="w-12 h-12 rounded-full bg-red-500/10 flex items-center justify-center text-red-400">
        <AlertTriangle className="w-6 h-6" />
      </div>
      <div className="space-y-1 max-w-sm">
        <h4 className="font-heading text-lg font-semibold text-white">{title}</h4>
        <p className="text-sm text-red-300">{message}</p>
      </div>
      {onRetry && (
        <button
          onClick={onRetry}
          className="px-4 py-2 bg-red-600 hover:bg-red-700 text-white font-medium text-sm rounded-xl transition-colors"
        >
          Try Again
        </button>
      )}
    </div>
  );
};

export * from '../common/ProviderImage';

