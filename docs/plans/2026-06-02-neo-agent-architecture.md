# Neo Agent Architecture Implementation Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Re-architect the Admin Panel layout to a 3-panel system (Nav, Central Widgets, Neo Agent) and implement a Context/EventBus pattern for decoupled agent-to-widget communication.

**Architecture:** Use `ModuleContext` to store the active module and page state, enabling Neo to provide context-aware suggestions. Use `AgentEventBus` for Neo to publish data mutation events (like bulk CSV uploads) so standalone central widgets can auto-refresh without prop drilling.

**Tech Stack:** React, TailwindCSS, TypeScript (No external state libraries).

---

### Task 1: Create ModuleContext and AgentEventBus

**Files:**
- Create: `Design/src/app/components/admin/core/ModuleContext.tsx`
- Create: `Design/src/app/components/admin/core/AgentEventBus.ts`

**Step 1: Write AgentEventBus implementation**

Create `Design/src/app/components/admin/core/AgentEventBus.ts` with the following content:

```typescript
type EventCallback = (data?: any) => void;

export const AgentEventBus = {
  listeners: {} as Record<string, EventCallback[]>,
  
  subscribe(event: string, callback: EventCallback) {
    if (!this.listeners[event]) {
      this.listeners[event] = [];
    }
    this.listeners[event].push(callback);
    
    // Return unsubscribe function
    return () => {
      this.listeners[event] = this.listeners[event].filter(cb => cb !== callback);
    };
  },
  
  publish(event: string, data?: any) {
    if (this.listeners[event]) {
      this.listeners[event].forEach(cb => cb(data));
    }
  }
};
```

**Step 2: Write ModuleContext implementation**

Create `Design/src/app/components/admin/core/ModuleContext.tsx` with the following content:

```typescript
import React, { createContext, useContext, useState, ReactNode } from 'react';

type ModuleContextType = {
  activeModule: string;
  setActiveModule: (module: string) => void;
  pageContext: any;
  setPageContext: (context: any) => void;
};

const ModuleContext = createContext<ModuleContextType | undefined>(undefined);

export function ModuleProvider({ children }: { children: ReactNode }) {
  const [activeModule, setActiveModule] = useState<string>('users');
  const [pageContext, setPageContext] = useState<any>({});
  
  return (
    <ModuleContext.Provider value={{ activeModule, setActiveModule, pageContext, setPageContext }}>
      {children}
    </ModuleContext.Provider>
  );
}

export const useModuleContext = () => {
  const context = useContext(ModuleContext);
  if (!context) {
    throw new Error("useModuleContext must be used within a ModuleProvider");
  }
  return context;
};
```

**Step 3: Commit**

```bash
git add Design/src/app/components/admin/core
git commit -m "feat: add AgentEventBus and ModuleContext for admin architecture"
```

---

### Task 2: Refactor AdminPanel Layout

**Files:**
- Modify: `Design/src/app/components/admin/AdminPanel.tsx`

**Step 1: Apply ModuleProvider and update layout structure**

Modify `AdminPanel.tsx` to import and wrap everything in `<ModuleProvider>`. Restructure the UI to have a thin left nav (e.g., `w-20` or icon-only for modules), a central flex area, and a large right panel for Neo (`w-[40%]`).
*Note: Due to file length, replace the entire return block in `AdminPanel.tsx` and strip out the old activeSection state, passing that responsibility to `ModuleContext` inside a wrapper or moving the internal state.*

Let's restructure `AdminPanel.tsx` carefully. We will create a wrapper `AdminPanelWrapper` and an inner `AdminPanelInner`.

```tsx
// Inside Design/src/app/components/admin/AdminPanel.tsx
// Add imports:
import { ModuleProvider, useModuleContext } from './core/ModuleContext';

// Replace the main export with a Wrapper:
export function AdminPanel({ onBack }: AdminPanelProps) {
  return (
    <ModuleProvider>
      <AdminPanelInner onBack={onBack} />
    </ModuleProvider>
  );
}

function AdminPanelInner({ onBack }: AdminPanelProps) {
  const { activeModule, setActiveModule } = useModuleContext();
  const [authToken, setAuthToken] = useState('');
  const [tenantSlug] = useState('demo');

  // ... keep the auto login useEffect ...

  const menuItems = [
    { id: 'users', label: 'User Management', icon: Users },
    { id: 'content', label: 'Learning Content', icon: FileText },
    { id: 'classroom', label: 'Classroom', icon: Network },
    { id: 'assessments', label: 'Assessments', icon: FileText }
  ];

  const renderSection = () => {
    switch (activeModule) {
      case 'users': return <UserManagement authToken={authToken} refreshTrigger={0} />;
      case 'content': return <ContentManagement authToken={authToken} refreshTrigger={0} />;
      default: return <div className="p-8">Module coming soon</div>;
    }
  };

  return (
    <div className="flex h-screen bg-[#FAFAFA] overflow-hidden">
      {/* Thin Left Sidebar for Navigation */}
      <div className="w-64 bg-white border-r border-[rgba(0,0,0,0.08)] flex flex-col flex-shrink-0">
        <div className="p-6 border-b border-[rgba(0,0,0,0.08)]">
           <h1 className="text-xl font-medium text-black tracking-tight">Admin</h1>
        </div>
        <nav className="flex-1 p-4">
          <ul className="space-y-1">
            {menuItems.map((item) => {
              const Icon = item.icon;
              const isActive = activeModule === item.id;
              return (
                <li key={item.id}>
                  <button
                     onClick={() => setActiveModule(item.id)}
                     className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg transition-colors ${
                      isActive ? 'bg-[#003D82] text-white' : 'text-[#575757] hover:bg-[#F5F5F5]'
                     }`}
                  >
                    <Icon className="w-5 h-5" strokeWidth={1.5} />
                    <span className="text-sm font-medium tracking-tight text-left">{item.label}</span>
                  </button>
                </li>
              );
            })}
          </ul>
        </nav>
      </div>

      {/* Central Panel for Widgets */}
      <div className="flex-1 overflow-auto bg-[#FAFAFA]">
        {renderSection()}
      </div>

      {/* Right Sidebar: 40% Width Neo Agent */}
      <div className="w-[40%] bg-white border-l border-[rgba(0,0,0,0.08)] flex-shrink-0">
        <AgentCopilot
          activeSection={activeModule}
          authToken={authToken}
          tenantSlug={tenantSlug}
          onRefresh={() => {}}
        />
      </div>
    </div>
  );
}
```

**Step 2: Commit**

```bash
git add Design/src/app/components/admin/AdminPanel.tsx
git commit -m "refactor: restructure AdminPanel to 3-panel layout and use ModuleContext"
```

---

### Task 3: Refactor Widgets to Use EventBus

**Files:**
- Modify: `Design/src/app/components/admin/UserManagement.tsx`
- Modify: `Design/src/app/components/admin/ContentManagement.tsx`

**Step 1: Update UserManagement.tsx**

Remove the `refreshTrigger` dependency logic and add `AgentEventBus.subscribe`.

```tsx
import { AgentEventBus } from './core/AgentEventBus';

// Inside UserManagement component:
useEffect(() => {
  const unsubscribe = AgentEventBus.subscribe('REFRESH_USERS', () => {
    fetchUsers();
  });
  return () => unsubscribe();
}, [authToken]); 
// ensure fetchUsers is stable or wrapped in useCallback if needed
```

**Step 2: Update ContentManagement.tsx**

```tsx
import { AgentEventBus } from './core/AgentEventBus';

// Inside ContentManagement component:
useEffect(() => {
  const unsubscribe = AgentEventBus.subscribe('REFRESH_CONTENT', () => {
    fetchContent();
  });
  return () => unsubscribe();
}, [authToken]);
```

**Step 3: Commit**

```bash
git add Design/src/app/components/admin/UserManagement.tsx Design/src/app/components/admin/ContentManagement.tsx
git commit -m "refactor: widgets now listen to AgentEventBus for refresh events"
```

---

### Task 4: Make Neo Agent Context-Aware & Event-Driven

**Files:**
- Modify: `Design/src/app/components/admin/AgentCopilot.tsx`

**Step 1: Integrate context and event publishing**

Update `AgentCopilot` to publish events when destructive actions are completed.

```tsx
import { AgentEventBus } from './core/AgentEventBus';
import { useModuleContext } from './core/ModuleContext';

// Inside AgentCopilot:
const { activeModule } = useModuleContext();

// Find the area where a confirmation or action succeeds (e.g., executeAction)
// In executeAction, after a successful API call:
if (activeModule === 'users') {
   AgentEventBus.publish('REFRESH_USERS');
} else if (activeModule === 'content') {
   AgentEventBus.publish('REFRESH_CONTENT');
}
```

*Note: You will also want to replace the `activeSection` prop with reading directly from `activeModule` to conditionally render Neo's suggested starting prompts (e.g., "Upload a CSV of users" vs "Create a new course").*

**Step 2: Commit**

```bash
git add Design/src/app/components/admin/AgentCopilot.tsx
git commit -m "feat: make Neo Agent context-aware and emit refresh events"
```

---
