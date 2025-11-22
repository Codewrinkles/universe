export interface CardProps {
  children: React.ReactNode;
  className?: string;
}

export function Card({ children, className = "" }: CardProps): JSX.Element {
  return (
    <div
      className={`rounded-2xl border border-border bg-surface-card1 p-5 shadow-sm transition-all duration-150 hover:border-border-deep/80 ${className}`}
    >
      {children}
    </div>
  );
}
