import { NavLink, Routes, Route } from "react-router";
import UploadPage from "@/pages/UploadPage";
import DocumentsPage from "@/pages/DocumentsPage";

function App() {
  return (
    <div className="min-h-screen bg-slate-50">
      {/* Sidebar */}
      <aside className="fixed inset-y-0 left-0 w-60 border-r bg-white p-6">
        <h1 className="text-xl font-bold">DocuFlow</h1>

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
        <header className="flex h-16 items-center border-b bg-white px-8">
          <h2 className="text-lg font-semibold">
            Document Dashboard
          </h2>
        </header>

        <main className="p-8">
          <Routes>
            <Route path="/upload" element={<UploadPage />} />
            <Route path="/documents" element={<DocumentsPage />} />
            <Route path="*" element={<UploadPage />} />
          </Routes>
        </main>
      </div>
    </div>
  );
}

export default App;