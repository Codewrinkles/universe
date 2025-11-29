import { useState, useCallback, useEffect } from "react";
import { config } from "../../config";
import type { Hashtag, HashtagsResponse } from "../../types";

export function useHashtagSearch() {
  const [query, setQuery] = useState<string>("");
  const [results, setResults] = useState<Hashtag[]>([]);
  const [allHashtags, setAllHashtags] = useState<Hashtag[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  // Fetch trending hashtags on mount
  useEffect(() => {
    const fetchTrendingHashtags = async (): Promise<void> => {
      try {
        const response = await fetch(`${config.api.baseUrl}/api/pulse/hashtags/trending?limit=20`);

        if (!response.ok) {
          throw new Error("Failed to fetch trending hashtags");
        }

        const data: HashtagsResponse = await response.json();
        setAllHashtags(data.hashtags);
      } catch (err) {
        console.error("Failed to fetch trending hashtags:", err);
      }
    };

    void fetchTrendingHashtags();
  }, []);

  const search = useCallback((searchQuery: string) => {
    setQuery(searchQuery);

    // If query is empty, show all trending hashtags
    if (!searchQuery || searchQuery.length === 0) {
      setResults(allHashtags.slice(0, 5));
      setIsLoading(false);
      setError(null);
      return;
    }

    // Filter hashtags client-side
    const normalizedQuery = searchQuery.toLowerCase();
    const filtered = allHashtags
      .filter(h =>
        h.tag.toLowerCase().startsWith(normalizedQuery) ||
        h.tagDisplay.toLowerCase().startsWith(normalizedQuery)
      )
      .slice(0, 5);

    setResults(filtered);
    setIsLoading(false);
    setError(null);
  }, [allHashtags]);

  const clearResults = useCallback(() => {
    setResults([]);
    setQuery("");
    setError(null);
  }, []);

  return {
    query,
    results,
    isLoading,
    error,
    search,
    clearResults,
  };
}
