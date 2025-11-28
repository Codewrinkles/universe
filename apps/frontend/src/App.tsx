import { Routes, Route, Navigate } from "react-router-dom";
import { useTheme } from "./hooks/useTheme";
import { useAuth } from "./hooks/useAuth";
import { ShellLayout } from "./features/shell/ShellLayout";
import { HomePage } from "./features/home/HomePage";
import { PulsePage } from "./features/pulse/PulsePage";
import { ThreadView } from "./features/pulse/ThreadView";
import { TwinPage } from "./features/twin/TwinPage";
import { SettingsPage } from "./features/settings/SettingsPage";
import {
  SettingsProfile,
  SettingsAccount,
  SettingsApps,
  SettingsNotifications,
} from "./features/settings/SettingsSections";
import { LoginPage } from "./features/auth/LoginPage";
import { RegisterPage } from "./features/auth/RegisterPage";
import { OnboardingPage } from "./features/onboarding/OnboardingPage";
import { ProtectedRoute } from "./features/auth/ProtectedRoute";

/**
 * Main application component that manages:
 * - Routing with React Router
 * - Theme state with localStorage persistence
 * - Route protection based on authentication
 */
export function App(): JSX.Element {
  const { theme, toggleTheme } = useTheme();
  const { isLoading } = useAuth();

  // Show loading state while checking authentication
  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface-page">
        <div className="text-text-secondary">Loading...</div>
      </div>
    );
  }

  return (
    <Routes>
      {/* Protected routes - require authentication */}
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <ShellLayout theme={theme} onThemeToggle={toggleTheme} />
          </ProtectedRoute>
        }
      >
        <Route index element={<HomePage />} />
        <Route path="pulse" element={<PulsePage />} />
        <Route path="pulse/:pulseId" element={<ThreadView />} />
        <Route path="nova" element={<TwinPage />} />
        {/* Redirects for old routes */}
        <Route path="social" element={<Navigate to="/pulse" replace />} />
        <Route path="learn" element={<Navigate to="/nova" replace />} />
        <Route path="twin" element={<Navigate to="/nova" replace />} />
        <Route path="settings" element={<SettingsPage />}>
          <Route index element={<Navigate to="/settings/profile" replace />} />
          <Route path="profile" element={<SettingsProfile />} />
          <Route path="account" element={<SettingsAccount />} />
          <Route path="apps" element={<SettingsApps />} />
          <Route path="notifications" element={<SettingsNotifications />} />
        </Route>
        <Route path="onboarding" element={<OnboardingPage />} />
      </Route>

      {/* Public routes - accessible without authentication */}
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      {/* Catch-all redirect */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
