import { Routes, Route, Navigate } from "react-router-dom";
import { useTheme } from "./hooks/useTheme";
import { useAuth } from "./hooks/useAuth";
import { ShellLayout } from "./features/shell/ShellLayout";
import { LandingPage } from "./features/landing/LandingPage";
import { PulseLayout } from "./features/pulse/PulseLayout";
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
} from "./features/settings/SettingsSections";
import { LoginPage } from "./features/auth/LoginPage";
import { RegisterPage } from "./features/auth/RegisterPage";
import { OAuthSuccessPage } from "./features/auth/OAuthSuccessPage";
import { OAuthErrorPage } from "./features/auth/OAuthErrorPage";
import { OnboardingFlow } from "./features/onboarding/OnboardingFlow";
import { ProtectedRoute } from "./features/auth/ProtectedRoute";
import { AdminRoute } from "./features/auth/AdminRoute";
import { NovaRoute } from "./features/auth/NovaRoute";
import { AdminPage } from "./features/admin/AdminPage";
import { DashboardPage } from "./features/admin/DashboardPage";
import { AlphaApplicationsPage } from "./features/admin/AlphaApplicationsPage";
import { TermsPage, PrivacyPage } from "./features/legal";
import { NovaLayout, NovaChatPage, NovaSettingsPage } from "./features/nova";
import { AlphaApplyPage, AlphaRedeemPage } from "./features/alpha";

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
        {/* Pulse routes wrapped in PulseLayout for shared composer modal */}
        <Route element={<PulseLayout />}>
          <Route path="pulse" element={<PulsePage />} />
          <Route path="pulse/search" element={<SearchResultsPage />} />
          <Route path="pulse/notifications" element={<NotificationsPage />} />
          <Route path="pulse/bookmarks" element={<BookmarksPage />} />
          <Route path="pulse/u/:handle" element={<ProfilePage />} />
          <Route path="pulse/:pulseId" element={<ThreadView />} />
          <Route path="social" element={<Navigate to="/pulse" replace />} />
          <Route path="social/hashtag/:tag" element={<HashtagPage />} />
        </Route>
      </Route>

      {/* Nova - requires authentication AND Nova access */}
      <Route
        path="/"
        element={
          <NovaRoute>
            <ShellLayout theme={theme} onThemeToggle={toggleTheme} />
          </NovaRoute>
        }
      >
        <Route path="nova" element={<NovaLayout />}>
          <Route index element={<Navigate to="/nova/c/new" replace />} />
          <Route path="c/new" element={<NovaChatPage />} />
          <Route path="c/:conversationId" element={<NovaChatPage />} />
          <Route path="settings" element={<NovaSettingsPage />} />
        </Route>
      </Route>

      {/* Protected routes - require authentication, redirects to login */}
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
          <Route path="alpha" element={<AlphaApplicationsPage />} />
        </Route>
      </Route>

      {/* Public routes - accessible without authentication */}
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/auth/success" element={<OAuthSuccessPage />} />
      <Route path="/auth/error" element={<OAuthErrorPage />} />
      <Route path="/terms" element={<TermsPage />} />
      <Route path="/privacy" element={<PrivacyPage />} />
      <Route path="/alpha/apply" element={<AlphaApplyPage />} />
      <Route path="/nova/redeem" element={<AlphaRedeemPage />} />

      {/* Catch-all redirect - auth-aware */}
      <Route path="*" element={<Navigate to={isAuthenticated ? "/pulse" : "/"} replace />} />
    </Routes>
  );
}
