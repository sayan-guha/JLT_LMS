# Neo File Upload Implementation Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Allow users to upload files (CSV, PDF, text) to the Neo Agent via the UI, storing them in a custom RAG-lite backend, and giving the agent tools to query the documents.

**Architecture:** Approach A. The UI uploads files to a new `JLT.AIProxy` endpoint. The proxy parses the file, chunks it, and stores it in memory. A new tool `query_document` is registered so the LLM can query the text of the uploaded documents.

**Tech Stack:** React (Frontend), .NET 8 Minimal APIs (AI Proxy), `PdfPig` (PDF Parsing - optional for MVP), `CsvHelper` (CSV parsing - optional for MVP).

---

### Task 1: Create Document Store and Upload Endpoint in AI Proxy

**Files:**
- Create: `src/ai-proxy/JLT.AIProxy/Services/DocumentStore.cs`
- Modify: `src/ai-proxy/JLT.AIProxy/Program.cs`

**Step 1: Write the Document Store**
Create `DocumentStore.cs` as a singleton service to hold `Dictionary<string, string>` (documentId -> content) in memory. Provide methods `AddDocument(content)` -> `id` and `QueryDocument(id, query)` -> `results`.

**Step 2: Add Upload Endpoint**
In `Program.cs`, register `DocumentStore` as a singleton.
Add `app.MapPost("/api/agent/upload")` which accepts `IFormFile`, reads it into text, and calls `DocumentStore.AddDocument(content)`.

**Step 3: Run AI Proxy to verify**
Run: `dotnet run --project src/ai-proxy/JLT.AIProxy`
Expected: Server starts successfully without errors.

**Step 4: Commit**
```bash
git add src/ai-proxy/JLT.AIProxy/Services/DocumentStore.cs src/ai-proxy/JLT.AIProxy/Program.cs
git commit -m "feat: Add DocumentStore and upload endpoint"
```

---

### Task 2: Register `query_document` Tool

**Files:**
- Modify: `src/ai-proxy/JLT.AIProxy/Services/ToolRegistry.cs`
- Modify: `src/ai-proxy/JLT.AIProxy/Program.cs`

**Step 1: Add Search Endpoint**
In `Program.cs`, add `app.MapGet("/api/agent/documents/{id}/search")` which calls `DocumentStore.QueryDocument(id, query)` and returns JSON results.

**Step 2: Register Tool**
In `ToolRegistry.cs`, add `query_document` tool. Set `HttpMethod = "GET"`, `EndpointTemplate = "/api/agent/documents/{documentId}/search?q={query}"`. Wait, `ToolRegistry` calls the Backend API `JLT.API` via `backendClient` normally. If the document store is inside `AIProxy`, we need the proxy to call itself or handle this tool internally inside `OpenAIService.cs`.

*Alternative Step 2:* Register it in `ToolRegistry` with a special `HttpMethod` like `INTERNAL_RAG` and handle it in `OpenAIService.cs` directly via the `DocumentStore` singleton, similar to how we handle `UI` tools.

**Step 3: Update OpenAIService**
In `OpenAIService.cs`, inject `DocumentStore`. Intercept tools with `HttpMethod == "INTERNAL_RAG"`. Execute the `QueryDocument` method locally instead of calling the external backend.

**Step 4: Run AI Proxy to verify**
Run: `dotnet run --project src/ai-proxy/JLT.AIProxy`
Expected: Server starts successfully.

**Step 5: Commit**
```bash
git add .
git commit -m "feat: Add query_document tool and internal handler"
```

---

### Task 3: Add File Upload UI in Agent Copilot

**Files:**
- Modify: `Design/src/app/components/admin/AgentCopilot.tsx`

**Step 1: Add UI Components**
Import `Paperclip` icon from `lucide-react`. Add a hidden `<input type="file" />` and a button to trigger it next to the chat input field. 

**Step 2: Implement Upload Logic**
Add `handleFileUpload` function. When a file is selected, create `FormData` and `fetch('http://localhost:5200/api/agent/upload', { method: 'POST', body: formData })`.

**Step 3: Inject Context to Chat**
On successful upload, the endpoint returns a `documentId`. Append a message to the chat: "System: Uploaded file {filename}. Document ID is {documentId}. You can query this document." Send this to the AI Proxy so the LLM is aware.

**Step 4: Build Frontend to verify**
Run: `npm run dev` in `Design/`
Expected: Frontend compiles without errors and the upload button appears.

**Step 5: Commit**
```bash
git add Design/src/app/components/admin/AgentCopilot.tsx
git commit -m "feat: Add file upload UI and connect to AI Proxy"
```
