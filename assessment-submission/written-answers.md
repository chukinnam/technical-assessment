**Technical Assessment**

**Course Inquiry System**

Troubleshooting · Security · Accessibility · Code Quality

**Prepared by Leo Chu**
June 2026

---

# 1. Troubleshooting

A staff member reports that a course inquiry submitted through the public website is not appearing in the system. The approach below moves from gathering context, to inspecting each layer, to confirming where the failure occurs and preventing it from recurring.

## 1.1 Logs and systems to check

Start by getting specifics from the staff member: which inquiry (name, email, or course) and roughly when it was submitted.

- **Frontend** — Open the browser dev tools on the public form and confirm whether the submission succeeded (a 201 response). A 4xx means validation rejected the request; a network error means it never reached the API. On the admin page, also confirm with staff whether the list is being filtered by status, since that is part of the application design.

- **Backend / API** — Review the server or cloud log files to identify the error being thrown (for example, a validation error or a missing request body). Use Postman to exercise the POST Inquiry and GET Inquiry endpoints directly.

- **Database** — Query the table to confirm whether the row actually exists.

- **CMS** — Check the CMS log table and the record's data status, and review the CMS system log.

## 1.2 Isolating the layer

Begin by reproducing the issue locally — submit the inquiry through the form, then retrieve it.

- **Frontend** — In the dev tools, inspect the POST request header and payload, then send the same request directly through Postman. If the bad data reproduces without the UI in the path, the frontend is ruled out and the cause lies in the backend, database, CMS, or caching. Also check the GET request: if it returns null or an error, the issue is on the backend.

- **Backend** — Review the log file to identify the error. A 500 points to a backend exception. If there is no error log and every request and response looks correct, it is likely a logic error — use the IDE debugger to step through and trace the data state, then repeat for the GET request.

- **Database** — Query the database directly and compare against the user's input. If the database holds the correct data but the GET API returns something different, the cause is a backend transformation, mapping, or EF Core issue. If the database holds incorrect or missing data even though the backend passed the correct values, a background trigger or process may be altering the data.

- **Caching** — Open the public website in an incognito window (or hard-refresh); if the new inquiry now appears, it is a browser caching issue. Also check whether the CDN is serving stale content by hitting the origin directly.

## 1.3 SQL queries

*Does the specific inquiry exist?*

```sql
SELECT * FROM CourseInquiries
WHERE Email = 'user@mail.com'
ORDER BY CreatedDate DESC;
```

*Was anything created very recently?*

```sql
SELECT * FROM CourseInquiries
WHERE CreatedDate >= DATEADD(DAY, -1, SYSUTCDATETIME());
```

*Check the log table against the inquiry.*

```sql
SELECT * FROM LogTable AS L
INNER JOIN CourseInquiries AS C ON L.CourseID = C.Id
WHERE L.Status = 'Attempted' OR L.Status = 'Pending';
```

## 1.4 Communicating progress to non-technical stakeholders

Use plain language and lead with impact rather than jargon. Walk through the process step by step with the staff member, confirming their understanding at each stage and inviting questions wherever something is unclear.

## 1.5 Preventing recurrence

- Add end-to-end tests covering both submission and retrieval.

- Provide clear validation and error messages so invalid submissions never reach the backend.

- Notify an administrator automatically if the CRM sync fails.

# 2. Security

- **Input validation** — Validate every field on the server — required fields, format, and length limits. On failure, return a 400 response with problem details. Sanitize all user-submitted data (form text, file uploads) to remove security threats. The client validates as well, using built-in checks or a trusted third-party library.

- **SQL injection** — All data access goes through EF Core, which generates parameterized queries, so user input is never concatenated into SQL. Any raw SQL also uses parameters rather than string interpolation.

- **Authentication / authorization** — Use SSO, OAuth, or JWT, restricted to staff roles. The public submit endpoint can remain anonymous but should be rate-limited and protected with CAPTCHA to prevent spam and abuse.

- **Error handling** — Return only the necessary error message to the client, and never leak full exceptions or internal details.

- **Logging** — Use structured logs with masked email addresses and sensitive personal information, recording an error code rather than the name, full email, or message body. Never log secrets or tokens.

- **Sensitive data handling** — Serve everything over HTTPS and keep secret keys in cloud key storage such as Azure Key Vault rather than `appsettings.json`. Never handle credit card data directly in the backend — call the payment SDK and work only with a temporary transaction token.

# 3. Accessibility

- **Semantic HTML** — Use the correct element for each purpose: `<button>` for click actions (it provides focus and screen-reader semantics) and `<a>` for navigation. Follow a proper heading hierarchy (`<h1>` then `<h2>`) and use `<section>` to separate content; these also improve SEO.

- **Responsive design** — Ensure pages adapt their layout, content, and images to any screen size or orientation, using CSS flexbox, grid, and media queries.

- **Images and media** — Provide descriptive `alt` text for meaningful images and `alt=""` for decorative ones so screen readers skip them. Add captions for video and transcripts for audio.

# 4. Code Quality

The solution uses a layered structure with clear responsibilities. Separation of concerns keeps each piece small, readable, and unit-testable in isolation — including the service and the CRM connector.

- **Controllers** — Handle HTTP only (routing, status codes, model binding) and stay thin.

- **Services** — Hold the business logic: creating inquiries, filtering, status updates, the soft-delete rule, and triggering the CRM sync.

- **Data** — The EF Core persistence layer.

- **Models** — Give the data a typed shape that the controller, service, and database layers can share.

- **DTOs** — Kept separate from the models, so the API never exposes or accepts the persistence model directly.

- **CRM integration** — Isolated behind an interface and run through a background worker, with structured errors and retry.
