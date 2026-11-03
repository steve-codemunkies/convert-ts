/**
 * Browser port of the ConvertTs command palette extension converter logic.
 * Mirrors command-palette/ConvertTs/Utilities/UnixTimestampConverter.cs and
 * ConversionResults.cs so the site behaves the same way as the extension.
 *
 * This module is dependency-free and runs in both the browser and Node.js
 * (so it can be unit tested with `node --test`).
 */

/** Default boundary: timestamps greater than this are treated as milliseconds. */
export const DEFAULT_BOUNDARY = 32503680000;

/** Same range limits as System.DateTimeOffset (0001-01-01 .. 9999-12-31T23:59:59.999Z). */
const MIN_UNIX_MS = -62135596800000;
const MAX_UNIX_MS = 253402300799999;

/** Labels match ConversionResults.cs. */
export const LABELS = {
  userTimeZone: 'User time zone date/time',
  utc: 'UTC Date/Time',
  milliseconds: 'Unix Epoch Milliseconds',
  seconds: 'Unix Epoch Seconds',
};

/**
 * Creates a timestamp result. `milliseconds` is the Unix epoch in ms,
 * `seconds` the truncated-to-zero whole seconds, `date` the same instant.
 */
function makeTimestamp(milliseconds, seconds) {
  return { milliseconds, seconds, date: new Date(milliseconds) };
}

/** True when the zone name is semantically UTC (offset zero, like ConversionResults.IsUtc). */
export function isUtcZone(timeZone) {
  if (!timeZone) {
    return true;
  }
  return /^(UTC|Etc\/UTC|Etc\/Universal|Universal|GMT|Etc\/GMT|Greenwich|Etc\/Greenwich)$/i.test(timeZone);
}

/**
 * Date.UTC treats years 0-99 as 1900+year; this helper does not.
 */
function utcFromParts(year, monthIndex, day, hour, minute, second, ms) {
  const date = new Date(Date.UTC(year, monthIndex, day, hour, minute, second, ms));
  if (year >= 0 && year < 100) {
    date.setUTCFullYear(year);
  }
  return date.getTime();
}

const dtfCache = new Map();

function getDateTimeFormat(timeZone) {
  let dtf = dtfCache.get(timeZone);
  if (!dtf) {
    dtf = new Intl.DateTimeFormat('en-GB', {
      timeZone,
      hourCycle: 'h23',
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });
    dtfCache.set(timeZone, dtf);
  }
  return dtf;
}

function formatToPartsMap(timeZone, dateMs) {
  const map = Object.create(null);
  for (const part of getDateTimeFormat(timeZone).formatToParts(new Date(dateMs))) {
    if (part.type !== 'literal') {
      map[part.type] = part.value;
    }
  }
  return map;
}

/**
 * Offset of `timeZone` from UTC, in milliseconds, at instant `dateMs`
 * (i.e. wallClockInZone - utc). E.g. +19800000 for Asia/Kolkata.
 */
export function getTimeZoneOffset(timeZone, dateMs) {
  const map = formatToPartsMap(timeZone, dateMs);
  const asUtc = utcFromParts(
    Number(map.year),
    Number(map.month) - 1,
    Number(map.day),
    Number(map.hour),
    Number(map.minute),
    Number(map.second),
    0,
  );
  return asUtc - Math.floor(dateMs / 1000) * 1000;
}

/**
 * Converts a wall-clock date/time expressed in `timeZone` into a UTC instant (ms).
 */
export function zonedWallTimeToUtc(parts, timeZone) {
  const guess = utcFromParts(
    parts.year,
    parts.month - 1,
    parts.day,
    parts.hour,
    parts.minute,
    parts.second,
    parts.ms,
  );
  if (isUtcZone(timeZone)) {
    return guess;
  }
  let offset = getTimeZoneOffset(timeZone, guess);
  let utc = guess - offset;
  // Refine in case the offset differs at the computed instant (DST transitions).
  for (let i = 0; i < 2; i += 1) {
    const refined = getTimeZoneOffset(timeZone, utc);
    if (refined === offset) {
      break;
    }
    utc = guess - refined;
    offset = refined;
  }
  return utc;
}

/**
 * Parses a bare integer timestamp. Values greater than `boundary` are treated as
 * milliseconds, otherwise seconds. Returns null when the input is not an integer
 * or the result is outside the representable DateTimeOffset range.
 */
export function tryConvertFromTimestamp(input, boundary = DEFAULT_BOUNDARY) {
  const trimmed = String(input).trim();
  if (!/^[+-]?\d+$/.test(trimmed)) {
    return null;
  }

  const value = BigInt(trimmed);
  const boundaryValue = BigInt(boundary);

  let seconds;
  let milliseconds;
  if (value > boundaryValue) {
    milliseconds = value;
    seconds = value / 1000n; // BigInt division truncates toward zero, like C#.
  } else {
    seconds = value;
    milliseconds = value * 1000n;
  }

  if (milliseconds < BigInt(MIN_UNIX_MS) || milliseconds > BigInt(MAX_UNIX_MS)) {
    return null;
  }

  return makeTimestamp(Number(milliseconds), Number(seconds));
}

/**
 * Matches the date/time forms accepted by the extension documentation:
 *   2023-11-14 | 2023-11-14T22:13 | 2023-11-14T22:13:20 | 2023-11-14 22:13:20
 * with optional fractional seconds and an optional trailing offset
 * (Z, +02:00, -0500). Like the C# ExplicitOffsetPattern, the offset must be
 * at the end of the string.
 */
const DATE_TIME_PATTERN = /^(\d{4})-(\d{2})-(\d{2})(?:[T ](\d{2}):(\d{2})(?::(\d{2})(?:\.(\d{1,9}))?)?)?(?:\s*(Z|[+-]\d{2}:?\d{2}))?$/i;

/**
 * Parses a date/time string into a timestamp. Inputs carrying an explicit
 * offset honor that offset; everything else is interpreted in `timeZone`.
 * Returns null when the input cannot be parsed.
 */
export function tryConvertToTimestamp(input, timeZone = 'UTC') {
  const match = DATE_TIME_PATTERN.exec(String(input).trim());
  if (!match) {
    return null;
  }

  const [, year, month, day, hour = '0', minute = '0', second = '0', fraction = '', offset] = match;
  const parts = {
    year: Number(year),
    month: Number(month),
    day: Number(day),
    hour: Number(hour),
    minute: Number(minute),
    second: Number(second),
    ms: Number(fraction.slice(0, 3).padEnd(3, '0') || 0),
  };

  if (
    parts.month < 1 || parts.month > 12
    || parts.day < 1 || parts.day > 31
    || parts.hour > 23
    || parts.minute > 59
    || parts.second > 59
  ) {
    return null;
  }

  let utcMs;
  if (offset) {
    // An explicit offset is honored as-is, ignoring the configured timezone.
    const wallMs = utcFromParts(parts.year, parts.month - 1, parts.day, parts.hour, parts.minute, parts.second, parts.ms);
    if (/^z$/i.test(offset)) {
      utcMs = wallMs;
    } else {
      const sign = offset.startsWith('-') ? -1 : 1;
      const digits = offset.slice(1).replace(':', '');
      const offsetMs = sign * (Number(digits.slice(0, 2)) * 60 + Number(digits.slice(2))) * 60000;
      utcMs = wallMs - offsetMs;
    }
  } else {
    // Reject impossible calendar dates (e.g. 2023-02-30) before zone conversion.
    const wallMs = utcFromParts(parts.year, parts.month - 1, parts.day, parts.hour, parts.minute, parts.second, parts.ms);
    const check = new Date(wallMs);
    if (
      check.getUTCFullYear() !== parts.year
      || check.getUTCMonth() !== parts.month - 1
      || check.getUTCDate() !== parts.day
    ) {
      return null;
    }
    utcMs = zonedWallTimeToUtc(parts, timeZone);
  }

  if (utcMs < MIN_UNIX_MS || utcMs > MAX_UNIX_MS) {
    return null;
  }

  return makeTimestamp(utcMs, Math.trunc(utcMs / 1000));
}

/** Current time as a timestamp (the extension's live "Now" item). */
export function getNowTimestamp(nowMs = Date.now()) {
  const milliseconds = Math.trunc(nowMs);
  return makeTimestamp(milliseconds, Math.trunc(milliseconds / 1000));
}

function pad(value, width = 2) {
  return String(value).padStart(width, '0');
}

/** Formats an instant as yyyy-MM-ddTHH:mm:ss.fffZ (matches FormatUtc). */
export function formatUtc(ms) {
  const d = new Date(ms);
  return `${pad(d.getUTCFullYear(), 4)}-${pad(d.getUTCMonth() + 1)}-${pad(d.getUTCDate())}`
    + `T${pad(d.getUTCHours())}:${pad(d.getUTCMinutes())}:${pad(d.getUTCSeconds())}`
    + `.${pad(d.getUTCMilliseconds(), 3)}Z`;
}

/** Formats an instant in `timeZone` as yyyy-MM-ddTHH:mm:ss.fffzzz (matches FormatInTimeZone). */
export function formatInTimeZone(ms, timeZone) {
  if (isUtcZone(timeZone)) {
    return `${formatUtc(ms).slice(0, -1)}+00:00`;
  }
  const map = formatToPartsMap(timeZone, ms);
  const offsetMs = getTimeZoneOffset(timeZone, ms);
  const sign = offsetMs < 0 ? '-' : '+';
  const absMinutes = Math.abs(Math.trunc(offsetMs / 60000));
  return `${pad(Number(map.year), 4)}-${map.month}-${map.day}`
    + `T${map.hour}:${map.minute}:${map.second}.${pad(new Date(ms).getUTCMilliseconds(), 3)}`
    + `${sign}${pad(Math.trunc(absMinutes / 60))}:${pad(absMinutes % 60)}`;
}

/**
 * Builds the result rows in the same order as ConversionResults.Build:
 * user timezone (only when non-UTC), UTC, milliseconds, seconds.
 */
export function buildConversionRows(ts, timeZone) {
  const rows = [];
  if (!isUtcZone(timeZone)) {
    rows.push({ label: `${LABELS.userTimeZone} (${timeZone})`, text: formatInTimeZone(ts.milliseconds, timeZone) });
  }
  rows.push({ label: LABELS.utc, text: formatUtc(ts.milliseconds) });
  rows.push({ label: LABELS.milliseconds, text: String(ts.milliseconds) });
  rows.push({ label: LABELS.seconds, text: String(ts.seconds) });
  return rows;
}

/** Parses a boundary input field; falls back to the default when invalid. */
export function parseBoundary(input) {
  const trimmed = String(input ?? '').trim();
  if (!/^[+-]?\d+$/.test(trimmed)) {
    return DEFAULT_BOUNDARY;
  }
  const value = Number(trimmed);
  return Number.isSafeInteger(value) ? value : DEFAULT_BOUNDARY;
}

/**
 * Produces the view model for the converter UI, mirroring UnixTimestampPage.GetItems:
 * empty input shows the live "now" values plus a hint; a parseable input shows the
 * converted values; anything else shows "now" plus a "no result" hint.
 */
export function buildViewModel(input, { boundary = DEFAULT_BOUNDARY, timeZone = 'UTC', nowMs = Date.now() } = {}) {
  const query = String(input ?? '').trim();

  if (query.length === 0) {
    return {
      rows: buildConversionRows(getNowTimestamp(nowMs), timeZone),
      hint: 'Enter a Unix timestamp or date/time string',
      showingNow: true,
    };
  }

  const ts = tryConvertFromTimestamp(query, boundary) ?? tryConvertToTimestamp(query, timeZone);
  if (ts) {
    return { rows: buildConversionRows(ts, timeZone), hint: null, showingNow: false };
  }

  return {
    rows: buildConversionRows(getNowTimestamp(nowMs), timeZone),
    hint: `No conversion result for '${query}'`,
    showingNow: true,
  };
}
