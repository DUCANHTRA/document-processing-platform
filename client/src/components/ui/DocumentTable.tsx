
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Link } from "react-router";
import type { Document, ProcessingStatus } from "@/api/documents";

interface DocumentTableProps {
  documents: Document[];
  documentStatuses: Record<number, ProcessingStatus | null>;
}

function getStatusBadgeClass(status: string | undefined): string {
  switch (status?.toLowerCase()) {
    case "completed":
      return "bg-green-100 text-green-800";
    case "processing":
      return "bg-blue-100 text-blue-800";
    case "pending":
      return "bg-amber-100 text-amber-800";
    case "failed":
      return "bg-red-100 text-red-800";
    default:
      return "bg-slate-100 text-slate-700";
  }
}

function DocumentTable({ documents, documentStatuses }: DocumentTableProps) {
  return (
    <div className="mt-8 rounded-lg border bg-white">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Document</TableHead>
            <TableHead>ID</TableHead>
            <TableHead>Status</TableHead>
            <TableHead></TableHead>
          </TableRow>
        </TableHeader>

        <TableBody>
          {documents.map((document) => {
            const status = documentStatuses[document.id]?.status;
            const isCompleted = status?.toLowerCase() === "completed";
            const label =
              documentStatuses[document.id] === undefined
                ? "Loading..."
                : status ?? "No status";

            return (
              <TableRow key={document.id}>
              <TableCell className="font-medium">
                {document.fileName}
              </TableCell>
              <TableCell>{document.id}</TableCell>
              <TableCell>
                  <Badge className={getStatusBadgeClass(status)}>
                    {label}
                  </Badge>
                </TableCell>
              <TableCell>
                {isCompleted && (
                  <Button asChild variant="ghost" size="sm">
                    <Link to={`/documents/${document.id}`}>View</Link>
                  </Button>
                )}
              </TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>

      {documents.length === 0 && (
        <p className="p-6 text-center text-sm text-slate-500">
          No documents found.
        </p>
      )}
    </div>
  );
}

export default DocumentTable;
