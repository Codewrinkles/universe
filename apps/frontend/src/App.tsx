import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { useTheme } from "./hooks/useTheme";
import { ShellLayout } from "./features/shell/ShellLayout";
import { HomePage } from "./features/home/HomePage";
import { PulsePage } from "./features/pulse/PulsePage";
import { LearnPage } from "./features/learn/LearnPage";
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

/**
 * Main application component that manages:
 * - Routing with React Router
 * - Theme state with localStorage persistence
 */
export function App(): JSX.Element {
  const { theme, toggleTheme } = useTheme();

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<ShellLayout theme={theme} onThemeToggle={toggleTheme} />}>
          <Route index element={<HomePage />} />
          <Route path="social" element={<PulsePage />} />
          <Route path="learn" element={<LearnPage />} />
          <Route path="twin" element={<TwinPage />} />
          <Route path="settings" element={<SettingsPage />}>
            <Route index element={<Navigate to="/settings/profile" replace />} />
            <Route path="profile" element={<SettingsProfile />} />
            <Route path="account" element={<SettingsAccount />} />
            <Route path="apps" element={<SettingsApps />} />
            <Route path="notifications" element={<SettingsNotifications />} />
          </Route>
          <Route path="login" element={<LoginPage />} />
          <Route path="register" element={<RegisterPage />} />
          <Route path="onboarding" element={<OnboardingPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
