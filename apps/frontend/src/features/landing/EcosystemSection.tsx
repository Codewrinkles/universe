import { Card } from "../../components/ui/Card";

/**
 * Ecosystem section showing the three pillars: Nova, Pulse, YouTube
 */
export function EcosystemSection(): JSX.Element {
  return (
    <section className="mx-auto max-w-6xl px-4 pb-24 lg:pb-32">
      <div className="text-center mb-12">
        <h2 className="text-2xl lg:text-3xl font-bold tracking-tight text-text-primary mb-4">
          One Ecosystem. Three Ways to Grow.
        </h2>
        <p className="text-base text-text-secondary max-w-2xl mx-auto">
          Learn with AI, share insights with peers, and dive deep with long-form content.
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Nova - AI Coach */}
        <Card className="relative overflow-hidden">
          <div className="absolute top-0 right-0 px-3 py-1 rounded-bl-lg bg-violet-600/20 border-l border-b border-violet-500/30">
            <span className="text-xs font-medium text-violet-400">Alpha</span>
          </div>
          <div className="text-center pt-4">
            <div className="inline-flex h-14 w-14 items-center justify-center rounded-2xl bg-violet-600/10 mb-4">
              <svg className="h-7 w-7 text-violet-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9.75 3.104v5.714a2.25 2.25 0 01-.659 1.591L5 14.5M9.75 3.104c-.251.023-.501.05-.75.082m.75-.082a24.301 24.301 0 014.5 0m0 0v5.714c0 .597.237 1.17.659 1.591L19.8 15.3M14.25 3.104c.251.023.501.05.75.082M19.8 15.3l-1.57.393A9.065 9.065 0 0112 15a9.065 9.065 0 00-6.23.693L5 14.5m14.8.8l1.402 1.402c1.232 1.232.65 3.318-1.067 3.611A48.309 48.309 0 0112 21c-2.773 0-5.491-.235-8.135-.687-1.718-.293-2.3-2.379-1.067-3.61L5 14.5" />
              </svg>
            </div>
            <h3 className="text-lg font-semibold tracking-tight text-text-primary mb-2">
              Codewrinkles Nova
            </h3>
            <p className="text-sm text-text-secondary">
              Your AI learning coach. Personalized guidance that remembers your goals and adapts to your level.
            </p>
          </div>
        </Card>

        {/* Pulse - Community */}
        <Card className="relative overflow-hidden">
          <div className="absolute top-0 right-0 px-3 py-1 rounded-bl-lg bg-brand/20 border-l border-b border-brand/30">
            <span className="text-xs font-medium text-brand">Live</span>
          </div>
          <div className="text-center pt-4">
            <div className="inline-flex h-14 w-14 items-center justify-center rounded-2xl bg-brand/10 mb-4">
              <svg className="h-7 w-7 text-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7.5 8.25h9m-9 3H12m-9.75 1.51c0 1.6 1.123 2.994 2.707 3.227 1.129.166 2.27.293 3.423.379.35.026.67.21.865.501L12 21l2.755-4.133a1.14 1.14 0 01.865-.501 48.172 48.172 0 003.423-.379c1.584-.233 2.707-1.626 2.707-3.228V6.741c0-1.602-1.123-2.995-2.707-3.228A48.394 48.394 0 0012 3c-2.392 0-4.744.175-7.043.513C3.373 3.746 2.25 5.14 2.25 6.741v6.018z" />
              </svg>
            </div>
            <h3 className="text-lg font-semibold tracking-tight text-text-primary mb-2">
              Codewrinkles Pulse
            </h3>
            <p className="text-sm text-text-secondary">
              Share insights, learn from peers. A community built for signal, not noise.
            </p>
          </div>
        </Card>

        {/* YouTube - Content */}
        <Card className="relative overflow-hidden">
          <div className="absolute top-0 right-0 px-3 py-1 rounded-bl-lg bg-brand/20 border-l border-b border-brand/30">
            <span className="text-xs font-medium text-brand">Live</span>
          </div>
          <div className="text-center pt-4">
            <div className="inline-flex h-14 w-14 items-center justify-center rounded-2xl bg-red-600/10 mb-4">
              <svg className="h-7 w-7 text-red-500" fill="currentColor" viewBox="0 0 24 24">
                <path d="M23.498 6.186a3.016 3.016 0 0 0-2.122-2.136C19.505 3.545 12 3.545 12 3.545s-7.505 0-9.377.505A3.017 3.017 0 0 0 .502 6.186C0 8.07 0 12 0 12s0 3.93.502 5.814a3.016 3.016 0 0 0 2.122 2.136c1.871.505 9.376.505 9.376.505s7.505 0 9.377-.505a3.015 3.015 0 0 0 2.122-2.136C24 15.93 24 12 24 12s0-3.93-.502-5.814zM9.545 15.568V8.432L15.818 12l-6.273 3.568z"/>
              </svg>
            </div>
            <h3 className="text-lg font-semibold tracking-tight text-text-primary mb-2">
              YouTube
            </h3>
            <p className="text-sm text-text-secondary">
              Deep dives into architecture, patterns, and real-world development.
            </p>
          </div>
        </Card>
      </div>

      {/* Flow text */}
      <div className="mt-8 text-center">
        <p className="text-sm text-text-tertiary">
          <span className="text-brand">Learn with AI</span>
          <span className="mx-3 text-text-tertiary/50">→</span>
          <span className="text-brand">Share insights</span>
          <span className="mx-3 text-text-tertiary/50">→</span>
          <span className="text-brand">Watch deep dives</span>
        </p>
      </div>
    </section>
  );
}
