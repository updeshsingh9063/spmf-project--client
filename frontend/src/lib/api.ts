// HTTP client for the SPFM ASP.NET Core API.
//
// Every request carries the signed-in user's Microsoft Entra access token.
// Endpoints are documented in docs/API-CONTRACT.md.

import { config } from "./config";

type TokenProvider = () => Promise<string | null>;
let tokenProvider: TokenProvider = async () => null;

/** Called by the AuthProvider once sign-in is ready. */
export function setTokenProvider(fn: TokenProvider) {
  tokenProvider = fn;
}

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
    /** Server-side correlation id, for matching a report to a log entry. */
    public traceId?: string,
  ) {
    super(message);
  }
}

/** Response plus the headers callers need (ETag for optimistic concurrency). */
export interface ApiResponse<T> {
  data: T;
  etag: string | null;
}

interface RequestOptions extends RequestInit {
  /** Sent as If-Match so a stale write is rejected rather than overwriting. */
  ifMatch?: string | null;
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<ApiResponse<T>> {
  const { ifMatch, ...init } = options;
  const token = await tokenProvider();

  let res: Response;
  try {
    res = await fetch(`${config.apiBaseUrl}${path}`, {
      ...init,
      headers: {
        Accept: "application/json",
        ...(init.body ? { "Content-Type": "application/json" } : {}),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...(ifMatch ? { "If-Match": ifMatch } : {}),
        ...init.headers,
      },
    });
  } catch {
    throw new ApiError(0, "Can't reach the SPFM API. Check that it's running and the address is correct.");
  }

  if (!res.ok) throw await toApiError(res);

  const etag = res.headers.get("ETag");
  if (res.status === 204) return { data: undefined as T, etag };
  return { data: (await res.json()) as T, etag };
}

/**
 * Turns a failed response into a message a person can act on.
 *
 * The API returns RFC 9457 ProblemDetails, so `detail` is already written for a
 * human — prefer it over anything invented here.
 */
async function toApiError(res: Response): Promise<ApiError> {
  let detail = "";
  let traceId: string | undefined;
  let fieldErrors: Record<string, string[]> | undefined;

  try {
    const body = await res.json();
    detail = body?.detail || body?.title || "";
    traceId = body?.traceId;
    fieldErrors = body?.errors;
  } catch {
    /* not JSON */
  }

  // A validation problem is reported against its fields, not as one sentence.
  if (res.status === 400 && fieldErrors) {
    const error = new ValidationError(fieldErrors, traceId);
    return error;
  }

  const message =
    res.status === 401
      ? "Your sign-in has expired. Sign in again."
      : res.status === 403
        ? "Your account doesn't have access to SPFM."
        : res.status === 404
          ? "Not found."
          : res.status === 409
            ? detail || "Someone else changed this project while you were editing it."
            : res.status === 428
              ? detail || "Re-open the project and try again."
              : detail || `The API returned an error (${res.status}).`;

  return new ApiError(res.status, message, traceId);
}

/** 400 with field-keyed messages, so the UI can place each one on its input. */
export class ValidationError extends ApiError {
  constructor(
    public fields: Record<string, string[]>,
    traceId?: string,
  ) {
    super(400, "Some details need fixing before this can be saved.", traceId);
  }

  /** Flattens to the single-message-per-field shape the form components expect. */
  toFieldErrors(): Record<string, string> {
    return Object.fromEntries(
      Object.entries(this.fields).map(([field, messages]) => [field, messages[0]]),
    );
  }
}

export const apiGet = <T>(path: string) => request<T>(path);

export const apiPost = <T>(path: string, body?: unknown) =>
  request<T>(path, { method: "POST", body: body === undefined ? undefined : JSON.stringify(body) });

export const apiPut = <T>(path: string, body: unknown, ifMatch: string | null) =>
  request<T>(path, { method: "PUT", body: JSON.stringify(body), ifMatch });

export interface Me {
  id: string;
  name: string;
  email: string;
}

export const getMe = () => apiGet<Me>("/api/v1/users/me").then((r) => r.data);
