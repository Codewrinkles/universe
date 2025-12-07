export const config = {
  runtime: 'edge',
};

// Bot detection patterns (comprehensive list)
const BOT_PATTERNS = [
  // Social Media Crawlers
  /facebookexternalhit/i,
  /facebot/i,
  /twitterbot/i,
  /linkedinbot/i,
  /discordbot/i,
  /slackbot-linkexpanding/i,
  /slackbot/i,
  /telegrambot/i,
  /whatsapp/i,
  /pinterestbot/i,
  /redditbot/i,

  // Search Engines
  /googlebot/i,
  /bingbot/i,
  /bingpreview/i,
  /baiduspider/i,
  /yandexbot/i,
  /duckduckbot/i,

  // Generic
  /bot|crawler|spider|scraper/i,
];

// Environment variables
const API_BASE_URL = process.env.VITE_API_BASE_URL || 'https://app-codwrinkles-api.azurewebsites.net';
const SITE_URL = 'https://www.codewrinkles.com';

/**
 * Type definitions matching the exact backend DTOs
 * Based on actual backend response structures
 */
interface PulseDto {
  id: string;
  content: string;
  createdAt: string;
  author: {
    id: string;
    name: string;
    handle: string;
    avatarUrl: string | null;
  };
  imageUrl: string | null;
  // Other fields exist but aren't needed for meta tags
}

interface ProfileDto {
  profileId: string;
  name: string;
  handle: string;
  bio: string | null;
  avatarUrl: string | null;
  location: string | null;
  websiteUrl: string | null;
}

/**
 * Detects if the request is from a bot
 */
function isBot(userAgent: string): boolean {
  return BOT_PATTERNS.some((pattern) => pattern.test(userAgent));
}

/**
 * Escapes HTML to prevent XSS
 */
function escapeHtml(text: string): string {
  const map: Record<string, string> = {
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#039;',
  };
  return text.replace(/[&<>"']/g, (char) => map[char]);
}

/**
 * Ensures URL is absolute
 */
function ensureAbsoluteUrl(url: string | null | undefined, fallback: string): string {
  if (!url) return fallback;
  if (url.startsWith('http')) return url;
  return `${SITE_URL}${url}`;
}

/**
 * Fetches pulse data from existing backend endpoint
 */
async function fetchPulse(pulseId: string): Promise<PulseDto | null> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/pulse/${pulseId}`, {
      headers: {
        'Accept': 'application/json',
      },
    });

    if (!response.ok) {
      console.error(`Failed to fetch pulse: ${response.status}`);
      return null;
    }

    return await response.json();
  } catch (error) {
    console.error('Error fetching pulse:', error);
    return null;
  }
}

/**
 * Fetches profile data from existing backend endpoint
 */
async function fetchProfile(handle: string): Promise<ProfileDto | null> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/profile/handle/${handle}`, {
      headers: {
        'Accept': 'application/json',
      },
    });

    if (!response.ok) {
      console.error(`Failed to fetch profile: ${response.status}`);
      return null;
    }

    return await response.json();
  } catch (error) {
    console.error('Error fetching profile:', error);
    return null;
  }
}

/**
 * Generates structured data (JSON-LD) for different page types
 */
function generateStructuredData(
  type: 'website' | 'article' | 'profile',
  data: {
    title: string;
    description: string;
    url: string;
    imageUrl?: string;
    authorName?: string;
    publishedDate?: string;
  }
): string {
  if (type === 'website') {
    return `<script type="application/ld+json">
    {
      "@context": "https://schema.org",
      "@type": "WebSite",
      "name": "Codewrinkles",
      "url": "${data.url}",
      "description": "${escapeHtml(data.description)}",
      "potentialAction": {
        "@type": "SearchAction",
        "target": "https://codewrinkles.com/pulse?q={search_term_string}",
        "query-input": "required name=search_term_string"
      }
    }
    </script>`;
  }

  if (type === 'article') {
    return `<script type="application/ld+json">
    {
      "@context": "https://schema.org",
      "@type": "SocialMediaPosting",
      "headline": "${escapeHtml(data.title)}",
      "description": "${escapeHtml(data.description)}",
      "url": "${data.url}",
      "datePublished": "${data.publishedDate || new Date().toISOString()}",
      "author": {
        "@type": "Person",
        "name": "${escapeHtml(data.authorName || 'Unknown')}"
      },
      "image": "${data.imageUrl || 'https://codewrinkles.com/og-image.png'}"
    }
    </script>`;
  }

  if (type === 'profile') {
    return `<script type="application/ld+json">
    {
      "@context": "https://schema.org",
      "@type": "ProfilePage",
      "name": "${escapeHtml(data.title)}",
      "description": "${escapeHtml(data.description)}",
      "url": "${data.url}",
      "image": "${data.imageUrl || 'https://codewrinkles.com/default-avatar.png'}"
    }
    </script>`;
  }

  // Fallback
  return '';
}

/**
 * Injects meta tags into HTML
 */
function injectMetaTags(
  html: string,
  title: string,
  description: string,
  imageUrl: string,
  url: string,
  keywords: string = 'social media, microblogging, pulse, content creation, genuine connections',
  author: string = 'Codewrinkles',
  ogType: 'article' | 'profile' | 'website' = 'website',
  structuredData: string = ''
): string {
  return html
    .replace(/__META_TITLE__/g, escapeHtml(title))
    .replace(/__META_DESCRIPTION__/g, escapeHtml(description))
    .replace(/__META_KEYWORDS__/g, escapeHtml(keywords))
    .replace(/__META_AUTHOR__/g, escapeHtml(author))
    .replace(/__META_IMAGE__/g, escapeHtml(imageUrl))
    .replace(/__META_URL__/g, escapeHtml(url))
    .replace(/__META_OG_TYPE__/g, ogType)
    .replace(/__STRUCTURED_DATA__/g, structuredData);
}

/**
 * Default meta tags for static pages
 * These match your EXACT current SEO setup from index.html
 */
const STATIC_PAGE_META: Record<string, {
  title: string;
  description: string;
  keywords: string;
}> = {
  '/': {
    title: 'Codewrinkles – Built for value, not engagement',
    description: 'An ecosystem designed to create genuine connections and meaningful content. Your posts reach your followers. Guaranteed.',
    keywords: 'social media, microblogging, pulse, content creation, genuine connections',
  },
  '/privacy': {
    title: 'Privacy Policy - Codewrinkles',
    description: 'Learn how Codewrinkles collects, uses, and protects your personal information.',
    keywords: 'privacy policy, data protection, user privacy, codewrinkles',
  },
  '/terms': {
    title: 'Terms of Service - Codewrinkles',
    description: 'Read our terms of service and user agreement.',
    keywords: 'terms of service, user agreement, legal, codewrinkles',
  },
  // TODO: Add more static routes as needed
};

/**
 * Main edge function handler
 */
export default async function handler(request: Request) {
  const url = new URL(request.url!);
  const userAgent = request.headers.get('user-agent') || '';
  const pathname = url.pathname;

  // Prevent infinite loop: if this request came from ourselves, just serve the static file
  if (request.headers.get('x-middleware-bypass') === 'true') {
    return fetch(request);
  }

  // Skip static assets (performance optimization)
  if (
    pathname.startsWith('/assets/') ||
    pathname.startsWith('/src/') ||
    pathname.match(/\.(js|css|png|jpg|jpeg|gif|svg|ico|woff|woff2|ttf|eot|webp)$/)
  ) {
    return fetch(request);
  }

  // Fetch the original HTML with bypass header to prevent loop
  const modifiedRequest = new Request(request.url, {
    headers: {
      ...Object.fromEntries(request.headers.entries()),
      'x-middleware-bypass': 'true',
    },
  });
  const response = await fetch(modifiedRequest);
  let html = await response.text();

  // Log bot detection
  const isBotRequest = isBot(userAgent);
  if (isBotRequest) {
    console.log(`Bot detected: ${userAgent} requesting ${pathname}`);
  }

  try {
    // Route: /pulse/{id} - Pulse detail page (DYNAMIC)
    const pulseMatch = pathname.match(/^\/pulse\/([a-f0-9-]{36})$/i);
    if (pulseMatch && isBotRequest) {
      const pulseId = pulseMatch[1];
      const pulse = await fetchPulse(pulseId);

      if (pulse) {
        // Extract data for meta tags
        const title = `${pulse.author.name} on Pulse`;
        const description = pulse.content.length > 200
          ? pulse.content.substring(0, 200) + '...'
          : pulse.content;
        const imageUrl = ensureAbsoluteUrl(
          pulse.imageUrl,
          `${SITE_URL}/og-image.png`
        );
        const pageUrl = `${SITE_URL}/pulse/${pulse.id}`;
        const keywords = 'pulse, social media post, microblogging, content creation';
        const structuredData = generateStructuredData('article', {
          title,
          description,
          url: pageUrl,
          imageUrl,
          authorName: pulse.author.name,
          publishedDate: pulse.createdAt,
        });

        html = injectMetaTags(
          html,
          title,
          description,
          imageUrl,
          pageUrl,
          keywords,
          pulse.author.name, // author
          'article',
          structuredData
        );
        return new Response(html, {
          status: response.status,
          headers: {
            'Content-Type': 'text/html; charset=utf-8',
            'Cache-Control': 'public, s-maxage=3600, stale-while-revalidate=86400',
          },
        });
      }
    }

    // Route: /pulse/u/{handle} - Profile page (DYNAMIC)
    const profileMatch = pathname.match(/^\/pulse\/u\/([a-zA-Z0-9_-]+)$/);
    if (profileMatch && isBotRequest) {
      const handle = profileMatch[1];
      const profile = await fetchProfile(handle);

      if (profile) {
        // Extract data for meta tags
        const title = `${profile.name} (@${profile.handle})`;
        const description = profile.bio || `${profile.name} on Codewrinkles Pulse`;
        const imageUrl = ensureAbsoluteUrl(
          profile.avatarUrl,
          `${SITE_URL}/default-avatar.png`
        );
        const pageUrl = `${SITE_URL}/pulse/u/${profile.handle}`;
        const keywords = `${profile.handle}, profile, user, codewrinkles, pulse`;
        const structuredData = generateStructuredData('profile', {
          title,
          description,
          url: pageUrl,
          imageUrl,
        });

        html = injectMetaTags(
          html,
          title,
          description,
          imageUrl,
          pageUrl,
          keywords,
          profile.name, // author
          'profile',
          structuredData
        );
        return new Response(html, {
          status: response.status,
          headers: {
            'Content-Type': 'text/html; charset=utf-8',
            'Cache-Control': 'public, s-maxage=3600, stale-while-revalidate=86400',
          },
        });
      }
    }

    // Route: /pulse - Feed page (STATIC)
    if (pathname === '/pulse' || pathname === '/pulse/') {
      const title = 'Pulse • Codewrinkles';
      const description = 'See what\'s happening on Pulse. Your posts reach your followers. Guaranteed.';
      const keywords = 'pulse, social media, feed, microblogging, posts';
      const pageUrl = `${SITE_URL}/pulse`;
      const structuredData = generateStructuredData('website', {
        title,
        description,
        url: pageUrl,
      });

      html = injectMetaTags(
        html,
        title,
        description,
        `${SITE_URL}/og-image.png`,
        pageUrl,
        keywords,
        'Codewrinkles',
        'website',
        structuredData
      );
      return new Response(html, {
        status: response.status,
        headers: {
          'Content-Type': 'text/html; charset=utf-8',
          'Cache-Control': 'public, s-maxage=3600, stale-while-revalidate=86400',
        },
      });
    }

    // Handle other static pages (/, /privacy, /terms, etc.)
    const staticMeta = STATIC_PAGE_META[pathname];
    if (staticMeta) {
      const pageUrl = pathname === '/' ? 'https://codewrinkles.com/' : `${SITE_URL}${pathname}`;
      const structuredData = generateStructuredData('website', {
        title: staticMeta.title,
        description: staticMeta.description,
        url: pageUrl,
      });

      html = injectMetaTags(
        html,
        staticMeta.title,
        staticMeta.description,
        `${SITE_URL}/og-image.png`,
        pageUrl,
        staticMeta.keywords,
        'Codewrinkles',
        'website',
        structuredData
      );
      return new Response(html, {
        status: response.status,
        headers: {
          'Content-Type': 'text/html; charset=utf-8',
          'Cache-Control': 'public, s-maxage=86400, stale-while-revalidate=604800',
        },
      });
    }

    // Fallback: Inject generic meta tags for any other route
    // This ensures bots never see placeholders
    const fallbackTitle = 'Codewrinkles – Built for value, not engagement';
    const fallbackDesc = 'An ecosystem designed to create genuine connections and meaningful content.';
    const fallbackKeywords = 'social media, microblogging, pulse, content creation, genuine connections';
    const structuredData = generateStructuredData('website', {
      title: fallbackTitle,
      description: fallbackDesc,
      url: url.toString(),
    });

    html = injectMetaTags(
      html,
      fallbackTitle,
      fallbackDesc,
      `${SITE_URL}/og-image.png`,
      url.toString(),
      fallbackKeywords,
      'Codewrinkles',
      'website',
      structuredData
    );
  } catch (error) {
    console.error('Error injecting meta tags:', error);
    // Even on error, inject basic fallback to avoid showing placeholders
    const fallbackTitle = 'Codewrinkles – Built for value, not engagement';
    const fallbackDesc = 'An ecosystem designed to create genuine connections and meaningful content.';
    const fallbackKeywords = 'social media, microblogging, pulse, content creation, genuine connections';
    const structuredData = generateStructuredData('website', {
      title: fallbackTitle,
      description: fallbackDesc,
      url: url.toString(),
    });

    html = injectMetaTags(
      html,
      fallbackTitle,
      fallbackDesc,
      `${SITE_URL}/og-image.png`,
      url.toString(),
      fallbackKeywords,
      'Codewrinkles',
      'website',
      structuredData
    );
  }

  return new Response(html, {
    status: response.status,
    headers: {
      'Content-Type': 'text/html; charset=utf-8',
      'Cache-Control': 'public, s-maxage=3600, stale-while-revalidate=86400',
    },
  });
}
