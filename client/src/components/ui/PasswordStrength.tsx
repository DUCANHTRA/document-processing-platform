interface Rule {
  label: string;
  test: (password: string) => boolean;
}

const rules: Rule[] = [
  { label: "At least 8 characters", test: (p) => p.length >= 8 },
  { label: "One uppercase letter", test: (p) => /[A-Z]/.test(p) },
  { label: "One number", test: (p) => /[0-9]/.test(p) },
  { label: "One special character", test: (p) => /[^A-Za-z0-9]/.test(p) },
];

interface PasswordStrengthProps {
  password: string;
}

function PasswordStrength({ password }: PasswordStrengthProps) {
  if (password.length === 0) return null;

  return (
    <ul className="mt-2 space-y-1">
      {rules.map((rule) => {
        const passed = rule.test(password);
        return (
          <li key={rule.label} className="flex items-center gap-2 text-xs">
            <span
              className={
                passed
                  ? "size-1.5 rounded-full bg-green-500"
                  : "size-1.5 rounded-full bg-slate-300"
              }
              aria-hidden="true"
            />
            <span className={passed ? "text-green-700" : "text-slate-400"}>
              {rule.label}
            </span>
          </li>
        );
      })}
    </ul>
  );
}

/** Returns true when all strength rules pass for the given password. */
function isPasswordStrong(password: string): boolean {
  return rules.every((rule) => rule.test(password));
}

export { PasswordStrength, isPasswordStrong };
