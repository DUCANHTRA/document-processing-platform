import { useEffect, useState } from "react";
import DocumentTable from "@/components/ui/DocumentTable";
import { getDocuments, getDocumentStatus } from "@/api/documents";
import type { Document, ProcessingStatus } from "@/api/documents";
import { HubConnectionBuilder } from "@microsoft/signalr";
import { getToken } from "@/api/auth";
import { useAuth } from "@/context/AuthContext";
import { Button } from "@/components/ui/button";

function DocumentsPage() {
  const { isAuthenticated, openAuthModal } = useAuth();

  const [documents, setDocuments] = useState<Document[]>([]);
  const [documentStatuses, setDocumentStatuses] = useState<
    Record<number, ProcessingStatus | null>
  >({});

  // Fetch documents only when authenticated
  useEffect(() => {
    if (!isAuthenticated) return;

    async function loadDocuments() {
      try {
        const data = await getDocuments();
        setDocuments(data);

        const statusEntries = await Promise.all(
          data.map(async (document) => {
            const status = await getDocumentStatus(document.id);
            return [document.id, status] as const;
          })
        );

        setDocumentStatuses(Object.fromEntries(statusEntries));
      } catch (error) {
        console.error("Could not load documents:", error);
      }
    }

    loadDocuments();
  }, [isAuthenticated]);

  // SignalR — only connect when authenticated
  useEffect(() => {
    if (!isAuthenticated) return;

    const connection = new HubConnectionBuilder()
      .withUrl("/hubs/processing", {
        accessTokenFactory: () => getToken() ?? "",
      })
      .withAutomaticReconnect()
      .build();

    connection.on("JobStatusChanged", (update: ProcessingStatus) => {
      setDocumentStatuses((currentStatuses) => ({
        ...currentStatuses,
        [update.documentId]: update,
      }));
    });

    connection.start().catch((error) => {
      console.error("Could not connect to the status hub:", error);
    });

    return () => {
      void connection.stop();
    };
  }, [isAuthenticated]);

  return (
    <section>
      <h2 className="text-2xl font-bold">Your Documents</h2>

      <p className="mt-1 text-sm text-slate-500">
        View your uploaded documents and processing statuses.
      </p>

      {isAuthenticated ? (
        <div className="mt-6">
          <DocumentTable
            documents={documents}
            documentStatuses={documentStatuses}
          />
        </div>
      ) : (
        <div className="mt-6 rounded-lg border bg-white p-12 text-center">
          <p className="text-sm text-slate-500">
            Sign in to view your documents.
          </p>
          <div className="mt-4 flex justify-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => openAuthModal("login")}
            >
              Log in
            </Button>
            <Button size="sm" onClick={() => openAuthModal("signup")}>
              Sign up
            </Button>
          </div>
        </div>
      )}
    </section>
  );
}

export default DocumentsPage;
