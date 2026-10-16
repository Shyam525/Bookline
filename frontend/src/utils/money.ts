/**
 * Section 86: Decimal-safe and Currency-aware Financial Formatting & Calculations
 * Prevents floating-point drift, distinguishes absent vs zero amounts,
 * and respects ISO 4217 currency scales (e.g. 0 decimals for JPY, 3 for BHD, 2 for USD/EUR/INR).
 */

export type SupportedCurrency =
  | 'USD'
  | 'EUR'
  | 'GBP'
  | 'INR'
  | 'CAD'
  | 'AUD'
  | 'SGD'
  | 'AED'
  | 'JPY'
  | 'KRW'
  | 'VND'
  | 'BHD'
  | 'KWD'
  | 'OMR'
  | 'CHF'
  | 'SEK'
  | 'NZD';

export interface CurrencyMetadata {
  code: SupportedCurrency;
  scale: number;
  symbol: string;
  name: string;
}

export const CURRENCY_REGISTRY: Record<string, CurrencyMetadata> = {
  USD: { code: 'USD', scale: 2, symbol: '$', name: 'US Dollar' },
  EUR: { code: 'EUR', scale: 2, symbol: '€', name: 'Euro' },
  GBP: { code: 'GBP', scale: 2, symbol: '£', name: 'British Pound' },
  INR: { code: 'INR', scale: 2, symbol: '₹', name: 'Indian Rupee' },
  CAD: { code: 'CAD', scale: 2, symbol: 'CA$', name: 'Canadian Dollar' },
  AUD: { code: 'AUD', scale: 2, symbol: 'A$', name: 'Australian Dollar' },
  SGD: { code: 'SGD', scale: 2, symbol: 'S$', name: 'Singapore Dollar' },
  AED: { code: 'AED', scale: 2, symbol: 'AED', name: 'UAE Dirham' },
  CHF: { code: 'CHF', scale: 2, symbol: 'CHF', name: 'Swiss Franc' },
  SEK: { code: 'SEK', scale: 2, symbol: 'kr', name: 'Swedish Krona' },
  NZD: { code: 'NZD', scale: 2, symbol: 'NZ$', name: 'New Zealand Dollar' },

  // Zero-decimal currencies
  JPY: { code: 'JPY', scale: 0, symbol: '¥', name: 'Japanese Yen' },
  KRW: { code: 'KRW', scale: 0, symbol: '₩', name: 'South Korean Won' },
  VND: { code: 'VND', scale: 0, symbol: '₫', name: 'Vietnamese Dong' },

  // Three-decimal currencies
  BHD: { code: 'BHD', scale: 3, symbol: 'BD', name: 'Bahraini Dinar' },
  KWD: { code: 'KWD', scale: 3, symbol: 'KD', name: 'Kuwaiti Dinar' },
  OMR: { code: 'OMR', scale: 3, symbol: 'OMR', name: 'Omani Rial' },
};

/**
 * Normalizes a currency code to uppercase ISO standard, falling back to USD.
 */
export function normalizeCurrency(currency?: string | null): string {
  if (!currency || !currency.trim()) return 'USD';
  const upper = currency.trim().toUpperCase();
  return CURRENCY_REGISTRY[upper] ? upper : upper;
}

/**
 * Returns the minor-unit decimal scale for a given currency code.
 */
export function getCurrencyScale(currency?: string | null): number {
  const code = normalizeCurrency(currency);
  return CURRENCY_REGISTRY[code]?.scale ?? 2;
}

/**
 * Performs decimal-safe rounding using exact decimal scaling to prevent float representation drift.
 */
export function roundToCurrencyScale(amount: number, currency?: string | null): number {
  const scale = getCurrencyScale(currency);
  const factor = Math.pow(10, scale);
  return Math.round((amount + Number.EPSILON) * factor) / factor;
}

export interface FormatMoneyOptions {
  showCode?: boolean;
  locale?: string;
  absentPlaceholder?: string;
}

/**
 * Formats a monetary amount adhering to Section 86:
 * - Distinguishes absent amounts (null/undefined) from zero amounts (0).
 * - Formats with currency-specific scale (e.g. 0 decimals for JPY, 2 for USD, 3 for BHD).
 */
export function formatMoney(
  amount: number | string | null | undefined,
  currency: string = 'USD',
  options?: FormatMoneyOptions
): string {
  if (amount === null || amount === undefined || amount === '') {
    return options?.absentPlaceholder ?? '—';
  }

  const num = typeof amount === 'string' ? parseFloat(amount) : amount;
  if (isNaN(num)) {
    return options?.absentPlaceholder ?? '—';
  }

  const code = normalizeCurrency(currency);
  const scale = getCurrencyScale(code);
  const rounded = roundToCurrencyScale(num, code);
  const locale = options?.locale || 'en-US';

  try {
    const formatter = new Intl.NumberFormat(locale, {
      style: 'currency',
      currency: code,
      minimumFractionDigits: scale,
      maximumFractionDigits: scale,
    });

    const formatted = formatter.format(rounded);
    if (options?.showCode && !formatted.includes(code)) {
      return `${formatted} ${code}`;
    }
    return formatted;
  } catch {
    // Fallback if browser locale doesn't support the currency
    const symbol = CURRENCY_REGISTRY[code]?.symbol || code;
    const fixed = rounded.toFixed(scale);
    return options?.showCode ? `${symbol}${fixed} ${code}` : `${symbol}${fixed}`;
  }
}

/**
 * Converts a major units decimal amount to integer minor-units (e.g., cents, paise, yen).
 */
export function toMinorUnits(amount: number, currency: string = 'USD'): number {
  const scale = getCurrencyScale(currency);
  const factor = Math.pow(10, scale);
  return Math.round(amount * factor);
}

/**
 * Converts integer minor-units (e.g., cents, paise) back to a major unit amount.
 */
export function fromMinorUnits(minorUnits: number, currency: string = 'USD'): number {
  const scale = getCurrencyScale(currency);
  const factor = Math.pow(10, scale);
  return minorUnits / factor;
}

/**
 * Adds multiple monetary amounts of identical currency with decimal-safe precision.
 * Throws if currencies do not match.
 */
export function addMoney(
  items: Array<{ amount: number; currency: string }>,
  targetCurrency?: string
): { amount: number; currency: string } {
  if (items.length === 0) {
    const cur = normalizeCurrency(targetCurrency);
    return { amount: 0, currency: cur };
  }

  const baseCurrency = normalizeCurrency(targetCurrency || items[0].currency);

  let total = 0;
  for (const item of items) {
    const itemCur = normalizeCurrency(item.currency);
    if (itemCur !== baseCurrency) {
      throw new Error(
        `Currency mismatch: cannot sum '${itemCur}' with '${baseCurrency}'. Cross-currency operations require conversion.`
      );
    }
    total += item.amount;
  }

  return {
    amount: roundToCurrencyScale(total, baseCurrency),
    currency: baseCurrency,
  };
}
