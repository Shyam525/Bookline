import { useState, useEffect } from 'react';

/**
 * Phase 40 (Performance): Debounce hook to throttle fast inputs (e.g. search bars)
 * avoiding excessive filtering calculations or repeated API invocations.
 */
export function useDebounce<T>(value: T, delayMs: number = 300): T {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedValue(value);
    }, delayMs);

    return () => {
      clearTimeout(timer);
    };
  }, [value, delayMs]);

  return debouncedValue;
}
