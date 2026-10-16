import { test, describe } from 'node:test';
import assert from 'node:assert/strict';
import {
  formatMoney,
  getCurrencyScale,
  roundToCurrencyScale,
  toMinorUnits,
  fromMinorUnits,
  addMoney,
  normalizeCurrency,
} from './money.ts';

describe('Section 86: Financial Correctness & Decimal-Safe Money Formatting', () => {
  test('accurately identifies currency scale for standard, zero-decimal, and three-decimal currencies', () => {
    assert.equal(getCurrencyScale('USD'), 2);
    assert.equal(getCurrencyScale('EUR'), 2);
    assert.equal(getCurrencyScale('INR'), 2);
    assert.equal(getCurrencyScale('JPY'), 0);
    assert.equal(getCurrencyScale('KRW'), 0);
    assert.equal(getCurrencyScale('BHD'), 3);
    assert.equal(getCurrencyScale('KWD'), 3);
  });

  test('distinguishes absent amounts from zero amounts', () => {
    // Absent amounts
    assert.equal(formatMoney(null, 'USD'), '—');
    assert.equal(formatMoney(undefined, 'USD'), '—');
    assert.equal(formatMoney(null, 'USD', { absentPlaceholder: 'N/A' }), 'N/A');

    // Zero amounts
    const zeroUsd = formatMoney(0, 'USD');
    assert.ok(zeroUsd.includes('0.00'), `Expected formatted zero to contain 0.00, got ${zeroUsd}`);

    const zeroJpy = formatMoney(0, 'JPY');
    assert.ok(zeroJpy.includes('0') && !zeroJpy.includes('.'), `Expected JPY zero to have no decimals, got ${zeroJpy}`);
  });

  test('formats currencies with appropriate scale', () => {
    const usd = formatMoney(49.99, 'USD');
    assert.ok(usd.includes('49.99'));

    const jpy = formatMoney(1500.85, 'JPY'); // rounds to 1501
    assert.ok(jpy.includes('1,501') || jpy.includes('1501'));
  });

  test('converts to and from integer minor units accurately', () => {
    // 2-decimal USD
    assert.equal(toMinorUnits(12.34, 'USD'), 1234);
    assert.equal(fromMinorUnits(1234, 'USD'), 12.34);

    // 0-decimal JPY
    assert.equal(toMinorUnits(500, 'JPY'), 500);
    assert.equal(fromMinorUnits(500, 'JPY'), 500);

    // 3-decimal BHD
    assert.equal(toMinorUnits(1.234, 'BHD'), 1234);
    assert.equal(fromMinorUnits(1234, 'BHD'), 1.234);
  });

  test('sums same-currency amounts without float drift', () => {
    const items = [
      { amount: 0.1, currency: 'USD' },
      { amount: 0.2, currency: 'USD' },
    ];
    const total = addMoney(items);
    assert.equal(total.amount, 0.3); // avoids 0.30000000000000004
    assert.equal(total.currency, 'USD');
  });

  test('rejects cross-currency summation without conversion', () => {
    const items = [
      { amount: 10, currency: 'USD' },
      { amount: 10, currency: 'EUR' },
    ];
    assert.throws(() => addMoney(items), /Currency mismatch/);
  });
});
