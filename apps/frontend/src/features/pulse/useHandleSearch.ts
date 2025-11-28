import { useState, useCallback } from "react";
import { config } from "../../config";
import type { HandleSearchResult, SearchHandlesResponse } from "../../types";

export function useHandleSearch() {
  const [query, setQuery] = useState<string>("");
  const [results, setResults] = useState<HandleSearchResult[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  const search = useCallback(async (searchQuery: string) => {
    setQuery(searchQuery);

    // Reset if query is empty
    if (!searchQuery || searchQuery.length < 2) {
      setResults([]);
      setIsLoading(false);
      setError(null);
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const accessToken = localStorage.getItem(config.auth.accessTokenKey);
      const response = await fetch(config.api.endpoints.searchHandles(searchQuery, 10), {
        headers: {
          "Authorization": `Bearer ${accessToken}`,
          "Content-Type": "application/json",
        },
      });

      if (!response.ok) {
        throw new Error("Failed to search handles");
      }

      const data: SearchHandlesResponse = await response.json();
      setResults(data.handles);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to search handles");
      setResults([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

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
