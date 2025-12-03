import { Link } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import { Card } from "../../components/ui/Card";
import { YouTubeButton } from "../../components/ui/YouTubeButton";
import { generateWebsiteStructuredData, getDefaultOGImage } from "../../utils/seo";

export function LandingPage(): JSX.Element {
  const title = "Codewrinkles – Built for value, not engagement";
  const description =
    "An ecosystem designed to create genuine connections and meaningful content. Your posts reach your followers. Guaranteed.";
  const url = "https://codewrinkles.com/";

  return (
    <>
      <Helmet>
        {/* Primary Meta Tags */}
        <title>{title}</title>
        <meta name="title" content={title} />
        <meta name="description" content={description} />

        {/* Open Graph / Facebook */}
        <meta property="og:type" content="website" />
        <meta property="og:url" content={url} />
        <meta property="og:title" content={title} />
        <meta property="og:description" content={description} />
        <meta property="og:image" content={getDefaultOGImage()} />

        {/* Twitter */}
        <meta name="twitter:card" content="summary_large_image" />
        <meta name="twitter:url" content={url} />
        <meta name="twitter:title" content={title} />
        <meta name="twitter:description" content={description} />
        <meta name="twitter:image" content={getDefaultOGImage()} />

        {/* Canonical URL */}
        <link rel="canonical" href={url} />

        {/* Structured Data */}
        <script type="application/ld+json">{generateWebsiteStructuredData()}</script>
      </Helmet>

      <div className="min-h-screen bg-surface-page">
      {/* Hero Section */}
      <section className="mx-auto max-w-6xl px-4 pt-20 pb-24 lg:pt-32 lg:pb-32">
        <div className="text-center">
          {/* Decorative gradient blob */}
          <div className="relative mb-12">
            {/* Dark theme gradient - subtle and glowing */}
            <div className="hidden dark:block absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[600px] h-[600px] opacity-20 blur-3xl pointer-events-none">
              <div className="absolute inset-0 bg-gradient-to-r from-brand via-brand-soft to-brand rounded-full animate-pulse" />
            </div>
            {/* Light theme gradient - more visible with darker colors */}
            <div className="block dark:hidden absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[500px] h-[500px] opacity-30 blur-2xl pointer-events-none">
              <div className="absolute inset-0 bg-gradient-to-r from-teal-400 via-cyan-400 to-teal-500 rounded-full animate-pulse" />
            </div>
            <div className="relative">
              <h1 className="text-4xl lg:text-6xl font-bold tracking-tight text-text-primary mb-6">
                Built for value,
                <br />
                <span className="bg-gradient-to-r from-brand to-brand-soft bg-clip-text text-transparent">not engagement.</span>
              </h1>
            </div>
          </div>
          <p className="text-lg lg:text-xl text-text-secondary max-w-2xl mx-auto mb-10">
            An ecosystem designed to create genuine connections and meaningful content.
          </p>
          <div className="flex flex-col gap-6 justify-center items-center">
            <div className="flex flex-col sm:flex-row gap-4 justify-center items-center">
              <Link
                to="/register"
                className="inline-flex items-center justify-center rounded-full bg-brand text-black px-8 py-3 text-base font-medium hover:bg-brand-soft transition-colors shadow-lg shadow-brand/20"
              >
                Join Codewrinkles
              </Link>
              <Link
                to="/login"
                className="text-sm text-text-secondary hover:text-text-primary transition-colors"
              >
                Already have an account? <span className="text-brand-soft">Log in</span>
              </Link>
            </div>

            {/* YouTube channel link */}
            <div className="flex flex-col items-center gap-2">
              <p className="text-xs text-text-tertiary">Follow the journey</p>
              <YouTubeButton variant="landing" />
            </div>
          </div>
        </div>
      </section>

      {/* Value Propositions */}
      <section className="mx-auto max-w-6xl px-4 pb-24 lg:pb-32">
        <div className="grid gap-6 lg:grid-cols-3">
          {/* Column 1: Guaranteed Reach */}
          <Card>
            <div className="text-center">
              <div className="inline-flex h-12 w-12 items-center justify-center rounded-full bg-brand/10 mb-4">
                <svg className="h-6 w-6 text-brand-soft" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
                </svg>
              </div>
              <h3 className="text-lg font-semibold tracking-tight text-text-primary mb-2">
                You post it. They see it.
              </h3>
              <p className="text-sm text-text-secondary">
                We call it Pulse because every post reaches your followers like a heartbeat. Consistent, guaranteed, unavoidable. No algorithmic games.
              </p>
            </div>
          </Card>

          {/* Column 2: No Algorithm Gaming */}
          <Card>
            <div className="text-center">
              <div className="inline-flex h-12 w-12 items-center justify-center rounded-full bg-brand/10 mb-4">
                <svg className="h-6 w-6 text-brand-soft" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
                </svg>
              </div>
              <h3 className="text-lg font-semibold tracking-tight text-text-primary mb-2">
                Stop playing the algorithm game
              </h3>
              <p className="text-sm text-text-secondary">
                Focus on creating value, not chasing engagement metrics. Your content reaches your audience based on their choice to follow you, not a black box.
              </p>
            </div>
          </Card>

          {/* Column 3: Own Your Audience */}
          <Card>
            <div className="text-center">
              <div className="inline-flex h-12 w-12 items-center justify-center rounded-full bg-brand/10 mb-4">
                <svg className="h-6 w-6 text-brand-soft" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                </svg>
              </div>
              <h3 className="text-lg font-semibold tracking-tight text-text-primary mb-2">
                Build your platform, not theirs
              </h3>
              <p className="text-sm text-text-secondary">
                Your followers, your content, your community. No platform holding your audience hostage.
              </p>
            </div>
          </Card>
        </div>
      </section>

      {/* Bottom CTA */}
      <section className="mx-auto max-w-6xl px-4 pb-24 lg:pb-32">
        <div className="text-center border border-border rounded-2xl bg-surface-card1 px-8 py-12 lg:px-12 lg:py-16">
          <h2 className="text-3xl font-bold tracking-tight text-text-primary mb-4">
            Ready to join?
          </h2>
          <p className="text-base text-text-secondary mb-8 max-w-lg mx-auto">
            Start sharing your insights with an audience that actually cares.
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center items-center">
            <Link
              to="/register"
              className="inline-flex items-center justify-center rounded-full bg-brand text-black px-8 py-3 text-base font-medium hover:bg-brand-soft transition-colors"
            >
              Create Account
            </Link>
            <Link
              to="/login"
              className="inline-flex items-center justify-center rounded-full border border-border bg-surface-card2 text-text-primary px-8 py-3 text-base font-medium hover:border-brand-soft/60 hover:bg-surface-card1 transition-colors"
            >
              Sign In
            </Link>
          </div>
        </div>
      </section>
      </div>
    </>
  );
}
