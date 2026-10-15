import { useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import {
  uploadDocument,
  getDocumentStatus,
} from "@/api/documents";
import type { ProcessingStatus } from "@/api/documents";

function UploadPage() {
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);

  const [processingStatus, setProcessingStatus] =
    useState<ProcessingStatus | null>(null);

  async function handleUpload() {
    if (!selectedFile) {
      setUploadError("Please select a PDF first.");
      return;
    }

    setIsUploading(true);
    setUploadError(null);
    setProcessingStatus(null);

    try {
      await uploadDocument(selectedFile);

      setSelectedFile(null);

      if (fileInputRef.current) {
        fileInputRef.current.value = "";
      }

      // We'll add the new document's status lookup here next.
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
    <section>
      <h2 className="text-2xl font-bold">Upload Document</h2>

      <p className="mt-1 text-sm text-slate-500">
        Upload a PDF for processing.
      </p>

      <div className="mt-6 flex items-center gap-3">
        <input
          ref={fileInputRef}
          type="file"
          accept="application/pdf"
          className="hidden"
          onChange={(event) => {
            setSelectedFile(event.target.files?.[0] ?? null);
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
    </section>
  );
}

export default UploadPage;
