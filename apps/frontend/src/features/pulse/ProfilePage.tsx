import { useState, useEffect } from "react";
import { useParams, useNavigate, Link } from "react-router";
import { useAuth } from "../../hooks/useAuth";
import { config } from "../../config";
import { buildAvatarUrl } from "../../utils/avatarUtils";
import { PulseNavigation } from "./PulseNavigation";
import { PulseRightSidebar } from "./PulseRightSidebar";
import { PostCard } from "./PostCard";
import { UnifiedComposer } from "./UnifiedComposer";
import type { User, Pulse, FollowerDto, FollowingDto } from "../../types";
import { LoadingProfile, LoadingCard, LoadingUserItem, Skeleton } from "../../components/ui";

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
            {isOwnProfile && (
              <Link
                to="/settings/profile"
                className="rounded-full border border-border px-4 py-1.5 text-xs font-medium text-text-primary hover:bg-surface-card2 transition-colors flex-shrink-0"
              >
                Edit profile
              </Link>
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
  const [pulses, setPulses] = useState<Pulse[]>([]);
  const [followers, setFollowers] = useState<FollowerDto[]>([]);
  const [following, setFollowing] = useState<FollowingDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingPulses, setIsLoadingPulses] = useState(false);
  const [isLoadingFollowers, setIsLoadingFollowers] = useState(false);
  const [isLoadingFollowing, setIsLoadingFollowing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [replyingToPulseId, setReplyingToPulseId] = useState<string | null>(null);

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

  // Fetch all profile data in parallel when profile is loaded
  useEffect(() => {
    if (!profile) return;

    const fetchPulses = async (): Promise<void> => {
      setIsLoadingPulses(true);
      try {
        const token = localStorage.getItem(config.auth.accessTokenKey);
        const response = await fetch(`${config.api.baseUrl}/api/pulse/author/${profile.profileId}?limit=20`, {
          headers: {
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
          },
        });

        if (response.ok) {
          const data = await response.json();
          setPulses(data.pulses || []);
        }
      } catch (err) {
        console.error("Error fetching pulses:", err);
      } finally {
        setIsLoadingPulses(false);
      }
    };

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
        }
      } catch (err) {
        console.error("Error fetching following:", err);
      } finally {
        setIsLoadingFollowing(false);
      }
    };

    // Execute all fetches in parallel
    Promise.all([
      fetchPulses(),
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
    if (profile) {
      const fetchPulses = async (): Promise<void> => {
        try {
          const token = localStorage.getItem(config.auth.accessTokenKey);
          const response = await fetch(`${config.api.baseUrl}/api/pulse/author/${profile.profileId}?limit=20`, {
            headers: {
              ...(token ? { Authorization: `Bearer ${token}` } : {}),
            },
          });

          if (response.ok) {
            const data = await response.json();
            setPulses(data.pulses || []);
          }
        } catch (err) {
          console.error("Error fetching pulses:", err);
        }
      };

      fetchPulses();
    }
  };

  const handleDelete = (pulseId: string): void => {
    // Remove the pulse from the local state
    setPulses((prevPulses) => prevPulses.filter((p) => p.id !== pulseId));
  };

  const isOwnProfile = currentUser?.profileId === profile?.profileId;

  if (isLoading) {
    return (
      <div className="flex justify-center">
        {/* Left Navigation */}
        <aside className="hidden lg:flex w-[320px] flex-shrink-0 justify-end pr-8">
          <div className="w-[240px]">
            <PulseNavigation />
          </div>
        </aside>

        {/* Main Content */}
        <main className="w-full max-w-[600px] border-x border-border lg:w-[600px]">
          <LoadingProfile />
        </main>

        {/* Right Sidebar */}
        <aside className="hidden lg:flex w-[320px] flex-shrink-0 pl-8">
          <div className="w-[280px]">
            <PulseRightSidebar />
          </div>
        </aside>
      </div>
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

  return (
    <div className="flex justify-center">
      {/* Left Navigation */}
      <aside className="hidden lg:flex w-[320px] flex-shrink-0 justify-end pr-8">
        <div className="w-[240px]">
          <PulseNavigation />
        </div>
      </aside>

      {/* Main Content */}
      <main className="w-full max-w-[600px] border-x border-border lg:w-[600px]">
        <ProfileHeader
          profile={profile}
          isOwnProfile={isOwnProfile}
          followersCount={followers.length}
          followingCount={following.length}
          pulsesCount={pulses.length}
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
                pulses.map((pulse) => (
                  <div key={pulse.id}>
                    <div className="border-b border-border">
                      <PostCard
                        post={pulse}
                        onReplyClick={handleReplyClick}
                        onDelete={handleDelete}
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
                ))
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
                      <div className="h-12 w-12 rounded-full overflow-hidden border border-border flex-shrink-0">
                        {user.avatarUrl ? (
                          <img src={buildAvatarUrl(user.avatarUrl) ?? ''} alt={user.name} className="h-full w-full object-cover" />
                        ) : (
                          <div className="h-full w-full bg-surface-card2 flex items-center justify-center">
                            <span className="text-lg text-text-tertiary">{user.name.charAt(0).toUpperCase()}</span>
                          </div>
                        )}
                      </div>
                      <div className="flex-1 min-w-0">
                        <Link
                          to={`/pulse/u/${user.handle}`}
                          className="font-medium text-sm text-text-primary hover:text-brand-soft transition-colors truncate block"
                        >
                          {user.name}
                        </Link>
                        <p className="text-xs text-text-secondary">@{user.handle}</p>
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
                      <div className="h-12 w-12 rounded-full overflow-hidden border border-border flex-shrink-0">
                        {user.avatarUrl ? (
                          <img src={buildAvatarUrl(user.avatarUrl) ?? ''} alt={user.name} className="h-full w-full object-cover" />
                        ) : (
                          <div className="h-full w-full bg-surface-card2 flex items-center justify-center">
                            <span className="text-lg text-text-tertiary">{user.name.charAt(0).toUpperCase()}</span>
                          </div>
                        )}
                      </div>
                      <div className="flex-1 min-w-0">
                        <Link
                          to={`/pulse/u/${user.handle}`}
                          className="font-medium text-sm text-text-primary hover:text-brand-soft transition-colors truncate block"
                        >
                          {user.name}
                        </Link>
                        <p className="text-xs text-text-secondary">@{user.handle}</p>
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
      </main>

      {/* Right Sidebar */}
      <aside className="hidden lg:flex w-[320px] flex-shrink-0 pl-8">
        <div className="w-[280px]">
          <PulseRightSidebar />
        </div>
      </aside>
    </div>
  );
}
