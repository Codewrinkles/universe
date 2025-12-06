import { useState, useEffect } from "react";

interface FeedFilters {
  hideReplies: boolean;
}

const STORAGE_KEY = "pulse_feed_filters";

const defaultFilters: FeedFilters = {
  hideReplies: false,
};

export function useFeedFilter() {
  const [hideReplies, setHideReplies] = useState<boolean>(false);

  // Load filters from localStorage on mount
  useEffect(() => {
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      if (saved) {
        const filters = JSON.parse(saved) as FeedFilters;
        setHideReplies(filters.hideReplies ?? defaultFilters.hideReplies);
      }
    } catch (error) {
      console.error("Failed to load feed filters from localStorage:", error);
      // If there's an error, use default filters
      setHideReplies(defaultFilters.hideReplies);
    }
  }, []);

  // Save filters to localStorage whenever they change
  const toggleHideReplies = (hide: boolean): void => {
    setHideReplies(hide);
    try {
      const filters: FeedFilters = {
        hideReplies: hide,
      };
      localStorage.setItem(STORAGE_KEY, JSON.stringify(filters));
    } catch (error) {
      console.error("Failed to save feed filters to localStorage:", error);
    }
  };

  return {
    hideReplies,
    toggleHideReplies,
  };
}
