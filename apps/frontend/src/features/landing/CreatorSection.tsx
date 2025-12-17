/**
 * Creator section with quote from Dan
 * Tells the brand evolution story
 */
export function CreatorSection(): JSX.Element {
  return (
    <section className="mx-auto max-w-6xl px-4 pb-24 lg:pb-32">
      <div className="max-w-3xl mx-auto">
        <div className="relative rounded-2xl border border-border bg-surface-card1 px-8 py-10 lg:px-12 lg:py-12">
          {/* Decorative quote mark */}
          <div className="absolute -top-4 left-8 lg:left-12">
            <span className="text-5xl text-brand/30 font-serif">"</span>
          </div>

          <blockquote className="relative">
            <p className="text-base lg:text-lg text-text-secondary leading-relaxed mb-6">
              Codewrinkles started on YouTube with one goal: help developers grow. Codewrinkles Pulse built a community around that mission. Now, Codewrinkles Nova takes it to the next level - an AI coach that actually knows your journey. I'm excited to build this with you.
            </p>

            <footer className="flex items-center gap-4">
              <div className="h-12 w-12 rounded-full bg-gradient-to-br from-brand to-brand-soft flex items-center justify-center text-black font-semibold text-lg">
                D
              </div>
              <div>
                <p className="text-sm font-medium text-text-primary">Dan</p>
                <p className="text-xs text-text-tertiary">Founder of Codewrinkles</p>
              </div>
            </footer>
          </blockquote>
        </div>
      </div>
    </section>
  );
}
