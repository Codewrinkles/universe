import { Link } from "react-router-dom";

/**
 * Hero section for the landing page
 * Features Nova as the flagship product with dual CTAs
 */
export function HeroSection(): JSX.Element {
  return (
    <section className="mx-auto max-w-6xl px-4 pt-20 pb-24 lg:pt-32 lg:pb-32">
      <div className="text-center">
        {/* Decorative gradient blob - brand teal */}
        <div className="relative mb-12">
          <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[600px] h-[600px] opacity-20 blur-3xl pointer-events-none">
            <div className="absolute inset-0 bg-gradient-to-r from-brand via-brand-soft to-brand rounded-full animate-pulse" />
          </div>
          <div className="relative">
            <h1 className="text-4xl lg:text-6xl font-bold tracking-tight text-text-primary mb-6">
              Your Personal Path to
              <br />
              <span className="bg-gradient-to-r from-brand to-brand-soft bg-clip-text text-transparent">
                Technical Excellence
              </span>
            </h1>
          </div>
        </div>

        <p className="text-lg lg:text-xl text-text-secondary max-w-2xl mx-auto mb-10">
          Codewrinkles Nova is an AI coach that remembers your background, tracks your growth,
          and adapts every conversation to where you are in your journey.
        </p>

        {/* Dual CTAs */}
        <div className="flex flex-col sm:flex-row gap-4 justify-center items-center mb-6">
          <Link
            to="/alpha/apply"
            className="inline-flex items-center justify-center rounded-full bg-brand text-black px-8 py-3 text-base font-medium hover:bg-brand-soft transition-colors shadow-lg shadow-brand/20"
          >
            Apply for Alpha Access
          </Link>
          <Link
            to="/register"
            className="inline-flex items-center justify-center rounded-full border border-border bg-surface-card2 text-text-primary px-8 py-3 text-base font-medium hover:border-brand-soft/60 hover:bg-surface-card1 transition-colors"
          >
            Join Codewrinkles Pulse - Free
          </Link>
        </div>

        {/* Priority access badge */}
        <p className="text-sm text-text-tertiary">
          <span className="inline-flex items-center gap-1.5">
            <span className="text-brand">*</span>
            Active Codewrinkles Pulse members get priority Alpha access
          </span>
        </p>
      </div>
    </section>
  );
}
