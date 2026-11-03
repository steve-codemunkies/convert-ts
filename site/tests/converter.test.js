import { describe, it } from 'node:test';
import assert from 'node:assert/strict';

import {
  DEFAULT_BOUNDARY,
  LABELS,
  buildConversionRows,
  buildViewModel,
  formatInTimeZone,
  formatUtc,
  getNowTimestamp,
  getTimeZoneOffset,
  isUtcZone,
  parseBoundary,
  tryConvertFromTimestamp,
  tryConvertToTimestamp,
  zonedWallTimeToUtc,
} from '../src/js/converter.js';

// Mirrors ConvertsSecondsTimestampUsingDefaultBoundary (UnixTimestampConverterTests.cs)
it('converts a seconds timestamp using the default boundary', () => {
  const result = tryConvertFromTimestamp('1700000000', DEFAULT_BOUNDARY);

  assert.ok(result);
  assert.equal(formatUtc(result.milliseconds), '2023-11-14T22:13:20.000Z');
  assert.equal(result.seconds, 1700000000);
  assert.equal(result.milliseconds, 1700000000000);
});

// Mirrors ConvertsMillisecondsTimestampUsingDefaultBoundary
it('converts a milliseconds timestamp using the default boundary', () => {
  const result = tryConvertFromTimestamp('1700000000000', DEFAULT_BOUNDARY);

  assert.ok(result);
  assert.equal(formatUtc(result.milliseconds), '2023-11-14T22:13:20.000Z');
  assert.equal(result.seconds, 1700000000);
  assert.equal(result.milliseconds, 1700000000000);
});

// Mirrors ConvertsTimestampUsingCustomBoundary
it('converts a timestamp using a custom boundary', () => {
  const result = tryConvertFromTimestamp('10000000000', 9999999999);

  assert.ok(result);
  assert.equal(formatUtc(result.milliseconds), '1970-04-26T17:46:40.000Z');
  assert.equal(result.seconds, 10000000);
  assert.equal(result.milliseconds, 10000000000);
});

it('treats a value equal to the boundary as seconds (strictly-greater comparison)', () => {
  const result = tryConvertFromTimestamp('32503680000', DEFAULT_BOUNDARY);

  assert.ok(result);
  assert.equal(result.seconds, 32503680000);
  assert.equal(formatUtc(result.milliseconds), '3000-01-01T00:00:00.000Z');
});

it('converts negative timestamps to pre-epoch instants', () => {
  const result = tryConvertFromTimestamp('-1', DEFAULT_BOUNDARY);

  assert.ok(result);
  assert.equal(result.seconds, -1);
  assert.equal(result.milliseconds, -1000);
  assert.equal(formatUtc(result.milliseconds), '1969-12-31T23:59:59.000Z');
});

it('rejects non-numeric input', () => {
  assert.equal(tryConvertFromTimestamp('not-a-number', DEFAULT_BOUNDARY), null);
  assert.equal(tryConvertFromTimestamp('12.5', DEFAULT_BOUNDARY), null);
});

it('rejects timestamps outside the DateTimeOffset range', () => {
  // 253402300800000 ms is 9999-12-31T23:59:59.999Z + 1ms.
  assert.equal(tryConvertFromTimestamp('253402300800000', DEFAULT_BOUNDARY), null);
});

// Mirrors ParsesIso8601UtcDateStringToEpoch
it('parses an ISO 8601 UTC date string to epoch values', () => {
  const result = tryConvertToTimestamp('2023-11-14T22:13:20Z');

  assert.ok(result);
  assert.equal(result.seconds, 1700000000);
  assert.equal(result.milliseconds, 1700000000000);
});

// Mirrors ParsesDateStringWithExplicitOffsetRegardlessOfConfiguredTimeZone
it('honors an explicit offset regardless of the configured timezone', () => {
  const result = tryConvertToTimestamp('2023-11-14T22:13:20+02:00', 'Asia/Kolkata');

  assert.ok(result);
  assert.equal(formatUtc(result.milliseconds), '2023-11-14T20:13:20.000Z');
  assert.equal(result.seconds, 1699992800);
  assert.equal(result.milliseconds, 1699992800000);
});

it('supports offsets without a colon and lowercase z', () => {
  const result = tryConvertToTimestamp('2023-11-14T22:13:20+0530', 'UTC');

  assert.ok(result);
  assert.equal(result.seconds, 1699980200);

  const lowerZ = tryConvertToTimestamp('2023-11-14T22:13:20z', 'Europe/London');
  assert.ok(lowerZ);
  assert.equal(lowerZ.seconds, 1700000000);
});

// Mirrors ParsesDateStringWithoutOffsetUsingConfiguredTimeZone
it('interprets inputs without an offset using the configured timezone', () => {
  const result = tryConvertToTimestamp('2023-11-14 22:13:20', 'Asia/Kolkata');

  assert.ok(result);
  assert.equal(formatUtc(result.milliseconds), '2023-11-14T16:43:20.000Z');
  assert.equal(Math.trunc(result.milliseconds / 1000), result.seconds);
});

// Mirrors ParsesDateStringWithoutOffsetAsUtcWhenNoTimeZoneOverloadUsed
it('interprets inputs without an offset as UTC when no timezone is given', () => {
  const result = tryConvertToTimestamp('2023-11-14 22:13:20');

  assert.ok(result);
  assert.equal(result.seconds, 1700000000);
  assert.equal(result.milliseconds, 1700000000000);
});

it('parses date-only and fractional-second inputs', () => {
  const dateOnly = tryConvertToTimestamp('2024-03-15', 'UTC');
  assert.ok(dateOnly);
  assert.equal(formatUtc(dateOnly.milliseconds), '2024-03-15T00:00:00.000Z');
  assert.equal(dateOnly.seconds, 1710460800);

  const fractional = tryConvertToTimestamp('2023-11-14T22:13:20.500Z', 'UTC');
  assert.ok(fractional);
  assert.equal(fractional.milliseconds, 1700000000500);
});

it('rejects unparseable and impossible dates', () => {
  assert.equal(tryConvertToTimestamp('hello', 'UTC'), null);
  assert.equal(tryConvertToTimestamp('2023-02-30', 'UTC'), null);
  assert.equal(tryConvertToTimestamp('2023-13-01', 'UTC'), null);
  assert.equal(tryConvertToTimestamp('2023-11-14T25:00:00Z', 'UTC'), null);
});

// Mirrors ReturnsCurrentUtcTimestampValues
it('returns consistent values for the current time', () => {
  const now = getNowTimestamp(1700000000123);

  assert.equal(now.milliseconds, 1700000000123);
  assert.equal(now.seconds, 1700000000);
  assert.deepEqual(now.date, new Date(1700000000123));
});

// Mirrors FormatsUtcInstantUsingCanonicalFormat
it('formats a UTC instant using the canonical format', () => {
  assert.equal(formatUtc(1700000000000), '2023-11-14T22:13:20.000Z');
});

// Mirrors FormatsUtcInstantInTargetTimeZoneWithOffset
it('formats a UTC instant in the target timezone with its offset', () => {
  assert.equal(formatInTimeZone(1700000000000, 'Asia/Kolkata'), '2023-11-15T03:43:20.000+05:30');
});

it('formats negative offsets in the target timezone', () => {
  assert.equal(formatInTimeZone(1700000000000, 'America/New_York'), '2023-11-14T17:13:20.000-05:00');
});

it('computes the timezone offset at an instant', () => {
  assert.equal(getTimeZoneOffset('Asia/Kolkata', 1700000000000), 19800000);
  assert.equal(getTimeZoneOffset('UTC', 1700000000000), 0);
});

it('converts zoned wall-clock values to UTC', () => {
  const ms = zonedWallTimeToUtc(
    { year: 2023, month: 11, day: 14, hour: 22, minute: 13, second: 20, ms: 0 },
    'Asia/Kolkata',
  );
  assert.equal(ms, 1699980200000);
});

it('detects UTC-equivalent zone names', () => {
  assert.ok(isUtcZone('UTC'));
  assert.ok(isUtcZone('Etc/UTC'));
  assert.ok(isUtcZone(undefined));
  assert.ok(!isUtcZone('Asia/Kolkata'));
});

// Mirrors ConversionResults.Build ordering and the UTC-only suppression of the zone row
it('builds conversion rows without a zone row for UTC', () => {
  const rows = buildConversionRows({ milliseconds: 1700000000000, seconds: 1700000000 }, 'UTC');

  assert.deepEqual(rows, [
    { label: LABELS.utc, text: '2023-11-14T22:13:20.000Z' },
    { label: LABELS.milliseconds, text: '1700000000000' },
    { label: LABELS.seconds, text: '1700000000' },
  ]);
});

it('builds conversion rows with the zone row first for a non-UTC zone', () => {
  const rows = buildConversionRows({ milliseconds: 1700000000000, seconds: 1700000000 }, 'Asia/Kolkata');

  assert.equal(rows.length, 4);
  assert.equal(rows[0].label, `${LABELS.userTimeZone} (Asia/Kolkata)`);
  assert.equal(rows[0].text, '2023-11-15T03:43:20.000+05:30');
  assert.equal(rows[3].label, LABELS.seconds);
});

describe('buildViewModel', () => {
  const nowMs = 1700000000000;

  it('shows live "now" values and a hint for empty input', () => {
    const vm = buildViewModel('', { nowMs, timeZone: 'UTC' });

    assert.equal(vm.showingNow, true);
    assert.equal(vm.hint, 'Enter a Unix timestamp or date/time string');
    assert.equal(vm.rows[0].text, '2023-11-14T22:13:20.000Z');
  });

  it('converts a timestamp input without a hint', () => {
    const vm = buildViewModel('1700000000', { nowMs, timeZone: 'UTC' });

    assert.equal(vm.showingNow, false);
    assert.equal(vm.hint, null);
    assert.equal(vm.rows[0].text, '2023-11-14T22:13:20.000Z');
  });

  it('converts a date/time input without a hint', () => {
    const vm = buildViewModel('2023-11-14T22:13:20Z', { nowMs, timeZone: 'UTC' });

    assert.equal(vm.showingNow, false);
    assert.equal(vm.rows.find((r) => r.label === LABELS.seconds).text, '1700000000');
  });

  it('shows "now" values and a no-result hint for invalid input', () => {
    const vm = buildViewModel('garbage', { nowMs, timeZone: 'UTC' });

    assert.equal(vm.showingNow, true);
    assert.equal(vm.hint, "No conversion result for 'garbage'");
    assert.equal(vm.rows[0].text, '2023-11-14T22:13:20.000Z');
  });
});

describe('parseBoundary', () => {
  it('parses valid integers and rejects invalid input', () => {
    assert.equal(parseBoundary('9999999999'), 9999999999);
    assert.equal(parseBoundary('abc'), DEFAULT_BOUNDARY);
    assert.equal(parseBoundary(''), DEFAULT_BOUNDARY);
    assert.equal(parseBoundary('99999999999999999999'), DEFAULT_BOUNDARY);
  });
});
