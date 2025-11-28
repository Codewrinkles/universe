import type { Post } from "../../types";
import { PostCard } from "./PostCard";
import { UnifiedComposer } from "./UnifiedComposer";

export interface FeedProps {
  posts: Post[];
  onFollowChange?: () => void;
  onReplyClick?: (pulseId: string) => void;
  replyingToPulseId?: string | null;
  onReplyCreated?: () => void;
}

export function Feed({ posts, onFollowChange, onReplyClick, replyingToPulseId, onReplyCreated }: FeedProps): JSX.Element {
  return (
    <>
      {posts.map((post) => (
        <div key={post.id}>
          <div className="border-b border-border">
            <PostCard
              post={post}
              onFollowChange={onFollowChange}
              onReplyClick={onReplyClick}
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
