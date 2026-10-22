import { useEffect, useState } from "react";
import DocumentTable from "@/components/ui/DocumentTable";
import {
  getDocuments,
  getDocumentStatus,
} from "@/api/documents";
import type {
  Document,
  ProcessingStatus,
} from "@/api/documents";
import { HubConnectionBuilder } from "@microsoft/signalr";
import { getToken } from "@/api/auth";

function DocumentsPage() {
  const [documents, setDocuments] = useState<Document[]>([]);

  const [documentStatuses, setDocumentStatuses] = useState<
    Record<number, ProcessingStatus | null>
  >({});

  useEffect(() => {
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
  }, []);

  //SignalR
  useEffect(() => {
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
  }, []);

  return (
    <section>
      <h2 className="text-2xl font-bold">
        Your Documents
      </h2>

      <p className="mt-1 text-sm text-slate-500">
        View your uploaded documents and processing statuses.
      </p>

      <div className="mt-6">
        <DocumentTable
          documents={documents}
          documentStatuses={documentStatuses}
        />
      </div>
    </section>
  );
}

export default DocumentsPage;

