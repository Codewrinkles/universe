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
// MAIN PAGE COMPONENT
// ============================================

export function NovaMetricsPage(): JSX.Element {
  const [metrics, setMetrics] = useState<NovaAlphaMetrics | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

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

    void fetchMetrics();
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

      {/* Footer */}
      <div className="text-center pt-4 border-t border-border/30">
        <p className="text-xs text-text-tertiary">
          Metrics updated in real-time • Codewrinkles Nova Alpha
        </p>
      </div>
    </div>
  );
}
