import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router";
import { login } from "@/api/auth";
import { Button } from "@/components/ui/button";

function LoginPage() {
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isLoggingIn, setIsLoggingIn] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsLoggingIn(true);

    try {
      await login(email, password);
      navigate("/documents");
    } catch (loginError) {
      setError(
        loginError instanceof Error
          ? loginError.message
          : "Login failed."
      );
    } finally {
      setIsLoggingIn(false);
    }
  }

  return (
    <section className="mx-auto max-w-md">
      <h2 className="text-2xl font-bold">Log in</h2>
      <p className="mt-1 text-sm text-slate-500">
        Enter your account details.
      </p>

      <form className="mt-6 space-y-4" onSubmit={handleSubmit}>
        <label className="block text-sm">
          Email
          <input
            className="mt-1 w-full rounded-md border px-3 py-2"
            type="email"
            autoComplete="email"
            required
            value={email}
            onChange={(event) => setEmail(event.target.value)}
          />
        </label>

        <label className="block text-sm">
          Password
          <input
            className="mt-1 w-full rounded-md border px-3 py-2"
            type="password"
            autoComplete="current-password"
            required
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
        </label>

        {error && <p className="text-sm text-red-600">{error}</p>}

        <Button type="submit" disabled={isLoggingIn}>
          {isLoggingIn ? "Logging in..." : "Log in"}
        </Button>
      </form>
    </section>
  );
}

export default LoginPage;