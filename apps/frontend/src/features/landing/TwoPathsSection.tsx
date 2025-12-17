import { Link } from "react-router-dom";
import { Card } from "../../components/ui/Card";

/**
 * Two paths to Alpha access: Apply or Earn via Pulse
 */
export function TwoPathsSection(): JSX.Element {
  return (
    <section className="mx-auto max-w-6xl px-4 pb-24 lg:pb-32">
      <div className="text-center mb-12">
        <h2 className="text-2xl lg:text-3xl font-bold tracking-tight text-text-primary mb-4">
          Two Ways to Get Codewrinkles Nova Alpha Access
        </h2>
        <p className="text-base text-text-secondary max-w-2xl mx-auto">
          Apply directly or earn your spot through community engagement.
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-2 max-w-4xl mx-auto">
        {/* Path 1: Apply */}
        <Card className="relative">
          <div className="flex flex-col h-full">
            <div className="flex items-center gap-4 mb-4">
              <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-brand/10">
                <svg className="h-6 w-6 text-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 002.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 00-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75 2.25 2.25 0 00-.1-.664m-5.8 0A2.251 2.251 0 0113.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25zM6.75 12h.008v.008H6.75V12zm0 3h.008v.008H6.75V15zm0 3h.008v.008H6.75V18z" />
                </svg>
              </div>
              <h3 className="text-xl font-semibold text-text-primary">Apply</h3>
            </div>

            <p className="text-sm text-text-secondary mb-6 flex-1">
              Tell us about your learning goals. We're hand-picking 50 developers for our Alpha program.
            </p>

            <Link
              to="/alpha/apply"
              className="inline-flex items-center justify-center rounded-full bg-brand text-black px-6 py-2.5 text-sm font-medium hover:bg-brand-soft transition-colors"
            >
              Apply Now
            </Link>
          </div>
        </Card>

        {/* Path 2: Earn It */}
        <Card className="relative">
          <div className="flex flex-col h-full">
            <div className="flex items-center gap-4 mb-4">
              <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-amber-500/10">
                <svg className="h-6 w-6 text-amber-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M16.5 18.75h-9m9 0a3 3 0 013 3h-15a3 3 0 013-3m9 0v-3.375c0-.621-.503-1.125-1.125-1.125h-.871M7.5 18.75v-3.375c0-.621.504-1.125 1.125-1.125h.872m5.007 0H9.497m5.007 0a7.454 7.454 0 01-.982-3.172M9.497 14.25a7.454 7.454 0 00.981-3.172M5.25 4.236c-.982.143-1.954.317-2.916.52A6.003 6.003 0 007.73 9.728M5.25 4.236V4.5c0 2.108.966 3.99 2.48 5.228M5.25 4.236V2.721C7.456 2.41 9.71 2.25 12 2.25c2.291 0 4.545.16 6.75.47v1.516M7.73 9.728a6.726 6.726 0 002.748 1.35m8.272-6.842V4.5c0 2.108-.966 3.99-2.48 5.228m2.48-5.492a46.32 46.32 0 012.916.52 6.003 6.003 0 01-5.395 4.972m0 0a6.726 6.726 0 01-2.749 1.35m0 0a6.772 6.772 0 01-3.044 0" />
                </svg>
              </div>
              <h3 className="text-xl font-semibold text-text-primary">Earn It</h3>
            </div>

            <p className="text-sm text-text-secondary mb-6 flex-1">
              Post 15+ Pulses in 30 days and unlock automatic access. Show us you're serious about growth.
            </p>

            <Link
              to="/register"
              className="inline-flex items-center justify-center rounded-full border border-border bg-surface-card2 text-text-primary px-6 py-2.5 text-sm font-medium hover:border-brand-soft/60 hover:bg-surface-card1 transition-colors"
            >
              Join Codewrinkles Pulse
            </Link>
          </div>
        </Card>
      </div>
    </section>
  );
}
