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
 */
export async function apiRequest<TResponse>(
  endpoint: string,
  options: RequestInit = {}
): Promise<TResponse> {
  const token = getAccessToken();

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
    if (response.status === 401) {
      // Emit event to notify auth context to logout user
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
