/**
 * Password strength indicator component
 * Shows visual feedback for password requirements
 */

import { checkPasswordStrength } from "../../utils/validation";

export interface PasswordStrengthProps {
  password: string;
}

interface RequirementProps {
  label: string;
  met: boolean;
}

function Requirement({ label, met }: RequirementProps): JSX.Element {
  return (
    <li className={`flex items-center gap-1.5 ${met ? "text-green-500" : "text-text-tertiary"}`}>
      <span className="text-[10px]">{met ? "\u2713" : "\u25CB"}</span>
      <span>{label}</span>
    </li>
  );
}

export function PasswordStrength({ password }: PasswordStrengthProps): JSX.Element {
  if (!password) {
    return <></>;
  }

  const strength = checkPasswordStrength(password);

  const strengthColors = {
    weak: "bg-red-500",
    medium: "bg-yellow-500",
    strong: "bg-green-500",
  };

  const requirements = [
    { label: "At least 8 characters", met: strength.hasMinLength },
    { label: "Uppercase letter (A-Z)", met: strength.hasUpperCase },
    { label: "Lowercase letter (a-z)", met: strength.hasLowerCase },
    { label: "Number (0-9)", met: strength.hasNumber },
    { label: "Special character (!@#$...)", met: strength.hasSpecialChar },
  ];

  return (
    <div className="space-y-2">
      {/* Strength bar */}
      <div className="flex gap-1">
        {[1, 2, 3, 4, 5].map((segment) => (
          <div
            key={segment}
            className={`h-1 flex-1 rounded-full transition-colors ${
              segment <= strength.metCount ? strengthColors[strength.strength] : "bg-border"
            }`}
          />
        ))}
      </div>

      {/* Requirements checklist */}
      <ul className="space-y-0.5 text-[11px]">
        {requirements.map((req) => (
          <Requirement key={req.label} label={req.label} met={req.met} />
        ))}
      </ul>
    </div>
  );
}
