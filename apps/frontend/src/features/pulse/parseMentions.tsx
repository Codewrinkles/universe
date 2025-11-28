import type { Mention } from "../../types";

export interface ParsedContent {
  type: "text" | "mention";
  content: string;
  handle?: string;
}

export function parseMentions(content: string, mentions: Mention[]): ParsedContent[] {
  const mentionHandles = new Set(mentions.map((m) => m.handle.toLowerCase()));
  const parts: ParsedContent[] = [];

  // Regex to match @handle (alphanumeric + underscore, 3-30 chars)
  const mentionRegex = /@(\w{3,30})/g;
  let lastIndex = 0;
  let match: RegExpExecArray | null;

  while ((match = mentionRegex.exec(content)) !== null) {
    if (!match[1]) continue;
    const handle = match[1].toLowerCase();

    // Only treat as mention if it exists in the mentions array
    if (mentionHandles.has(handle)) {
      // Add text before mention
      if (match.index > lastIndex) {
        parts.push({
          type: "text",
          content: content.slice(lastIndex, match.index),
        });
      }

      // Add mention
      parts.push({
        type: "mention",
        content: `@${match[1]}`,
        handle: match[1],
      });

      lastIndex = match.index + match[0].length;
    }
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
