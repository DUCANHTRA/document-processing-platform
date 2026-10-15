import { Button } from "@/components/ui/button";
import { useRef, useState, useEffect } from "react";
import DocumentTable from "@/components/ui/DocumentTable";
import { getDocuments, getDocumentStatus, uploadDocument } from "@/api/documents";
import type { Document, ProcessingStatus } from "@/api/documents";

function App() {

  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [documents, setDocuments] = useState<Document[]>([]);
  const [documentStatuses, setDocumentStatues] = useState<Record<number, ProcessingStatus>>({});

  
  const [isUploading, setIsUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);

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

        setDocumentStatues(Object.fromEntries(statusEntries));
      } catch (error) {
        console.error("Could not load documents:", error);
      }
    }

    loadDocuments();
  }, []);


  async function handleUpload() {
    if (!selectedFile) {
      setUploadError("Please select a PDF first.");
      return;
    }

    setIsUploading(true);
    setUploadError(null);

    try {
      await uploadDocument(selectedFile);

      const updatedDocuments = await getDocuments();
      setDocuments(updatedDocuments);

      setSelectedFile(null);

      if (fileInputRef.current) {
        fileInputRef.current.value = "";
      }
    } catch (error) {
      setUploadError(
        error instanceof Error
          ? error.message
          : "Something went wrong while uploading."
      );
    } finally {
      setIsUploading(false);
    }
  }


  return (
    <div className="min-h-screen bg-slate-50">
      {/* Sidebar */}
      <aside className="fixed inset-y-0 left-0 w-60 border-r bg-white p-6">
        <h1 className="text-xl font-bold">DocuFlow</h1>

        <nav className="mt-8 space-y-2">
          <Button className="w-full justify-start">
            Documents
          </Button>

          <Button variant="ghost" className="w-full justify-start">
            Processing Jobs
          </Button>
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
          <div className="flex items-center justify-between">
            <div>
              <h3 className="text-2xl font-bold">
                Your Documents
              </h3>
              <p className="mt-1 text-sm text-slate-500">
                Upload and track your document processing.
              </p>
            </div>

          <div>
            <div className="flex items-center gap-3">
              <input
                ref={fileInputRef}
                type="file"
                accept="application/pdf"
                className="hidden"
                onChange={(event) => {
                  const file = event.target.files?.[0] ?? null;
                  setSelectedFile(file);
                  setUploadError(null);
                }}
              />

              <Button
                type="button"
                variant="outline"
                onClick={() => fileInputRef.current?.click()}
                disabled={isUploading}
              >
                Select PDF
              </Button>

              <Button
                type="button"
                onClick={handleUpload}
                disabled={!selectedFile || isUploading}
              >
                {isUploading ? "Uploading..." : "Upload PDF"}
              </Button>
            </div>

            {selectedFile && (
              <p className="mt-2 text-sm text-slate-600">
                Selected file: {selectedFile.name}
              </p>
            )}

            {uploadError && (
              <p className="mt-2 text-sm text-red-600">
                {uploadError}
              </p>
            )}
          </div>
          </div>

          {/* Temporary placeholder */}
          <DocumentTable
            documents={documents}
            documentStatuses={documentStatuses}
          />
        </main>
      </div>
    </div>
  );
}

export default App;
