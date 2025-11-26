import { WhoToFollow } from "./WhoToFollow";

export function PulseRightSidebar(): JSX.Element {
  return (
    <div className="space-y-4">
      {/* Search bar */}
      <div className="relative">
        <input
          type="text"
          placeholder="Search Pulse"
          className="w-full rounded-full border border-border bg-surface-card1 px-4 py-2.5 pl-10 text-sm text-text-primary placeholder:text-text-tertiary focus:outline-none focus:ring-2 focus:ring-brand-soft/70 focus:border-transparent"
        />
        <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-text-tertiary">
          🔍
        </span>
      </div>

      <WhoToFollow />

      {/* Footer links */}
      <div className="px-4 text-[11px] text-text-tertiary">
        <div className="flex flex-wrap gap-x-2 gap-y-1">
          <a href="#" className="hover:underline">Terms of Service</a>
          <a href="#" className="hover:underline">Privacy Policy</a>
          <a href="#" className="hover:underline">Cookie Policy</a>
          <a href="#" className="hover:underline">Accessibility</a>
        </div>
        <p className="mt-2">© 2025 Codewrinkles</p>
      </div>
    </div>
  );
}
