
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import type { Document, ProcessingStatus } from "@/api/documents";

interface DocumentTableProps {
  documents: Document[];
  documentStatuses: Record<number, ProcessingStatus | null>;
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
          </TableRow>
        </TableHeader>

        <TableBody>
          {documents.map((document) => (
            <TableRow key={document.id}>
              <TableCell className="font-medium">
                {document.fileName}
              </TableCell>
              <TableCell>{document.id}</TableCell>
              <TableCell>
              <Badge>
                {documentStatuses[document.id] === undefined
                  ? "Loading..."
                  : documentStatuses[document.id]?.status ?? "No status"}
              </Badge>
            </TableCell>
            </TableRow>
          ))}
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
