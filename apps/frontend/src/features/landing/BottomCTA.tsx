import { Link } from "react-router-dom";

/**
 * Bottom CTA section with final call to action
 */
export function BottomCTA(): JSX.Element {
  return (
    <section className="mx-auto max-w-6xl px-4 pb-24 lg:pb-32">
      <div className="text-center border border-border rounded-2xl bg-surface-card1 px-8 py-12 lg:px-12 lg:py-16">
        <h2 className="text-3xl font-bold tracking-tight text-text-primary mb-4">
          Ready to level up?
        </h2>
        <p className="text-base text-text-secondary mb-8 max-w-lg mx-auto">
          Join 50 developers shaping the future of AI-powered learning.
        </p>
        <div className="flex flex-col sm:flex-row gap-4 justify-center items-center">
          <Link
            to="/alpha/apply"
            className="inline-flex items-center justify-center rounded-full bg-brand text-black px-8 py-3 text-base font-medium hover:bg-brand-soft transition-colors shadow-lg shadow-brand/20"
          >
            Apply for Alpha
          </Link>
          <Link
            to="/register"
            className="inline-flex items-center justify-center rounded-full border border-border bg-surface-card2 text-text-primary px-8 py-3 text-base font-medium hover:border-brand-soft/60 hover:bg-surface-card1 transition-colors"
          >
            Join Codewrinkles Pulse
          </Link>
        </div>
      </div>
    </section>
  );
}
