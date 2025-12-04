import { useState, useEffect, useRef } from "react";
import { useParams, useNavigate, useSearchParams } from "react-router";
import { Helmet } from "react-helmet-async";
import { useThread } from "./hooks/useThread";
import { PostCard } from "./PostCard";
import { UnifiedComposer } from "./UnifiedComposer";
import { PulseNavigation } from "./PulseNavigation";
import { PulseRightSidebar } from "./PulseRightSidebar";
import { LoadingCard, Spinner } from "../../components/ui";
import { useAuth } from "../../hooks/useAuth";
import {
  truncateText,
  getFullUrl,
  generatePulseStructuredData,
  getDefaultOGImage,
} from "../../utils/seo";

export function ThreadView(): JSX.Element {
  const { pulseId } = useParams<{ pulseId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [searchParams] = useSearchParams();
  const highlightedReplyId = searchParams.get("highlight");
  const [replyingToPulseId, setReplyingToPulseId] = useState<string | null>(null);
  const highlightedReplyRef = useRef<HTMLDivElement>(null);

  if (!pulseId) {
    navigate("/pulse");
    return <></>;
  }

  const {
    parentPulse,
    replies,
    totalReplyCount,
    isLoading,
    error,
    hasMore,
    loadMore,
    refetch,
  } = useThread(pulseId);

  const handleReplyClick = (pulseId: string): void => {
    setReplyingToPulseId(pulseId);
  };

  const handleReplyCreated = (): void => {
    setReplyingToPulseId(null);
    refetch();
  };

  const handleDelete = (pulseId: string): void => {
    // If the deleted pulse is the parent, navigate back to feed
    if (pulseId === parentPulse?.id) {
      navigate("/pulse");
    } else {
      // Otherwise, refetch to update the thread
      refetch();
    }
  };

  // Auto-scroll to highlighted reply when it renders
  useEffect(() => {
    if (highlightedReplyId && highlightedReplyRef.current && !isLoading) {
      // Small delay to ensure DOM is fully rendered
      setTimeout(() => {
        highlightedReplyRef.current?.scrollIntoView({
          behavior: "smooth",
          block: "center",
        });
      }, 100);
    }
  }, [highlightedReplyId, replies, isLoading]);

  // Generate SEO metadata from parentPulse
  const title = parentPulse
    ? `${parentPulse.author.name} (@${parentPulse.author.handle}) on Codewrinkles`
    : "Thread • Codewrinkles";
  const description = parentPulse
    ? truncateText(parentPulse.content, 160)
    : "See the conversation on Codewrinkles";
  const url = getFullUrl(`/pulse/${pulseId}`);
  const ogImage =
    parentPulse?.imageUrl ||
    parentPulse?.author.avatarUrl ||
    getDefaultOGImage();

  return (
    <>
      <Helmet>
        {/* Primary Meta Tags */}
        <title>{title}</title>
        <meta name="title" content={title} />
        <meta name="description" content={description} />

        {/* Open Graph / Facebook */}
        <meta property="og:type" content="article" />
        <meta property="og:url" content={url} />
        <meta property="og:title" content={title} />
        <meta property="og:description" content={description} />
        <meta property="og:image" content={ogImage} />
        {parentPulse && (
          <>
            <meta property="article:published_time" content={parentPulse.createdAt} />
            <meta property="article:author" content={parentPulse.author.name} />
          </>
        )}

        {/* Twitter */}
        <meta
          name="twitter:card"
          content={parentPulse?.imageUrl ? "summary_large_image" : "summary"}
        />
        <meta name="twitter:url" content={url} />
        <meta name="twitter:title" content={title} />
        <meta name="twitter:description" content={description} />
        <meta name="twitter:image" content={ogImage} />

        {/* Canonical URL */}
        <link rel="canonical" href={url} />

        {/* Structured Data */}
        {parentPulse && (
          <script type="application/ld+json">
            {generatePulseStructuredData(parentPulse)}
          </script>
        )}
      </Helmet>

      <div className="flex justify-center">
      {/* Left Navigation - only show if authenticated */}
      {user && (
        <aside className="hidden lg:flex w-[320px] flex-shrink-0 justify-end pr-8">
          <div className="w-[240px]">
            <PulseNavigation />
          </div>
        </aside>
      )}

      {/* Main Content */}
      <main className="w-full max-w-[600px] border-x border-border lg:w-[600px]">
        {/* Header */}
        <div className="sticky top-0 z-10 flex items-center gap-4 border-b border-border bg-surface-page/80 backdrop-blur px-4 py-3">
          <button
            type="button"
            onClick={() => navigate(-1)}
            className="flex h-8 w-8 items-center justify-center rounded-full hover:bg-surface-card1 transition-colors text-text-primary"
            title="Go back"
          >
            ←
          </button>
          <h1 className="text-base font-semibold tracking-tight text-text-primary">
            Thread
          </h1>
        </div>

        {/* Error State */}
        {error && (
          <div className="p-4 border-b border-border bg-red-500/10 text-red-400 text-sm">
            {error}
          </div>
        )}

        {/* Loading State */}
        {isLoading && !parentPulse ? (
          <div>
            <LoadingCard />
            <div className="border-b border-border px-4 py-3 bg-surface-card1/20">
              <div className="h-4 w-20 bg-surface-card2 rounded animate-pulse" />
            </div>
            {Array.from({ length: 2 }).map((_, i) => (
              <LoadingCard key={i} />
            ))}
          </div>
        ) : parentPulse ? (
          <>
            {/* Parent Pulse */}
            <div className="border-b border-border">
              <PostCard
                post={parentPulse}
                onReplyClick={handleReplyClick}
                onDelete={handleDelete}
              />
            </div>

            {/* Inline Reply Composer for Parent Pulse */}
            {replyingToPulseId === parentPulse.id && (
              <div className="border-b border-border p-4 bg-surface-card1/30">
                <UnifiedComposer
                  mode="reply"
                  parentPulseId={parentPulse.id}
                  onSuccess={handleReplyCreated}
                  placeholder="Post your reply"
                  rows={2}
                  focusedRows={4}
                />
              </div>
            )}

            {/* Replies Section */}
            {totalReplyCount > 0 && (
              <div className="border-b border-border px-4 py-3 bg-surface-card1/20">
                <h2 className="text-sm font-semibold text-text-secondary">
                  {totalReplyCount} {totalReplyCount === 1 ? "Reply" : "Replies"}
                </h2>
              </div>
            )}

            {/* Replies List */}
            {replies.length > 0 ? (
              <>
                <div className="divide-y divide-border">
                  {replies.map((reply) => {
                    const isHighlighted = highlightedReplyId === reply.id;
                    return (
                      <div
                        key={reply.id}
                        ref={isHighlighted ? highlightedReplyRef : null}
                        className={isHighlighted ? "bg-brand-soft/5" : ""}
                      >
                        <PostCard
                          post={reply}
                          onReplyClick={handleReplyClick}
                          onDelete={handleDelete}
                        />
                        {/* Inline Reply Composer for this Reply */}
                        {replyingToPulseId === reply.id && (
                          <div className="p-4 bg-surface-card1/30 border-t border-border">
                            <UnifiedComposer
                              mode="reply"
                              parentPulseId={reply.id}
                              onSuccess={handleReplyCreated}
                              placeholder="Post your reply"
                              rows={2}
                              focusedRows={4}
                            />
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>

                {/* Load More Button */}
                {hasMore && (
                  <div className="p-4 border-t border-border text-center">
                    <button
                      type="button"
                      onClick={loadMore}
                      disabled={isLoading}
                      className="px-4 py-2 rounded-full bg-surface-card2 hover:bg-surface-card1 text-brand-soft text-sm font-semibold transition-colors disabled:opacity-50 flex items-center justify-center gap-2 mx-auto"
                    >
                      {isLoading && <Spinner size="sm" />}
                      {isLoading ? "Loading..." : "Load More Replies"}
                    </button>
                  </div>
                )}
              </>
            ) : totalReplyCount === 0 ? (
              <div className="p-8 text-center text-text-secondary">
                No replies yet. Be the first to reply!
              </div>
            ) : null}
          </>
        ) : null}
      </main>

      {/* Right Sidebar - only show if authenticated */}
      {user && (
        <>
          {/* Right Sidebar - placeholder for spacing */}
          <aside className="hidden lg:block w-[320px] flex-shrink-0 pl-8">
            {/* Empty placeholder */}
          </aside>

          {/* Right Sidebar - fixed position */}
          <div className="hidden lg:block fixed top-20 z-10 w-[288px] left-[calc(50%+332px)]">
            <PulseRightSidebar />
          </div>
        </>
      )}
      </div>
    </>
  );
}
