import { Helmet } from "react-helmet-async";
import { generateWebsiteStructuredData, getDefaultOGImage } from "../../utils/seo";
import { HeroSection } from "./HeroSection";
import { EcosystemSection } from "./EcosystemSection";
import { TwoPathsSection } from "./TwoPathsSection";
import { CreatorSection } from "./CreatorSection";
import { BottomCTA } from "./BottomCTA";

/**
 * Landing page for Codewrinkles
 * Positions Nova as flagship with ecosystem overview
 */
export function LandingPage(): JSX.Element {
  const title = "Codewrinkles - Your Personal Path to Technical Excellence";
  const description =
    "Codewrinkles Nova is an AI coach that remembers your background and adapts to your learning journey. Join Alpha or earn access through Codewrinkles Pulse.";
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
        <meta name="twitter:card" content="summary" />
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
        <HeroSection />
        <EcosystemSection />
        <TwoPathsSection />
        <CreatorSection />
        <BottomCTA />
      </div>
    </>
  );
}
