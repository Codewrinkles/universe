import type { Post } from "../../types";
import { PostCard } from "./PostCard";

export interface FeedProps {
  posts: Post[];
  onFollowChange?: () => void;
}

export function Feed({ posts, onFollowChange }: FeedProps): JSX.Element {
  return (
    <>
      {posts.map((post) => (
        <div key={post.id} className="border-b border-border">
          <PostCard post={post} onFollowChange={onFollowChange} />
        </div>
      ))}
    </>
  );
}
