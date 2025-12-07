import type { Mention } from "../../types";

export interface ParsedContent {
  type: "text" | "mention" | "hashtag" | "url";
  content: string;
  handle?: string;
  tag?: string;
  url?: string;
}

export function parseMentions(content: string, mentions: Mention[]): ParsedContent[] {
  const mentionHandles = new Set(mentions.map((m) => m.handle.toLowerCase()));
  const parts: ParsedContent[] = [];

  // Combined regex to match @mentions, #hashtags, and URLs
  // Matches @handle (3-30 chars) or #hashtag (2-100 chars) or http(s)://url
  // URL pattern matches liberally, we'll clean trailing punctuation after
  const combinedRegex = /(@\w{3,30})|(#\w{2,100})|(https?:\/\/[^\s]+)/gi;
  let lastIndex = 0;
  let match: RegExpExecArray | null;

  while ((match = combinedRegex.exec(content)) !== null) {
    // Add text before match
    if (match.index > lastIndex) {
      parts.push({
        type: "text",
        content: content.slice(lastIndex, match.index),
      });
    }

    if (match[1]) {
      // This is a mention (@handle)
      const handle = match[1].slice(1).toLowerCase(); // Remove @ prefix

      // Only treat as clickable mention if it exists in the mentions array
      if (mentionHandles.has(handle)) {
        parts.push({
          type: "mention",
          content: match[1],
          handle: handle,
        });
      } else {
        // Not a valid mention, treat as text
        parts.push({
          type: "text",
          content: match[1],
        });
      }
    } else if (match[2]) {
      // This is a hashtag (#tag)
      const tag = match[2].slice(1); // Remove # prefix
      parts.push({
        type: "hashtag",
        content: match[2],
        tag: tag,
      });
    } else if (match[3]) {
      // This is a URL (http:// or https://)
      // Clean trailing sentence punctuation that's not part of the URL
      let url = match[3];

      // Remove common sentence-ending punctuation from the end
      // Examples: "Check this out https://example.com." → "https://example.com"
      // But preserve URL-specific punctuation like query params: "?foo=bar"
      // Only trim single trailing . , ; : ! ? characters
      url = url.replace(/[.,;:!?]+$/, '');

      parts.push({
        type: "url",
        content: url,
        url: url,
      });
    }

    lastIndex = match.index + match[0].length;
  }

  // Add remaining text
  if (lastIndex < content.length) {
    parts.push({
      type: "text",
      content: content.slice(lastIndex),
    });
  }

  return parts.length > 0 ? parts : [{ type: "text", content }];
}
