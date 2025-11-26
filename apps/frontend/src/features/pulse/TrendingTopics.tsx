import { Card } from "../../components/ui/Card";

interface TrendingTopic {
  id: string;
  category: string;
  topic: string;
  posts: string;
}

const MOCK_TRENDING: TrendingTopic[] = [
  {
    id: "1",
    category: "Technology",
    topic: "#CleanArchitecture",
    posts: "1.2K posts",
  },
  {
    id: "2",
    category: "AI & ML",
    topic: "Claude 4",
    posts: "5.8K posts",
  },
  {
    id: "3",
    category: "Software",
    topic: "#DotNet10",
    posts: "892 posts",
  },
  {
    id: "4",
    category: "Development",
    topic: "Vertical Slices",
    posts: "421 posts",
  },
];

interface TrendingItemProps {
  topic: TrendingTopic;
}

function TrendingItem({ topic }: TrendingItemProps): JSX.Element {
  return (
    <div className="py-3 hover:bg-surface-card2 -mx-4 px-4 cursor-pointer transition-colors">
      <p className="text-[11px] text-text-tertiary">{topic.category}</p>
      <p className="text-sm font-medium text-text-primary">{topic.topic}</p>
      <p className="text-xs text-text-tertiary">{topic.posts}</p>
    </div>
  );
}

export function TrendingTopics(): JSX.Element {
  return (
    <Card>
      <h2 className="text-base font-semibold tracking-tight text-text-primary">
        Trending
      </h2>
      <div className="divide-y divide-border">
        {MOCK_TRENDING.map((topic) => (
          <TrendingItem key={topic.id} topic={topic} />
        ))}
      </div>
      <button
        type="button"
        className="mt-2 text-xs text-brand-soft hover:text-brand transition-colors"
      >
        Show more
      </button>
    </Card>
  );
}
