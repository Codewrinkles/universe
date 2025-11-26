import { Card } from "../../components/ui/Card";

interface SuggestedUser {
  id: string;
  name: string;
  handle: string;
  bio: string;
  avatarInitial: string;
}

const MOCK_SUGGESTIONS: SuggestedUser[] = [
  {
    id: "1",
    name: "Sarah Chen",
    handle: "sarahcodes",
    bio: "Full-stack dev • Building in public",
    avatarInitial: "S",
  },
  {
    id: "2",
    name: "Marcus Dev",
    handle: "marcusdev",
    bio: ".NET enthusiast • Clean architecture advocate",
    avatarInitial: "M",
  },
  {
    id: "3",
    name: "AI Weekly",
    handle: "aiweekly",
    bio: "Curating the best AI content",
    avatarInitial: "A",
  },
];

interface UserSuggestionProps {
  user: SuggestedUser;
}

function UserSuggestion({ user }: UserSuggestionProps): JSX.Element {
  return (
    <div className="flex items-start gap-3 py-3">
      <div className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-full bg-surface-card2 border border-border text-sm font-semibold text-text-primary">
        {user.avatarInitial}
      </div>
      <div className="flex-1 min-w-0">
        <div className="flex items-start justify-between gap-2">
          <div className="min-w-0">
            <p className="text-sm font-medium text-text-primary truncate">{user.name}</p>
            <p className="text-xs text-text-tertiary truncate">@{user.handle}</p>
          </div>
          <button
            type="button"
            className="flex-shrink-0 rounded-full border border-text-primary bg-text-primary px-3 py-1 text-xs font-semibold text-surface-page hover:bg-text-secondary transition-colors"
          >
            Follow
          </button>
        </div>
        <p className="mt-1 text-xs text-text-secondary line-clamp-2">{user.bio}</p>
      </div>
    </div>
  );
}

export function WhoToFollow(): JSX.Element {
  return (
    <Card>
      <h2 className="text-base font-semibold tracking-tight text-text-primary">
        Who to follow
      </h2>
      <div className="divide-y divide-border">
        {MOCK_SUGGESTIONS.map((user) => (
          <UserSuggestion key={user.id} user={user} />
        ))}
      </div>
      <button
        type="button"
        className="mt-2 text-xs text-brand-soft hover:text-brand transition-colors"
      >
        Show more
      </button>
    </Card>
  );
}
