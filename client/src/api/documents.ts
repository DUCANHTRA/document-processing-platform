import { authenticatedFetch } from "@/api/auth";

export interface Document {
  id: number;
  fileName: string;
  contentType: string;
}

export interface ProcessingStatus {
  documentId: number;
  jobId: number;
  status: string;
  errorMessage: string | null;
}

export interface ProcessingResult {
  documentId: number;
  extractedText: string;
  createdAt: string;
}

export async function getDocuments(): Promise<Document[]> {
  const response = await authenticatedFetch("/api/documents");

  if (!response.ok) {
    throw new Error("Failed to fetch documents");
  }

  return response.json();
}

export async function uploadDocument(file: File): Promise<Document> {
    const formData = new FormData();
    formData.append("file", file);

    const response = await authenticatedFetch("api/documents", {
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
    const response = await authenticatedFetch(
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

export async function getDocument(documentId: number): Promise<Document> {
    const response = await authenticatedFetch(`/api/documents/${documentId}`);

    if (!response.ok) {
        throw new Error(`Failed to fetch document (${response.status})`);
    }

    return response.json();
}

export async function getProcessingResult(documentId: number): Promise<ProcessingResult> {
    const response = await authenticatedFetch(`/api/documents/${documentId}/result`);

    if (!response.ok) {
        throw new Error(`Failed to fetch processing result (${response.status})`);
    }

    return response.json();
}