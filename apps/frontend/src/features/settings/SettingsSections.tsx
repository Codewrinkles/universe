import { useState, useRef, useEffect } from "react";
import { Toggle } from "../../components/ui/Toggle";
import { useAuth } from "../../hooks/useAuth";
import { useProfile } from "../../hooks/useProfile";
import { buildAvatarUrl } from "../../utils/avatarUtils";

interface FieldProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  maxLength?: number;
  disabled?: boolean;
}

function Field({ label, value, onChange, placeholder, maxLength, disabled }: FieldProps): JSX.Element {
  return (
    <div className="space-y-1">
      <label className="block text-xs text-text-tertiary">{label}</label>
      <input
        type="text"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        maxLength={maxLength}
        disabled={disabled}
        className="w-full rounded-xl border border-border bg-surface-page px-3 py-2 text-sm text-text-primary placeholder:text-text-tertiary focus:outline-none focus:ring-2 focus:ring-brand-soft/70 focus:ring-offset-2 focus:ring-offset-surface-page disabled:opacity-50"
      />
    </div>
  );
}

interface TextAreaFieldProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  maxLength?: number;
  disabled?: boolean;
}

function TextAreaField({ label, value, onChange, placeholder, maxLength, disabled }: TextAreaFieldProps): JSX.Element {
  return (
    <div className="space-y-1">
      <div className="flex items-center justify-between">
        <label className="block text-xs text-text-tertiary">{label}</label>
        {maxLength && (
          <span className="text-xs text-text-tertiary">{value.length}/{maxLength}</span>
        )}
      </div>
      <textarea
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        maxLength={maxLength}
        disabled={disabled}
        rows={3}
        className="w-full rounded-xl border border-border bg-surface-page px-3 py-2 text-sm text-text-primary placeholder:text-text-tertiary focus:outline-none focus:ring-2 focus:ring-brand-soft/70 focus:ring-offset-2 focus:ring-offset-surface-page disabled:opacity-50 resize-none"
      />
    </div>
  );
}

export function SettingsProfile(): JSX.Element {
  const { user, updateProfile, updateAvatar } = useAuth();
  const { profile, isLoading: isLoadingProfile, error: profileError } = useProfile();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [name, setName] = useState("");
  const [handle, setHandle] = useState("");
  const [bio, setBio] = useState("");
  const [location, setLocation] = useState("");
  const [websiteUrl, setWebsiteUrl] = useState("");

  const [isSaving, setIsSaving] = useState(false);
  const [isUploadingAvatar, setIsUploadingAvatar] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  // Sync form fields when profile data is loaded from API
  useEffect(() => {
    if (profile) {
      setName(profile.name ?? "");
      setHandle(profile.handle ?? "");
      setBio(profile.bio ?? "");
      setLocation(profile.location ?? "");
      setWebsiteUrl(profile.websiteUrl ?? "");
    }
  }, [profile]);

  const hasChanges =
    name !== (profile?.name ?? "") ||
    handle !== (profile?.handle ?? "") ||
    bio !== (profile?.bio ?? "") ||
    location !== (profile?.location ?? "") ||
    websiteUrl !== (profile?.websiteUrl ?? "");

  const handleSave = async (): Promise<void> => {
    if (!hasChanges) return;

    setIsSaving(true);
    setError(null);
    setSuccess(null);

    try {
      await updateProfile({
        name,
        handle: handle || null,
        bio: bio || null,
        location: location || null,
        websiteUrl: websiteUrl || null,
      });
      setSuccess("Profile updated successfully");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update profile");
    } finally {
      setIsSaving(false);
    }
  };

  const handleAvatarClick = (): void => {
    fileInputRef.current?.click();
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>): Promise<void> => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Validate file type
    const allowedTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
    if (!allowedTypes.includes(file.type)) {
      setError("Please upload a valid image (JPEG, PNG, GIF, or WebP)");
      return;
    }

    // Validate file size (5MB)
    if (file.size > 5 * 1024 * 1024) {
      setError("Image must be less than 5MB");
      return;
    }

    setIsUploadingAvatar(true);
    setError(null);
    setSuccess(null);

    try {
      await updateAvatar(file);
      setSuccess("Avatar updated successfully");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to upload avatar");
    } finally {
      setIsUploadingAvatar(false);
      // Reset file input
      if (fileInputRef.current) {
        fileInputRef.current.value = "";
      }
    }
  };

  const avatarUrl = buildAvatarUrl(user?.avatarUrl);

  // Show loading state while fetching profile
  if (isLoadingProfile) {
    return (
      <div className="space-y-6">
        <div className="text-sm text-text-secondary">Loading profile...</div>
      </div>
    );
  }

  // Show error if profile failed to load
  if (profileError) {
    return (
      <div className="space-y-6">
        <div className="text-xs px-3 py-2 rounded-lg bg-red-500/10 text-red-400">
          {profileError}
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-sm font-semibold tracking-tight text-text-primary">Profile picture</h2>
        <p className="text-xs text-text-tertiary mt-1">Square image, will be resized to 500x500</p>

        <div className="mt-3 flex items-center gap-4">
          <button
            type="button"
            onClick={handleAvatarClick}
            disabled={isUploadingAvatar}
            className="relative h-20 w-20 rounded-full overflow-hidden border-2 border-border hover:border-brand-soft/60 transition-colors focus:outline-none focus:ring-2 focus:ring-brand-soft/70 focus:ring-offset-2 focus:ring-offset-surface-page disabled:opacity-50"
          >
            {avatarUrl ? (
              <img
                src={avatarUrl}
                alt="Profile"
                className="h-full w-full object-cover"
              />
            ) : (
              <div className="h-full w-full bg-surface-card2 flex items-center justify-center">
                <span className="text-2xl text-text-tertiary">
                  {user?.name?.charAt(0).toUpperCase() ?? "?"}
                </span>
              </div>
            )}
            {isUploadingAvatar && (
              <div className="absolute inset-0 bg-black/50 flex items-center justify-center">
                <span className="text-xs text-white">Uploading...</span>
              </div>
            )}
          </button>
          <div className="flex flex-col gap-1">
            <button
              type="button"
              onClick={handleAvatarClick}
              disabled={isUploadingAvatar}
              className="text-xs text-brand-soft hover:text-brand transition-colors disabled:opacity-50"
            >
              {avatarUrl ? "Change photo" : "Upload photo"}
            </button>
            <span className="text-xs text-text-tertiary">JPEG, PNG, GIF, or WebP. Max 5MB.</span>
          </div>
          <input
            ref={fileInputRef}
            type="file"
            accept="image/jpeg,image/png,image/gif,image/webp"
            onChange={handleFileChange}
            className="hidden"
          />
        </div>
      </div>

      <div>
        <h2 className="text-sm font-semibold tracking-tight text-text-primary">Profile details</h2>
        <div className="mt-3 grid gap-3 sm:grid-cols-2">
          <Field
            label="Display name"
            value={name}
            onChange={setName}
            placeholder="Your name"
            maxLength={100}
            disabled={isSaving}
          />
          <Field
            label="Handle"
            value={handle}
            onChange={setHandle}
            placeholder="your_handle"
            maxLength={50}
            disabled={isSaving}
          />
        </div>
        <div className="mt-3">
          <TextAreaField
            label="Bio"
            value={bio}
            onChange={setBio}
            placeholder="A short bio about yourself..."
            maxLength={500}
            disabled={isSaving}
          />
        </div>
        <div className="mt-3 grid gap-3 sm:grid-cols-2">
          <Field
            label="Location"
            value={location}
            onChange={setLocation}
            placeholder="City, Country"
            maxLength={100}
            disabled={isSaving}
          />
          <Field
            label="Website URL"
            value={websiteUrl}
            onChange={setWebsiteUrl}
            placeholder="https://example.com"
            maxLength={500}
            disabled={isSaving}
          />
        </div>
      </div>

      {(error || success) && (
        <div className={`text-xs px-3 py-2 rounded-lg ${error ? "bg-red-500/10 text-red-400" : "bg-green-500/10 text-green-400"}`}>
          {error || success}
        </div>
      )}

      <div className="flex justify-end">
        <button
          type="button"
          onClick={handleSave}
          disabled={!hasChanges || isSaving}
          className="rounded-full bg-brand-soft px-4 py-2 text-xs font-medium text-black hover:bg-brand transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isSaving ? "Saving..." : "Save changes"}
        </button>
      </div>
    </div>
  );
}

export function SettingsAccount(): JSX.Element {
  const { user, changePassword } = useAuth();

  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const [isChangingPassword, setIsChangingPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const canSubmit =
    currentPassword.length > 0 &&
    newPassword.length >= 8 &&
    newPassword === confirmPassword;

  const handleChangePassword = async (): Promise<void> => {
    if (!canSubmit) return;

    // Client-side validation
    if (newPassword !== confirmPassword) {
      setError("New passwords do not match");
      return;
    }

    if (newPassword.length < 8) {
      setError("Password must be at least 8 characters");
      return;
    }

    const hasUpperCase = /[A-Z]/.test(newPassword);
    const hasLowerCase = /[a-z]/.test(newPassword);
    const hasDigit = /[0-9]/.test(newPassword);
    const hasSpecialChar = /[^A-Za-z0-9]/.test(newPassword);

    if (!hasUpperCase || !hasLowerCase || !hasDigit || !hasSpecialChar) {
      setError("Password must contain uppercase, lowercase, number, and special character");
      return;
    }

    setIsChangingPassword(true);
    setError(null);
    setSuccess(null);

    try {
      await changePassword(currentPassword, newPassword);
      setSuccess("Password changed successfully");
      // Clear form
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to change password");
    } finally {
      setIsChangingPassword(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="space-y-4">
        <h2 className="text-sm font-semibold tracking-tight text-text-primary">
          Account
        </h2>
        <div className="space-y-1">
          <label className="block text-xs text-text-tertiary">Email</label>
          <input
            type="email"
            value={user?.email ?? ""}
            disabled
            className="w-full rounded-xl border border-border bg-surface-page px-3 py-2 text-sm text-text-primary placeholder:text-text-tertiary disabled:opacity-50 cursor-not-allowed"
          />
        </div>
      </div>

      <div className="space-y-4">
        <h2 className="text-sm font-semibold tracking-tight text-text-primary">
          Change password
        </h2>
        <div className="space-y-3">
          <div className="space-y-1">
            <label className="block text-xs text-text-tertiary">Current password</label>
            <input
              type="password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              disabled={isChangingPassword}
              className="w-full rounded-xl border border-border bg-surface-page px-3 py-2 text-sm text-text-primary placeholder:text-text-tertiary focus:outline-none focus:ring-2 focus:ring-brand-soft/70 focus:ring-offset-2 focus:ring-offset-surface-page disabled:opacity-50"
            />
          </div>
          <div className="space-y-1">
            <label className="block text-xs text-text-tertiary">New password</label>
            <input
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              disabled={isChangingPassword}
              className="w-full rounded-xl border border-border bg-surface-page px-3 py-2 text-sm text-text-primary placeholder:text-text-tertiary focus:outline-none focus:ring-2 focus:ring-brand-soft/70 focus:ring-offset-2 focus:ring-offset-surface-page disabled:opacity-50"
            />
            <p className="text-xs text-text-tertiary">
              Min 8 characters with uppercase, lowercase, number, and special character
            </p>
          </div>
          <div className="space-y-1">
            <label className="block text-xs text-text-tertiary">Confirm new password</label>
            <input
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              disabled={isChangingPassword}
              className="w-full rounded-xl border border-border bg-surface-page px-3 py-2 text-sm text-text-primary placeholder:text-text-tertiary focus:outline-none focus:ring-2 focus:ring-brand-soft/70 focus:ring-offset-2 focus:ring-offset-surface-page disabled:opacity-50"
            />
          </div>
        </div>

        {(error || success) && (
          <div className={`text-xs px-3 py-2 rounded-lg ${error ? "bg-red-500/10 text-red-400" : "bg-green-500/10 text-green-400"}`}>
            {error || success}
          </div>
        )}

        <div className="flex justify-end">
          <button
            type="button"
            onClick={handleChangePassword}
            disabled={!canSubmit || isChangingPassword}
            className="rounded-full bg-brand-soft px-4 py-2 text-xs font-medium text-black hover:bg-brand transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isChangingPassword ? "Changing..." : "Change password"}
          </button>
        </div>
      </div>
    </div>
  );
}

export function SettingsApps(): JSX.Element {
  return (
    <div className="space-y-4">
      <h2 className="text-sm font-semibold tracking-tight text-text-primary">Connected apps</h2>
      <ul className="space-y-2 text-xs">
        <li className="flex items-center justify-between rounded-xl border border-border bg-surface-card2 px-3 py-2">
          <span>Social</span>
          <span className="rounded-full bg-sky-900/40 px-2 py-[2px] text-[11px] text-sky-200 border border-sky-700">
            Active
          </span>
        </li>
        <li className="flex items-center justify-between rounded-xl border border-border bg-surface-card2 px-3 py-2">
          <span>Learn</span>
          <span className="rounded-full bg-violet-900/40 px-2 py-[2px] text-[11px] text-violet-200 border border-violet-700">
            Active
          </span>
        </li>
        <li className="flex items-center justify-between rounded-xl border border-border bg-surface-card2 px-3 py-2">
          <span>Twin</span>
          <span className="rounded-full bg-brand-soft/10 px-2 py-[2px] text-[11px] text-brand-soft border border-brand-soft/40">
            Active
          </span>
        </li>
        <li className="flex items-center justify-between rounded-xl border border-border bg-surface-card2 px-3 py-2">
          <span>Legal</span>
          <span className="rounded-full bg-amber-900/30 px-2 py-[2px] text-[11px] text-amber-200 border border-amber-700">
            Coming soon
          </span>
        </li>
      </ul>
    </div>
  );
}

export function SettingsNotifications(): JSX.Element {
  return (
    <div className="space-y-4">
      <h2 className="text-sm font-semibold tracking-tight text-text-primary">Notifications</h2>
      <Toggle label="Email me weekly summaries" enabled={true} />
      <Toggle label="Notify me when Learn unlocks a new module" enabled={true} />
      <Toggle label="Twin recap of my week" enabled={true} />
    </div>
  );
}
