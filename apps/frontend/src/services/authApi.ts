/**
 * Auth API service
 * Handles authentication-related API calls
 */

import type { RegisterRequest, RegisterResponse, LoginRequest, LoginResponse } from "../types";
import { config } from "../config";
import { apiRequest } from "../utils/api";

export const authApi = {
  /**
   * Register a new user
   */
  register(data: RegisterRequest): Promise<RegisterResponse> {
    return apiRequest<RegisterResponse>(config.api.endpoints.register, {
      method: "POST",
      body: JSON.stringify(data),
    });
  },

  /**
   * Login with email and password
   */
  login(data: LoginRequest): Promise<LoginResponse> {
    return apiRequest<LoginResponse>(config.api.endpoints.login, {
      method: "POST",
      body: JSON.stringify(data),
    });
  },
};
