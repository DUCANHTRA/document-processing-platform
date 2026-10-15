
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import type { Document } from "@/api/documents";

interface DocumentTableProps {
  documents: Document[];
}

function DocumentTable({ documents }: DocumentTableProps) {
  return (
    <div className="mt-8 rounded-lg border bg-white">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Document</TableHead>
            <TableHead>ID</TableHead>
          </TableRow>
        </TableHeader>

        <TableBody>
          {documents.map((document) => (
            <TableRow key={document.id}>
              <TableCell className="font-medium">
                {document.fileName}
              </TableCell>
              <TableCell>{document.id}</TableCell>
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
