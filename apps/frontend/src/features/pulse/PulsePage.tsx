import { useState } from "react";
import type { Post, PostAuthor } from "../../types";
import { Composer } from "./Composer";
import { Feed } from "./Feed";
import { PulseNavigation } from "./PulseNavigation";
import { PulseRightSidebar } from "./PulseRightSidebar";

// Mock author for demo
const MOCK_AUTHOR: PostAuthor = {
  id: "1",
  name: "Daniel @ Codewrinkles",
  handle: "codewrinkles",
};

const MOCK_POSTS: Post[] = [
  // Text only post
  {
    id: 1,
    author: MOCK_AUTHOR,
    timeAgo: "2h",
    content: "Working on the Codewrinkles multi-app shell. One account, multiple worlds. 🚀\n\nThe idea is simple: your identity follows you across Social, Learn, and Nova.",
    replyCount: 12,
    repostCount: 5,
    likeCount: 89,
    viewCount: 1420,
  },
  // Post with single image
  {
    id: 2,
    author: MOCK_AUTHOR,
    timeAgo: "5h",
    content: "A visual I'm using to explain vertical slices. This diagram changed how I think about feature organization.",
    images: [
      {
        url: "https://images.unsplash.com/photo-1555949963-aa79dcee981c?w=800&h=450&fit=crop",
        alt: "Architecture diagram",
      },
    ],
    replyCount: 24,
    repostCount: 18,
    likeCount: 156,
    viewCount: 3200,
  },
  // Post with multiple images (2)
  {
    id: 3,
    author: {
      id: "2",
      name: "Sarah Chen",
      handle: "sarahcodes",
    },
    timeAgo: "8h",
    content: "Before and after refactoring to clean architecture. The difference is night and day.",
    images: [
      {
        url: "https://images.unsplash.com/photo-1461749280684-dccba630e2f6?w=600&h=400&fit=crop",
        alt: "Before refactoring",
      },
      {
        url: "https://images.unsplash.com/photo-1498050108023-c5249f4df085?w=600&h=400&fit=crop",
        alt: "After refactoring",
      },
    ],
    replyCount: 8,
    repostCount: 3,
    likeCount: 67,
  },
  // Post with video
  {
    id: 4,
    author: MOCK_AUTHOR,
    timeAgo: "1d",
    content: "New video: Building agentic AI features in a legacy SaaS. Full walkthrough from zero to deployed.",
    video: {
      url: "https://youtube.com/watch?v=example",
      thumbnailUrl: "https://images.unsplash.com/photo-1633356122544-f134324a6cee?w=800&h=450&fit=crop",
      duration: "24:35",
    },
    replyCount: 45,
    repostCount: 32,
    likeCount: 289,
    viewCount: 8500,
  },
  // Post with link preview
  {
    id: 5,
    author: {
      id: "3",
      name: "AI Weekly",
      handle: "aiweekly",
    },
    timeAgo: "1d",
    content: "Great article on RAG implementation patterns. Worth a read if you're building AI-powered search.",
    linkPreview: {
      url: "https://example.com/rag-patterns",
      title: "The Complete Guide to RAG Architecture Patterns",
      description: "Learn how to build production-ready retrieval augmented generation systems with practical examples and best practices.",
      imageUrl: "https://images.unsplash.com/photo-1677442136019-21780ecad995?w=800&h=400&fit=crop",
      domain: "example.com",
    },
    replyCount: 18,
    repostCount: 24,
    likeCount: 142,
  },
  // Repost (quote tweet style) - text only
  {
    id: 6,
    author: MOCK_AUTHOR,
    timeAgo: "2d",
    content: "This. Users don't care about your architecture. They care if the app feels good.",
    repostedPost: {
      id: 100,
      author: {
        id: "4",
        name: "Dev Wisdom",
        handle: "devwisdom",
      },
      content: "The best code is code that solves real problems. Not code that demonstrates clever patterns.",
      timeAgo: "3d",
    },
    replyCount: 31,
    repostCount: 89,
    likeCount: 412,
    viewCount: 12000,
  },
  // Repost of an image post
  {
    id: 8,
    author: {
      id: "5",
      name: "Code Aesthetics",
      handle: "codeaesthetics",
    },
    timeAgo: "2d",
    content: "When your terminal setup finally looks as good as your code runs 🔥",
    repostedPost: {
      id: 101,
      author: {
        id: "6",
        name: "Terminal Pro",
        handle: "terminalpro",
      },
      content: "My new dev setup. Took me a whole weekend to configure but totally worth it.",
      timeAgo: "4d",
      images: [
        {
          url: "https://images.unsplash.com/photo-1629654297299-c8506221ca97?w=600&h=400&fit=crop",
          alt: "Terminal setup",
        },
        {
          url: "https://images.unsplash.com/photo-1542831371-29b0f74f9713?w=600&h=400&fit=crop",
          alt: "Code editor",
        },
      ],
    },
    replyCount: 45,
    repostCount: 67,
    likeCount: 523,
    viewCount: 15000,
  },
  // Repost of a URL preview post
  {
    id: 9,
    author: MOCK_AUTHOR,
    timeAgo: "3d",
    content: "This is a must-read for anyone building AI features. Saved me hours of debugging.",
    repostedPost: {
      id: 102,
      author: {
        id: "3",
        name: "AI Weekly",
        handle: "aiweekly",
      },
      content: "New deep dive on prompt engineering best practices for production apps.",
      timeAgo: "5d",
      linkPreview: {
        url: "https://example.com/prompt-engineering",
        title: "Prompt Engineering Best Practices for Production",
        description: "A comprehensive guide to writing effective prompts.",
        imageUrl: "https://images.unsplash.com/photo-1684487747720-1ba29cda82f8?w=400&h=200&fit=crop",
        domain: "example.com",
      },
    },
    replyCount: 28,
    repostCount: 56,
    likeCount: 312,
    viewCount: 8900,
  },
  // Repost of a video post
  {
    id: 10,
    author: {
      id: "7",
      name: "Learning Dev",
      handle: "learningdev",
    },
    timeAgo: "3d",
    content: "Finally someone explains microservices without making it overcomplicated 👏",
    repostedPost: {
      id: 103,
      author: MOCK_AUTHOR,
      content: "Quick explainer: When to use microservices vs monolith.",
      timeAgo: "6d",
      video: {
        url: "https://youtube.com/watch?v=example2",
        thumbnailUrl: "https://images.unsplash.com/photo-1518770660439-4636190af475?w=800&h=450&fit=crop",
        duration: "8:42",
      },
    },
    replyCount: 89,
    repostCount: 134,
    likeCount: 756,
    viewCount: 23000,
  },
  // Thread post
  {
    id: 7,
    author: MOCK_AUTHOR,
    timeAgo: "3d",
    content: "🧵 A thread on how I structure my .NET solutions for maximum maintainability.\n\nLet me share what I've learned after 15+ years of enterprise development...",
    isThread: true,
    threadLength: 8,
    replyCount: 67,
    repostCount: 45,
    likeCount: 234,
    viewCount: 5600,
  },
];

export function PulsePage(): JSX.Element {
  const [composerValue, setComposerValue] = useState("");
  const maxChars = 280;
  const charsLeft = maxChars - composerValue.length;
  const isOverLimit = charsLeft < 0;

  return (
    <div className="flex justify-center">
      {/* Left Navigation */}
      <aside className="hidden md:flex md:w-[88px] xl:w-[275px] flex-shrink-0 justify-end pr-2 xl:pr-8">
        <div className="w-[68px] xl:w-[240px]">
          <PulseNavigation />
        </div>
      </aside>

      {/* Main Content */}
      <main className="w-full max-w-[600px] border-x border-border">
        {/* Composer */}
        <div className="border-b border-border p-4">
          <Composer
            value={composerValue}
            onChange={setComposerValue}
            maxChars={maxChars}
            isOverLimit={isOverLimit}
            charsLeft={charsLeft}
          />
        </div>

        {/* Feed */}
        <div className="divide-y divide-border">
          <Feed posts={MOCK_POSTS} />
        </div>
      </main>

      {/* Right Sidebar */}
      <aside className="hidden lg:block w-[350px] flex-shrink-0 pl-8">
        <div className="w-[320px]">
          <PulseRightSidebar />
        </div>
      </aside>
    </div>
  );
}
