import { useState } from "react";
import { Link, NavLink, useLocation, useNavigate } from "react-router-dom";
import { useConversations } from "./coach/hooks/useConversations";
import { useLearnerProfile } from "./hooks/useLearnerProfile";

interface NovaSidebarProps {
  onMobileClose: () => void;
}

export function NovaSidebar({ onMobileClose }: NovaSidebarProps): JSX.Element {
  const location = useLocation();
  const navigate = useNavigate();
  const { conversations, isLoading, error, deleteConversation } = useConversations();
  const { profile, isLoading: isProfileLoading } = useLearnerProfile();
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const handleDelete = async (e: React.MouseEvent, conversationId: string): Promise<void> => {
    e.preventDefault();
    e.stopPropagation();

    setDeletingId(conversationId);
    try {
      await deleteConversation(conversationId);
      // If we deleted the active conversation, redirect to new chat
      if (location.pathname === `/nova/c/${conversationId}`) {
        navigate("/nova/c/new");
      }
    } catch {
      // Error handling could be improved with a toast
    } finally {
      setDeletingId(null);
    }
  };

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

      {/* Conversations List */}
      <div className="flex-1 overflow-y-auto p-3">
        <h3 className="px-2 mb-2 text-[11px] font-semibold uppercase tracking-wider text-text-tertiary">
          Recent Chats
        </h3>

        {isLoading && (
          <div className="px-3 py-8 text-center">
            <div className="inline-block w-5 h-5 border-2 border-violet-500/30 border-t-violet-500 rounded-full animate-spin" />
          </div>
        )}

        {error && (
          <div className="px-3 py-4 text-center text-xs text-red-400">
            {error}
          </div>
        )}

        {!isLoading && !error && conversations.length === 0 && (
          <div className="px-3 py-8 text-center text-xs text-text-tertiary">
            No conversations yet.
            <br />
            Start a new chat!
          </div>
        )}

        {!isLoading && !error && conversations.length > 0 && (
          <div className="space-y-1">
            {conversations.map((conversation) => {
              const isActive = location.pathname === `/nova/c/${conversation.id}`;
              const isDeleting = deletingId === conversation.id;
              return (
                <div key={conversation.id} className="group relative">
                  <NavLink
                    to={`/nova/c/${conversation.id}`}
                    onClick={onMobileClose}
                    className={`
                      block px-3 py-2 pr-8 rounded-xl text-sm transition-colors truncate
                      ${isActive
                        ? "bg-violet-500/20 border border-violet-500/40 text-text-primary"
                        : "text-text-secondary hover:bg-surface-card1 hover:text-text-primary"
                      }
                      ${isDeleting ? "opacity-50" : ""}
                    `}
                  >
                    {conversation.title}
                  </NavLink>
                  <button
                    type="button"
                    onClick={(e) => handleDelete(e, conversation.id)}
                    disabled={isDeleting}
                    className="absolute right-1.5 top-1/2 -translate-y-1/2 p-1 rounded-lg text-text-tertiary opacity-0 group-hover:opacity-100 hover:text-red-400 hover:bg-red-500/10 transition-all disabled:opacity-50"
                    aria-label="Delete conversation"
                  >
                    {isDeleting ? (
                      <div className="w-4 h-4 border-2 border-red-400/30 border-t-red-400 rounded-full animate-spin" />
                    ) : (
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                      </svg>
                    )}
                  </button>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Your Learning Profile */}
      <div className="p-3 border-t border-border">
        <div className="flex items-center justify-between px-2 mb-2">
          <h3 className="text-[11px] font-semibold uppercase tracking-wider text-text-tertiary">
            Your Learning
          </h3>
          <Link
            to="/nova/settings"
            onClick={onMobileClose}
            className="p-1 rounded-lg text-text-tertiary hover:text-violet-400 hover:bg-surface-card1 transition-colors"
            aria-label="Learning settings"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
            </svg>
          </Link>
        </div>

        {isProfileLoading ? (
          <div className="px-2 py-4 text-center">
            <div className="inline-block w-4 h-4 border-2 border-violet-500/30 border-t-violet-500 rounded-full animate-spin" />
          </div>
        ) : profile?.hasUserData ? (
          <div className="px-2 py-3 rounded-xl bg-surface-card1 border border-border">
            <div className="flex items-center gap-2 mb-1">
              <span className="text-sm">
                {profile.currentRole?.toLowerCase().includes("senior") ? "👨‍💻" : "💻"}
              </span>
              <span className="text-xs font-medium text-text-primary truncate">
                {profile.currentRole || "Developer"}
                {profile.experienceYears ? ` • ${profile.experienceYears} yrs` : ""}
              </span>
            </div>
            {profile.learningGoals && (
              <p className="text-[11px] text-text-tertiary line-clamp-2">
                Goal: {profile.learningGoals}
              </p>
            )}
          </div>
        ) : (
          <Link
            to="/nova/settings"
            onClick={onMobileClose}
            className="block px-3 py-3 rounded-xl bg-surface-card1 border border-dashed border-violet-500/40 text-center hover:bg-violet-500/10 transition-colors"
          >
            <p className="text-xs text-violet-400 font-medium">Set up your profile</p>
            <p className="text-[11px] text-text-tertiary mt-1">
              Help Cody personalize your learning
            </p>
          </Link>
        )}
      </div>
    </nav>
  );
}
