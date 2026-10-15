export interface Document {
  id: number;
  fileName: string;
}

export interface ProcessingStatus {
  documentId: number;
  jobId: number;
  status: string;
  errorMessage: string | null;
}

export async function getDocuments(): Promise<Document[]> {
  const response = await fetch("/api/documents");

  if (!response.ok) {
    throw new Error("Failed to fetch documents");
  }

  return response.json();
}

export async function uploadDocument(file: File): Promise<Document> {
    const formData = new FormData();
    formData.append("file", file);

    const response = await fetch("api/documents", {
        method: "POST",
        body: formData,
    });

    if (!response.ok) {
        throw new Error("Failed to upload document");
    }

    return response.json();
}

export async function getDocumentStatus(documentId: number): Promise<ProcessingStatus | null>
{
    const response = await fetch(
      `/api/documents/${documentId}/status`
    );

    // A document can exist before a processing job has been created.
    if (response.status === 404) {
      return null;
    }

    if (!response.ok) {
      throw new Error(`Failed to fetch document status (${response.status})`);
    }

    return response.json();
}