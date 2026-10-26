import { useEffect, useState } from "react";
import { useParams, Link } from "react-router";
import { getDocument, getDocumentStatus, getProcessingResult } from "@/api/documents";
import type { Document, ProcessingStatus, ProcessingResult } from "@/api/documents";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/context/AuthContext";

type LoadState = "loading" | "not-complete" | "ready" | "error";

function DocumentDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { isAuthenticated, openAuthModal } = useAuth();

  const [loadState, setLoadState] = useState<LoadState>("loading");
  const [document, setDocument] = useState<Document | null>(null);
  const [status, setStatus] = useState<ProcessingStatus | null>(null);
  const [result, setResult] = useState<ProcessingResult | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!isAuthenticated) return;

    const documentId = Number(id);
    if (isNaN(documentId)) {
      setLoadState("error");
      setErrorMessage("Invalid document ID.");
      return;
    }

    async function load() {
      setLoadState("loading");
      try {
        const [doc, processingStatus] = await Promise.all([
          getDocument(documentId),
          getDocumentStatus(documentId),
        ]);

        setDocument(doc);
        setStatus(processingStatus);

        if (processingStatus?.status?.toLowerCase() !== "completed") {
          setLoadState("not-complete");
          return;
        }

        const processingResult = await getProcessingResult(documentId);
        setResult(processingResult);
        setLoadState("ready");
      } catch (error) {
        setLoadState("error");
        setErrorMessage(
          error instanceof Error ? error.message : "Failed to load document."
        );
      }
    }

    load();
  }, [id, isAuthenticated]);

  if (!isAuthenticated) {
    return (
      <section>
        <h2 className="text-2xl font-bold">Document Result</h2>
        <div className="mt-6 rounded-lg border bg-white p-12 text-center">
          <p className="text-sm text-slate-500">Sign in to view document results.</p>
          <div className="mt-4 flex justify-center gap-2">
            <Button variant="outline" size="sm" onClick={() => openAuthModal("login")}>
              Log in
            </Button>
            <Button size="sm" onClick={() => openAuthModal("signup")}>
              Sign up
            </Button>
          </div>
        </div>
      </section>
    );
  }

  return (
    <section>
      <div className="mb-6 flex items-center gap-3">
        <Link
          to="/documents"
          className="text-sm text-slate-500 hover:text-slate-800 transition-colors"
        >
          ← Back to Documents
        </Link>
      </div>

      <h2 className="text-2xl font-bold">Document Result</h2>

      {loadState === "loading" && (
        <p className="mt-6 text-sm text-slate-500">Loading...</p>
      )}

      {loadState === "error" && (
        <div className="mt-6 rounded-lg border border-red-200 bg-red-50 p-6">
          <p className="text-sm text-red-700">{errorMessage}</p>
          <Link to="/documents">
            <Button variant="outline" size="sm" className="mt-4">
              Back to Documents
            </Button>
          </Link>
        </div>
      )}

      {loadState === "not-complete" && (
        <div className="mt-6 rounded-lg border bg-white p-8">
          <p className="text-sm font-medium text-slate-700">
            Processing is not complete for this document.
          </p>
          {status && (
            <p className="mt-1 text-sm text-slate-500">
              Current status:{" "}
              <span className="font-medium capitalize">{status.status}</span>
            </p>
          )}
          <p className="mt-4 text-sm text-slate-500">
            Check back on the{" "}
            <Link to="/documents" className="text-blue-600 hover:underline">
              Documents page
            </Link>{" "}
            once processing has completed.
          </p>
        </div>
      )}

      {loadState === "ready" && document && result && (
        <div className="mt-6 space-y-6">
          {/* Metadata card */}
          <div className="rounded-lg border bg-white p-6">
            <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-500">
              Document Info
            </h3>
            <dl className="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-3">
              <div>
                <dt className="text-xs text-slate-500">Filename</dt>
                <dd className="mt-0.5 text-sm font-medium text-slate-800 break-all">
                  {document.fileName}
                </dd>
              </div>
              <div>
                <dt className="text-xs text-slate-500">Document ID</dt>
                <dd className="mt-0.5 text-sm font-medium text-slate-800">
                  {document.id}
                </dd>
              </div>
              <div>
                <dt className="text-xs text-slate-500">Processed At</dt>
                <dd className="mt-0.5 text-sm font-medium text-slate-800">
                  {new Date(result.createdAt).toLocaleString()}
                </dd>
              </div>
            </dl>
          </div>

          {/* Extracted text card */}
          <div className="rounded-lg border bg-white p-6">
            <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-500">
              Extracted Text
            </h3>
            <pre className="mt-4 max-h-[60vh] overflow-y-auto whitespace-pre-wrap break-words rounded-md bg-slate-50 p-4 text-sm text-slate-800 leading-relaxed">
              {result.extractedText || (
                <span className="text-slate-400 italic">No text was extracted.</span>
              )}
            </pre>
          </div>
        </div>
      )}
    </section>
  );
}

export default DocumentDetailPage;
