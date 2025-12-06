import type { Post } from "../../types";
import { PostCard } from "./PostCard";
import { UnifiedComposer } from "./UnifiedComposer";

export interface FeedProps {
  posts: Post[];
  onFollowChange?: () => void;
  onReplyClick?: (pulseId: string) => void;
  replyingToPulseId?: string | null;
  onReplyCreated?: () => void;
  onDelete?: (pulseId: string) => void;
  hideReplies?: boolean;
}

export function Feed({ posts, onFollowChange, onReplyClick, replyingToPulseId, onReplyCreated, onDelete, hideReplies = false }: FeedProps): JSX.Element {
  // Filter posts based on hideReplies preference
  const filteredPosts = hideReplies
    ? posts.filter((post) => post.type !== "reply")
    : posts;

  return (
    <>
      {filteredPosts.map((post) => (
        <div key={post.id}>
          <div className="border-b border-border">
            <PostCard
              post={post}
              onFollowChange={onFollowChange}
              onReplyClick={onReplyClick}
              onDelete={onDelete}
            />
          </div>
          {replyingToPulseId === post.id && (
            <div className="border-b border-border bg-surface-card1/30 px-4 py-3">
              <UnifiedComposer
                mode="reply"
                parentPulseId={post.id}
                onSuccess={onReplyCreated}
                placeholder="Post your reply"
                rows={2}
                focusedRows={4}
              />
            </div>
          )}
        </div>
      ))}
    </>
  );
}
