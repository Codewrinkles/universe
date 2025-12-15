import { useState, useEffect } from "react";
import { useSearchParams, Link } from "react-router-dom";
import { searchApi } from "../../services/searchApi";
import type { SearchProfileResult } from "../../services/searchApi";
import { FollowButton } from "../social/components/FollowButton";
import { PulseThreeColumnLayout } from "./PulseThreeColumnLayout";

interface UserSearchResultProps {
  user: SearchProfileResult;
  onFollowChange: () => void;
}

function UserSearchResult({ user, onFollowChange }: UserSearchResultProps): JSX.Element {
  const getAvatarDisplay = (): JSX.Element => {
    if (user.avatarUrl) {
      return (
        <img
          src={user.avatarUrl}
          alt={user.name}
          className="h-12 w-12 flex-shrink-0 rounded-full object-cover border border-border"
        />
      );
    }

    // Fallback to initial
    const initial = user.name.charAt(0).toUpperCase();
    return (
      <div className="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-full bg-surface-card2 border border-border text-base font-semibold text-text-primary">
        {initial}
      </div>
    );
  };

  return (
    <div className="flex items-start gap-4 px-4 py-4 border-b border-border last:border-b-0 hover:bg-surface-card1 transition-colors">
      {getAvatarDisplay()}
      <div className="flex-1 min-w-0">
        <div className="flex items-start justify-between gap-2">
          <Link to={`/pulse/u/${user.handle}`} className="min-w-0 flex-1">
            <p className="text-base font-semibold text-text-primary truncate hover:underline">{user.name}</p>
            {user.handle && (
              <p className="text-sm text-text-tertiary truncate">@{user.handle}</p>
            )}
          </Link>
          <FollowButton profileId={user.profileId} size="md" onFollowChange={onFollowChange} />
        </div>
        {user.bio && (
          <p className="mt-2 text-sm text-text-secondary line-clamp-2">{user.bio}</p>
        )}
      </div>
    </div>
  );
}

export function SearchResultsPage(): JSX.Element {
  const [searchParams] = useSearchParams();
  const query = searchParams.get("q") || "";
  const [results, setResults] = useState<SearchProfileResult[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const performSearch = async (): Promise<void> => {
      if (!query.trim()) {
        setResults([]);
        return;
      }

      setIsLoading(true);
      setError(null);

      try {
        const response = await searchApi.searchProfiles(query);
        setResults(response.handles);
      } catch (err) {
        setError("Failed to search users. Please try again.");
        setResults([]);
      } finally {
        setIsLoading(false);
      }
    };

    performSearch();
  }, [query]);

  const refetchResults = (): void => {
    // Re-run search when follow state changes
    if (query.trim()) {
      searchApi.searchProfiles(query)
        .then(response => setResults(response.handles))
        .catch(() => {});
    }
  };

  return (
    <PulseThreeColumnLayout>
      {/* Header */}
      <div className="sticky top-0 z-10 border-b border-border bg-surface-page/80 backdrop-blur px-4 py-3">
        <h1 className="text-base font-semibold tracking-tight text-text-primary">
          Search
        </h1>
        {query && (
          <p className="mt-0.5 text-sm text-text-secondary">
            Results for "{query}"
          </p>
        )}
      </div>

      {/* Empty State - No Query */}
      {!query && (
        <div className="p-8 text-center">
          <p className="text-sm text-text-tertiary">Enter a search query to find users</p>
          <p className="mt-2 text-xs text-text-tertiary">
            Search by name or handle in the search box above
          </p>
        </div>
      )}

      {/* Loading State */}
      {query && isLoading && results.length === 0 && (
        <div className="p-8 text-center">
          <p className="text-sm text-text-tertiary">Searching...</p>
        </div>
      )}

      {/* Error State */}
      {query && !isLoading && error && (
        <div className="p-4 border-b border-border bg-red-500/10 text-red-400 text-sm">
          {error}
        </div>
      )}

      {/* Empty Results */}
      {query && !isLoading && !error && results.length === 0 && (
        <div className="p-8 text-center">
          <p className="text-sm text-text-tertiary">No users found for "{query}"</p>
          <p className="mt-2 text-xs text-text-tertiary">Try searching by name or handle</p>
        </div>
      )}

      {/* Results */}
      {results.length > 0 && (
        <div>
          <div className="px-4 py-3 border-b border-border bg-surface-card1/50">
            <h2 className="text-sm font-semibold tracking-tight text-text-primary">
              People
            </h2>
          </div>
          <div>
            {results.map((result) => (
              <UserSearchResult
                key={result.profileId}
                user={result}
                onFollowChange={refetchResults}
              />
            ))}
          </div>
        </div>
      )}
    </PulseThreeColumnLayout>
  );
}
