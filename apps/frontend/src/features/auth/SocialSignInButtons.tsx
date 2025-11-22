function SocialButton({ label, short }: { label: string; short: string }): JSX.Element {
  return (
    <button
      type="button"
      className="flex items-center justify-center gap-2 rounded-xl border border-border bg-surface-card2 px-3 py-2 text-xs text-text-secondary hover:border-brand-soft/50 hover:bg-surface-page transition-colors"
    >
      <span className="flex h-5 w-5 items-center justify-center rounded-full bg-surface-page text-[11px] text-text-primary">
        {short}
      </span>
      <span>{label}</span>
    </button>
  );
}

export function SocialSignInButtons(): JSX.Element {
  return (
    <>
      <div className="mt-4 flex items-center gap-2 text-[11px] text-text-tertiary">
        <span className="h-px flex-1 bg-border-deep" />
        <span>or continue with</span>
        <span className="h-px flex-1 bg-border-deep" />
      </div>

      <div className="mt-3 grid grid-cols-3 gap-2 text-xs">
        <SocialButton label="Google" short="G" />
        <SocialButton label="GitHub" short="GH" />
        <SocialButton label="Facebook" short="f" />
      </div>
    </>
  );
}
