/**
 * Admin page for viewing all users
 * Shows paginated list with avatar, name, email, and handle
 */

import { useEffect, useState } from "react";
import { Card } from "../../components/ui/Card";
import { config } from "../../config";

interface AdminUser {
  profileId: string;
  name: string;
  handle: string | null;
  avatarUrl: string | null;
  email: string;
  createdAt: string;
}

interface UsersResponse {
  users: AdminUser[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export function UsersPage(): JSX.Element {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const pageSize = 20;

  const fetchUsers = async (page: number): Promise<void> => {
    try {
      setIsLoading(true);
      setError(null);

      const token = localStorage.getItem(config.auth.accessTokenKey);
      const url = `${config.api.baseUrl}/api/admin/users?page=${page}&pageSize=${pageSize}`;

      const response = await fetch(url, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        throw new Error("Failed to fetch users");
      }

      const data: UsersResponse = await response.json();
      setUsers(data.users);
      setTotalPages(data.totalPages);
      setTotalCount(data.totalCount);
      setCurrentPage(data.page);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load users");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void fetchUsers(currentPage);
  }, [currentPage]);

  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
  };

  const handlePreviousPage = (): void => {
    if (currentPage > 1) {
      setCurrentPage(currentPage - 1);
    }
  };

  const handleNextPage = (): void => {
    if (currentPage < totalPages) {
      setCurrentPage(currentPage + 1);
    }
  };

  return (
    <div className="p-6">
      <div className="mb-6">
        <h1 className="text-xl font-bold text-text-primary">Users</h1>
        <p className="text-sm text-text-secondary mt-1">
          {totalCount} total users
        </p>
      </div>

      {/* Error message */}
      {error && (
        <div className="mb-4 rounded-lg border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-400">
          {error}
          <button
            onClick={() => setError(null)}
            className="ml-2 text-red-300 hover:text-red-200"
          >
            Dismiss
          </button>
        </div>
      )}

      {/* Loading */}
      {isLoading && (
        <p className="text-sm text-text-secondary">Loading users...</p>
      )}

      {/* Users list */}
      {!isLoading && users.length === 0 && (
        <Card>
          <p className="text-sm text-text-secondary text-center py-8">
            No users found
          </p>
        </Card>
      )}

      {!isLoading && users.length > 0 && (
        <>
          <div className="space-y-2">
            {users.map((user) => (
              <Card key={user.profileId} className="!p-3">
                <div className="flex items-center gap-4">
                  {/* Avatar */}
                  <div className="flex-shrink-0">
                    {user.avatarUrl ? (
                      <img
                        src={user.avatarUrl}
                        alt={user.name}
                        className="w-10 h-10 rounded-full object-cover"
                      />
                    ) : (
                      <div className="w-10 h-10 rounded-full bg-surface-card2 flex items-center justify-center">
                        <span className="text-sm font-medium text-text-tertiary">
                          {user.name.charAt(0).toUpperCase()}
                        </span>
                      </div>
                    )}
                  </div>

                  {/* User info */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <p className="text-sm font-medium text-text-primary truncate">
                        {user.name}
                      </p>
                      {user.handle && (
                        <span className="text-xs text-text-tertiary">
                          @{user.handle}
                        </span>
                      )}
                    </div>
                    <p className="text-xs text-text-secondary truncate">
                      {user.email}
                    </p>
                  </div>

                  {/* Created date */}
                  <div className="flex-shrink-0 text-right">
                    <p className="text-xs text-text-tertiary">
                      {formatDate(user.createdAt)}
                    </p>
                  </div>
                </div>
              </Card>
            ))}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between mt-6 pt-4 border-t border-border">
              <p className="text-xs text-text-tertiary">
                Page {currentPage} of {totalPages}
              </p>
              <div className="flex gap-2">
                <button
                  onClick={handlePreviousPage}
                  disabled={currentPage === 1}
                  className="px-4 py-2 text-sm font-medium rounded-full bg-surface-card1 text-text-secondary hover:text-text-primary hover:bg-surface-card2 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Previous
                </button>
                <button
                  onClick={handleNextPage}
                  disabled={currentPage === totalPages}
                  className="px-4 py-2 text-sm font-medium rounded-full bg-surface-card1 text-text-secondary hover:text-text-primary hover:bg-surface-card2 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Next
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}
