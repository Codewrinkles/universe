export interface PathProgressProps {
  label: string;
  progress: number;
}

export function PathProgress({ label, progress }: PathProgressProps): JSX.Element {
  return (
    <div>
      <div className="flex items-center justify-between text-xs text-text-tertiary">
        <span>{label}</span>
        <span className="text-text-secondary">{progress}%</span>
      </div>
      <div className="mt-1 h-2 rounded-full bg-surface-card2">
        <div
          className="h-2 rounded-full bg-violet-400"
          style={{ width: `${progress}%` }}
        ></div>
      </div>
    </div>
  );
}
