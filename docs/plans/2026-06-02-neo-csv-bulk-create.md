# Neo CSV Bulk Create Implementation Plan

## Overview
Retrain Neo with examples for user APIs, and implement a robust CSV bulk create pipeline.

## Tasks
1. **Task 1: Add Bulk Import to JLT.API Backend**
   - Create `BulkImportUsersCommand` and handler in `JLT.Application/Features/Users/UserCommands.cs`.
   - Add `[HttpPost("bulk-import-csv")]` in `UsersController`.
   - The endpoint receives raw CSV text, parses it, and creates users.

2. **Task 2: Register `process_user_csv` Tool in AI Proxy**
   - Add `process_user_csv` tool to `ToolRegistry.cs`.
   - Add internal handler in `OpenAIService.cs` that fetches the document from `DocumentStore` and posts it to the backend's `/api/users/bulk-import-csv`.

3. **Task 3: Retrain Neo System Prompt**
   - Update `GetSystemPrompt()` in `OpenAIService.cs`.
   - Add JSON payload examples for `list_users` and `create_user`.
   - Add instruction to provide CSV template (`Email, Password, FirstName, LastName, Role, Department, Location`).
   - Add instruction to verify CSV headers using `query_document` before calling `process_user_csv`.
