import { useState, useEffect } from 'react';
import { Users, Network, FileText, Bot, BarChart3, ArrowLeft } from 'lucide-react';
import { UserManagement } from './UserManagement';
import { SkillStructure } from './SkillStructure';
import { ContentManagement } from './ContentManagement';
import { AIAgents } from './AIAgents';
import { Analytics } from './Analytics';
import { AgentCopilot } from './AgentCopilot';
import { AgentEventBus } from './core/AgentEventBus';
import { AgentResultWidget } from './AgentResultWidget';

type AdminSection = 'users' | 'skills' | 'content' | 'agents' | 'analytics';

interface AdminPanelProps {
  onBack?: () => void;
}

export function AdminPanel({ onBack }: AdminPanelProps) {
  const [activeSection, setActiveSection] = useState<AdminSection>('users');
  const [authToken, setAuthToken] = useState('');
  const [tenantSlug] = useState('demo');
  const [refreshTrigger, setRefreshTrigger] = useState<Record<string, number>>({});
  const [customView, setCustomView] = useState<{title: string, columns: string[], data: any[]} | null>(null);

  const menuItems = [
    { id: 'users' as AdminSection, label: 'User Management', icon: Users },
    { id: 'skills' as AdminSection, label: 'Skill Structure', icon: Network },
    { id: 'content' as AdminSection, label: 'Content', icon: FileText },
    { id: 'agents' as AdminSection, label: 'AI Agents', icon: Bot },
    { id: 'analytics' as AdminSection, label: 'Analytics', icon: BarChart3 },
  ];

  const handleRefresh = (section: string) => {
    setRefreshTrigger(prev => ({
      ...prev,
      [section]: (prev[section] || 0) + 1
    }));
  };

  useEffect(() => {
    const unsubscribe = AgentEventBus.subscribe('UI_COMMAND', (cmd: any) => {
      if (cmd.action === 'ui_render_custom_view' && cmd.payload) {
        setCustomView({
          title: cmd.payload.title,
          columns: cmd.payload.columns,
          data: cmd.payload.data
        });
      }
    });
    return unsubscribe;
  }, []);

  useEffect(() => {
    async function performAutoLogin() {
      try {
        // 1. Resolve demo tenant ID
        const tenantResp = await fetch('http://localhost:5126/api/tenants?searchTerm=demo');
        const tenantData = await tenantResp.json();
        let tenantId = '00000000-0000-0000-0000-000000000000';
        if (tenantData.success && tenantData.data && tenantData.data.items) {
          const demoTenant = tenantData.data.items.find((t: any) => t.slug === 'demo');
          if (demoTenant) {
            tenantId = demoTenant.id;
          }
        }

        // 2. Perform direct admin login
        const loginResp = await fetch('http://localhost:5126/api/auth/login', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', 'X-Tenant-Slug': 'demo' },
          body: JSON.stringify({
            email: 'admin@demo.com',
            password: 'Admin@123!',
            tenantId
          })
        });
        const loginData = await loginResp.json();
        if (loginData.success && loginData.data && loginData.data.accessToken) {
          setAuthToken(loginData.data.accessToken);
          console.log("Admin Panel auto-authenticated successfully!");
        }
      } catch (err) {
        console.error("Failed to perform auto login in admin panel:", err);
      }
    }

    performAutoLogin();
  }, []);

  const renderSection = () => {
    if (customView) {
      return (
        <AgentResultWidget
          title={customView.title}
          columns={customView.columns}
          data={customView.data}
          onDismiss={() => setCustomView(null)}
        />
      );
    }

    switch (activeSection) {
      case 'users':
        return <UserManagement authToken={authToken} refreshTrigger={refreshTrigger.users || 0} />;
      case 'skills':
        return <SkillStructure />;
      case 'content':
        return <ContentManagement authToken={authToken} refreshTrigger={refreshTrigger.content || 0} />;
      case 'agents':
        return <AIAgents />;
      case 'analytics':
        return <Analytics />;
      default:
        return <UserManagement authToken={authToken} refreshTrigger={refreshTrigger.users || 0} />;
    }
  };

  return (
    <div className="flex h-screen bg-[#FAFAFA]">
      {/* Sidebar */}
      <div className="w-64 bg-white border-r border-[rgba(0,0,0,0.08)] flex flex-col">
        {/* Logo/Title */}
        <div className="p-6 border-b border-[rgba(0,0,0,0.08)]">
          <h1 className="text-xl font-medium text-black tracking-tight">Admin Panel</h1>
          <p className="text-xs text-[#575757] tracking-tight mt-1">Talent Development Platform</p>
        </div>

        {/* Menu Items */}
        <nav className="flex-1 p-4">
          <ul className="space-y-1">
            {menuItems.map((item) => {
              const Icon = item.icon;
              const isActive = activeSection === item.id;

              return (
                <li key={item.id}>
                  <button
                     onClick={() => setActiveSection(item.id)}
                     className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg transition-colors ${
                      isActive
                        ? 'bg-[#003D82] text-white'
                        : 'text-[#575757] hover:bg-[#F5F5F5]'
                     }`}
                  >
                    <Icon className="w-5 h-5" strokeWidth={1.5} />
                    <span className="text-sm font-medium tracking-tight">{item.label}</span>
                  </button>
                </li>
              );
            })}
          </ul>
        </nav>

        {/* Footer */}
        <div className="p-4 border-t border-[rgba(0,0,0,0.08)]">
          {onBack && (
            <button
              onClick={onBack}
              className="w-full flex items-center gap-2 px-4 py-2 mb-3 text-sm text-[#575757] hover:bg-[#F5F5F5] rounded-lg transition-colors"
            >
              <ArrowLeft className="w-4 h-4" strokeWidth={1.5} />
              Back to Platform
            </button>
          )}
          <p className="text-xs text-[#A3A3A3] tracking-tight">
            Admin v1.0 • Honeywell
          </p>
        </div>
      </div>

      {/* Main Content */}
      <div className="flex-1 overflow-auto">
        {renderSection()}
      </div>

      {/* Right Sidebar: AI Agent Copilot */}
      <div className="w-96 h-full flex-shrink-0">
        <AgentCopilot
          activeSection={activeSection}
          authToken={authToken}
          tenantSlug={tenantSlug}
          onRefresh={handleRefresh}
        />
      </div>
    </div>
  );
}
