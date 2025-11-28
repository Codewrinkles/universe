export interface Notification {
  id: string;
  actor: NotificationActor;
  type: NotificationType;
  entityId: string | null;
  isRead: boolean;
  createdAt: string;
}

export interface NotificationActor {
  id: string;
  name: string;
  handle: string;
  avatarUrl: string | null;
}

export type NotificationType =
  | 'pulselike'
  | 'pulsereply'
  | 'pulserepulse'
  | 'pulsemention'
  | 'follow';

export interface GetNotificationsResponse {
  notifications: Notification[];
  totalCount: number;
  unreadCount: number;
}

export interface UnreadCountResponse {
  unreadCount: number;
}
