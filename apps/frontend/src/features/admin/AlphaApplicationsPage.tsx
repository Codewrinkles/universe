/**
 * Admin page for managing Alpha applications
 * Allows admins to view, accept, and waitlist applications
 */

import { useEffect, useState } from "react";
import { Card } from "../../components/ui/Card";
import { config } from "../../config";

interface AlphaApplication {
  id: string;
  email: string;
  name: string;
  primaryTechStack: string;
  yearsOfExperience: number;
  goal: string;
  status: "pending" | "accepted" | "waitlisted";
  inviteCode: string | null;
  inviteCodeRedeemed: boolean;
  createdAt: string;
}

type StatusFilter = "all" | "pending" | "accepted" | "waitlisted";

export function AlphaApplicationsPage(): JSX.Element {
  const [applications, setApplications] = useState<AlphaApplication[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("pending");
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [expandedGoal, setExpandedGoal] = useState<string | null>(null);

  const fetchApplications = async (): Promise<void> => {
    try {
      setIsLoading(true);
      setError(null);

      const token = localStorage.getItem(config.auth.accessTokenKey);
      const url = statusFilter === "all"
        ? `${config.api.baseUrl}/api/admin/alpha/applications`
        : `${config.api.baseUrl}/api/admin/alpha/applications?status=${statusFilter}`;

      const response = await fetch(url, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        throw new Error("Failed to fetch applications");
      }

      const data = await response.json();
      setApplications(data.applications);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load applications");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void fetchApplications();
  }, [statusFilter]);

  const handleAccept = async (id: string): Promise<void> => {
    try {
      setActionLoading(id);
      const token = localStorage.getItem(config.auth.accessTokenKey);

      const response = await fetch(
        `${config.api.baseUrl}/api/admin/alpha/applications/${id}/accept`,
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);
        throw new Error(errorData?.message || "Failed to accept application");
      }

      // Refresh the list
      await fetchApplications();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to accept application");
    } finally {
      setActionLoading(null);
    }
  };

  const handleWaitlist = async (id: string): Promise<void> => {
    try {
      setActionLoading(id);
      const token = localStorage.getItem(config.auth.accessTokenKey);

      const response = await fetch(
        `${config.api.baseUrl}/api/admin/alpha/applications/${id}/waitlist`,
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);
        throw new Error(errorData?.message || "Failed to waitlist application");
      }

      // Refresh the list
      await fetchApplications();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to waitlist application");
    } finally {
      setActionLoading(null);
    }
  };

  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  const getStatusBadge = (status: string): JSX.Element => {
    const styles = {
      pending: "bg-yellow-500/20 text-yellow-300 border-yellow-500/30",
      accepted: "bg-emerald-500/20 text-emerald-300 border-emerald-500/30",
      waitlisted: "bg-violet-500/20 text-violet-300 border-violet-500/30",
    };

    return (
      <span
        className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium border ${styles[status as keyof typeof styles] || styles.pending}`}
      >
        {status}
      </span>
    );
  };

  const pendingCount = applications.filter((a) => a.status === "pending").length;
  const acceptedCount = applications.filter((a) => a.status === "accepted").length;
  const waitlistedCount = applications.filter((a) => a.status === "waitlisted").length;

  return (
    <div className="p-6">
      <div className="mb-6">
        <h1 className="text-xl font-bold text-text-primary">Alpha Applications</h1>
        <p className="text-sm text-text-secondary mt-1">
          Review and manage Nova Alpha applications
        </p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-3 gap-4 mb-6">
        <Card className="text-center">
          <p className="text-3xl font-bold text-yellow-400">{pendingCount}</p>
          <p className="text-xs text-text-tertiary">Pending</p>
        </Card>
        <Card className="text-center">
          <p className="text-3xl font-bold text-emerald-400">{acceptedCount}</p>
          <p className="text-xs text-text-tertiary">Accepted</p>
        </Card>
        <Card className="text-center">
          <p className="text-3xl font-bold text-violet-400">{waitlistedCount}</p>
          <p className="text-xs text-text-tertiary">Waitlisted</p>
        </Card>
      </div>

      {/* Filter tabs */}
      <div className="flex gap-2 mb-6">
        {(["all", "pending", "accepted", "waitlisted"] as StatusFilter[]).map((filter) => (
          <button
            key={filter}
            onClick={() => setStatusFilter(filter)}
            className={`px-4 py-2 text-sm font-medium rounded-full transition-colors ${
              statusFilter === filter
                ? "bg-violet-600 text-white"
                : "bg-surface-card1 text-text-secondary hover:text-text-primary"
            }`}
          >
            {filter.charAt(0).toUpperCase() + filter.slice(1)}
          </button>
        ))}
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
        <p className="text-sm text-text-secondary">Loading applications...</p>
      )}

      {/* Applications list */}
      {!isLoading && applications.length === 0 && (
        <Card>
          <p className="text-sm text-text-secondary text-center py-8">
            No {statusFilter === "all" ? "" : statusFilter} applications found
          </p>
        </Card>
      )}

      {!isLoading && applications.length > 0 && (
        <div className="space-y-4">
          {applications.map((app) => (
            <Card key={app.id} className="relative">
              <div className="flex justify-between items-start mb-3">
                <div>
                  <div className="flex items-center gap-2 mb-1">
                    <h3 className="text-sm font-semibold text-text-primary">{app.name}</h3>
                    {getStatusBadge(app.status)}
                  </div>
                  <p className="text-xs text-text-tertiary">{app.email}</p>
                </div>
                <p className="text-xs text-text-tertiary">{formatDate(app.createdAt)}</p>
              </div>

              <div className="grid grid-cols-2 gap-4 mb-3">
                <div>
                  <p className="text-xs text-text-tertiary">Tech Stack</p>
                  <p className="text-sm text-text-secondary">{app.primaryTechStack}</p>
                </div>
                <div>
                  <p className="text-xs text-text-tertiary">Experience</p>
                  <p className="text-sm text-text-secondary">{app.yearsOfExperience} years</p>
                </div>
              </div>

              <div className="mb-3">
                <p className="text-xs text-text-tertiary mb-1">Goal</p>
                <p
                  className={`text-sm text-text-secondary ${
                    expandedGoal === app.id ? "" : "line-clamp-2"
                  }`}
                >
                  {app.goal}
                </p>
                {app.goal.length > 150 && (
                  <button
                    onClick={() => setExpandedGoal(expandedGoal === app.id ? null : app.id)}
                    className="text-xs text-violet-400 hover:text-violet-300 mt-1"
                  >
                    {expandedGoal === app.id ? "Show less" : "Show more"}
                  </button>
                )}
              </div>

              {/* Invite code for accepted applications */}
              {app.status === "accepted" && app.inviteCode && (
                <div className="mb-3 p-2 rounded-lg bg-emerald-500/10 border border-emerald-500/30">
                  <p className="text-xs text-emerald-300">
                    Invite Code: <code className="font-mono">{app.inviteCode}</code>
                    {app.inviteCodeRedeemed && (
                      <span className="ml-2 text-emerald-400">(Redeemed)</span>
                    )}
                  </p>
                </div>
              )}

              {/* Actions for pending applications */}
              {app.status === "pending" && (
                <div className="flex gap-2 mt-4 pt-4 border-t border-border">
                  <button
                    onClick={() => handleAccept(app.id)}
                    disabled={actionLoading === app.id}
                    className="flex-1 px-4 py-2 text-sm font-medium rounded-full bg-emerald-600 text-white hover:bg-emerald-500 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {actionLoading === app.id ? "Processing..." : "Accept"}
                  </button>
                  <button
                    onClick={() => handleWaitlist(app.id)}
                    disabled={actionLoading === app.id}
                    className="flex-1 px-4 py-2 text-sm font-medium rounded-full bg-violet-600 text-white hover:bg-violet-500 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {actionLoading === app.id ? "Processing..." : "Waitlist"}
                  </button>
                </div>
              )}
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
