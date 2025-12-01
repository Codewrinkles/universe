import { Routes, Route, Navigate } from "react-router-dom";
import { useTheme } from "./hooks/useTheme";
import { useAuth } from "./hooks/useAuth";
import { ShellLayout } from "./features/shell/ShellLayout";
import { LandingPage } from "./features/landing/LandingPage";
import { PulsePage } from "./features/pulse/PulsePage";
import { ThreadView } from "./features/pulse/ThreadView";
import { ProfilePage } from "./features/pulse/ProfilePage";
import { NotificationsPage } from "./features/pulse/NotificationsPage";
import { BookmarksPage } from "./features/pulse/BookmarksPage";
import { HashtagPage } from "./features/pulse/HashtagPage";
import { SearchResultsPage } from "./features/pulse/SearchResultsPage";
import { SettingsPage } from "./features/settings/SettingsPage";
import {
  SettingsProfile,
  SettingsAccount,
  SettingsApps,
  SettingsNotifications,
} from "./features/settings/SettingsSections";
import { LoginPage } from "./features/auth/LoginPage";
import { RegisterPage } from "./features/auth/RegisterPage";
import { OAuthSuccessPage } from "./features/auth/OAuthSuccessPage";
import { OAuthErrorPage } from "./features/auth/OAuthErrorPage";
import { OnboardingFlow } from "./features/onboarding/OnboardingFlow";
import { ProtectedRoute } from "./features/auth/ProtectedRoute";
import { AdminRoute } from "./features/auth/AdminRoute";
import { AdminPage } from "./features/admin/AdminPage";
import { DashboardPage } from "./features/admin/DashboardPage";
import { TermsPage, PrivacyPage } from "./features/legal";

/**
 * Main application component that manages:
 * - Routing with React Router
 * - Theme state with localStorage persistence
 * - Route protection based on authentication
 */
export function App(): JSX.Element {
  const { theme, toggleTheme } = useTheme();
  const { isLoading, isAuthenticated } = useAuth();

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
      {/* Landing page - unauthenticated only */}
      <Route path="/" element={<ShellLayout theme={theme} onThemeToggle={toggleTheme} />}>
        <Route index element={isAuthenticated ? <Navigate to="/pulse" replace /> : <LandingPage />} />
      </Route>

      {/* Public routes with Shell layout - accessible without authentication */}
      <Route path="/" element={<ShellLayout theme={theme} onThemeToggle={toggleTheme} />}>
        <Route path="pulse" element={<PulsePage />} />
        <Route path="pulse/search" element={<SearchResultsPage />} />
        <Route path="pulse/notifications" element={<NotificationsPage />} />
        <Route path="pulse/bookmarks" element={<BookmarksPage />} />
        <Route path="pulse/u/:handle" element={<ProfilePage />} />
        <Route path="pulse/:pulseId" element={<ThreadView />} />
        <Route path="social" element={<Navigate to="/pulse" replace />} />
        <Route path="social/hashtag/:tag" element={<HashtagPage />} />
      </Route>

      {/* Protected routes - require authentication */}
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <ShellLayout theme={theme} onThemeToggle={toggleTheme} />
          </ProtectedRoute>
        }
      >
        <Route path="settings" element={<SettingsPage />}>
          <Route index element={<Navigate to="/settings/profile" replace />} />
          <Route path="profile" element={<SettingsProfile />} />
          <Route path="account" element={<SettingsAccount />} />
          <Route path="apps" element={<SettingsApps />} />
          <Route path="notifications" element={<SettingsNotifications />} />
        </Route>
        <Route path="onboarding" element={<OnboardingFlow />} />
      </Route>

      {/* Admin routes - require admin role */}
      <Route
        path="/"
        element={
          <AdminRoute>
            <ShellLayout theme={theme} onThemeToggle={toggleTheme} />
          </AdminRoute>
        }
      >
        <Route path="admin" element={<AdminPage />}>
          <Route index element={<DashboardPage />} />
        </Route>
      </Route>

      {/* Public routes - accessible without authentication */}
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/auth/success" element={<OAuthSuccessPage />} />
      <Route path="/auth/error" element={<OAuthErrorPage />} />
      <Route path="/terms" element={<TermsPage />} />
      <Route path="/privacy" element={<PrivacyPage />} />

      {/* Catch-all redirect - auth-aware */}
      <Route path="*" element={<Navigate to={isAuthenticated ? "/pulse" : "/"} replace />} />
    </Routes>
  );
}
