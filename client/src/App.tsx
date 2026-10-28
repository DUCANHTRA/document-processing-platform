import { NavLink, Routes, Route, Navigate } from "react-router";
import UploadPage from "@/pages/UploadPage";
import DocumentsPage from "@/pages/DocumentsPage";
import DocumentDetailPage from "@/pages/DocumentDetailPage";
import AuthModal from "@/components/AuthModal";
import { useAuth } from "@/context/AuthContext";
import { Button } from "@/components/ui/button";
import { useEffect, useState } from "react";
import { wakeServer } from "@/api/health";

function App() {
  const { isAuthenticated, userEmail, logout, openAuthModal } = useAuth();
  const [waking, setWaking] = useState(true);

  useEffect(() => {
  wakeServer()
    .then(() => setWaking(false)); // when finish waking up server, change state to false
  }, []);

  console.log("waking:", waking);

  return (
    <div className="min-h-screen bg-slate-50">
      {/* Sidebar */}
      <aside className="fixed inset-y-0 left-0 w-60 border-r bg-white p-6">
        <h1 className="text-xl font-bold">PDF Extractor</h1>

        <nav className="mt-8 space-y-2">
          <NavLink
            to="/upload"
            className={({ isActive }) =>
              `flex w-full justify-start rounded-md px-4 py-2 text-sm hover:bg-slate-100 ${
                isActive ? "bg-slate-100 font-medium" : ""
              }`
            }
          >
            Upload Document
          </NavLink>

          <NavLink
            to="/documents"
            className={({ isActive }) =>
              `flex w-full justify-start rounded-md px-4 py-2 text-sm hover:bg-slate-100 ${
                isActive ? "bg-slate-100 font-medium" : ""
              }`
            }
          >
            Documents
          </NavLink>
        </nav>
      </aside>

      {/* Main area */}
      <div className="ml-60">
        <header className="flex h-16 items-center justify-between border-b bg-white px-8">
          <h2 className="text-lg font-semibold">Document Dashboard</h2>

          {/* Auth controls */}
          {isAuthenticated ? (
            <div className="flex items-center gap-3">
              <span className="text-sm text-slate-500">{userEmail}</span>
              <Button variant="outline" size="sm" onClick={logout}>
                Log out
              </Button>
            </div>
          ) : (
            <div className="flex items-center gap-2">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => openAuthModal("login")}
              >
                Log in
              </Button>
              <Button size="sm" onClick={() => openAuthModal("signup")}>
                Sign up
              </Button>
            </div>
          )}
        </header>

        <main className="p-8">
          <Routes>
            <Route path="/upload" element={<UploadPage />} />
            <Route path="/documents" element={<DocumentsPage />} />
            <Route path="/documents/:id" element={<DocumentDetailPage />} />
            {/* Redirect /login to /upload (login is now a modal) */}
            <Route path="/login" element={<Navigate to="/upload" replace />} />
            {/* Default and catch-all → /upload */}
            <Route path="/" element={<Navigate to="/upload" replace />} />
            <Route path="*" element={<Navigate to="/upload" replace />} />
          </Routes>
        </main>
      </div>

      {/* Global auth modal — mounted once here */}
      <AuthModal />
    </div>
  );
}

export default App;
