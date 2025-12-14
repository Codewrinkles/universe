import type { Message } from "../../types";
import { useAuth } from "../../../../hooks/useAuth";
import { buildAvatarUrl } from "../../../../utils/avatarUtils";

interface UserMessageProps {
  message: Message;
}

/**
 * Get initials from a name (up to 2 characters)
 */
function getInitials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return "?";
  }
  const firstPart = parts[0];
  if (parts.length === 1 || !firstPart) {
    return (firstPart ?? "?").slice(0, 2).toUpperCase();
  }
  const lastPart = parts[parts.length - 1];
  const firstChar = firstPart[0] ?? "";
  const lastChar = lastPart?.[0] ?? "";
  return (firstChar + lastChar).toUpperCase();
}

/**
 * UserMessage - User's message bubble
 *
 * Shows the user's profile image and name from auth context.
 */
export function UserMessage({ message }: UserMessageProps): JSX.Element {
  const { user } = useAuth();
  const avatarUrl = user?.avatarUrl ? buildAvatarUrl(user.avatarUrl) : null;
  const displayName = user?.name ?? "You";
  const initials = user?.name ? getInitials(user.name) : "?";

  return (
    <div className="flex items-start gap-3 px-4 py-3 justify-end">
      {/* Message Content */}
      <div className="max-w-[80%]">
        <div className="flex items-center gap-2 mb-1 justify-end">
          <span className="text-xs font-medium text-text-secondary">
            {displayName}
          </span>
        </div>

        <div className="px-4 py-3 rounded-2xl bg-surface-card1 border border-border text-sm text-text-primary leading-relaxed whitespace-pre-wrap">
          {message.content}
        </div>
      </div>

      {/* User Avatar */}
      <div className="flex-shrink-0 w-8 h-8 rounded-full overflow-hidden bg-brand-soft flex items-center justify-center text-xs font-semibold text-black">
        {avatarUrl ? (
          <img
            src={avatarUrl}
            alt={displayName}
            className="h-full w-full object-cover"
          />
        ) : (
          initials
        )}
      </div>
    </div>
  );
}
