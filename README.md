# PDF Extractor

A full-stack document processing platform. Upload PDFs, extract their text content via a background worker, and view results through a React dashboard.

## Architecture

```
client/                        # React 19 + Vite + Tailwind frontend
DocumentProcessing.Api/        # ASP.NET Core 10 Web API + background worker
DocumentProcessing.Api.Tests/  # xUnit integration test suite
.github/workflows/ci-cd.yml    # GitHub Actions CI/CD pipeline
```

**Request flow**

```
Browser (React)
  → ASP.NET Core API  (JWT auth, REST endpoints)
  → PostgreSQL        (documents, jobs, results)
  → Supabase Storage  (raw PDF files)
  → Background Worker (PdfPig text extraction)
  → SignalR Hub       (real-time processing status to browser)
```

## Tech stack

| Layer | Technology |
|---|---|
| Frontend | React 19, React Router 7, Tailwind CSS 4, shadcn/ui, SignalR client |
| API | ASP.NET Core 10, Entity Framework Core 10, Npgsql |
| PDF processing | PdfPig |
| Database | PostgreSQL 17 |
| File storage | Supabase Storage |
| Auth | JWT (HS256) |
| CI/CD | GitHub Actions → Render |

## Local development

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### 1. Configure environment variables

```bash
cp DocumentProcessing.Api/.env.example DocumentProcessing.Api/.env
```

Open `.env` and fill in:

| Variable | Description |
|---|---|
| `POSTGRES_DB` | Database name (e.g. `document_processing`) |
| `POSTGRES_USER` | Postgres user |
| `POSTGRES_PASSWORD` | Postgres password |
| `SUPABASE_URL` | Your Supabase project URL |
| `SUPABASE_BUCKET` | Storage bucket name |
| `SUPABASE_SECRET_KEY` | Supabase service role key |
| `JWT_SECRET` | Long random string — generate with `openssl rand -base64 32` |

### 2. Start everything with Docker Compose

```bash
cd DocumentProcessing.Api
docker compose up --build
```

This starts three containers: the API, the React frontend, and PostgreSQL. EF Core migrations run automatically on startup.

| Service | URL |
|---|---|
| Frontend | http://localhost:3000 |
| API | http://localhost:5116 |
| API docs (OpenAPI) | http://localhost:5116/openapi/v1.json |

### 3. Run the frontend in dev mode (optional, for hot reload)

```bash
cd client
npm install
npm run dev
```

The Vite dev server proxies `/api` calls to the API container.

## Running tests

The test suite uses real PostgreSQL. Start the database first (Docker Compose is the easiest way), then:

```bash
dotnet test DocumentProcessing.Api.Tests/DocumentProcessing.Api.Tests.csproj
```

Tests run serially (parallelization is disabled) and truncate all tables between test cases via `TestDatabase.CreateCleanContextAsync()`. External services (Supabase, the background worker) are replaced with in-memory fakes.

## CI/CD pipeline

The pipeline is defined in `.github/workflows/ci-cd.yml` and runs on every push and pull request to `main`.

```
Push / PR to main
  └─ test-and-build job
       ├─ postgres:17-alpine service container
       ├─ dotnet restore + dotnet test
       ├─ dotnet build (Release)
       ├─ npm ci + npm run build
       ├─ docker build ./client
       └─ docker build ./DocumentProcessing.Api
            │
            │ (only on push to main, after all steps pass)
            ▼
       deploy job
            ├─ curl RENDER_API_DEPLOY_HOOK_URL
            └─ curl RENDER_CLIENT_DEPLOY_HOOK_URL
```

**Pull requests** run the full `test-and-build` job but the deploy job is skipped. Nothing reaches Render until a PR is merged into `main` and CI passes again on the merge commit.

### Setting up deployment

1. In each Render service go to **Settings → Deploy Hook** and create a hook URL.
2. Add two secrets to your GitHub repository (**Settings → Secrets and variables → Actions**):

   | Secret | Value |
   |---|---|
   | `RENDER_API_DEPLOY_HOOK_URL` | Deploy hook URL from your Render API service |
   | `RENDER_CLIENT_DEPLOY_HOOK_URL` | Deploy hook URL from your Render frontend service |

3. In each Render service, **disable Auto-Deploy** from Git pushes so Render only deploys when GitHub Actions tells it to.

## Project structure

```
document-processing-platform/
├── .github/
│   └── workflows/
│       └── ci-cd.yml
├── client/
│   ├── src/
│   │   ├── api/          # fetch wrappers (auth, documents)
│   │   ├── components/   # AuthModal, shadcn UI primitives
│   │   ├── context/      # AuthContext (JWT storage + state)
│   │   └── pages/        # UploadPage, DocumentsPage, DocumentDetailPage
│   ├── Dockerfile
│   └── nginx.conf
├── DocumentProcessing.Api/
│   ├── Controllers/      # AuthController, DocumentsController
│   ├── Data/             # AppDbContext, EF Core migrations
│   ├── DTOs/
│   ├── Hubs/             # ProcessingHub (SignalR)
│   ├── Models/
│   ├── Services/         # DocumentsService, AuthService, JwtTokenService, SupabaseFileStorage
│   ├── Workers/          # DocumentProcessingWorker (background PDF processor)
│   ├── Dockerfile
│   └── compose.yaml
└── DocumentProcessing.Api.Tests/
    ├── ApiFactory.cs     # WebApplicationFactory with fakes wired in
    ├── TestDatabase.cs   # Shared Postgres helpers
    └── *Tests.cs
```
