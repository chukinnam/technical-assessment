# Course Inquiry Management

A small full-stack app for capturing and managing course inquiries, with a background worker that syncs new inquiries to a CRM.

## Tech stack

| Layer    | Technology                                          |
| -------- | --------------------------------------------------- |
| Backend  | ASP.NET Core Web API (.NET 8, C#), controller-based |
| Data     | Entity Framework Core, SQL Server (LocalDB)         |
| API docs | Swagger / OpenAPI (Swashbuckle)                     |
| Frontend | React 19.2.7 (Create React App / react-scripts)     |

## Getting started

### Prerequisites

Install these before running the steps below:

- **.NET 8 SDK** — for `dotnet run`.
- **Node.js 18+ (LTS)** — for `npm install && npm start`.
- **SQL Server LocalDB** — ships with SQL Server Express LocalDB or the Visual Studio installer.

### 1. Set up the database

The API connects to SQL Server LocalDB using the connection string in
[`backend/appsettings.json`](backend/appsettings.json) (database `CourseInquiryDb`).

```cmd
sqlcmd -S "(localdb)\MSSQLLocalDB" -i database\database.sql
```

**Or** use SQL Server Management Studio:

1. Connect to LocalDB:
   - **Server name:** `(localdb)\MSSQLLocalDB`
   - **Authentication:** Windows Authentication
2. Open and run [`database/database.sql`](database/database.sql).

### 2. Run the backend

```bash
cd backend
dotnet run
```

Once the API is running, the Swagger UI is at
[http://localhost:5000/swagger]

Protected  endpoints require an `X-Api-Key` header. before test the api click **Authorize** and paste the vlaue =  `testapiKey`

### 3. Run the frontend

```bash
cd frontend
npm install
npm start
```

The app runs on [http://localhost:3000](http://localhost:3000) by default.

## Design decisions

- **Soft delete (chosen).** `DELETE` marks an inquiry as **Archived** instead of removing the row.
  Inquiries are business records, so keeping them preserves history and auditability and avoids
  conflicts with the CRM sync log.
- **Layered structure:**
  - **Controllers** — HTTP only.
  - **Services** — business logic only.
  - **EF Core** — data access only.
  - **DTOs** — the EF models are never exposed directly.
- **CRM background worker.** New inquiries are written to the `CrmSyncLogs` outbox table with a
  **Pending** status. A hosted background worker polls the table every 5 seconds and dispatches any
  rows still marked **Pending** or **Attempted**. Rows are processed one at a time (awaited
  sequentially) to avoid duplicate syncs, and the worker idles between sweeps so it does not hammer
  the CRM.
- **Validation.** DataAnnotations on the DTOs (required fields, email format, lengths, and so on).

## Assumptions

- **Simple authentication.** local testing only ,In production this would use OAuth or JWT, with MFA to strengthen security.
- **No rate limiting.** becasue local need to test api many time, In production request limits would be added to guard against DDoS attacks.
- **Open CORS** (`AllowAnyOrigin`) so the local frontend can call the API. In production this would be
  restricted to known origins.

## What I'd improve with more time

- A durable CRM queue using the Outbox pattern, with retry/backoff and a dead-letter path.
- A documented operations runbook.

## AI assistance disclosure

AI tools were used to:

- Generate boilerplate (the SQL script and EF configuration).
- Help with frontend styling.
- Review README.md and written-answers.md, and make them more readable.
- Assist with bug fixes and research.
- Unit test boilerplate

## brief CMS or CRM integration note.

Most CRMs authenticate via OAuth 2.0. For Dynamics 365, you register an app in Entra ID to get credentials, then exchange them for an access token. To do CRUD, you either use the official SDK — which manages auth for you — or call the REST API directly and pass the token yourself as a bearer header.
