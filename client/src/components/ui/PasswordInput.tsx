import { useState } from "react";
import { Eye, EyeOff } from "lucide-react";
import { cn } from "cn";

type PasswordInputProps = Omit<React.InputHTMLAttributes<HTMLInputElement>, "type">;

function PasswordInput({ className, ...props }: PasswordInputProps) {
  const [show, setShow] = useState(false);

  return (
    <div className="relative mt-1">
      <input
        {...props}
        type={show ? "text" : "password"}
        className={cn(
          "w-full rounded-md border border-slate-200 px-3 py-2 pr-10 text-sm outline-none focus:border-slate-400 focus:ring-2 focus:ring-slate-200",
          className
        )}
      />
      <button
        type="button"
        aria-label={show ? "Hide password" : "Show password"}
        tabIndex={0}
        onClick={() => setShow((prev) => !prev)}
        className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 focus:outline-none"
      >
        {show ? (
          <EyeOff className="size-4" />
        ) : (
          <Eye className="size-4" />
        )}
      </button>
    </div>
  );
}

export { PasswordInput };
