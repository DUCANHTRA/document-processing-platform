import { useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { uploadDocument } from "@/api/documents";
import { useAuth } from "@/context/AuthContext";

function UploadPage() {
  const { isAuthenticated, openAuthModal } = useAuth();

  const fileInputRef = useRef<HTMLInputElement>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);

  function handleSelectClick() {
    if (!isAuthenticated) {
      openAuthModal("login");
      return;
    }
    fileInputRef.current?.click();
  }

  async function handleUpload() {
    if (!isAuthenticated) {
      openAuthModal("login");
      return;
    }

    if (!selectedFile) {
      setUploadError("Please select a PDF first.");
      return;
    }

    setIsUploading(true);
    setUploadError(null);

    try {
      await uploadDocument(selectedFile);
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
          onClick={handleSelectClick}
          disabled={isUploading}
        >
          Select PDF
        </Button>

        <Button
          type="button"
          onClick={handleUpload}
          disabled={isAuthenticated && (!selectedFile || isUploading)}
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
        <p className="mt-2 text-sm text-red-600">{uploadError}</p>
      )}
    </section>
  );
}

export default UploadPage;
