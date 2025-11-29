import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Card } from "../../components/ui/Card";
import { config } from "../../config";
import type { Hashtag } from "../../types";

interface TrendingItemProps {
  hashtag: Hashtag;
}

function TrendingItem({ hashtag }: TrendingItemProps): JSX.Element {
  const formatCount = (count: number): string => {
    if (count >= 1000) {
      return `${(count / 1000).toFixed(1)}K`;
    }
    return count.toString();
  };

  return (
    <Link
      to={`/social/hashtag/${hashtag.tag}`}
      className="block py-3 hover:bg-surface-card2 -mx-4 px-4 transition-colors"
    >
      <p className="text-[11px] text-text-tertiary">Trending</p>
      <p className="text-sm font-medium text-text-primary">#{hashtag.tagDisplay}</p>
      <p className="text-xs text-text-tertiary">
        {formatCount(hashtag.pulseCount)} {hashtag.pulseCount === 1 ? "pulse" : "pulses"}
      </p>
    </Link>
  );
}

export function TrendingTopics(): JSX.Element {
  const [hashtags, setHashtags] = useState<Hashtag[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchTrendingHashtags = async (): Promise<void> => {
      try {
        const response = await fetch(`${config.api.baseUrl}/api/pulse/hashtags/trending?limit=5`);

        if (!response.ok) {
          throw new Error("Failed to fetch trending hashtags");
        }

        const data = await response.json() as { hashtags: Hashtag[] };
        setHashtags(data.hashtags);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Unknown error");
      } finally {
        setLoading(false);
      }
    };

    void fetchTrendingHashtags();
  }, []);

  if (loading) {
    return (
      <Card>
        <h2 className="text-base font-semibold tracking-tight text-text-primary">
          Trending
        </h2>
        <div className="py-8 text-center text-sm text-text-secondary">
          Loading...
        </div>
      </Card>
    );
  }

  if (error) {
    return (
      <Card>
        <h2 className="text-base font-semibold tracking-tight text-text-primary">
          Trending
        </h2>
        <div className="py-8 text-center text-sm text-text-secondary">
          Failed to load trending topics
        </div>
      </Card>
    );
  }

  if (hashtags.length === 0) {
    return (
      <Card>
        <h2 className="text-base font-semibold tracking-tight text-text-primary">
          Trending
        </h2>
        <div className="py-8 text-center text-sm text-text-secondary">
          No trending topics yet
        </div>
      </Card>
    );
  }

  return (
    <Card>
      <h2 className="text-base font-semibold tracking-tight text-text-primary">
        Trending
      </h2>
      <div className="divide-y divide-border">
        {hashtags.map((hashtag) => (
          <TrendingItem key={hashtag.id} hashtag={hashtag} />
        ))}
      </div>
    </Card>
  );
}
