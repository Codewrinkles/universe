/**
 * Auth context and hook
 * Manages authentication state across the application
 */

import { createContext, useContext, useState, useEffect, useCallback } from "react";
import type { ReactNode } from "react";
import type { User, RegisterRequest, LoginRequest } from "../types";
import { config } from "../config";
import { authApi } from "../services/authApi";
import { setAuthTokens, clearAuthData } from "../utils/api";

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  register: (data: RegisterRequest) => Promise<void>;
  login: (data: LoginRequest) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
  children: ReactNode;
}

/**
 * Auth provider component
 * Wraps the application and provides auth state and methods
 */
export function AuthProvider({ children }: AuthProviderProps): JSX.Element {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Initialize auth state from localStorage on mount
  useEffect(() => {
    const initializeAuth = (): void => {
      try {
        const token = localStorage.getItem(config.auth.accessTokenKey);
        const savedUser = localStorage.getItem(config.auth.userKey);

        if (token && savedUser) {
          const parsedUser = JSON.parse(savedUser) as User;
          setUser(parsedUser);
        }
      } catch {
        // Invalid stored data, clear it
        clearAuthData();
      } finally {
        setIsLoading(false);
      }
    };

    initializeAuth();
  }, []);

  /**
   * Register a new user
   * On success, stores tokens and user data, updates state
   */
  const register = useCallback(async (data: RegisterRequest): Promise<void> => {
    const response = await authApi.register(data);

    // Store tokens
    setAuthTokens(response.accessToken, response.refreshToken);

    // Create user object from response
    const userData: User = {
      identityId: response.identityId,
      profileId: response.profileId,
      email: response.email,
      name: response.name,
      handle: response.handle,
    };

    // Store user data
    localStorage.setItem(config.auth.userKey, JSON.stringify(userData));

    // Update state
    setUser(userData);
  }, []);

  /**
   * Login an existing user
   * On success, stores tokens and user data, updates state
   */
  const login = useCallback(async (data: LoginRequest): Promise<void> => {
    const response = await authApi.login(data);

    // Store tokens
    setAuthTokens(response.accessToken, response.refreshToken);

    // Create user object from response
    const userData: User = {
      identityId: response.identityId,
      profileId: response.profileId,
      email: response.email,
      name: response.name,
      handle: response.handle,
    };

    // Store user data
    localStorage.setItem(config.auth.userKey, JSON.stringify(userData));

    // Update state
    setUser(userData);
  }, []);

  /**
   * Logout the current user
   * Clears all auth data and resets state
   */
  const logout = useCallback((): void => {
    clearAuthData();
    setUser(null);
  }, []);

  const value: AuthContextType = {
    user,
    isAuthenticated: user !== null,
    isLoading,
    register,
    login,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

/**
 * Hook to access auth context
 * Must be used within an AuthProvider
 */
export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);

  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider");
  }

  return context;
}
