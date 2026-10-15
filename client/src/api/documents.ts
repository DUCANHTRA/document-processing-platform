export interface Document {
  id: number;
  fileName: string;
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