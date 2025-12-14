import { Link, NavLink, useLocation } from "react-router-dom";
import type { Conversation } from "./types";

// Mock data for development - will be replaced with real API calls
// Flat list of recent conversations (no time groupings)
const MOCK_CONVERSATIONS: Conversation[] = [
  {
    id: "conv-1",
    title: "Clean Architecture in .NET",
    createdAt: new Date().toISOString(),
    lastMessageAt: new Date().toISOString(),
    messageCount: 8,
    topicEmoji: "🏗️",
  },
  {
    id: "conv-2",
    title: "CQRS vs traditional MVC",
    createdAt: new Date().toISOString(),
    lastMessageAt: new Date().toISOString(),
    messageCount: 5,
    topicEmoji: "📦",
  },
  {
    id: "conv-3",
    title: "DDD aggregate design",
    createdAt: new Date(Date.now() - 86400000).toISOString(),
    lastMessageAt: new Date(Date.now() - 86400000).toISOString(),
    messageCount: 12,
    topicEmoji: "🎯",
  },
  {
    id: "conv-4",
    title: "Microservices communication",
    createdAt: new Date(Date.now() - 259200000).toISOString(),
    lastMessageAt: new Date(Date.now() - 259200000).toISOString(),
    messageCount: 15,
    topicEmoji: "🔗",
  },
  {
    id: "conv-5",
    title: "Event sourcing basics",
    createdAt: new Date(Date.now() - 345600000).toISOString(),
    lastMessageAt: new Date(Date.now() - 345600000).toISOString(),
    messageCount: 7,
    topicEmoji: "📝",
  },
];

interface NovaSidebarProps {
  onMobileClose: () => void;
}

export function NovaSidebar({ onMobileClose }: NovaSidebarProps): JSX.Element {
  const location = useLocation();

  return (
    <nav className="h-full flex flex-col bg-surface-page border-r border-border">
      {/* Header */}
      <div className="p-4 border-b border-border">
        <div className="flex items-center justify-between mb-4 lg:hidden">
          <span className="text-sm font-semibold text-text-primary">Nova</span>
          <button
            type="button"
            onClick={onMobileClose}
            className="flex items-center justify-center w-8 h-8 rounded-lg text-text-secondary hover:bg-surface-card1 transition-colors"
            aria-label="Close sidebar"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* New Chat Button */}
        <Link
          to="/nova/c/new"
          onClick={onMobileClose}
          className="flex items-center gap-2 w-full px-4 py-2.5 rounded-xl bg-violet-500/20 border border-violet-500/40 text-violet-400 hover:bg-violet-500/30 transition-colors text-sm font-medium"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
          New Chat
        </Link>
      </div>

      {/* Conversations List - flat list, no time groupings */}
      <div className="p-3">
        <h3 className="px-2 mb-2 text-[11px] font-semibold uppercase tracking-wider text-text-tertiary">
          Recent Chats
        </h3>
        <div className="space-y-1">
          {MOCK_CONVERSATIONS.map((conversation) => {
            const isActive = location.pathname === `/nova/c/${conversation.id}`;
            return (
              <NavLink
                key={conversation.id}
                to={`/nova/c/${conversation.id}`}
                onClick={onMobileClose}
                className={`
                  block px-3 py-2 rounded-xl text-sm transition-colors truncate
                  ${isActive
                    ? "bg-violet-500/20 border border-violet-500/40 text-text-primary"
                    : "text-text-secondary hover:bg-surface-card1 hover:text-text-primary"
                  }
                `}
              >
                {conversation.title}
              </NavLink>
            );
          })}
        </div>
      </div>

      {/* Learning Paths Preview (Future - M8) */}
      <div className="p-3 border-t border-border">
        <h3 className="px-2 mb-2 text-[11px] font-semibold uppercase tracking-wider text-text-tertiary">
          Learning Paths
        </h3>
        <div className="px-2 py-3 rounded-xl bg-surface-card1 border border-border">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-medium text-text-primary">Clean Architecture</span>
            <span className="text-[10px] text-text-tertiary">65%</span>
          </div>
          <div className="h-1.5 bg-surface-card2 rounded-full overflow-hidden">
            <div
              className="h-full bg-gradient-to-r from-violet-500 to-violet-400 rounded-full"
              style={{ width: "65%" }}
            />
          </div>
          <p className="mt-2 text-[11px] text-text-tertiary">4 of 6 topics completed</p>
        </div>
        <button
          type="button"
          className="mt-2 w-full px-3 py-2 text-xs text-text-secondary hover:text-violet-400 transition-colors text-center"
          disabled
        >
          View all paths (coming soon)
        </button>
      </div>
    </nav>
  );
}
