/**
 * API client utility
 * Centralized fetch wrapper with error handling and auth token management
 */

import { jwtDecode } from "jwt-decode";
import type { ApiErrorResponse, ValidationErrorItem } from "../types";
import { config } from "../config";

/**
 * Custom error class for API errors
 * Provides structured error information for different error types
 */
export class ApiError extends Error {
  constructor(
    message: string,
    public statusCode: number,
    public validationErrors?: ValidationErrorItem[]
  ) {
    super(message);
    this.name = "ApiError";
  }

  /**
   * Check if this is a validation error (400)
   */
  isValidationError(): boolean {
    return this.statusCode === 400;
  }

  /**
   * Check if this is a conflict error (409 - e.g., duplicate email)
   */
  isConflictError(): boolean {
    return this.statusCode === 409;
  }

  /**
   * Check if this is an unauthorized error (401)
   */
  isUnauthorizedError(): boolean {
    return this.statusCode === 401;
  }

  /**
   * Get validation error for a specific field
   */
  getFieldError(field: string): string | undefined {
    const normalizedField = field.toLowerCase();
    return this.validationErrors?.find(
      (e) => e.property.toLowerCase() === normalizedField
    )?.message;
  }
}

/**
 * Get the stored access token
 */
export function getAccessToken(): string | null {
  return localStorage.getItem(config.auth.accessTokenKey);
}

/**
 * Store authentication tokens
 */
export function setAuthTokens(accessToken: string, refreshToken: string): void {
  localStorage.setItem(config.auth.accessTokenKey, accessToken);
  localStorage.setItem(config.auth.refreshTokenKey, refreshToken);
}

/**
 * Clear all authentication data
 */
export function clearAuthData(): void {
  localStorage.removeItem(config.auth.accessTokenKey);
  localStorage.removeItem(config.auth.refreshTokenKey);
  localStorage.removeItem(config.auth.userKey);
}

/**
 * Check if a JWT token is expired
 * Returns true if token is invalid or expired
 */
export function isTokenExpired(token: string): boolean {
  try {
    const decoded = jwtDecode<{ exp: number }>(token);
    // JWT exp is in seconds, Date.now() is in milliseconds
    return decoded.exp * 1000 < Date.now();
  } catch {
    // Invalid token format = treat as expired
    return true;
  }
}

/**
 * Check if a JWT token will expire soon (within 5 minutes)
 * Used for proactive token refresh
 */
export function isTokenExpiringSoon(token: string): boolean {
  try {
    const decoded = jwtDecode<{ exp: number }>(token);
    const expirationTime = decoded.exp * 1000;
    const now = Date.now();
    const fiveMinutes = 5 * 60 * 1000;

    // Token expires in less than 5 minutes
    return expirationTime - now < fiveMinutes;
  } catch {
    // Invalid token format = treat as expiring
    return true;
  }
}

/**
 * Get the stored refresh token
 */
export function getRefreshToken(): string | null {
  return localStorage.getItem(config.auth.refreshTokenKey);
}

// Track if a token refresh is in progress to prevent concurrent refresh attempts
let isRefreshing = false;
let refreshPromise: Promise<void> | null = null;

/**
 * Refresh the access token using the refresh token
 * Returns true if successful, false if refresh failed
 */
export async function refreshAccessToken(): Promise<boolean> {
  // If a refresh is already in progress, wait for it to complete
  if (isRefreshing && refreshPromise) {
    try {
      await refreshPromise;
      return true;
    } catch {
      return false;
    }
  }

  const refreshToken = getRefreshToken();

  if (!refreshToken) {
    return false;
  }

  // Mark refresh as in progress
  isRefreshing = true;
  refreshPromise = (async () => {
    try {
      const response = await fetch(`${config.api.baseUrl}/api/identity/refresh`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ refreshToken }),
      });

      if (!response.ok) {
        // Refresh failed - clear auth data and emit event
        clearAuthData();
        window.dispatchEvent(new CustomEvent("auth:session-expired"));
        throw new Error("Token refresh failed");
      }

      const data = await response.json() as { accessToken: string; refreshToken: string };

      // Store new tokens
      setAuthTokens(data.accessToken, data.refreshToken);
    } finally {
      isRefreshing = false;
      refreshPromise = null;
    }
  })();

  try {
    await refreshPromise;
    return true;
  } catch {
    return false;
  }
}

/**
 * Parse error response from API
 * Handles ASP.NET Core ProblemDetails format
 */
async function parseErrorResponse(response: Response): Promise<ApiError> {
  let errorData: ApiErrorResponse;

  try {
    errorData = await response.json();
  } catch {
    return new ApiError(
      `Request failed with status ${response.status}`,
      response.status
    );
  }

  // Handle validation errors (400) with detailed field errors
  // ASP.NET Core sends: { errors: { "FieldName": ["error1", "error2"] } }
  if (response.status === 400 && errorData.errors) {
    const validationErrors: ValidationErrorItem[] = [];

    for (const [field, messages] of Object.entries(errorData.errors)) {
      if (Array.isArray(messages)) {
        const firstMessage = messages[0];
        if (firstMessage) {
          validationErrors.push({
            property: field,
            message: firstMessage,
          });
        }
      }
    }

    return new ApiError(
      errorData.detail || errorData.title || "Validation failed",
      response.status,
      validationErrors
    );
  }

  // Handle other errors with simple message
  const message =
    errorData.detail ||
    errorData.error ||
    errorData.title ||
    `Request failed with status ${response.status}`;

  return new ApiError(message, response.status);
}

/**
 * Generic API request function
 * Handles authentication, error parsing, and type safety
 * Includes proactive token refresh and 401 retry logic
 */
export async function apiRequest<TResponse>(
  endpoint: string,
  options: RequestInit = {},
  isRetry = false
): Promise<TResponse> {
  let token = getAccessToken();

  // Proactive token refresh: if token is expiring soon, refresh it before making the request
  if (token && !isRetry && isTokenExpiringSoon(token)) {
    const refreshed = await refreshAccessToken();
    if (refreshed) {
      token = getAccessToken(); // Get the new token
    }
  }

  const headers: HeadersInit = {
    ...options.headers,
  };

  // Only set Content-Type if body is not FormData
  // (FormData sets its own Content-Type with boundary)
  if (!(options.body instanceof FormData)) {
    (headers as Record<string, string>)["Content-Type"] = "application/json";
  }

  if (token) {
    (headers as Record<string, string>)["Authorization"] = `Bearer ${token}`;
  }

  let response: Response;

  try {
    response = await fetch(endpoint, {
      ...options,
      headers,
    });
  } catch (error) {
    // Network error (server unreachable, CORS, etc.)
    throw new ApiError(
      "Unable to connect to the server. Please check your connection.",
      0
    );
  }

  if (!response.ok) {
    // Handle 401 Unauthorized - token expired or invalid
    if (response.status === 401 && !isRetry) {
      // Attempt to refresh the token and retry the request once
      const refreshed = await refreshAccessToken();

      if (refreshed) {
        // Retry the request with the new token
        return apiRequest<TResponse>(endpoint, options, true);
      }

      // Refresh failed - emit event to notify auth context
      window.dispatchEvent(new CustomEvent("auth:unauthorized"));
    }

    throw await parseErrorResponse(response);
  }

  // Handle 204 No Content
  if (response.status === 204) {
    return undefined as TResponse;
  }

  return response.json();
}
