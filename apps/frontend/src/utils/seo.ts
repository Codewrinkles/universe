/**
 * SEO utilities for generating meta tags and structured data
 */

export interface SEOConfig {
  title: string;
  description: string;
  url: string;
  image?: string;
  type?: "website" | "article" | "profile";
  author?: {
    name: string;
    handle: string;
    url: string;
  };
  publishedTime?: string;
  modifiedTime?: string;
}

/**
 * Base URL for the application
 */
const BASE_URL = "https://codewrinkles.com";

/**
 * Get full URL from path
 */
export function getFullUrl(path: string): string {
  return `${BASE_URL}${path.startsWith("/") ? path : `/${path}`}`;
}

/**
 * Truncate text to specified length with ellipsis
 */
export function truncateText(text: string, maxLength: number): string {
  if (text.length <= maxLength) return text;
  return text.substring(0, maxLength - 3) + "...";
}

/**
 * Generate structured data (JSON-LD) for a website
 */
export function generateWebsiteStructuredData(): string {
  return JSON.stringify({
    "@context": "https://schema.org",
    "@type": "WebSite",
    name: "Codewrinkles",
    url: BASE_URL,
    description: "Built for value, not engagement",
    potentialAction: {
      "@type": "SearchAction",
      target: `${BASE_URL}/pulse?q={search_term_string}`,
      "query-input": "required name=search_term_string",
    },
  });
}

/**
 * Generate structured data (JSON-LD) for a pulse post
 */
export function generatePulseStructuredData(pulse: {
  id: string;
  content: string;
  author: {
    name: string;
    handle: string;
    avatarUrl?: string | null;
  };
  createdAt: string;
  imageUrl?: string | null;
}): string {
  return JSON.stringify({
    "@context": "https://schema.org",
    "@type": "SocialMediaPosting",
    headline: truncateText(pulse.content, 100),
    text: pulse.content,
    datePublished: pulse.createdAt,
    author: {
      "@type": "Person",
      name: pulse.author.name,
      url: getFullUrl(`/pulse/u/${pulse.author.handle}`),
      image: pulse.author.avatarUrl || undefined,
    },
    url: getFullUrl(`/pulse/${pulse.id}`),
    ...(pulse.imageUrl && {
      image: {
        "@type": "ImageObject",
        url: pulse.imageUrl,
      },
    }),
  });
}

/**
 * Generate structured data (JSON-LD) for a user profile
 */
export function generateProfileStructuredData(profile: {
  name: string;
  handle: string | null;
  bio?: string | null;
  avatarUrl?: string | null;
  websiteUrl?: string | null;
  location?: string | null;
}): string {
  const handle = profile.handle || "user";
  return JSON.stringify({
    "@context": "https://schema.org",
    "@type": "Person",
    name: profile.name,
    url: getFullUrl(`/pulse/u/${handle}`),
    description: profile.bio || undefined,
    image: profile.avatarUrl || undefined,
    ...(profile.websiteUrl && { sameAs: profile.websiteUrl }),
    ...(profile.location && { address: profile.location }),
  });
}

/**
 * Generate structured data (JSON-LD) for a breadcrumb list
 */
export function generateBreadcrumbStructuredData(
  items: Array<{ name: string; url: string }>
): string {
  return JSON.stringify({
    "@context": "https://schema.org",
    "@type": "BreadcrumbList",
    itemListElement: items.map((item, index) => ({
      "@type": "ListItem",
      position: index + 1,
      name: item.name,
      item: getFullUrl(item.url),
    })),
  });
}

/**
 * Get default OG image URL
 * Using the landing page screenshot for all pages
 */
export function getDefaultOGImage(): string {
  return `${BASE_URL}/og-image.png`;
}

/**
 * Get OG image URL for pulse feed
 * Using the same landing page screenshot
 */
export function getPulseFeedOGImage(): string {
  return `${BASE_URL}/og-image.png`;
}
