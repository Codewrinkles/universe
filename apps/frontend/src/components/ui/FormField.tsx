/**
 * Reusable form field component
 * Includes label, input, and error message display
 */

import type { InputHTMLAttributes, ReactNode } from "react";

export interface FormFieldProps extends Omit<InputHTMLAttributes<HTMLInputElement>, "className"> {
  label: string;
  error?: string;
  children?: ReactNode;
}

export function FormField({
  label,
  error,
  children,
  id,
  ...inputProps
}: FormFieldProps): JSX.Element {
  const inputId = id || label.toLowerCase().replace(/\s+/g, "-");
  const hasError = Boolean(error);

  return (
    <div className="space-y-1">
      <label htmlFor={inputId} className="block text-xs text-text-tertiary">
        {label}
      </label>
      <input
        id={inputId}
        className={`w-full rounded-xl border bg-surface-page px-3 py-2 text-sm text-text-primary placeholder:text-text-tertiary focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-surface-page transition-colors ${
          hasError
            ? "border-red-500 focus:ring-red-500/50"
            : "border-border focus:ring-brand-soft/70"
        }`}
        aria-invalid={hasError}
        aria-describedby={hasError ? `${inputId}-error` : undefined}
        {...inputProps}
      />
      {hasError && (
        <p id={`${inputId}-error`} className="text-xs text-red-500" role="alert">
          {error}
        </p>
      )}
      {children}
    </div>
  );
}
