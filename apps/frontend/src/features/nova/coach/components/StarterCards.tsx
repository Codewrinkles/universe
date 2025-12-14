interface StarterCard {
  emoji: string;
  title: string;
  prompt: string;
}

const STARTER_CARDS: StarterCard[] = [
  {
    emoji: "🏗️",
    title: "Architecture",
    prompt: "What are the key principles of good software architecture?",
  },
  {
    emoji: "💡",
    title: "Best Practices",
    prompt: "What are the SOLID principles and why do they matter?",
  },
  {
    emoji: "🧩",
    title: "Design Patterns",
    prompt: "When should I use composition over inheritance?",
  },
  {
    emoji: "⚖️",
    title: "Trade-offs",
    prompt: "How do I decide between simplicity and flexibility in my code?",
  },
];

interface StarterCardsProps {
  onSelectPrompt: (prompt: string) => void;
}

/**
 * StarterCards - 4 capability showcase cards shown in empty state
 *
 * Helps users understand what Cody can do and provides quick-start prompts.
 * Clicking a card populates the input with the associated prompt.
 */
export function StarterCards({ onSelectPrompt }: StarterCardsProps): JSX.Element {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 px-6 max-w-4xl mx-auto">
      {STARTER_CARDS.map((card) => (
        <button
          key={card.title}
          type="button"
          onClick={() => onSelectPrompt(card.prompt)}
          className="flex flex-col items-start gap-2 p-4 rounded-xl bg-surface-card1 border border-border hover:border-violet-500/40 hover:bg-surface-card2 transition-all text-left group"
        >
          <div className="flex items-center gap-2">
            <span className="text-lg">{card.emoji}</span>
            <span className="text-sm font-medium text-text-primary group-hover:text-violet-400 transition-colors">
              {card.title}
            </span>
          </div>
          <p className="text-xs text-text-secondary line-clamp-2">
            "{card.prompt}"
          </p>
        </button>
      ))}
    </div>
  );
}
