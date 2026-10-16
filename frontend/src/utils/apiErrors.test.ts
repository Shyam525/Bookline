import test from 'node:test';
import assert from 'node:assert/strict';
import {
  ApiError,
  parseApiResponse,
  getLocalizedError,
  ERROR_TRANSLATIONS,
  UNKNOWN_ERROR_FALLBACK,
} from './apiErrors.ts';

test('Section 82: ApiError class preserves all contract fields', () => {
  const envelope = {
    code: 'SLOT_UNAVAILABLE',
    message: 'This time is no longer available.',
    traceId: 'trace-test-12345',
    status: 409,
    extensions: { nearestFreeSlots: ['2026-10-16T15:00:00Z'] },
  };

  const err = new ApiError(envelope);

  assert.equal(err.code, 'SLOT_UNAVAILABLE');
  assert.equal(err.message, 'This time is no longer available.');
  assert.equal(err.traceId, 'trace-test-12345');
  assert.equal(err.status, 409);
  assert.equal(err.isApiError, true);
  assert.deepEqual(err.extensions, { nearestFreeSlots: ['2026-10-16T15:00:00Z'] });
});

test('Section 82: ApiError supports structured validation errors with case-insensitive lookup', () => {
  const err = new ApiError({
    code: 'VALIDATION_FAILED',
    message: 'Validation failed.',
    traceId: 'trace-val-001',
    status: 400,
    errors: {
      email: ['Email is invalid.', 'Email cannot be empty.'],
      slotStartUtc: ['Start time must be in future.'],
    },
  });

  assert.equal(err.hasFieldErrors(), true);
  assert.equal(err.getFieldError('email'), 'Email is invalid.');
  assert.equal(err.getFieldError('EMAIL'), 'Email is invalid.');
  assert.equal(err.getFieldError('slotStartUtc'), 'Start time must be in future.');
  assert.equal(err.getFieldError('nonExistentField'), undefined);
});

test('Section 82: parseApiResponse parses 200 OK successfully', async () => {
  const response = new Response(JSON.stringify({ id: '123', name: 'Studio One' }), {
    status: 200,
    headers: { 'Content-Type': 'application/json' },
  });

  const data = await parseApiResponse<{ id: string; name: string }>(response);
  assert.deepEqual(data, { id: '123', name: 'Studio One' });
});

test('Section 82: parseApiResponse parses 409 SLOT_UNAVAILABLE envelope', async () => {
  const payload = {
    code: 'SLOT_UNAVAILABLE',
    message: 'This time is no longer available.',
    traceId: 'server-corr-999',
    status: 409,
    extensions: { nearestFreeSlots: [] },
  };

  const response = new Response(JSON.stringify(payload), {
    status: 409,
    headers: { 'Content-Type': 'application/json' },
  });

  await assert.rejects(
    async () => {
      await parseApiResponse(response);
    },
    (err: any) => {
      assert.equal(err instanceof ApiError, true);
      assert.equal(err.code, 'SLOT_UNAVAILABLE');
      assert.equal(err.traceId, 'server-corr-999');
      assert.equal(err.status, 409);
      return true;
    }
  );
});

test('Section 82: parseApiResponse extracts correlation ID from header when body is HTML', async () => {
  const response = new Response('<html>500 Bad Gateway</html>', {
    status: 502,
    headers: {
      'Content-Type': 'text/html',
      'X-Correlation-ID': 'gw-trace-555',
    },
  });

  await assert.rejects(
    async () => {
      await parseApiResponse(response);
    },
    (err: any) => {
      assert.equal(err instanceof ApiError, true);
      assert.equal(err.traceId, 'gw-trace-555');
      assert.equal(err.status, 502);
      assert.equal(err.code, 'INTERNAL_SERVER_ERROR');
      return true;
    }
  );
});

test('Section 82: getLocalizedError maps stable codes to translation keys', () => {
  const slotErr = new ApiError({
    code: 'SLOT_UNAVAILABLE',
    message: 'Raw server message',
    traceId: 'corr-123',
    status: 409,
  });

  const result = getLocalizedError(slotErr);

  assert.equal(result.code, 'SLOT_UNAVAILABLE');
  assert.equal(result.translationKey, 'booking.errors.slotUnavailable');
  assert.equal(result.message, ERROR_TRANSLATIONS.SLOT_UNAVAILABLE.defaultMessage);
  assert.equal(result.traceId, 'corr-123');
});

test('Section 82: getLocalizedError provides safe fallback for unknown codes and NEVER leaks raw exception details', () => {
  const leakErr = new ApiError({
    code: 'SQL_SYNTAX_FATAL_LEAK',
    message: 'NpgsqlException: relation "secret_schema" does not exist at line 42',
    traceId: 'support-diag-777',
    status: 500,
  });

  const result = getLocalizedError(leakErr);

  assert.equal(result.code, 'SQL_SYNTAX_FATAL_LEAK');
  assert.equal(result.translationKey, UNKNOWN_ERROR_FALLBACK.key);
  // Must NOT leak internal database/exception details!
  assert.equal(result.message, UNKNOWN_ERROR_FALLBACK.defaultMessage);
  assert.doesNotMatch(result.message, /NpgsqlException/);
  assert.doesNotMatch(result.message, /secret_schema/);
  // Must preserve real trace ID for support diagnostics
  assert.equal(result.traceId, 'support-diag-777');
});

test('Section 82: getLocalizedError handles network fetch failure gracefully', () => {
  const netErr = new TypeError('Failed to fetch');

  const result = getLocalizedError(netErr);

  assert.equal(result.code, 'NETWORK_ERROR');
  assert.equal(result.translationKey, 'common.errors.networkError');
  assert.equal(result.message, ERROR_TRANSLATIONS.NETWORK_ERROR.defaultMessage);
});
