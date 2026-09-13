// Failure reporting for the conformance suite. The one rule that matters
// here: a failure names the field and the expected value. "Assertion
// failed" is useless at 2am — every ConformanceFailure carries structured
// `details` alongside a human-readable message built from them.

export class ConformanceFailure extends Error {
  constructor(message, details = {}) {
    super(message);
    this.name = 'ConformanceFailure';
    this.details = details;
  }
}

function formatValue(value) {
  if (value === undefined) return 'undefined';
  try {
    return JSON.stringify(value);
  } catch {
    return String(value);
  }
}

/**
 * Raise a conformance failure.
 * @param {string} message What went wrong, in plain language.
 * @param {{field?: string, expected?: unknown, actual?: unknown}} [details]
 */
export function fail(message, details = {}) {
  const { field, expected, actual } = details;
  const parts = [message];
  if (field !== undefined) parts.push(`field: ${field}`);
  if (expected !== undefined) parts.push(`expected: ${formatValue(expected)}`);
  if (actual !== undefined) parts.push(`actual: ${formatValue(actual)}`);
  throw new ConformanceFailure(parts.join(' — '), details);
}
