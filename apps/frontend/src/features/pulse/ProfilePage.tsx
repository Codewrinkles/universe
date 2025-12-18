import { useState, useEffect } from "react";
import { useParams, useNavigate, Link } from "react-router";
import { Helmet } from "react-helmet-async";
import { useAuth } from "../../hooks/useAuth";
import { useInfiniteScroll } from "../../hooks/useInfiniteScroll";
import { config } from "../../config";
import { buildAvatarUrl } from "../../utils/avatarUtils";
import { PulseThreeColumnLayout } from "./PulseThreeColumnLayout";
import { PostCard } from "./PostCard";
import { UnifiedComposer } from "./UnifiedComposer";
import { useProfilePulses } from "./hooks/useProfilePulses";
import type { User, FollowerDto, FollowingDto } from "../../types";
import { LoadingProfile, LoadingCard, LoadingUserItem, Skeleton, Spinner, ScrollToTopButton } from "../../components/ui";
import {
  generateProfileStructuredData,
  getFullUrl,
  getDefaultOGImage,
} from "../../utils/seo";
import { FollowButton } from "../social/components/FollowButton";

interface ProfileHeaderProps {
  profile: User;
  isOwnProfile: boolean;
  followersCount: number;
  followingCount: number;
  pulsesCount: number;
  isLoadingCounts?: boolean;
}

function ProfileHeader({ profile, isOwnProfile, followersCount, followingCount, pulsesCount, isLoadingCounts = false }: ProfileHeaderProps): JSX.Element {
  const avatarUrl = buildAvatarUrl(profile.avatarUrl);

  return (
    <div className="border-b border-border bg-surface-card1 p-4">
      <div className="flex gap-4">
        {/* Avatar */}
        <div className="h-20 w-20 rounded-full overflow-hidden border-2 border-border flex-shrink-0">
          {avatarUrl ? (
            <img
              src={avatarUrl}
              alt={profile.name}
              className="h-full w-full object-cover"
            />
          ) : (
            <div className="h-full w-full bg-surface-card2 flex items-center justify-center">
              <span className="text-2xl text-text-tertiary">
                {profile.name.charAt(0).toUpperCase()}
              </span>
            </div>
          )}
        </div>

        {/* Profile Info */}
        <div className="flex-1 min-w-0">
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0">
              <h1 className="text-base font-semibold tracking-tight text-text-primary truncate">
                {profile.name}
              </h1>
              {profile.handle && (
                <p className="text-sm text-text-secondary">@{profile.handle}</p>
              )}
            </div>
            {isOwnProfile ? (
              <Link
                to="/settings/profile"
                className="rounded-full border border-border px-4 py-1.5 text-xs font-medium text-text-primary hover:bg-surface-card2 transition-colors flex-shrink-0"
              >
                Edit profile
              </Link>
            ) : (
              <FollowButton profileId={profile.profileId} size="md" />
            )}
          </div>

          {profile.bio && (
            <p className="mt-2 text-sm text-text-primary whitespace-pre-wrap break-words">
              {profile.bio}
            </p>
          )}

          {(profile.location || profile.websiteUrl) && (
            <div className="mt-2 flex flex-wrap gap-3 text-xs text-text-secondary">
              {profile.location && (
                <div className="flex items-center gap-1">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                  </svg>
                  <span>{profile.location}</span>
                </div>
              )}
              {profile.websiteUrl && (
                <a
                  href={profile.websiteUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex items-center gap-1 hover:text-brand-soft transition-colors"
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" />
                  </svg>
                  <span className="truncate max-w-xs">{profile.websiteUrl.replace(/^https?:\/\//, '')}</span>
                </a>
              )}
            </div>
          )}

          {/* Stats */}
          <div className="mt-3 flex gap-4 text-sm">
            {isLoadingCounts ? (
              <>
                <Skeleton variant="text" className="h-5 w-20" />
                <Skeleton variant="text" className="h-5 w-24" />
                <Skeleton variant="text" className="h-5 w-24" />
              </>
            ) : (
              <>
                <div>
                  <span className="font-semibold text-text-primary">{pulsesCount}</span>{" "}
                  <span className="text-text-secondary">Pulses</span>
                </div>
                <div>
                  <span className="font-semibold text-text-primary">{followingCount}</span>{" "}
                  <span className="text-text-secondary">Following</span>
                </div>
                <div>
                  <span className="font-semibold text-text-primary">{followersCount}</span>{" "}
                  <span className="text-text-secondary">Followers</span>
                </div>
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

type Tab = "pulses" | "following" | "followers";

export function ProfilePage(): JSX.Element {
  const { handle } = useParams<{ handle: string }>();
  const navigate = useNavigate();
  const { user: currentUser } = useAuth();

  const [activeTab, setActiveTab] = useState<Tab>("pulses");
  const [profile, setProfile] = useState<User | null>(null);
  const [followers, setFollowers] = useState<FollowerDto[]>([]);
  const [totalFollowersCount, setTotalFollowersCount] = useState<number>(0);
  const [following, setFollowing] = useState<FollowingDto[]>([]);
  const [totalFollowingCount, setTotalFollowingCount] = useState<number>(0);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingFollowers, setIsLoadingFollowers] = useState(false);
  const [isLoadingFollowing, setIsLoadingFollowing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [replyingToPulseId, setReplyingToPulseId] = useState<string | null>(null);

  // Fetch pulses with pagination
  const {
    pulses,
    totalCount: totalPulsesCount,
    isLoading: isLoadingPulses,
    hasMore: hasMorePulses,
    loadMore: loadMorePulses,
    refetch: refetchPulses,
    removePulse,
  } = useProfilePulses(profile?.profileId ?? null);

  // Infinite scroll for pulses
  const { sentinelRef: pulsesSentinelRef } = useInfiniteScroll({
    hasMore: hasMorePulses,
    isLoading: isLoadingPulses,
    onLoadMore: loadMorePulses,
  });

  // Fetch profile by handle
  useEffect(() => {
    const fetchProfile = async (): Promise<void> => {
      if (!handle) {
        setError("Profile handle is required");
        setIsLoading(false);
        return;
      }

      setIsLoading(true);
      setError(null);

      try {
        const token = localStorage.getItem(config.auth.accessTokenKey);
        const response = await fetch(`${config.api.baseUrl}/api/profile/handle/${handle}`, {
          headers: {
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
          },
        });

        if (!response.ok) {
          if (response.status === 404) {
            setError("Profile not found");
          } else {
            setError("Failed to load profile");
          }
          setIsLoading(false);
          return;
        }

        const data = await response.json();
        setProfile(data);
      } catch (err) {
        setError("Failed to load profile");
        console.error("Error fetching profile:", err);
      } finally {
        setIsLoading(false);
      }
    };

    fetchProfile();
  }, [handle]);

  // Fetch followers and following when profile is loaded
  // (Pulses are fetched via useProfilePulses hook)
  useEffect(() => {
    if (!profile) return;

    const fetchFollowers = async (): Promise<void> => {
      setIsLoadingFollowers(true);
      try {
        const token = localStorage.getItem(config.auth.accessTokenKey);
        if (!token) {
          setIsLoadingFollowers(false);
          return;
        }

        const url = new URL(config.api.endpoints.socialFollowers(profile.profileId));
        url.searchParams.set('limit', '50');

        const response = await fetch(url.toString(), {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        });

        if (response.ok) {
          const data = await response.json();
          setFollowers(data.followers || []);
          setTotalFollowersCount(data.totalCount ?? 0);
        }
      } catch (err) {
        console.error("Error fetching followers:", err);
      } finally {
        setIsLoadingFollowers(false);
      }
    };

    const fetchFollowing = async (): Promise<void> => {
      setIsLoadingFollowing(true);
      try {
        const token = localStorage.getItem(config.auth.accessTokenKey);
        if (!token) {
          setIsLoadingFollowing(false);
          return;
        }

        const url = new URL(config.api.endpoints.socialFollowing(profile.profileId));
        url.searchParams.set('limit', '50');

        const response = await fetch(url.toString(), {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        });

        if (response.ok) {
          const data = await response.json();
          setFollowing(data.following || []);
          setTotalFollowingCount(data.totalCount ?? 0);
        }
      } catch (err) {
        console.error("Error fetching following:", err);
      } finally {
        setIsLoadingFollowing(false);
      }
    };

    // Execute fetches in parallel
    Promise.all([
      fetchFollowers(),
      fetchFollowing()
    ]);
  }, [profile]);

  const handleReplyClick = (pulseId: string): void => {
    setReplyingToPulseId(pulseId);
  };

  const handleReplyCreated = (): void => {
    setReplyingToPulseId(null);
    // Refetch pulses to update reply counts
    refetchPulses();
  };

  const handleDelete = (pulseId: string): void => {
    // Remove the pulse from the local state
    removePulse(pulseId);
  };

  const handleEdit = (): void => {
    // Refetch pulses to show updated content
    refetchPulses();
  };

  const isOwnProfile = currentUser?.profileId === profile?.profileId;

  if (isLoading) {
    return (
      <PulseThreeColumnLayout>
        <LoadingProfile />
      </PulseThreeColumnLayout>
    );
  }

  if (error || !profile) {
    return (
      <div className="flex flex-col items-center justify-center min-h-screen bg-surface-page gap-3">
        <div className="text-sm text-text-secondary">{error || "Profile not found"}</div>
        <button
          onClick={() => navigate("/social")}
          className="text-xs text-brand-soft hover:text-brand transition-colors"
        >
          Back to feed
        </button>
      </div>
    );
  }

  // Generate SEO metadata from profile
  const profileHandle = profile.handle || "user";
  const title = `${profile.name} (@${profileHandle}) • Codewrinkles`;
  const description =
    profile.bio || `See pulses from ${profile.name} on Codewrinkles`;
  const url = getFullUrl(`/pulse/u/${profileHandle}`);
  const ogImage = buildAvatarUrl(profile.avatarUrl) || getDefaultOGImage();

  return (
    <>
      <Helmet>
        {/* Primary Meta Tags */}
        <title>{title}</title>
        <meta name="title" content={title} />
        <meta name="description" content={description} />

        {/* Open Graph / Facebook */}
        <meta property="og:type" content="profile" />
        <meta property="og:url" content={url} />
        <meta property="og:title" content={title} />
        <meta property="og:description" content={description} />
        <meta property="og:image" content={ogImage} />
        <meta property="profile:username" content={profileHandle} />

        {/* Twitter */}
        <meta name="twitter:card" content="summary" />
        <meta name="twitter:url" content={url} />
        <meta name="twitter:title" content={title} />
        <meta name="twitter:description" content={description} />
        <meta name="twitter:image" content={ogImage} />

        {/* Canonical URL */}
        <link rel="canonical" href={url} />

        {/* Structured Data */}
        <script type="application/ld+json">
          {generateProfileStructuredData(profile)}
        </script>
      </Helmet>

      <PulseThreeColumnLayout>
        <ProfileHeader
          profile={profile}
          isOwnProfile={isOwnProfile}
          followersCount={totalFollowersCount}
          followingCount={totalFollowingCount}
          pulsesCount={totalPulsesCount}
          isLoadingCounts={isLoadingPulses || isLoadingFollowers || isLoadingFollowing}
        />

        {/* Tabs */}
        <div className="border-b border-border bg-surface-card1">
          <div className="flex">
            <button
              onClick={() => setActiveTab("pulses")}
              className={`flex-1 px-4 py-3 text-sm font-medium transition-colors ${
                activeTab === "pulses"
                  ? "text-text-primary border-b-2 border-brand-soft"
                  : "text-text-secondary hover:text-text-primary"
              }`}
            >
              Pulses
            </button>
            <button
              onClick={() => setActiveTab("following")}
              className={`flex-1 px-4 py-3 text-sm font-medium transition-colors ${
                activeTab === "following"
                  ? "text-text-primary border-b-2 border-brand-soft"
                  : "text-text-secondary hover:text-text-primary"
              }`}
            >
              Following
            </button>
            <button
              onClick={() => setActiveTab("followers")}
              className={`flex-1 px-4 py-3 text-sm font-medium transition-colors ${
                activeTab === "followers"
                  ? "text-text-primary border-b-2 border-brand-soft"
                  : "text-text-secondary hover:text-text-primary"
              }`}
            >
              Followers
            </button>
          </div>
        </div>

        {/* Tab Content */}
        <div>
          {activeTab === "pulses" && (
            <div>
              {isLoadingPulses && pulses.length === 0 ? (
                <div>
                  {Array.from({ length: 3 }).map((_, i) => (
                    <LoadingCard key={i} />
                  ))}
                </div>
              ) : pulses.length === 0 ? (
                <div className="text-center text-sm text-text-secondary py-8 px-4">
                  No pulses yet
                </div>
              ) : (
                <>
                  {pulses.map((pulse) => (
                    <div key={pulse.id}>
                      <div className="border-b border-border">
                        <PostCard
                          post={pulse}
                          onReplyClick={handleReplyClick}
                          onDelete={handleDelete}
                          onEdit={handleEdit}
                        />
                      </div>
                      {replyingToPulseId === pulse.id && (
                        <div className="border-b border-border bg-surface-card1/30 px-4 py-3">
                          <UnifiedComposer
                            mode="reply"
                            parentPulseId={pulse.id}
                            onSuccess={handleReplyCreated}
                            placeholder="Post your reply"
                            rows={2}
                            focusedRows={4}
                          />
                        </div>
                      )}
                    </div>
                  ))}
                  {/* Infinite scroll sentinel */}
                  <div ref={pulsesSentinelRef} className="h-1" />
                  {isLoadingPulses && pulses.length > 0 && (
                    <div className="p-4 flex justify-center">
                      <Spinner size="sm" />
                    </div>
                  )}
                </>
              )}
            </div>
          )}

          {activeTab === "following" && (
            <div>
              {isLoadingFollowing && following.length === 0 ? (
                <div>
                  {Array.from({ length: 5 }).map((_, i) => (
                    <LoadingUserItem key={i} />
                  ))}
                </div>
              ) : following.length === 0 ? (
                <div className="text-center text-sm text-text-secondary py-8 px-4">
                  Not following anyone yet
                </div>
              ) : (
                following.map((user) => (
                  <div key={user.profileId} className="border-b border-border p-4">
                    <div className="flex items-start gap-3">
                      <Link to={`/pulse/u/${user.handle}`} className="h-12 w-12 rounded-full overflow-hidden border border-border flex-shrink-0">
                        {user.avatarUrl ? (
                          <img src={buildAvatarUrl(user.avatarUrl) ?? ''} alt={user.name} className="h-full w-full object-cover" />
                        ) : (
                          <div className="h-full w-full bg-surface-card2 flex items-center justify-center">
                            <span className="text-lg text-text-tertiary">{user.name.charAt(0).toUpperCase()}</span>
                          </div>
                        )}
                      </Link>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-start justify-between gap-2">
                          <div className="min-w-0">
                            <Link
                              to={`/pulse/u/${user.handle}`}
                              className="font-medium text-sm text-text-primary hover:text-brand-soft transition-colors truncate block"
                            >
                              {user.name}
                            </Link>
                            <p className="text-xs text-text-secondary">@{user.handle}</p>
                          </div>
                          <FollowButton profileId={user.profileId} initialIsFollowing={user.isFollowing} size="sm" />
                        </div>
                        {user.bio && (
                          <p className="mt-1 text-xs text-text-secondary line-clamp-2">{user.bio}</p>
                        )}
                      </div>
                    </div>
                  </div>
                ))
              )}
            </div>
          )}

          {activeTab === "followers" && (
            <div>
              {isLoadingFollowers && followers.length === 0 ? (
                <div>
                  {Array.from({ length: 5 }).map((_, i) => (
                    <LoadingUserItem key={i} />
                  ))}
                </div>
              ) : followers.length === 0 ? (
                <div className="text-center text-sm text-text-secondary py-8 px-4">
                  No followers yet
                </div>
              ) : (
                followers.map((user) => (
                  <div key={user.profileId} className="border-b border-border p-4">
                    <div className="flex items-start gap-3">
                      <Link to={`/pulse/u/${user.handle}`} className="h-12 w-12 rounded-full overflow-hidden border border-border flex-shrink-0">
                        {user.avatarUrl ? (
                          <img src={buildAvatarUrl(user.avatarUrl) ?? ''} alt={user.name} className="h-full w-full object-cover" />
                        ) : (
                          <div className="h-full w-full bg-surface-card2 flex items-center justify-center">
                            <span className="text-lg text-text-tertiary">{user.name.charAt(0).toUpperCase()}</span>
                          </div>
                        )}
                      </Link>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-start justify-between gap-2">
                          <div className="min-w-0">
                            <Link
                              to={`/pulse/u/${user.handle}`}
                              className="font-medium text-sm text-text-primary hover:text-brand-soft transition-colors truncate block"
                            >
                              {user.name}
                            </Link>
                            <p className="text-xs text-text-secondary">@{user.handle}</p>
                          </div>
                          <FollowButton profileId={user.profileId} initialIsFollowing={user.isFollowing} size="sm" />
                        </div>
                        {user.bio && (
                          <p className="mt-1 text-xs text-text-secondary line-clamp-2">{user.bio}</p>
                        )}
                      </div>
                    </div>
                  </div>
                ))
              )}
            </div>
          )}
        </div>
      </PulseThreeColumnLayout>

      <ScrollToTopButton />
    </>
  );
}
