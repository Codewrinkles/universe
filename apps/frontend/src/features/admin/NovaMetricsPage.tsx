import { useEffect, useState } from "react";
import { config } from "../../config";

interface NovaAlphaMetrics {
  totalApplications: number;
  pendingApplications: number;
  acceptedApplications: number;
  waitlistedApplications: number;
  redeemedCodes: number;
  novaUsers: number;
  activatedUsers: number;
  activationRate: number;
  activeLast7Days: number;
  activeRate: number;
  totalSessions: number;
  totalMessages: number;
  avgSessionsPerUser: number;
  avgMessagesPerSession: number;
}

interface UserNovaUsage {
  profileId: string;
  name: string;
  handle: string | null;
  avatarUrl: string | null;
  accessLevel: number;
  accessLevelName: string;
  sessionsLast24Hours: number;
  sessionsLast3Days: number;
  sessionsLast7Days: number;
  sessionsLast30Days: number;
  totalMessages: number;
  avgMessagesPerSession: number;
  lastActiveAt: string | null;
  firstSessionAt: string | null;
  sessionsPrevious7Days: number;
  trendPercentage: number;
}

type SortField = "name" | "sessions7d" | "sessions30d" | "avgMsgs" | "lastActive" | "trend";
type SortDirection = "asc" | "desc";

// ============================================
// METRIC CARD COMPONENTS
// ============================================

interface MetricCardProps {
  label: string;
  value: number | string;
  icon?: string;
  subtitle?: string;
  accentColor?: "brand" | "violet" | "amber" | "blue" | "emerald";
}

function MetricCard({
  label,
  value,
  icon,
  subtitle,
  accentColor = "brand",
}: MetricCardProps): JSX.Element {
  const accentClasses = {
    brand: "from-brand/20 to-transparent border-brand/30",
    violet: "from-violet-500/20 to-transparent border-violet-500/30",
    amber: "from-amber-500/20 to-transparent border-amber-500/30",
    blue: "from-blue-500/20 to-transparent border-blue-500/30",
    emerald: "from-emerald-500/20 to-transparent border-emerald-500/30",
  };

  const iconBgClasses = {
    brand: "bg-brand/10 text-brand",
    violet: "bg-violet-500/10 text-violet-400",
    amber: "bg-amber-500/10 text-amber-400",
    blue: "bg-blue-500/10 text-blue-400",
    emerald: "bg-emerald-500/10 text-emerald-400",
  };

  return (
    <div
      className={`relative overflow-hidden rounded-xl border bg-gradient-to-br p-4 backdrop-blur-sm transition-all hover:scale-[1.02] hover:shadow-lg ${accentClasses[accentColor]}`}
    >
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <p className="text-xs font-medium text-text-tertiary uppercase tracking-wide mb-1">
            {label}
          </p>
          <p className="text-3xl font-bold text-text-primary">
            {typeof value === "number" ? value.toLocaleString() : value}
          </p>
          {subtitle && (
            <p className="text-xs text-text-secondary mt-1">{subtitle}</p>
          )}
        </div>
        {icon && (
          <div
            className={`flex items-center justify-center w-10 h-10 rounded-lg ${iconBgClasses[accentColor]}`}
          >
            <span className="text-xl">{icon}</span>
          </div>
        )}
      </div>
    </div>
  );
}

interface ProgressCardProps {
  label: string;
  value: number;
  total: number;
  percentage: number;
  icon?: string;
  accentColor?: "brand" | "violet" | "amber" | "emerald";
}

function ProgressCard({
  label,
  value,
  total,
  percentage,
  icon,
  accentColor = "brand",
}: ProgressCardProps): JSX.Element {
  const accentClasses = {
    brand: "from-brand/20 to-transparent border-brand/30",
    violet: "from-violet-500/20 to-transparent border-violet-500/30",
    amber: "from-amber-500/20 to-transparent border-amber-500/30",
    emerald: "from-emerald-500/20 to-transparent border-emerald-500/30",
  };

  const progressBgClasses = {
    brand: "bg-brand",
    violet: "bg-violet-500",
    amber: "bg-amber-500",
    emerald: "bg-emerald-500",
  };

  const iconBgClasses = {
    brand: "bg-brand/10 text-brand",
    violet: "bg-violet-500/10 text-violet-400",
    amber: "bg-amber-500/10 text-amber-400",
    emerald: "bg-emerald-500/10 text-emerald-400",
  };

  return (
    <div
      className={`relative overflow-hidden rounded-xl border bg-gradient-to-br p-4 backdrop-blur-sm ${accentClasses[accentColor]}`}
    >
      <div className="flex items-start justify-between mb-3">
        <div>
          <p className="text-xs font-medium text-text-tertiary uppercase tracking-wide mb-1">
            {label}
          </p>
          <div className="flex items-baseline gap-2">
            <span className="text-3xl font-bold text-text-primary">
              {percentage}%
            </span>
            <span className="text-sm text-text-secondary">
              {value} of {total}
            </span>
          </div>
        </div>
        {icon && (
          <div
            className={`flex items-center justify-center w-10 h-10 rounded-lg ${iconBgClasses[accentColor]}`}
          >
            <span className="text-xl">{icon}</span>
          </div>
        )}
      </div>
      <div className="h-2 bg-surface-card1 rounded-full overflow-hidden">
        <div
          className={`h-full rounded-full transition-all duration-500 ${progressBgClasses[accentColor]}`}
          style={{ width: `${Math.min(percentage, 100)}%` }}
        />
      </div>
    </div>
  );
}

// ============================================
// FUNNEL VISUALIZATION
// ============================================

interface FunnelStepProps {
  label: string;
  value: number;
  percentage: number;
  isLast?: boolean;
  accentColor: "brand" | "violet" | "amber" | "emerald";
}

function FunnelStep({
  label,
  value,
  percentage,
  isLast = false,
  accentColor,
}: FunnelStepProps): JSX.Element {
  const bgClasses = {
    brand: "bg-brand/20 border-brand/40",
    violet: "bg-violet-500/20 border-violet-500/40",
    amber: "bg-amber-500/20 border-amber-500/40",
    emerald: "bg-emerald-500/20 border-emerald-500/40",
  };

  const textClasses = {
    brand: "text-brand",
    violet: "text-violet-400",
    amber: "text-amber-400",
    emerald: "text-emerald-400",
  };

  return (
    <div className="flex items-center gap-2 flex-1">
      <div
        className={`flex-1 rounded-xl border p-4 text-center ${bgClasses[accentColor]}`}
      >
        <p className={`text-2xl font-bold ${textClasses[accentColor]}`}>
          {value}
        </p>
        <p className="text-xs text-text-secondary mt-1">{label}</p>
        <p className="text-[10px] text-text-tertiary mt-0.5">{percentage}%</p>
      </div>
      {!isLast && (
        <div className="text-text-tertiary text-lg">→</div>
      )}
    </div>
  );
}

interface ApplicationFunnelProps {
  metrics: NovaAlphaMetrics;
}

function ApplicationFunnel({ metrics }: ApplicationFunnelProps): JSX.Element {
  const total = metrics.totalApplications || 1;
  const accepted = metrics.acceptedApplications || 1;

  // Key conversion: what % of accepted users redeemed their code
  const redemptionRate = Math.round((metrics.redeemedCodes / accepted) * 100);

  return (
    <div className="rounded-xl border border-border/50 bg-surface-card1/50 p-6">
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-2">
          <span className="text-xl">📊</span>
          <h2 className="text-sm font-semibold text-text-primary">
            Application Funnel
          </h2>
        </div>
        <div className="flex items-center gap-2 px-3 py-1.5 rounded-full bg-violet-500/10 border border-violet-500/30">
          <span className="text-xs text-text-secondary">Redemption Rate:</span>
          <span className="text-sm font-bold text-violet-400">{redemptionRate}%</span>
        </div>
      </div>

      <div className="flex items-center gap-2">
        <FunnelStep
          label="Total"
          value={metrics.totalApplications}
          percentage={100}
          accentColor="brand"
        />
        <FunnelStep
          label="Pending"
          value={metrics.pendingApplications}
          percentage={Math.round((metrics.pendingApplications / total) * 100)}
          accentColor="amber"
        />
        <FunnelStep
          label="Accepted"
          value={metrics.acceptedApplications}
          percentage={Math.round((metrics.acceptedApplications / total) * 100)}
          accentColor="emerald"
        />
        <FunnelStep
          label="Redeemed"
          value={metrics.redeemedCodes}
          percentage={redemptionRate}
          accentColor="violet"
          isLast
        />
      </div>

      {metrics.waitlistedApplications > 0 && (
        <div className="mt-4 pt-4 border-t border-border/30 flex items-center justify-center gap-2">
          <span className="text-amber-400">⏳</span>
          <span className="text-sm text-text-secondary">
            {metrics.waitlistedApplications} waitlisted
          </span>
        </div>
      )}
    </div>
  );
}

// ============================================
// SECTION HEADER
// ============================================

interface SectionHeaderProps {
  icon: string;
  title: string;
  subtitle?: string;
}

function SectionHeader({ icon, title, subtitle }: SectionHeaderProps): JSX.Element {
  return (
    <div className="flex items-center gap-3 mb-4">
      <div className="flex items-center justify-center w-8 h-8 rounded-lg bg-surface-card2">
        <span className="text-lg">{icon}</span>
      </div>
      <div>
        <h2 className="text-sm font-semibold text-text-primary">{title}</h2>
        {subtitle && (
          <p className="text-xs text-text-tertiary">{subtitle}</p>
        )}
      </div>
    </div>
  );
}

// ============================================
// USER USAGE COMPONENTS
// ============================================

function formatRelativeTime(dateString: string | null): string {
  if (!dateString) return "Never";

  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMs / 3600000);
  const diffDays = Math.floor(diffMs / 86400000);

  if (diffMins < 1) return "Just now";
  if (diffMins < 60) return `${diffMins}m ago`;
  if (diffHours < 24) return `${diffHours}h ago`;
  if (diffDays < 7) return `${diffDays}d ago`;
  if (diffDays < 30) return `${Math.floor(diffDays / 7)}w ago`;
  return `${Math.floor(diffDays / 30)}mo ago`;
}

function getAccessLevelBadge(levelName: string): { label: string; className: string } {
  switch (levelName.toLowerCase()) {
    case "alpha":
      return { label: "Alpha", className: "bg-violet-500/20 text-violet-400 border-violet-500/30" };
    case "pro":
      return { label: "Pro", className: "bg-brand/20 text-brand border-brand/30" };
    case "lifetime":
      return { label: "Lifetime", className: "bg-emerald-500/20 text-emerald-400 border-emerald-500/30" };
    case "free":
      return { label: "Free", className: "bg-blue-500/20 text-blue-400 border-blue-500/30" };
    default:
      return { label: levelName, className: "bg-surface-card2 text-text-secondary border-border" };
  }
}

interface TrendIndicatorProps {
  percentage: number;
  sessions7d: number;
  sessionsPrev7d: number;
}

function TrendIndicator({ percentage, sessions7d, sessionsPrev7d }: TrendIndicatorProps): JSX.Element {
  // New user with activity
  if (sessionsPrev7d === 0 && sessions7d > 0) {
    return (
      <div className="flex items-center gap-1 px-2 py-1 rounded-full bg-blue-500/20 border border-blue-500/30">
        <span className="text-xs font-medium text-blue-400">New</span>
      </div>
    );
  }

  // No activity
  if (sessions7d === 0 && sessionsPrev7d === 0) {
    return (
      <div className="flex items-center gap-1 px-2 py-1 rounded-full bg-surface-card2 border border-border">
        <span className="text-xs text-text-tertiary">—</span>
      </div>
    );
  }

  // Increasing (>10%)
  if (percentage >= 10) {
    return (
      <div className="flex items-center gap-1 px-2 py-1 rounded-full bg-emerald-500/20 border border-emerald-500/30">
        <span className="text-emerald-400">↑</span>
        <span className="text-xs font-medium text-emerald-400">+{Math.round(percentage)}%</span>
      </div>
    );
  }

  // Decreasing (<-10%)
  if (percentage <= -10) {
    return (
      <div className="flex items-center gap-1 px-2 py-1 rounded-full bg-red-500/20 border border-red-500/30">
        <span className="text-red-400">↓</span>
        <span className="text-xs font-medium text-red-400">{Math.round(percentage)}%</span>
      </div>
    );
  }

  // Stable (-10% to +10%)
  return (
    <div className="flex items-center gap-1 px-2 py-1 rounded-full bg-amber-500/20 border border-amber-500/30">
      <span className="text-amber-400">→</span>
      <span className="text-xs font-medium text-amber-400">Stable</span>
    </div>
  );
}

interface UserRowProps {
  user: UserNovaUsage;
}

function UserRow({ user }: UserRowProps): JSX.Element {
  const badge = getAccessLevelBadge(user.accessLevelName);

  return (
    <div className="flex items-center p-3 hover:bg-surface-card1/60 transition-colors">
      {/* Avatar + User info - fixed width */}
      <div className="flex items-center gap-3 w-56 flex-shrink-0">
        {user.avatarUrl ? (
          <img
            src={user.avatarUrl}
            alt={user.name}
            className="w-8 h-8 rounded-full object-cover ring-1 ring-border/50"
          />
        ) : (
          <div className="w-8 h-8 rounded-full bg-surface-card2 flex items-center justify-center ring-1 ring-border/50">
            <span className="text-xs font-medium text-text-tertiary">
              {user.name.charAt(0).toUpperCase()}
            </span>
          </div>
        )}
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-1.5">
            <p className="text-sm font-medium text-text-primary truncate">{user.name}</p>
            <span className={`text-[9px] px-1 py-0.5 rounded border flex-shrink-0 ${badge.className}`}>
              {badge.label}
            </span>
          </div>
          <p className="text-[10px] text-text-tertiary truncate">
            {user.handle ? `@${user.handle}` : ""} · {formatRelativeTime(user.lastActiveAt)}
          </p>
        </div>
      </div>

      {/* Session counts - fixed widths */}
      <div className="hidden md:flex items-center">
        <div className="w-12 text-center">
          <p className="text-sm font-semibold text-text-primary">{user.sessionsLast24Hours}</p>
        </div>
        <div className="w-12 text-center">
          <p className="text-sm font-semibold text-text-primary">{user.sessionsLast3Days}</p>
        </div>
        <div className="w-12 text-center">
          <p className="text-sm font-semibold text-brand">{user.sessionsLast7Days}</p>
        </div>
        <div className="w-12 text-center">
          <p className="text-sm font-semibold text-text-primary">{user.sessionsLast30Days}</p>
        </div>
      </div>

      {/* Avg messages - fixed width */}
      <div className="hidden sm:block w-16 text-center">
        <p className="text-sm font-semibold text-violet-400">{user.avgMessagesPerSession}</p>
      </div>

      {/* Trend - fixed width */}
      <div className="w-20 flex justify-center">
        <TrendIndicator
          percentage={user.trendPercentage}
          sessions7d={user.sessionsLast7Days}
          sessionsPrev7d={user.sessionsPrevious7Days}
        />
      </div>
    </div>
  );
}

interface SortableHeaderProps {
  label: string;
  field: SortField;
  currentSort: SortField;
  currentDirection: SortDirection;
  onSort: (field: SortField) => void;
  className?: string;
}

function SortableHeader({
  label,
  field,
  currentSort,
  currentDirection,
  onSort,
  className = "",
}: SortableHeaderProps): JSX.Element {
  const isActive = currentSort === field;

  return (
    <button
      onClick={() => onSort(field)}
      className={`inline-flex items-center justify-center gap-0.5 text-[10px] uppercase tracking-wide font-medium transition-colors ${
        isActive ? "text-brand" : "text-text-tertiary hover:text-text-secondary"
      } ${className}`}
    >
      {label}
      <span className={`w-2 ${isActive ? "text-brand" : "invisible"}`}>
        {currentDirection === "asc" ? "↑" : "↓"}
      </span>
    </button>
  );
}

interface UserUsageSectionProps {
  users: UserNovaUsage[];
  isLoading: boolean;
  error: string | null;
}

function UserUsageSection({ users, isLoading, error }: UserUsageSectionProps): JSX.Element {
  const [sortField, setSortField] = useState<SortField>("sessions7d");
  const [sortDirection, setSortDirection] = useState<SortDirection>("desc");
  const [showAll, setShowAll] = useState(false);

  const handleSort = (field: SortField): void => {
    if (sortField === field) {
      setSortDirection(sortDirection === "asc" ? "desc" : "asc");
    } else {
      setSortField(field);
      setSortDirection("desc");
    }
  };

  const sortedUsers = [...users].sort((a, b) => {
    let comparison = 0;

    switch (sortField) {
      case "name":
        comparison = a.name.localeCompare(b.name);
        break;
      case "sessions7d":
        comparison = a.sessionsLast7Days - b.sessionsLast7Days;
        break;
      case "sessions30d":
        comparison = a.sessionsLast30Days - b.sessionsLast30Days;
        break;
      case "avgMsgs":
        comparison = a.avgMessagesPerSession - b.avgMessagesPerSession;
        break;
      case "lastActive":
        const aTime = a.lastActiveAt ? new Date(a.lastActiveAt).getTime() : 0;
        const bTime = b.lastActiveAt ? new Date(b.lastActiveAt).getTime() : 0;
        comparison = aTime - bTime;
        break;
      case "trend":
        comparison = a.trendPercentage - b.trendPercentage;
        break;
    }

    return sortDirection === "asc" ? comparison : -comparison;
  });

  // Limit to top 10 unless "Show all" is clicked
  const displayedUsers = showAll ? sortedUsers : sortedUsers.slice(0, 10);
  const hasMoreUsers = users.length > 10;

  // Calculate summary stats
  const totalSessions7d = users.reduce((sum, u) => sum + u.sessionsLast7Days, 0);
  const activeThisWeek = users.filter(u => u.sessionsLast7Days > 0).length;
  const avgMsgsOverall = users.length > 0
    ? (users.reduce((sum, u) => sum + u.avgMessagesPerSession, 0) / users.length).toFixed(1)
    : "0";

  if (isLoading) {
    return (
      <div className="rounded-xl border border-border/50 bg-surface-card1/50 p-6">
        <div className="flex items-center gap-3 text-text-secondary">
          <div className="w-4 h-4 border-2 border-brand border-t-transparent rounded-full animate-spin" />
          <span className="text-sm">Loading user metrics...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="rounded-xl border border-red-500/30 bg-red-500/10 p-4">
        <p className="text-sm text-red-400">{error}</p>
      </div>
    );
  }

  if (users.length === 0) {
    return (
      <div className="rounded-xl border border-border/50 bg-surface-card1/50 p-6 text-center">
        <p className="text-sm text-text-secondary">No Nova users found</p>
      </div>
    );
  }

  return (
    <div className="rounded-xl border border-border/50 bg-surface-card1/50 overflow-hidden w-fit">
      {/* Header with sort controls - matching UserRow widths */}
      <div className="px-3 py-2 border-b border-border/30 bg-surface-card2/30">
        <div className="flex items-center">
          {/* User column - w-56 */}
          <div className="w-56 flex-shrink-0">
            <SortableHeader
              label="User"
              field="name"
              currentSort={sortField}
              currentDirection={sortDirection}
              onSort={handleSort}
            />
          </div>

          {/* Session columns - 4x w-12 */}
          <div className="hidden md:flex items-center">
            <div className="w-12 text-center">
              <span className="text-[10px] text-text-tertiary uppercase tracking-wide">24h</span>
            </div>
            <div className="w-12 text-center">
              <span className="text-[10px] text-text-tertiary uppercase tracking-wide">3d</span>
            </div>
            <div className="w-12 text-center">
              <SortableHeader
                label="7d"
                field="sessions7d"
                currentSort={sortField}
                currentDirection={sortDirection}
                onSort={handleSort}
              />
            </div>
            <div className="w-12 text-center">
              <SortableHeader
                label="30d"
                field="sessions30d"
                currentSort={sortField}
                currentDirection={sortDirection}
                onSort={handleSort}
              />
            </div>
          </div>

          {/* Msgs column - w-16 */}
          <div className="hidden sm:block w-16 text-center">
            <SortableHeader
              label="Msgs"
              field="avgMsgs"
              currentSort={sortField}
              currentDirection={sortDirection}
              onSort={handleSort}
            />
          </div>

          {/* Trend column - w-20 */}
          <div className="w-20 text-center">
            <SortableHeader
              label="Trend"
              field="trend"
              currentSort={sortField}
              currentDirection={sortDirection}
              onSort={handleSort}
            />
          </div>
        </div>
      </div>

      {/* User rows */}
      <div className="divide-y divide-border/20">
        {displayedUsers.map((user) => (
          <UserRow key={user.profileId} user={user} />
        ))}
      </div>

      {/* Show more button */}
      {hasMoreUsers && (
        <div className="px-4 py-2 border-t border-border/20">
          <button
            onClick={() => setShowAll(!showAll)}
            className="w-full text-xs text-text-secondary hover:text-brand transition-colors"
          >
            {showAll ? `Show top 10 only` : `Show all ${users.length} users`}
          </button>
        </div>
      )}

      {/* Summary footer */}
      <div className="px-4 py-2 border-t border-border/30 bg-surface-card2/30">
        <div className="flex items-center justify-center gap-4 text-xs text-text-secondary">
          <span>
            <span className="font-medium text-text-primary">{users.length}</span> users
          </span>
          <span className="text-border">•</span>
          <span>
            <span className="font-medium text-brand">{activeThisWeek}</span> active (7d)
          </span>
          <span className="text-border">•</span>
          <span>
            <span className="font-medium text-violet-400">{avgMsgsOverall}</span> msgs/ses
          </span>
          <span className="text-border">•</span>
          <span>
            <span className="font-medium text-text-primary">{totalSessions7d}</span> sessions (7d)
          </span>
        </div>
      </div>
    </div>
  );
}

// ============================================
// MAIN PAGE COMPONENT
// ============================================

export function NovaMetricsPage(): JSX.Element {
  const [metrics, setMetrics] = useState<NovaAlphaMetrics | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // User usage state
  const [userUsage, setUserUsage] = useState<UserNovaUsage[]>([]);
  const [userUsageLoading, setUserUsageLoading] = useState(true);
  const [userUsageError, setUserUsageError] = useState<string | null>(null);

  useEffect(() => {
    const fetchMetrics = async (): Promise<void> => {
      try {
        const token = localStorage.getItem(config.auth.accessTokenKey);
        const response = await fetch(
          `${config.api.baseUrl}/api/admin/nova/metrics`,
          {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }
        );

        if (!response.ok) {
          throw new Error("Failed to fetch metrics");
        }

        const data = (await response.json()) as NovaAlphaMetrics;
        setMetrics(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load metrics");
      } finally {
        setIsLoading(false);
      }
    };

    const fetchUserUsage = async (): Promise<void> => {
      try {
        const token = localStorage.getItem(config.auth.accessTokenKey);
        const response = await fetch(
          `${config.api.baseUrl}/api/admin/nova/metrics/users`,
          {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }
        );

        if (!response.ok) {
          throw new Error("Failed to fetch user usage");
        }

        const data = (await response.json()) as { users: UserNovaUsage[] };
        setUserUsage(data.users);
      } catch (err) {
        setUserUsageError(err instanceof Error ? err.message : "Failed to load user usage");
      } finally {
        setUserUsageLoading(false);
      }
    };

    void fetchMetrics();
    void fetchUserUsage();
  }, []);

  if (isLoading) {
    return (
      <div className="p-6">
        <h1 className="text-xl font-bold text-text-primary mb-6">
          Nova Alpha Metrics
        </h1>
        <div className="flex items-center gap-3 text-text-secondary">
          <div className="w-5 h-5 border-2 border-brand border-t-transparent rounded-full animate-spin" />
          <span className="text-sm">Loading metrics...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-6">
        <h1 className="text-xl font-bold text-text-primary mb-6">
          Nova Alpha Metrics
        </h1>
        <div className="rounded-xl border border-red-500/30 bg-red-500/10 p-4">
          <p className="text-sm text-red-400">{error}</p>
        </div>
      </div>
    );
  }

  if (!metrics) {
    return (
      <div className="p-6">
        <h1 className="text-xl font-bold text-text-primary mb-6">
          Nova Alpha Metrics
        </h1>
        <p className="text-sm text-text-secondary">No metrics available</p>
      </div>
    );
  }

  return (
    <div className="p-6 max-w-6xl">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-text-primary mb-1">
          Nova Alpha Metrics
        </h1>
        <p className="text-sm text-text-secondary">
          Track application funnel, user activation, and engagement
        </p>
      </div>

      {/* Application Funnel */}
      <div className="mb-8">
        <ApplicationFunnel metrics={metrics} />
      </div>

      {/* User Activation */}
      <div className="mb-8">
        <SectionHeader
          icon="🎯"
          title="User Activation"
          subtitle="Nova users who completed 3+ conversations"
        />
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <MetricCard
            label="Nova Users"
            value={metrics.novaUsers}
            icon="👥"
            subtitle="Users with Nova access"
            accentColor="violet"
          />
          <MetricCard
            label="Activated Users"
            value={metrics.activatedUsers}
            icon="✨"
            subtitle="3+ conversations"
            accentColor="emerald"
          />
          <ProgressCard
            label="Activation Rate"
            value={metrics.activatedUsers}
            total={metrics.novaUsers}
            percentage={metrics.activationRate}
            icon="📈"
            accentColor="brand"
          />
        </div>
      </div>

      {/* Engagement */}
      <div className="mb-8">
        <SectionHeader
          icon="🔥"
          title="Weekly Engagement"
          subtitle="Activity in the last 7 days"
        />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <MetricCard
            label="Active Users"
            value={metrics.activeLast7Days}
            icon="⚡"
            subtitle="Had at least 1 session this week"
            accentColor="amber"
          />
          <ProgressCard
            label="Active Rate"
            value={metrics.activeLast7Days}
            total={metrics.novaUsers}
            percentage={metrics.activeRate}
            icon="📊"
            accentColor="amber"
          />
        </div>
      </div>

      {/* Usage Stats */}
      <div className="mb-8">
        <SectionHeader
          icon="💬"
          title="Usage Statistics"
          subtitle="Conversation and message metrics"
        />
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <MetricCard
            label="Total Sessions"
            value={metrics.totalSessions}
            icon="🗂️"
            accentColor="blue"
          />
          <MetricCard
            label="Total Messages"
            value={metrics.totalMessages}
            icon="💬"
            accentColor="blue"
          />
          <MetricCard
            label="Avg Sessions/User"
            value={metrics.avgSessionsPerUser}
            icon="👤"
            accentColor="violet"
          />
          <MetricCard
            label="Avg Msgs/Session"
            value={metrics.avgMessagesPerSession}
            icon="📝"
            accentColor="violet"
          />
        </div>
      </div>

      {/* User Usage */}
      <div className="mb-8">
        <SectionHeader
          icon="👤"
          title="Usage by User"
          subtitle="Individual engagement metrics • Click columns to sort"
        />
        <UserUsageSection
          users={userUsage}
          isLoading={userUsageLoading}
          error={userUsageError}
        />
      </div>

      {/* Footer */}
      <div className="text-center pt-4 border-t border-border/30">
        <p className="text-xs text-text-tertiary">
          Metrics updated in real-time • Codewrinkles Nova Alpha
        </p>
      </div>
    </div>
  );
}
