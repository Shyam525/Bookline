import React, { useState } from 'react';
import { AlertCircle, Copy, Check, X } from 'lucide-react';
import { clsx } from 'clsx';
import { getLocalizedError, ApiError } from '../../utils/apiErrors';

export interface ApiErrorAlertProps {
  error: unknown;
  onDismiss?: () => void;
  className?: string;
  showTraceId?: boolean;
}

export const ApiErrorAlert: React.FC<ApiErrorAlertProps> = ({
  error,
  onDismiss,
  className,
  showTraceId = true,
}) => {
  const [copied, setCopied] = useState(false);

  if (!error) return null;

  const { code, message, traceId, fieldErrors } = getLocalizedError(error);

  const handleCopyTraceId = () => {
    if (!traceId) return;
    navigator.clipboard.writeText(traceId).then(() => {
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    });
  };

  const hasFieldErrors = fieldErrors && Object.keys(fieldErrors).length > 0;

  return (
    <div
      role="alert"
      className={clsx(
        'rounded-xl border border-red-500/30 bg-red-950/20 p-4 text-red-200 backdrop-blur-md shadow-lg',
        className
      )}
    >
      <div className="flex items-start gap-3">
        <AlertCircle className="w-5 h-5 text-red-400 shrink-0 mt-0.5" />
        <div className="flex-1 space-y-2 min-w-0">
          <div className="flex items-center justify-between gap-2">
            <span className="text-xs font-semibold uppercase tracking-wider text-red-400">
              {code.replace(/_/g, ' ')}
            </span>
            {onDismiss && (
              <button
                type="button"
                onClick={onDismiss}
                className="text-red-400/70 hover:text-red-300 p-0.5 rounded transition-colors"
                aria-label="Dismiss error"
              >
                <X className="w-4 h-4" />
              </button>
            )}
          </div>

          <p className="text-sm text-red-100 font-medium leading-relaxed">{message}</p>

          {/* Validation Field Errors */}
          {hasFieldErrors && (
            <ul className="mt-2 space-y-1 text-xs text-red-300/90 list-disc list-inside bg-red-900/20 p-2.5 rounded-lg border border-red-500/20">
              {Object.entries(fieldErrors).map(([field, errors]) => (
                <li key={field}>
                  <strong className="capitalize">{field}</strong>: {errors.join(', ')}
                </li>
              ))}
            </ul>
          )}

          {/* Correlation ID diagnostic badge */}
          {showTraceId && traceId && (
            <div className="pt-1 flex items-center gap-2 text-[11px] text-red-300/60">
              <span>Ref:</span>
              <code className="bg-black/30 px-1.5 py-0.5 rounded font-mono text-red-300/80">
                {traceId}
              </code>
              <button
                type="button"
                onClick={handleCopyTraceId}
                className="inline-flex items-center gap-1 hover:text-red-200 transition-colors p-0.5"
                title="Copy reference ID for support"
                aria-label="Copy reference ID"
              >
                {copied ? <Check className="w-3 h-3 text-green-400" /> : <Copy className="w-3 h-3" />}
                <span className="text-[10px]">{copied ? 'Copied' : 'Copy'}</span>
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
