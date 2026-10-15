import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";

const documents = [
  {
    id: 18,
    name: "invoice-001.pdf",
    uploadedAt: "15 Oct 2026",
    status: "Completed",
  },
  {
    id: 19,
    name: "invoice-002.pdf",
    uploadedAt: "15 Oct 2026",
    status: "Processing",
  },
  {
    id: 20,
    name: "invoice-003.pdf",
    uploadedAt: "15 Oct 2026",
    status: "Pending",
  },
];

function DocumentTable() {
  return (
    <div className="mt-8 rounded-lg border bg-white">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Document</TableHead>
            <TableHead>Uploaded</TableHead>
            <TableHead>Status</TableHead>
          </TableRow>
        </TableHeader>

        <TableBody>
          {documents.map((document) => (
            <TableRow key={document.id}>
              <TableCell className="font-medium">
                {document.name}
              </TableCell>
              <TableCell>{document.uploadedAt}</TableCell>
              <TableCell>
                <Badge variant="secondary">
                  {document.status}
                </Badge>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}

export default DocumentTable;
