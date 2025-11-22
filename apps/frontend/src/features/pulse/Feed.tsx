import type { Post } from "../../types";
import { PostCard } from "./PostCard";

export interface FeedProps {
  posts: Post[];
}

export function Feed({ posts }: FeedProps): JSX.Element {
  return (
    <div className="space-y-3">
      {posts.map((post) => (
        <PostCard key={post.id} post={post} />
      ))}
    </div>
  );
}
