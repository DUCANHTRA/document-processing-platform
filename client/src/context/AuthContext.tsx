import {
  createContext,
  useContext,
  useState,
  type ReactNode,
} from "react";
import {
  login as apiLogin,
  register as apiRegister,
  getToken,
} from "@/api/auth";

function decodeEmailFromToken(token: string): string | null {
  try {
    const payload = token.split(".")[1];
    const decoded = JSON.parse(atob(payload));
    return (decoded.email as string | undefined) ?? (decoded.sub as string | undefined) ?? null;
  } catch {
    return null;
  }
}

interface AuthContextValue {
  isAuthenticated: boolean;
  userEmail: string | null;
  modalOpen: boolean;
  modalTab: "login" | "signup";
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string) => Promise<void>;
  logout: () => void;
  openAuthModal: (tab: "login" | "signup") => void;
  closeAuthModal: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function getInitialEmail(): string | null {
  const token = getToken();
  if (!token) return null;
  return decodeEmailFromToken(token);
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(
    () => getToken() !== null
  );
  const [userEmail, setUserEmail] = useState<string | null>(getInitialEmail);
  const [modalOpen, setModalOpen] = useState(false);
  const [modalTab, setModalTab] = useState<"login" | "signup">("login");

  async function login(email: string, password: string): Promise<void> {
    await apiLogin(email, password);
    setIsAuthenticated(true);
    setUserEmail(email);
  }

  async function register(email: string, password: string): Promise<void> {
    await apiRegister(email, password);
    await login(email, password);
  }

  function logout(): void {
    sessionStorage.removeItem("document-processing-token");
    setIsAuthenticated(false);
    setUserEmail(null);
  }

  function openAuthModal(tab: "login" | "signup"): void {
    setModalTab(tab);
    setModalOpen(true);
  }

  function closeAuthModal(): void {
    setModalOpen(false);
  }

  return (
    <AuthContext.Provider
      value={{
        isAuthenticated,
        userEmail,
        modalOpen,
        modalTab,
        login,
        register,
        logout,
        openAuthModal,
        closeAuthModal,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return ctx;
}
