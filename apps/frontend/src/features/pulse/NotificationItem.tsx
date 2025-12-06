import { Link } from "react-router";
import type { Notification } from "../../types/notification";
import { formatTimeAgo } from "../../utils/timeUtils";
import { config } from "../../config";

export interface NotificationItemProps {
  notification: Notification;
  onMarkAsRead: (id: string) => void;
  onDelete: (id: string) => void;
}

export function NotificationItem({ notification, onMarkAsRead, onDelete }: NotificationItemProps): JSX.Element {
  const { actor, type, entityId, isRead, createdAt } = notification;

  const avatarUrl = actor.avatarUrl
    ? (actor.avatarUrl.startsWith('http') ? actor.avatarUrl : `${config.api.baseUrl}${actor.avatarUrl}`)
    : null;

  const timeAgo = formatTimeAgo(createdAt);

  const handleClick = (): void => {
    if (!isRead) {
      onMarkAsRead(notification.id);
    }
  };

  const handleDelete = (e: React.MouseEvent): void => {
    e.preventDefault();
    e.stopPropagation();
    onDelete(notification.id);
  };

  // Determine notification message and link
  let message = "";
  let linkTo = `/pulse/u/${actor.handle}`;

  switch (type) {
    case "pulselike":
      message = "liked your pulse";
      linkTo = entityId ? `/pulse/${entityId}` : linkTo;
      break;
    case "pulsereply":
      message = "replied to your pulse";
      linkTo = entityId ? `/pulse/${entityId}` : linkTo;
      break;
    case "pulserepulse":
      message = "re-pulsed your pulse";
      linkTo = entityId ? `/pulse/${entityId}` : linkTo;
      break;
    case "pulsemention":
      message = "mentioned you in a pulse";
      linkTo = entityId ? `/pulse/${entityId}` : linkTo;
      break;
    case "follow":
      message = "followed you";
      break;
  }

  return (
    <Link
      to={linkTo}
      onClick={handleClick}
      className={`group block px-4 py-3 hover:bg-surface-card1/50 transition-colors border-b border-border ${
        !isRead ? "bg-brand-soft/5" : ""
      }`}
    >
      <div className="flex gap-3">
        {/* Avatar */}
        <div className="flex-shrink-0">
          {avatarUrl ? (
            <img
              src={avatarUrl}
              alt={actor.name}
              className="h-10 w-10 rounded-full object-cover"
            />
          ) : (
            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-surface-card2 text-sm font-semibold text-text-primary border border-border">
              {actor.name.charAt(0).toUpperCase()}
            </div>
          )}
        </div>

        {/* Content */}
        <div className="flex-1 min-w-0">
          <div className="flex items-start justify-between gap-2">
            <div className="min-w-0 flex-1">
              <p className="text-sm text-text-primary">
                <span className="font-semibold">{actor.name}</span>{" "}
                <span className="text-text-secondary">{message}</span>
              </p>
              <p className="text-xs text-text-tertiary mt-0.5">{timeAgo}</p>
            </div>
            <div className="flex items-center gap-2 flex-shrink-0">
              <button
                type="button"
                onClick={handleDelete}
                className="opacity-0 group-hover:opacity-100 hover:bg-red-500/10 rounded-full p-1.5 transition-all text-red-500 text-2xl font-bold leading-none"
                title="Delete notification"
              >
                ×
              </button>
              {!isRead && (
                <div className="h-2 w-2 rounded-full bg-brand-soft" title="Unread" />
              )}
            </div>
          </div>
        </div>
      </div>
    </Link>
  );
}
