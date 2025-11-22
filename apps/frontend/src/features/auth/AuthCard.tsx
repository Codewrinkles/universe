import { Card } from "../../components/ui/Card";

export interface AuthCardProps {
  title: string;
  subtitle: string;
  children: React.ReactNode;
}

export function AuthCard({ title, subtitle, children }: AuthCardProps): JSX.Element {
  return (
    <div className="min-h-[60vh] flex items-center justify-center">
      <div className="w-full max-w-md">
        <Card className="relative overflow-hidden">
          <div className="absolute inset-x-8 top-0 h-px bg-gradient-to-r from-transparent via-brand-soft/60 to-transparent opacity-60" />
          <div className="mb-4">
            <h1 className="text-base font-semibold tracking-tight text-text-primary">{title}</h1>
            <p className="mt-1 text-xs text-text-secondary">{subtitle}</p>
          </div>
          {children}
        </Card>
      </div>
    </div>
  );
}
