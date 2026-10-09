import React from 'react';

export interface ScheduleErrorDefenseProps {
  onRetry?: () => void;
  message?: string;
  className?: string;
}

/**
 * Specification Section 58: INVALID DATE DEFENSE
 * NEVER render:
 * Invalid Date
 * undefined
 * null
 * NaN
 * raw timestamp
 *
 * If temporal data is invalid:
 * show:
 * Unable to load schedule.
 * [Retry]
 */
export const ScheduleErrorDefense: React.FC<ScheduleErrorDefenseProps> = ({
  onRetry,
  message = 'Unable to load schedule.',
  className = '',
}) => {
  return (
    <div
      className={`p-4 rounded-xl border border-red-500/30 bg-red-500/10 text-center space-y-2.5 ${className}`}
    >
      <p className="text-xs text-red-300 font-semibold">{message}</p>
      {onRetry && (
        <button
          type="button"
          onClick={onRetry}
          className="px-3 py-1.5 bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-xs font-bold text-white rounded-lg transition-colors shadow-sm"
        >
          Retry
        </button>
      )}
    </div>
  );
};
