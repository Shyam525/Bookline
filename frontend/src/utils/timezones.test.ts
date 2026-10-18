import { test, describe } from 'node:test';
import assert from 'node:assert/strict';
import {
  isValidIanaTimeZone,
  getUserTimeZone,
  formatInTimeZone,
  formatTimeInZone,
  formatDateInZone,
  getTimeZoneOffsetString,
  getTimeZoneAbbreviation,
  isSameDayInTimeZone,
  COMMON_IANA_TIMEZONES,
} from './timezones.ts';

describe('Section 87: Timezone Utilities and IANA Validation', () => {
  test('accurately validates IANA timezone identifiers', () => {
    // Valid IANA zones
    assert.equal(isValidIanaTimeZone('America/New_York'), true);
    assert.equal(isValidIanaTimeZone('UTC'), true);
    assert.equal(isValidIanaTimeZone('Asia/Kolkata'), true);
    assert.equal(isValidIanaTimeZone('Europe/London'), true);
    assert.equal(isValidIanaTimeZone('Australia/Sydney'), true);

    // Invalid / malformed zones
    assert.equal(isValidIanaTimeZone('Mars/Crater'), false);
    assert.equal(isValidIanaTimeZone('Invalid/Zone'), false);
    assert.equal(isValidIanaTimeZone(''), false);
    assert.equal(isValidIanaTimeZone('   '), false);
    assert.equal(isValidIanaTimeZone(null as any), false);
    assert.equal(isValidIanaTimeZone(undefined as any), false);
  });

  test('resolves user timezone safely with fallback', () => {
    const tz = getUserTimeZone();
    assert.ok(typeof tz === 'string' && tz.length > 0);
    assert.ok(isValidIanaTimeZone(tz));
  });

  test('formats instants safely across different timezones', () => {
    // 2026-10-19T14:30:00Z
    const iso = '2026-10-19T14:30:00Z';

    const utcFormatted = formatInTimeZone(iso, 'UTC');
    assert.ok(utcFormatted.includes('Oct 19, 2026'));
    assert.ok(utcFormatted.includes('2:30 PM') || utcFormatted.includes('14:30'));

    // In New York (EDT, UTC-4), 14:30 UTC is 10:30 AM
    const nyFormatted = formatInTimeZone(iso, 'America/New_York');
    assert.ok(nyFormatted.includes('10:30 AM') || nyFormatted.includes('10:30'));

    // In Kolkata (IST, UTC+5:30), 14:30 UTC is 8:00 PM (20:00)
    const kolkataFormatted = formatInTimeZone(iso, 'Asia/Kolkata');
    assert.ok(kolkataFormatted.includes('8:00 PM') || kolkataFormatted.includes('20:00'));

    // Invalid inputs return safe fallback
    assert.equal(formatInTimeZone(null, 'UTC'), '—');
    assert.equal(formatInTimeZone(undefined, 'UTC'), '—');
    assert.equal(formatInTimeZone('invalid-date', 'UTC'), '—');
  });

  test('formats time-only and date-only safely', () => {
    const iso = '2026-10-19T14:30:00Z';

    const timeNy = formatTimeInZone(iso, 'America/New_York');
    assert.ok(timeNy.includes('10:30'));

    const dateNy = formatDateInZone(iso, 'America/New_York');
    assert.ok(dateNy.includes('Oct') && dateNy.includes('2026'));
  });

  test('generates timezone offset strings and abbreviations', () => {
    const date = new Date('2026-10-19T12:00:00Z');

    const offsetUtc = getTimeZoneOffsetString('UTC', date);
    assert.ok(offsetUtc.includes('UTC'));

    const abbrUtc = getTimeZoneAbbreviation('UTC', date);
    assert.equal(abbrUtc, 'UTC');

    const abbrNy = getTimeZoneAbbreviation('America/New_York', date);
    assert.ok(abbrNy === 'EDT' || abbrNy === 'EST' || abbrNy.includes('America'));
  });

  test('correctly evaluates calendar days in specific timezone to prevent UTC day-bleeding', () => {
    // 2026-10-19T23:30:00Z is Oct 19 in UTC, but Oct 20 in Tokyo (UTC+9)
    const instant1 = '2026-10-19T23:30:00Z';
    const instant2 = '2026-10-20T01:30:00Z';

    // In Tokyo (+9), instant1 is 2026-10-20 08:30, instant2 is 2026-10-20 10:30 -> SAME DAY
    assert.equal(isSameDayInTimeZone(instant1, instant2, 'Asia/Tokyo'), true);

    // In UTC, instant1 is Oct 19, instant2 is Oct 20 -> DIFFERENT DAYS
    assert.equal(isSameDayInTimeZone(instant1, instant2, 'UTC'), false);
  });

  test('all common IANA timezone definitions are valid IANA zones', () => {
    assert.ok(COMMON_IANA_TIMEZONES.length >= 10);
    for (const tz of COMMON_IANA_TIMEZONES) {
      assert.ok(
        isValidIanaTimeZone(tz.value),
        `Expected ${tz.value} in COMMON_IANA_TIMEZONES to be a valid IANA timezone`
      );
      assert.ok(tz.label.length > 0);
      assert.ok(tz.region.length > 0);
    }
  });
});
