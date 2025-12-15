import { Card } from "../../../components/ui/Card";
import { Button } from "../../../components/ui/Button";
import { UnifiedComposer } from "../../pulse/UnifiedComposer";

interface FirstPulseProps {
  onComplete: () => void;
}

export function FirstPulse({ onComplete }: FirstPulseProps): JSX.Element {
  const handlePulseCreated = (): void => {
    onComplete();
  };

  return (
    <Card>
      <h2 className="text-xl font-bold text-text-primary mb-2">
        Share Your First Pulse
      </h2>
      <p className="text-sm text-text-secondary mb-6">
        Introduce yourself to the Codewrinkles community (you can skip this step)
      </p>

      <UnifiedComposer
        mode="post"
        onSuccess={handlePulseCreated}
        placeholder="Hey everyone! I'm new to Codewrinkles..."
        rows={3}
        focusedRows={6}
      />

      <div className="flex justify-end mt-4">
        <Button variant="secondary" onClick={onComplete}>
          Skip for Now
        </Button>
      </div>
    </Card>
  );
}
