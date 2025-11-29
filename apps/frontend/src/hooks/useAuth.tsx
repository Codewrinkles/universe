/**
 * Auth context and hook
 * Manages authentication state across the application
 */

import { createContext, useContext, useState, useEffect, useCallback } from "react";
import type { ReactNode } from "react";
import type { User, RegisterRequest, LoginRequest, UpdateProfileRequest, ChangePasswordRequest } from "../types";
import { config } from "../config";
import { authApi } from "../services/authApi";
import { profileApi } from "../services/profileApi";
import { setAuthTokens, clearAuthData, isTokenExpired } from "../utils/api";

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  register: (data: RegisterRequest) => Promise<void>;
  login: (data: LoginRequest) => Promise<void>;
  logout: () => void;
  updateProfile: (data: UpdateProfileRequest) => Promise<void>;
  updateAvatar: (file: File) => Promise<string>;
  changePassword: (currentPassword: string, newPassword: string) => Promise<void>;
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
          // Validate token is not expired
          if (isTokenExpired(token)) {
            // Token expired, clear auth data
            clearAuthData();
          } else {
            // Token valid, restore user session
            const parsedUser = JSON.parse(savedUser) as User;
            setUser(parsedUser);
          }
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
      bio: null,
      avatarUrl: null,
      location: null,
      websiteUrl: null,
      role: response.role,
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
      bio: response.bio,
      avatarUrl: response.avatarUrl,
      location: response.location,
      websiteUrl: response.websiteUrl,
      role: response.role,
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

  // Listen for 401 unauthorized events from API calls
  // Automatically logout user when token expires
  useEffect(() => {
    const handleUnauthorized = (): void => {
      logout();
    };

    window.addEventListener("auth:unauthorized", handleUnauthorized);
    return () => {
      window.removeEventListener("auth:unauthorized", handleUnauthorized);
    };
  }, [logout]);

  /**
   * Update user profile
   * Updates name, bio, and handle
   */
  const updateProfile = useCallback(async (data: UpdateProfileRequest): Promise<void> => {
    if (!user) {
      throw new Error("Not authenticated");
    }

    const response = await profileApi.updateProfile(user.profileId, data);

    // Update user state with new profile data
    const updatedUser: User = {
      ...user,
      name: response.name,
      handle: response.handle,
      bio: response.bio,
      avatarUrl: response.avatarUrl,
      location: response.location,
      websiteUrl: response.websiteUrl,
    };

    // Persist to localStorage
    localStorage.setItem(config.auth.userKey, JSON.stringify(updatedUser));

    // Update state
    setUser(updatedUser);
  }, [user]);

  /**
   * Upload new avatar
   * Returns the new avatar URL
   */
  const updateAvatar = useCallback(async (file: File): Promise<string> => {
    if (!user) {
      throw new Error("Not authenticated");
    }

    const response = await profileApi.uploadAvatar(user.profileId, file);

    // Update user state with new avatar URL
    const updatedUser: User = {
      ...user,
      avatarUrl: response.avatarUrl,
    };

    // Persist to localStorage
    localStorage.setItem(config.auth.userKey, JSON.stringify(updatedUser));

    // Update state
    setUser(updatedUser);

    return response.avatarUrl;
  }, [user]);

  /**
   * Change user password
   * Requires current password verification
   * IdentityId is extracted from JWT token on the backend
   */
  const changePassword = useCallback(async (currentPassword: string, newPassword: string): Promise<void> => {
    if (!user) {
      throw new Error("Not authenticated");
    }

    const request: ChangePasswordRequest = {
      currentPassword,
      newPassword,
    };

    await authApi.changePassword(request);
  }, [user]);

  const value: AuthContextType = {
    user,
    isAuthenticated: user !== null,
    isLoading,
    register,
    login,
    logout,
    updateProfile,
    updateAvatar,
    changePassword,
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
