/**
 * Universal Stable API Error Contract & Translation Layer (Section 82)
 *
 * Implements the standard error response contract:
 * {
 *   "code": "SLOT_UNAVAILABLE",
 *   "message": "This time is no longer available.",
 *   "traceId": "server-generated-correlation-id"
 * }
 */

export interface ApiErrorEnvelope {
  code: string;
  message: string;
  traceId: string;
  status?: number;
  errors?: Record<string, string[]>;
  extensions?: Record<string, unknown>;
}

export interface ErrorTranslationSpec {
  key: string;
  defaultMessage: string;
}

/**
 * Stable, documented error codes mapped to frontend translation keys and safe localized fallbacks.
 * Preserves the stable code contract when wording changes.
 */
export const ERROR_TRANSLATIONS: Record<string, ErrorTranslationSpec> = {
  SLOT_UNAVAILABLE: {
    key: 'booking.errors.slotUnavailable',
    defaultMessage: 'This time is no longer available. Please choose another time.',
  },
  INVENTORY_CONFLICT: {
    key: 'commerce.errors.inventoryConflict',
    defaultMessage: 'The requested product inventory is no longer available.',
  },
  INSUFFICIENT_INVENTORY: {
    key: 'commerce.errors.insufficientInventory',
    defaultMessage: 'Insufficient product inventory to fulfill this request.',
  },
  STATE_CONFLICT: {
    key: 'common.errors.stateConflict',
    defaultMessage: 'The requested operation could not be completed due to a conflict. Please refresh and try again.',
  },
  IDEMPOTENCY_CONFLICT: {
    key: 'payment.errors.idempotencyConflict',
    defaultMessage: 'This operation is already in progress or has been completed. Please do not submit duplicate requests.',
  },
  VALIDATION_FAILED: {
    key: 'common.errors.validationFailed',
    defaultMessage: 'Please review and correct the highlighted fields.',
  },
  NOT_FOUND: {
    key: 'common.errors.notFound',
    defaultMessage: 'The requested resource could not be found.',
  },
  UNAUTHENTICATED: {
    key: 'auth.errors.unauthenticated',
    defaultMessage: 'Authentication is required to access this resource. Please sign in.',
  },
  FORBIDDEN: {
    key: 'auth.errors.forbidden',
    defaultMessage: 'You do not have permission to perform this action.',
  },
  RATE_LIMITED: {
    key: 'common.errors.rateLimited',
    defaultMessage: 'Too many requests. Please wait a moment before trying again.',
  },
  SERVICE_UNAVAILABLE: {
    key: 'common.errors.serviceUnavailable',
    defaultMessage: 'The service is temporarily unavailable. Please retry in a few moments.',
  },
  INTERNAL_SERVER_ERROR: {
    key: 'common.errors.internalServerError',
    defaultMessage: 'An unexpected application error occurred. Please try again later.',
  },
  BAD_REQUEST: {
    key: 'common.errors.badRequest',
    defaultMessage: 'The request could not be processed. Please verify your input.',
  },
  CANCELLATION_DEADLINE_EXCEEDED: {
    key: 'booking.errors.cancellationDeadlineExceeded',
    defaultMessage: 'Appointments cannot be cancelled after the cancellation deadline has passed.',
  },
  VERIFIED_PURCHASE_REQUIRED: {
    key: 'review.errors.verifiedPurchaseRequired',
    defaultMessage: 'You must have a completed booking or order with this provider to submit a review.',
  },
  USER_EXISTS: {
    key: 'auth.errors.userExists',
    defaultMessage: 'An account with this email address already exists.',
  },
  INVALID_RESET_REQUEST: {
    key: 'auth.errors.invalidResetRequest',
    defaultMessage: 'Invalid or expired password reset request. Please request a new link.',
  },
  METHOD_NOT_ALLOWED: {
    key: 'common.errors.methodNotAllowed',
    defaultMessage: 'HTTP method not allowed for this endpoint.',
  },
  NETWORK_ERROR: {
    key: 'common.errors.networkError',
    defaultMessage: 'Unable to reach the server. Please check your internet connection.',
  },
};

export const UNKNOWN_ERROR_FALLBACK: ErrorTranslationSpec = {
  key: 'common.errors.unknown',
  defaultMessage: 'An unexpected error occurred. Please try again or contact support.',
};

/**
 * Standardized API client error class capturing the full error contract.
 */
export class ApiError extends Error implements ApiErrorEnvelope {
  readonly code: string;
  readonly traceId: string;
  readonly status: number;
  readonly errors?: Record<string, string[]>;
  readonly extensions?: Record<string, unknown>;
  readonly isApiError = true;

  constructor(envelope: ApiErrorEnvelope) {
    super(envelope.message);
    this.name = 'ApiError';
    this.code = envelope.code || 'UNKNOWN_ERROR';
    this.traceId = envelope.traceId || '';
    this.status = envelope.status ?? 500;
    this.errors = envelope.errors;
    this.extensions = envelope.extensions;

    // Maintain standard prototype chain
    Object.setPrototypeOf(this, ApiError.prototype);
  }

  /**
   * Retrieves the first validation error message for a given field name,
   * supporting case-insensitive lookup.
   */
  getFieldError(fieldName: string): string | undefined {
    if (!this.errors) return undefined;

    if (this.errors[fieldName]?.length) {
      return this.errors[fieldName][0];
    }

    const lower = fieldName.toLowerCase();
    for (const [key, msgs] of Object.entries(this.errors)) {
      if (key.toLowerCase() === lower && msgs.length > 0) {
        return msgs[0];
      }
    }

    return undefined;
  }

  hasFieldErrors(): boolean {
    return !!this.errors && Object.keys(this.errors).length > 0;
  }
}

/**
 * Resolves localized user-facing message, translation key, and trace ID from any error.
 * CRITICAL RULE (Section 82):
 * Never render error.message indiscriminately when it could contain a raw internal
 * exception or provider response. Always prefer localized translations or safe fallbacks.
 */
export function getLocalizedError(error: unknown): {
  code: string;
  message: string;
  traceId?: string;
  translationKey: string;
  fieldErrors?: Record<string, string[]>;
} {
  if (error instanceof ApiError) {
    const spec = ERROR_TRANSLATIONS[error.code];
    if (spec) {
      return {
        code: error.code,
        message: spec.defaultMessage,
        traceId: error.traceId || undefined,
        translationKey: spec.key,
        fieldErrors: error.errors,
      };
    }

    // For unknown codes, return safe fallback and preserve trace ID for support
    return {
      code: error.code,
      message: UNKNOWN_ERROR_FALLBACK.defaultMessage,
      traceId: error.traceId || undefined,
      translationKey: UNKNOWN_ERROR_FALLBACK.key,
      fieldErrors: error.errors,
    };
  }

  if (error instanceof TypeError && error.message.toLowerCase().includes('fetch')) {
    const spec = ERROR_TRANSLATIONS.NETWORK_ERROR;
    return {
      code: 'NETWORK_ERROR',
      message: spec.defaultMessage,
      translationKey: spec.key,
    };
  }

  if (error instanceof Error) {
    return {
      code: 'UNKNOWN_ERROR',
      message: UNKNOWN_ERROR_FALLBACK.defaultMessage,
      translationKey: UNKNOWN_ERROR_FALLBACK.key,
    };
  }

  return {
    code: 'UNKNOWN_ERROR',
    message: UNKNOWN_ERROR_FALLBACK.defaultMessage,
    translationKey: UNKNOWN_ERROR_FALLBACK.key,
  };
}

/**
 * Standard fetch response parser adhering to Section 82.
 * Validates HTTP status and parses either successful payloads or structured ApiError envelopes.
 * Preserves server-generated trace IDs from the payload or the X-Correlation-ID header.
 */
export async function parseApiResponse<T>(response: Response): Promise<T> {
  if (response.ok) {
    if (response.status === 204) {
      return undefined as unknown as T;
    }
    const contentType = response.headers.get('content-type') || '';
    if (contentType.includes('application/json')) {
      return response.json();
    }
    return (await response.text()) as unknown as T;
  }

  // Operation failed: extract correlation ID and parse error envelope
  const headerCorrelationId = response.headers.get('X-Correlation-ID') || response.headers.get('x-correlation-id') || '';

  let body: any = null;
  try {
    const text = await response.text();
    if (text) {
      body = JSON.parse(text);
    }
  } catch {
    // Non-JSON failure body (e.g., gateway 502/504 HTML)
  }

  const code = body?.code || (response.status === 404 ? 'NOT_FOUND' :
               response.status === 401 ? 'UNAUTHENTICATED' :
               response.status === 403 ? 'FORBIDDEN' :
               response.status === 409 ? 'CONFLICT' :
               response.status === 429 ? 'RATE_LIMITED' :
               response.status === 503 ? 'SERVICE_UNAVAILABLE' :
               response.status >= 500 ? 'INTERNAL_SERVER_ERROR' : 'BAD_REQUEST');

  const traceId = body?.traceId || headerCorrelationId || '';
  const message = body?.message || ERROR_TRANSLATIONS[code]?.defaultMessage || UNKNOWN_ERROR_FALLBACK.defaultMessage;

  const envelope: ApiErrorEnvelope = {
    code,
    message,
    traceId,
    status: response.status,
    errors: body?.errors,
    extensions: body?.extensions,
  };

  throw new ApiError(envelope);
}

/**
 * Standard HTTP client wrapper ensuring proper headers, credentials, and error envelope parsing.
 */
export async function apiClient<T>(url: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers || {});
  if (!headers.has('Content-Type') && init?.body && typeof init.body === 'string') {
    headers.set('Content-Type', 'application/json');
  }

  try {
    const response = await fetch(url, {
      ...init,
      headers,
    });
    return await parseApiResponse<T>(response);
  } catch (err) {
    if (err instanceof ApiError) {
      throw err;
    }
    // Convert fetch network failures into ApiError
    throw new ApiError({
      code: 'NETWORK_ERROR',
      message: ERROR_TRANSLATIONS.NETWORK_ERROR.defaultMessage,
      traceId: '',
      status: 0,
    });
  }
}
