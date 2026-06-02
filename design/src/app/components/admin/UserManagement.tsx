import { Search, Plus, MoreVertical, Download, Filter, Loader2 } from 'lucide-react';
import { useState, useEffect } from 'react';
import { AgentEventBus } from './core/AgentEventBus';

interface UserManagementProps {
  authToken: string;
  refreshTrigger: number;
}

export function UserManagement({ authToken, refreshTrigger }: UserManagementProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [usersList, setUsersList] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function fetchUsers() {
      if (!authToken) return;
      setLoading(true);
      setError(null);
      try {
        const response = await fetch('http://localhost:5126/api/users?pageSize=100', {
          headers: {
            'Authorization': `Bearer ${authToken}`,
            'X-Tenant-Slug': 'demo'
          }
        });
        if (!response.ok) {
          throw new Error(`Failed to fetch users: status ${response.status}`);
        }
        const resData = await response.json();
        if (resData.success && resData.data && resData.data.items) {
          setUsersList(resData.data.items);
        } else {
          setUsersList([]);
        }
      } catch (err: any) {
        console.error(err);
        setError(err.message || 'Failed to fetch users');
      } finally {
        setLoading(false);
      }
    }
    fetchUsers();
  }, [authToken, refreshTrigger]);

  useEffect(() => {
    const unsubscribe = AgentEventBus.subscribe('UI_COMMAND', (cmd: any) => {
      if (cmd.action === 'ui_set_widget_filter' && cmd.payload?.module === 'users') {
        setSearchQuery(cmd.payload.searchQuery || '');
      }
    });
    return unsubscribe;
  }, []);

  const filteredUsers = usersList.filter(user => {
    const query = searchQuery.toLowerCase();
    const fullName = (user.fullName || `${user.firstName || ''} ${user.lastName || ''}`).toLowerCase();
    const email = (user.email || '').toLowerCase();
    const department = (user.department || '').toLowerCase();
    return fullName.includes(query) || email.includes(query) || department.includes(query);
  });

  const totalUsers = usersList.length;
  const activeUsers = usersList.filter(u => u.isActive).length;
  const inactiveUsers = totalUsers - activeUsers;
  const departmentsCount = new Set(usersList.map(u => u.department).filter(Boolean)).size;

  return (
    <div className="p-8">
      {/* Header */}
      <div className="mb-8">
        <h2 className="text-3xl font-medium text-black tracking-tight mb-2">User Management</h2>
        <p className="text-sm text-[#575757] tracking-tight">Manage users, roles, and permissions</p>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-4 gap-6 mb-8">
        <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
          <p className="text-xs text-[#575757] tracking-tight mb-2">Total Users</p>
          <p className="text-3xl font-medium text-black tracking-tight">{totalUsers}</p>
          <p className="text-xs text-[#575757] tracking-tight mt-2">In database</p>
        </div>
        <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
          <p className="text-xs text-[#575757] tracking-tight mb-2">Active Users</p>
          <p className="text-3xl font-medium text-black tracking-tight">{activeUsers}</p>
          <p className="text-xs text-green-600 tracking-tight mt-2">
            {totalUsers > 0 ? `${Math.round((activeUsers / totalUsers) * 100)}%` : '0%'} active rate
          </p>
        </div>
        <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
          <p className="text-xs text-[#575757] tracking-tight mb-2">Inactive Users</p>
          <p className="text-3xl font-medium text-black tracking-tight">{inactiveUsers}</p>
          <p className="text-xs text-[#575757] tracking-tight mt-2">Pending / Deactivated</p>
        </div>
        <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
          <p className="text-xs text-[#575757] tracking-tight mb-2">Departments</p>
          <p className="text-3xl font-medium text-black tracking-tight">{departmentsCount}</p>
          <p className="text-xs text-green-600 tracking-tight mt-2">Active organizational units</p>
        </div>
      </div>

      {/* Actions Bar */}
      <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6 mb-6">
        <div className="flex items-center gap-4">
          <div className="flex-1 relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-[#A3A3A3]" strokeWidth={1.5} />
            <input
              type="text"
              placeholder="Search users by name, email, or department..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-10 pr-4 py-2 bg-[#F5F5F5] text-sm text-black placeholder-[#A3A3A3] tracking-tight focus:outline-none focus:bg-[#EFEFEF] transition-colors rounded-lg"
            />
          </div>
          <button className="flex items-center gap-2 px-4 py-2 bg-[#F5F5F5] text-sm text-black tracking-tight hover:bg-[#E5E5E5] transition-colors rounded-lg">
            <Filter className="w-4 h-4" strokeWidth={1.5} />
            Filters
          </button>
          <button className="flex items-center gap-2 px-4 py-2 bg-[#F5F5F5] text-sm text-black tracking-tight hover:bg-[#E5E5E5] transition-colors rounded-lg">
            <Download className="w-4 h-4" strokeWidth={1.5} />
            Export
          </button>
          <button className="flex items-center gap-2 px-4 py-2 bg-[#003D82] text-white text-sm tracking-tight hover:bg-[#002D62] transition-colors rounded-lg">
            <Plus className="w-4 h-4" strokeWidth={1.5} />
            Add User
          </button>
        </div>
      </div>

      {/* Users Table */}
      <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg overflow-hidden relative min-h-[200px]">
        {loading && (
          <div className="absolute inset-0 bg-white/50 backdrop-blur-[1px] flex items-center justify-center z-10">
            <Loader2 className="w-8 h-8 animate-spin text-[#003D82]" />
          </div>
        )}

        {error && (
          <div className="p-6 text-center text-sm text-red-600 bg-red-50 border-b border-red-100">
            {error}
          </div>
        )}

        <table className="w-full">
          <thead className="bg-[#F5F5F5] border-b border-[rgba(0,0,0,0.08)]">
            <tr>
              <th className="text-left px-6 py-4 text-xs font-medium text-[#575757] tracking-tight">User</th>
              <th className="text-left px-6 py-4 text-xs font-medium text-[#575757] tracking-tight">Roles</th>
              <th className="text-left px-6 py-4 text-xs font-medium text-[#575757] tracking-tight">Department</th>
              <th className="text-left px-6 py-4 text-xs font-medium text-[#575757] tracking-tight">Job Title</th>
              <th className="text-left px-6 py-4 text-xs font-medium text-[#575757] tracking-tight">Location</th>
              <th className="text-left px-6 py-4 text-xs font-medium text-[#575757] tracking-tight">Status</th>
              <th className="text-left px-6 py-4 text-xs font-medium text-[#575757] tracking-tight">Actions</th>
            </tr>
          </thead>
          <tbody>
            {!loading && filteredUsers.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-6 py-10 text-center text-sm text-[#575757]">
                  No users found
                </td>
              </tr>
            ) : (
              filteredUsers.map((user) => (
                <tr key={user.id} className="border-b border-[rgba(0,0,0,0.08)] hover:bg-[#FAFAFA] transition-colors">
                  <td className="px-6 py-4">
                    <div>
                      <p className="text-sm font-medium text-black tracking-tight">{user.fullName || `${user.firstName || ''} ${user.lastName || ''}`}</p>
                      <p className="text-xs text-[#575757] tracking-tight mt-0.5">{user.email}</p>
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <p className="text-sm text-black tracking-tight">
                      {user.roles && user.roles.length > 0 ? user.roles.join(', ') : 'Learner'}
                    </p>
                  </td>
                  <td className="px-6 py-4">
                    <p className="text-sm text-black tracking-tight">{user.department || 'N/A'}</p>
                  </td>
                  <td className="px-6 py-4">
                    <p className="text-sm text-black tracking-tight">{user.jobTitle || 'N/A'}</p>
                  </td>
                  <td className="px-6 py-4">
                    <p className="text-sm text-black tracking-tight">{user.location || 'N/A'}</p>
                  </td>
                  <td className="px-6 py-4">
                    <span className={`inline-block px-3 py-1 rounded-full text-xs tracking-tight font-medium ${
                      user.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-700'
                    }`}>
                      {user.isActive ? 'active' : 'inactive'}
                    </span>
                  </td>
                  <td className="px-6 py-4">
                    <button className="p-2 hover:bg-[#F5F5F5] rounded transition-colors">
                      <MoreVertical className="w-4 h-4 text-[#575757]" strokeWidth={1.5} />
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
