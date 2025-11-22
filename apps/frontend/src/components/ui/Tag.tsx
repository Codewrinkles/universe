export interface TagProps {
  label: string;
  variant?: "brand" | "pulse" | "nova" | "amber" | "emerald" | "default";
  size?: "sm" | "md";
  onClick?: () => void;
}

export function Tag({ label, variant = "default", size = "md", onClick }: TagProps): JSX.Element {
  const baseClasses = "inline-flex items-center rounded-full border font-medium";

  const variantClasses = {
    brand: "border-brand-soft/40 bg-brand-soft/10 text-brand-soft",
    pulse: "border-sky-800 bg-sky-900/30 text-sky-200",
    nova: "border-violet-800 bg-violet-900/30 text-violet-200",
    amber: "border-amber-800 bg-amber-900/30 text-amber-200",
    emerald: "border-emerald-800 bg-emerald-900/30 text-emerald-200",
    default: "border-border bg-surface-card2 text-text-secondary",
  };

  const sizeClasses = {
    sm: "px-2 py-[2px] text-[11px]",
    md: "px-3 py-[3px] text-[11px]",
  };

  const Component = onClick ? "button" : "span";

  return (
    <Component
      onClick={onClick}
      className={`${baseClasses} ${variantClasses[variant]} ${sizeClasses[size]}`}
    >
      {label}
    </Component>
  );
}
