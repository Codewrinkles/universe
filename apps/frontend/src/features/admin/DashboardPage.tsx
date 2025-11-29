import { useEffect, useState } from "react";
import { Card } from "../../components/ui/Card";
import { config } from "../../config";

interface DashboardMetrics {
  totalUsers: number;
  activeUsers: number;
  totalPulses: number;
}

interface MetricCardProps {
  title: string;
  value: number;
  icon: string;
  description: string;
}

function MetricCard({ title, value, icon, description }: MetricCardProps): JSX.Element {
  return (
    <Card>
      <div className="flex items-start justify-between">
        <div>
          <p className="text-xs text-text-tertiary mb-1">{title}</p>
          <p className="text-3xl font-bold text-text-primary">{value.toLocaleString()}</p>
          <p className="text-xs text-text-secondary mt-2">{description}</p>
        </div>
        <div className="text-4xl">{icon}</div>
      </div>
    </Card>
  );
}

export function DashboardPage(): JSX.Element {
  const [metrics, setMetrics] = useState<DashboardMetrics | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchMetrics = async (): Promise<void> => {
      try {
        const token = localStorage.getItem(config.auth.accessTokenKey);
        const response = await fetch(`${config.api.baseUrl}/api/admin/dashboard/metrics`, {
          headers: {
            "Authorization": `Bearer ${token}`,
          },
        });

        if (!response.ok) {
          throw new Error("Failed to fetch metrics");
        }

        const data = await response.json() as DashboardMetrics;
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
        <h1 className="text-xl font-bold text-text-primary mb-6">Dashboard</h1>
        <p className="text-sm text-text-secondary">Loading metrics...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-6">
        <h1 className="text-xl font-bold text-text-primary mb-6">Dashboard</h1>
        <p className="text-sm text-red-400">{error}</p>
      </div>
    );
  }

  if (!metrics) {
    return (
      <div className="p-6">
        <h1 className="text-xl font-bold text-text-primary mb-6">Dashboard</h1>
        <p className="text-sm text-text-secondary">No metrics available</p>
      </div>
    );
  }

  return (
    <div className="p-6">
      <h1 className="text-xl font-bold text-text-primary mb-6">Dashboard</h1>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <MetricCard
          title="Total Users"
          value={metrics.totalUsers}
          icon="👥"
          description="All registered users"
        />
        <MetricCard
          title="Active Users"
          value={metrics.activeUsers}
          icon="✨"
          description="Active in last 30 days"
        />
        <MetricCard
          title="Total Pulses"
          value={metrics.totalPulses}
          icon="📝"
          description="All pulses created"
        />
      </div>
    </div>
  );
}
