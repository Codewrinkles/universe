export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "secondary" | "ghost";
  size?: "sm" | "md" | "lg";
  children: React.ReactNode;
}

export function Button({
  variant = "primary",
  size = "md",
  className = "",
  children,
  ...props
}: ButtonProps): JSX.Element {
  const baseClasses = "btn-primary inline-flex items-center rounded-full font-medium transition-colors";

  const variantClasses = {
    primary: "bg-brand-soft text-black hover:bg-brand",
    secondary: "border border-border bg-surface-card1 text-text-secondary hover:border-brand-soft/60 hover:bg-surface-card2",
    ghost: "border border-transparent text-text-secondary hover:border-border hover:bg-surface-card1",
  };

  const sizeClasses = {
    sm: "px-3 py-1 text-xs",
    md: "px-3 py-1.5 text-xs",
    lg: "px-4 py-2 text-sm",
  };

  return (
    <button
      className={`${baseClasses} ${variantClasses[variant]} ${sizeClasses[size]} ${className}`}
      {...props}
    >
      {children}
    </button>
  );
}
