// Hand-rolled shape/type validators for every schema in
// `contract/openapi.yaml`'s `components.schemas`. No ajv, no dependency at
// all: these schemas are small and stable, and a validator every user
// inherits is worth keeping fully readable over pulling in a library to
// re-derive from the YAML at runtime.
//
// Every validator appends `{ field, expected, actual, note }` objects to a
// caller-supplied `violations` array instead of throwing — that lets a
// single check report every mismatch in a response body at once, not just
// the first one it trips over.

// Exactly the pattern pinned on `UtcDateTime` in the contract: RFC 3339,
// UTC, always a trailing `Z`, never a numeric offset like `+00:00`.
export const UTC_DATETIME_PATTERN = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,9})?Z$/;

function typeName(value) {
  if (value === null) return 'null';
  if (Array.isArray(value)) return 'array';
  return typeof value;
}

export function violation(field, expected, actual, note) {
  return { field, expected, actual, note };
}

export function violationsMessage(context, violations) {
  const lines = violations.map((v) => {
    const expected = typeof v.expected === 'string' ? v.expected : JSON.stringify(v.expected);
    const actual = typeof v.actual === 'string' ? v.actual : JSON.stringify(v.actual);
    const note = v.note ? ` (${v.note})` : '';
    return `- ${v.field}: expected ${expected}, got ${actual}${note}`;
  });
  return `${context}: ${violations.length} field violation(s)\n${lines.join('\n')}`;
}

export function validateUtcDateTime(value, field, violations, { nullable = false } = {}) {
  if (value === null) {
    if (!nullable) violations.push(violation(field, 'a UtcDateTime string (RFC 3339 UTC, trailing Z)', 'null'));
    return;
  }
  if (typeof value !== 'string') {
    violations.push(violation(field, 'string matching RFC 3339 UTC with a trailing Z', `type ${typeName(value)}`));
    return;
  }
  if (!UTC_DATETIME_PATTERN.test(value)) {
    violations.push(
      violation(
        field,
        "RFC 3339 UTC datetime ending in 'Z', e.g. 2026-09-13T10:15:30Z — not a numeric offset such as +00:00",
        value,
      ),
    );
  }
}

export function validateRole(value, field, violations) {
  if (typeof value !== 'object' || value === null || Array.isArray(value)) {
    violations.push(violation(field, 'object (Role)', typeName(value)));
    return;
  }
  for (const key of ['id', 'name', 'permissions']) {
    if (!(key in value)) violations.push(violation(`${field}.${key}`, 'present (required)', 'missing'));
  }
  if ('id' in value && typeof value.id !== 'string') {
    violations.push(violation(`${field}.id`, 'string (uuid)', `type ${typeName(value.id)}`));
  }
  if ('name' in value && typeof value.name !== 'string') {
    violations.push(violation(`${field}.name`, 'string', `type ${typeName(value.name)}`));
  }
  if ('description' in value && value.description !== null && typeof value.description !== 'string') {
    violations.push(violation(`${field}.description`, 'string | null', `type ${typeName(value.description)}`));
  }
  if ('permissions' in value) {
    if (!Array.isArray(value.permissions)) {
      violations.push(violation(`${field}.permissions`, 'array of strings', `type ${typeName(value.permissions)}`));
    } else {
      value.permissions.forEach((p, i) => {
        if (typeof p !== 'string') violations.push(violation(`${field}.permissions[${i}]`, 'string', `type ${typeName(p)}`));
      });
    }
  }
}

export function validateUser(value, field, violations) {
  const required = ['id', 'email', 'displayName', 'status', 'roles', 'createdAt', 'updatedAt'];
  if (typeof value !== 'object' || value === null || Array.isArray(value)) {
    violations.push(violation(field, 'object (User)', typeName(value)));
    return;
  }
  for (const key of required) {
    if (!(key in value)) violations.push(violation(`${field}.${key}`, 'present (required)', 'missing'));
  }
  if ('id' in value && typeof value.id !== 'string') {
    violations.push(violation(`${field}.id`, 'string (uuid)', `type ${typeName(value.id)}`));
  }
  if ('email' in value && typeof value.email !== 'string') {
    violations.push(violation(`${field}.email`, 'string (email)', `type ${typeName(value.email)}`));
  }
  if ('displayName' in value && typeof value.displayName !== 'string') {
    violations.push(violation(`${field}.displayName`, 'string', `type ${typeName(value.displayName)}`));
  }
  if ('status' in value && !['active', 'inactive'].includes(value.status)) {
    violations.push(violation(`${field}.status`, "'active' | 'inactive'", JSON.stringify(value.status)));
  }
  if ('roles' in value) {
    if (!Array.isArray(value.roles)) {
      violations.push(violation(`${field}.roles`, 'array of Role', `type ${typeName(value.roles)}`));
    } else {
      value.roles.forEach((r, i) => validateRole(r, `${field}.roles[${i}]`, violations));
    }
  }
  if ('createdAt' in value) validateUtcDateTime(value.createdAt, `${field}.createdAt`, violations);
  if ('updatedAt' in value) validateUtcDateTime(value.updatedAt, `${field}.updatedAt`, violations);
  if ('lastLoginAt' in value) validateUtcDateTime(value.lastLoginAt, `${field}.lastLoginAt`, violations, { nullable: true });
}

export function validateTokenPair(value, field, violations) {
  const required = ['accessToken', 'refreshToken', 'tokenType', 'expiresAt'];
  if (typeof value !== 'object' || value === null || Array.isArray(value)) {
    violations.push(violation(field, 'object (TokenPair)', typeName(value)));
    return;
  }
  for (const key of required) {
    if (!(key in value)) violations.push(violation(`${field}.${key}`, 'present (required)', 'missing'));
  }
  if ('accessToken' in value && typeof value.accessToken !== 'string') {
    violations.push(violation(`${field}.accessToken`, 'string', `type ${typeName(value.accessToken)}`));
  }
  if ('refreshToken' in value && typeof value.refreshToken !== 'string') {
    violations.push(violation(`${field}.refreshToken`, 'string', `type ${typeName(value.refreshToken)}`));
  }
  if ('tokenType' in value && value.tokenType !== 'Bearer') {
    violations.push(violation(`${field}.tokenType`, "'Bearer' (const)", JSON.stringify(value.tokenType)));
  }
  if ('expiresAt' in value) validateUtcDateTime(value.expiresAt, `${field}.expiresAt`, violations);
}

export function validateProblem(value, field, violations, { expectErrors = false } = {}) {
  const required = ['type', 'title', 'status'];
  if (typeof value !== 'object' || value === null || Array.isArray(value)) {
    violations.push(violation(field, 'object (RFC 9457 Problem Details)', typeName(value)));
    return;
  }
  for (const key of required) {
    if (!(key in value)) violations.push(violation(`${field}.${key}`, 'present (required by RFC 9457)', 'missing'));
  }
  if ('type' in value && typeof value.type !== 'string') {
    violations.push(violation(`${field}.type`, 'string (URI reference)', `type ${typeName(value.type)}`));
  }
  if ('title' in value && typeof value.title !== 'string') {
    violations.push(violation(`${field}.title`, 'string', `type ${typeName(value.title)}`));
  }
  if ('status' in value && typeof value.status !== 'number') {
    violations.push(violation(`${field}.status`, 'integer', `type ${typeName(value.status)}`));
  }
  if (!('code' in value)) {
    violations.push(violation(`${field}.code`, "present — StackBraid's Problem extension", 'missing'));
  } else if (typeof value.code !== 'string') {
    violations.push(violation(`${field}.code`, 'string', `type ${typeName(value.code)}`));
  }
  if (!('traceId' in value)) {
    violations.push(violation(`${field}.traceId`, "present — StackBraid's Problem extension", 'missing'));
  } else if (typeof value.traceId !== 'string') {
    violations.push(violation(`${field}.traceId`, 'string', `type ${typeName(value.traceId)}`));
  }
  if (expectErrors) {
    if (!('errors' in value)) {
      violations.push(violation(`${field}.errors`, 'object mapping field name -> string[] (this is a validation problem)', 'missing'));
    } else if (typeof value.errors !== 'object' || value.errors === null || Array.isArray(value.errors)) {
      violations.push(violation(`${field}.errors`, 'object', `type ${typeName(value.errors)}`));
    }
  }
}

/** `Page<T>` — the offset-pagination envelope shared by every list endpoint, plus its `items`. */
export function validatePageEnvelope(value, field, violations) {
  const required = ['page', 'pageSize', 'totalItems', 'totalPages', 'items'];
  if (typeof value !== 'object' || value === null || Array.isArray(value)) {
    violations.push(violation(field, 'object (offset-paginated Page<T>)', typeName(value)));
    return;
  }
  for (const key of required) {
    if (!(key in value)) violations.push(violation(`${field}.${key}`, 'present (required by offset pagination)', 'missing'));
  }
  for (const key of ['page', 'pageSize', 'totalItems', 'totalPages']) {
    if (key in value && !Number.isInteger(value[key])) {
      violations.push(violation(`${field}.${key}`, 'integer', `type ${typeName(value[key])} (${JSON.stringify(value[key])})`));
    }
  }
  if ('items' in value && !Array.isArray(value.items)) {
    violations.push(violation(`${field}.items`, 'array', `type ${typeName(value.items)}`));
  }
  // The contract specifies offset pagination, not cursor (see contract/README.md
  // "Pagination: offset, not cursor") — a cursor-shaped field leaking in is
  // exactly the class of drift this suite exists to catch.
  for (const cursorKey of ['nextCursor', 'cursor', 'next', 'previousCursor']) {
    if (cursorKey in value) {
      violations.push(
        violation(`${field}.${cursorKey}`, 'absent — StackBraid uses offset pagination (page/pageSize/totalItems/totalPages), not cursor', JSON.stringify(value[cursorKey])),
      );
    }
  }
}

/**
 * `RealtimeMessage` — the discriminated union both backends push over
 * their own realtime transport (SignalR for .NET, a native WebSocket for
 * Python; see `contract/openapi.yaml`'s `x-realtime-channels`). The two
 * transports are deliberately different; this validator is what proves the
 * JSON on the wire is not.
 */
export function validateRealtimeMessage(value, field, violations) {
  if (typeof value !== 'object' || value === null || Array.isArray(value)) {
    violations.push(violation(field, 'object (RealtimeMessage)', typeName(value)));
    return;
  }
  if (!('type' in value) || typeof value.type !== 'string') {
    violations.push(violation(`${field}.type`, "string discriminator ('user.deactivated' | 'user.role_changed' | 'job.progress')", typeName(value.type)));
    return;
  }
  switch (value.type) {
    case 'user.deactivated':
      validateUserDeactivatedMessage(value, field, violations);
      return;
    case 'user.role_changed':
      validateUserRoleChangedMessage(value, field, violations);
      return;
    case 'job.progress':
      validateJobProgressMessage(value, field, violations);
      return;
    default:
      violations.push(violation(`${field}.type`, "'user.deactivated' | 'user.role_changed' | 'job.progress'", JSON.stringify(value.type)));
  }
}

export function validateUserDeactivatedMessage(value, field, violations) {
  for (const key of ['type', 'userId', 'occurredAt']) {
    if (!(key in value)) violations.push(violation(`${field}.${key}`, 'present (required)', 'missing'));
  }
  if ('userId' in value && typeof value.userId !== 'string') {
    violations.push(violation(`${field}.userId`, 'string (uuid)', `type ${typeName(value.userId)}`));
  }
  if ('occurredAt' in value) validateUtcDateTime(value.occurredAt, `${field}.occurredAt`, violations);
}

export function validateUserRoleChangedMessage(value, field, violations) {
  for (const key of ['type', 'userId', 'roles', 'occurredAt']) {
    if (!(key in value)) violations.push(violation(`${field}.${key}`, 'present (required)', 'missing'));
  }
  if ('userId' in value && typeof value.userId !== 'string') {
    violations.push(violation(`${field}.userId`, 'string (uuid)', `type ${typeName(value.userId)}`));
  }
  if ('roles' in value) {
    if (!Array.isArray(value.roles)) {
      violations.push(violation(`${field}.roles`, 'array of Role', `type ${typeName(value.roles)}`));
    } else {
      value.roles.forEach((r, i) => validateRole(r, `${field}.roles[${i}]`, violations));
    }
  }
  if ('occurredAt' in value) validateUtcDateTime(value.occurredAt, `${field}.occurredAt`, violations);
}

export function validateJobProgressMessage(value, field, violations) {
  for (const key of ['type', 'jobId', 'status', 'progress', 'occurredAt']) {
    if (!(key in value)) violations.push(violation(`${field}.${key}`, 'present (required)', 'missing'));
  }
  if ('jobId' in value && typeof value.jobId !== 'string') {
    violations.push(violation(`${field}.jobId`, 'string (uuid)', `type ${typeName(value.jobId)}`));
  }
  if ('status' in value && !['queued', 'running', 'succeeded', 'failed'].includes(value.status)) {
    violations.push(violation(`${field}.status`, "'queued' | 'running' | 'succeeded' | 'failed'", JSON.stringify(value.status)));
  }
  if ('progress' in value) {
    if (!Number.isInteger(value.progress) || value.progress < 0 || value.progress > 100) {
      violations.push(violation(`${field}.progress`, 'integer 0-100', JSON.stringify(value.progress)));
    }
  }
  if ('occurredAt' in value) validateUtcDateTime(value.occurredAt, `${field}.occurredAt`, violations);
}
