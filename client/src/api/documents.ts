export async function getDocuments() {
    const response = await fetch("/api/documents");

    if (!response.ok) {
        throw new Error("Failed to fetch documents");
    }

    return response.json();
}