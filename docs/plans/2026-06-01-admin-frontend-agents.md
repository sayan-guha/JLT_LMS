# Admin Frontend with Multi-Agent Co-pilots — Implementation Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Create a simple and premium admin frontend dashboard for the JLT LMS, utilizing the design folder styling, which integrates conversational AI agents for User Management, Learning Content Management, and Classroom Training. The agents will interact with the admin and call the backend API endpoints to execute tasks.

**Architecture:** A static Single Page Application (SPA) located at `src/frontend`. It connects to the backend API running at `http://localhost:5126` using JWT authentication. It features a dashboard layout, a module workspace, and a modular AI agent sidebar context-synced to the current module. The AI agent parses commands, asks for confirmations, executes API calls, and prints execution logs.

**Tech Stack:** HTML5, CSS3 (Vanilla), JavaScript (ES6+), FontAwesome (for icons)

---

### Task 1: Frontend Folder Structure and Base Styling

**Files:**
- Create: `src/frontend/index.html`
- Create: `src/frontend/styles.css`
- Create: `src/frontend/app.js`

**Step 1: Create index.html shell**
Define the layout with a sidebar (navigation), a main workspace, and an Agent Co-pilot sidebar. Include login overlay.

**Step 2: Create styles.css**
Import rules and colors from `design/design-system.css`. Add dashboard layout styles, sidebar navigation, main content area, chat interface styling for agents (message bubbles, input bar, terminal log panel, typing indicator), and modern inputs.

**Step 3: Create app.js base**
Implement theme toggling, auth status check, login/logout logic, active module switching, and dynamic loading of module workspaces.

**Step 4: Verification**
Open the page in a browser and verify the visual layout and login dialog.

**Step 5: Commit**
```bash
git add src/frontend/index.html src/frontend/styles.css src/frontend/app.js
git commit -m "feat(frontend): create base admin dashboard layout and styling"
```

---

### Task 2: API Client and Auth Service

**Files:**
- Modify: `src/frontend/app.js`

**Step 1: Write base API client functions**
Create helper functions for handling auth token, tenant slug, headers, and HTTP requests (GET, POST, PUT, DELETE) with automatic header injection.

**Step 2: Implement Login UI and Backend Connection**
Hook up the login modal to the backend `/api/auth/login` endpoint. Retrieve the token and store it. Fetch available tenants from `/api/tenants` to populate the login slug list.

**Step 3: Verification**
Start the backend database and API, then test logging in via the frontend with admin credentials (`admin@demo.com` / `Admin@123!`). Confirm token is saved and dashboard becomes visible.

**Step 4: Commit**
```bash
git add src/frontend/app.js
git commit -m "feat(frontend): implement API client and authentication flow"
```

---

### Task 3: AI Agent Core Engine

**Files:**
- Create: `src/frontend/agents.js`
- Modify: `src/frontend/index.html` (include agents.js script tag)

**Step 1: Implement Agent base class**
Implement a base class `LmsAgent` that handles chat history rendering, message sending, typing animations, interactive card rendering, and execution log printing.

**Step 2: Implement User Management Agent**
Create `UserAgent` extending `LmsAgent`. Add a natural language parsing engine supporting commands like:
- "create user [name] [email]"
- "list users"
- "create group [name]"
Hook these commands up to corresponding tool actions that generate confirmation cards.

**Step 3: Verification**
Test typing "list users" in the User Agent chat. Ensure it parses the command, shows a typing indicator, prints `[API CALL] GET /api/users`, and renders the user list inside the chat.

**Step 4: Commit**
```bash
git add src/frontend/agents.js
git commit -m "feat(frontend): implement core AI agent engine and UserAgent"
```

---

### Task 4: Learning Content and Training Agents

**Files:**
- Modify: `src/frontend/agents.js`

**Step 1: Implement Learning Content Agent**
Create `ContentAgent` extending `LmsAgent` supporting:
- "list content" / "show courses"
- "create article [title] [url]"
- "delete content [id]"
Connect it to `/api/learning-content` endpoints.

**Step 2: Implement Classroom Training Agent**
Create `TrainingAgent` extending `LmsAgent` supporting:
- "list templates" / "show templates"
- "create template [name]"
- "create batch [name] from template [templateId]"
Connect it to `/api/training-templates` and `/api/training-batches` endpoints.

**Step 3: Verification**
Test typing content commands in the chat and verify successful API execution and output rendering.

**Step 4: Commit**
```bash
git add src/frontend/agents.js
git commit -m "feat(frontend): add LearningContent and ClassroomTraining agents"
```

---

### Task 5: Dynamic Workspace Dashboard and Lists

**Files:**
- Modify: `src/frontend/index.html`
- Modify: `src/frontend/app.js`

**Step 1: Build dashboard content grids**
Implement clean data lists and metric cards in the main workspace for the active module (e.g. Users Table, Courses Grid, Batch Calendar List). Clicking items in these lists should auto-fill commands or parameters in the agent's chat panel.

**Step 2: Verification**
Verify that the workspace matches the premium design elements in `design/components.html` and updates dynamically when switching modules.

**Step 3: Commit**
```bash
git add src/frontend/index.html src/frontend/app.js
git commit -m "feat(frontend): implement dynamic workspace tables and metrics dashboard"
```
