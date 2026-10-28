import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router";
import { Dialog } from "@base-ui/react/dialog";
import { Button } from "@/components/ui/button";
import { PasswordInput } from "@/components/ui/PasswordInput";
import { PasswordStrength, isPasswordStrong } from "@/components/ui/PasswordStrength";
import { useAuth } from "@/context/AuthContext";
import { cn } from "cn";

function AuthModal() {
  const { modalOpen, modalTab, closeAuthModal, login, register, openAuthModal } =
    useAuth();
  const navigate = useNavigate();

  // Login form state
  const [loginEmail, setLoginEmail] = useState("");
  const [loginPassword, setLoginPassword] = useState("");
  const [loginError, setLoginError] = useState<string | null>(null);
  const [isLoggingIn, setIsLoggingIn] = useState(false);

  // Signup form state
  const [signupEmail, setSignupEmail] = useState("");
  const [signupPassword, setSignupPassword] = useState("");
  const [signupConfirm, setSignupConfirm] = useState("");
  const [signupError, setSignupError] = useState<string | null>(null);
  const [isSigningUp, setIsSigningUp] = useState(false);

  function resetForms() {
    setLoginEmail("");
    setLoginPassword("");
    setLoginError(null);
    setSignupEmail("");
    setSignupPassword("");
    setSignupConfirm("");
    setSignupError(null);
  }

  function handleOpenChange(open: boolean) {
    if (!open) {
      closeAuthModal();
      resetForms();
    }
  }

  async function handleLogin(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setLoginError(null);
    setIsLoggingIn(true);
    try {
      await login(loginEmail, loginPassword, (_attempt, _total) => {
        setLoginError("Server is waking up, please wait\u2026");
      });
      closeAuthModal();
      resetForms();
      navigate("/documents");
    } catch (err) {
      setLoginError(
        err instanceof Error ? err.message : "Login failed."
      );
    } finally {
      setIsLoggingIn(false);
    }
  }

  async function handleSignup(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (signupPassword !== signupConfirm) {
      setSignupError("Passwords do not match.");
      return;
    }
    setSignupError(null);
    setIsSigningUp(true);
    try {
      await register(signupEmail, signupPassword);
      closeAuthModal();
      resetForms();
      navigate("/documents");
    } catch (err) {
      setSignupError(
        err instanceof Error ? err.message : "Registration failed."
      );
    } finally {
      setIsSigningUp(false);
    }
  }

  return (
    <Dialog.Root open={modalOpen} onOpenChange={handleOpenChange}>
      <Dialog.Portal>
        {/* Backdrop */}
        <Dialog.Backdrop className="fixed inset-0 z-40 bg-black/40 backdrop-blur-sm data-[ending-style]:opacity-0 data-[starting-style]:opacity-0 transition-opacity duration-150" />

        {/* Popup */}
        <Dialog.Popup className="fixed left-1/2 top-1/2 z-50 w-full max-w-md -translate-x-1/2 -translate-y-1/2 rounded-xl border bg-white p-6 shadow-lg data-[ending-style]:opacity-0 data-[ending-style]:scale-95 data-[starting-style]:opacity-0 data-[starting-style]:scale-95 transition-all duration-150">

          {/* Tabs */}
          <div className="flex gap-1 rounded-lg bg-slate-100 p-1">
            <button
              type="button"
              onClick={() => openAuthModal("login")}
              className={cn(
                "flex-1 rounded-md px-3 py-1.5 text-sm font-medium transition-colors",
                modalTab === "login"
                  ? "bg-white text-slate-900 shadow-sm"
                  : "text-slate-500 hover:text-slate-700"
              )}
            >
              Log in
            </button>
            <button
              type="button"
              onClick={() => openAuthModal("signup")}
              className={cn(
                "flex-1 rounded-md px-3 py-1.5 text-sm font-medium transition-colors",
                modalTab === "signup"
                  ? "bg-white text-slate-900 shadow-sm"
                  : "text-slate-500 hover:text-slate-700"
              )}
            >
              Sign up
            </button>
          </div>

          {/* Login form */}
          {modalTab === "login" && (
            <form className="mt-5 space-y-4" onSubmit={handleLogin}>
              <Dialog.Title className="text-lg font-semibold">
                Welcome back
              </Dialog.Title>
              <Dialog.Description className="text-sm text-slate-500">
                Enter your credentials to continue.
              </Dialog.Description>

              <label className="block text-sm font-medium">
                Email
                <input
                  className="mt-1 w-full rounded-md border border-slate-200 px-3 py-2 text-sm outline-none focus:border-slate-400 focus:ring-2 focus:ring-slate-200"
                  type="email"
                  autoComplete="email"
                  required
                  value={loginEmail}
                  onChange={(e) => setLoginEmail(e.target.value)}
                />
              </label>

              <label className="block text-sm font-medium">
                Password
                <PasswordInput
                  autoComplete="current-password"
                  required
                  value={loginPassword}
                  onChange={(e) => setLoginPassword(e.target.value)}
                />
              </label>

              {loginError && (
                <p className={`text-sm ${loginError.includes("waking up") ? "text-amber-600" : "text-red-600"}`}>
                  {loginError}
                </p>
              )}

              <div className="flex items-center justify-between gap-3 pt-1">
                <Dialog.Close
                  render={
                    <Button type="button" variant="ghost" size="sm">
                      Cancel
                    </Button>
                  }
                />
                <Button type="submit" disabled={isLoggingIn}>
                  {isLoggingIn ? "Logging in..." : "Log in"}
                </Button>
              </div>
            </form>
          )}

          {/* Signup form */}
          {modalTab === "signup" && (
            <form className="mt-5 space-y-4" onSubmit={handleSignup}>
              <Dialog.Title className="text-lg font-semibold">
                Create an account
              </Dialog.Title>
              <Dialog.Description className="text-sm text-slate-500">
                Sign up to start uploading documents.
              </Dialog.Description>

              <label className="block text-sm font-medium">
                Email
                <input
                  className="mt-1 w-full rounded-md border border-slate-200 px-3 py-2 text-sm outline-none focus:border-slate-400 focus:ring-2 focus:ring-slate-200"
                  type="email"
                  autoComplete="email"
                  required
                  value={signupEmail}
                  onChange={(e) => setSignupEmail(e.target.value)}
                />
              </label>

              <label className="block text-sm font-medium">
                Password
                <PasswordInput
                  autoComplete="new-password"
                  required
                  value={signupPassword}
                  onChange={(e) => setSignupPassword(e.target.value)}
                />
              </label>
              <PasswordStrength password={signupPassword} />

              <label className="block text-sm font-medium">
                Confirm password
                <PasswordInput
                  autoComplete="new-password"
                  required
                  value={signupConfirm}
                  onChange={(e) => setSignupConfirm(e.target.value)}
                />
              </label>
              {signupConfirm.length > 0 && signupPassword !== signupConfirm && (
                <p className="text-sm text-red-600">Passwords do not match.</p>
              )}

              {signupError && (
                <p className="text-sm text-red-600">{signupError}</p>
              )}

              <div className="flex items-center justify-between gap-3 pt-1">
                <Dialog.Close
                  render={
                    <Button type="button" variant="ghost" size="sm">
                      Cancel
                    </Button>
                  }
                />
                <Button
                  type="submit"
                  disabled={
                    isSigningUp ||
                    !isPasswordStrong(signupPassword) ||
                    signupPassword !== signupConfirm
                  }
                >
                  {isSigningUp ? "Creating account..." : "Sign up"}
                </Button>
              </div>
            </form>
          )}
        </Dialog.Popup>
      </Dialog.Portal>
    </Dialog.Root>
  );
}

export default AuthModal;
