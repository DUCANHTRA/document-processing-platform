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
