import { Plus, Edit2, MoreVertical, Power, MessageSquare } from 'lucide-react';
import { useState } from 'react';

const agents = [
  { id: 1, name: 'Aria', specialty: 'Industrial Automation', status: 'active', conversations: 1247, satisfactionRate: 94, responseTime: '2.3s', lastActive: '5 mins ago' },
  { id: 2, name: 'Marcus', specialty: 'Energy Transition', status: 'active', conversations: 892, satisfactionRate: 91, responseTime: '2.8s', lastActive: '12 mins ago' },
  { id: 3, name: 'Priya', specialty: 'Aerospace Systems', status: 'active', conversations: 634, satisfactionRate: 96, responseTime: '2.1s', lastActive: '3 mins ago' },
  { id: 4, name: 'Atlas', specialty: 'Building Automation', status: 'active', conversations: 543, satisfactionRate: 89, responseTime: '3.2s', lastActive: '8 mins ago' },
  { id: 5, name: 'Nova', specialty: 'Digital Transformation', status: 'inactive', conversations: 421, satisfactionRate: 92, responseTime: '2.5s', lastActive: '2 days ago' },
  { id: 6, name: 'Orion', specialty: 'Leadership Development', status: 'active', conversations: 1089, satisfactionRate: 97, responseTime: '1.9s', lastActive: '1 min ago' },
];

export function AIAgents() {
  const [selectedAgent, setSelectedAgent] = useState<number | null>(null);

  return (
    <div className="p-8">
      {/* Header */}
      <div className="mb-8">
        <h2 className="text-3xl font-medium text-black tracking-tight mb-2">AI Agents</h2>
        <p className="text-sm text-[#575757] tracking-tight">Manage AI expert agents, specialties, and performance</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-4 gap-6 mb-8">
        <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
          <p className="text-xs text-[#575757] tracking-tight mb-2">Total Agents</p>
          <p className="text-3xl font-medium text-black tracking-tight">6</p>
          <p className="text-xs text-green-600 tracking-tight mt-2">5 active</p>
        </div>
        <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
          <p className="text-xs text-[#575757] tracking-tight mb-2">Total Conversations</p>
          <p className="text-3xl font-medium text-black tracking-tight">4,826</p>
          <p className="text-xs text-green-600 tracking-tight mt-2">↑ 23% this month</p>
        </div>
        <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
          <p className="text-xs text-[#575757] tracking-tight mb-2">Avg Satisfaction</p>
          <p className="text-3xl font-medium text-black tracking-tight">93%</p>
          <p className="text-xs text-green-600 tracking-tight mt-2">↑ 2% from last month</p>
        </div>
        <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
          <p className="text-xs text-[#575757] tracking-tight mb-2">Avg Response Time</p>
          <p className="text-3xl font-medium text-black tracking-tight">2.5s</p>
          <p className="text-xs text-[#575757] tracking-tight mt-2">Within target</p>
        </div>
      </div>

      {/* Actions */}
      <div className="flex items-center justify-between mb-6">
        <button className="flex items-center gap-2 px-4 py-2 bg-[#003D82] text-white text-sm tracking-tight hover:bg-[#002D62] transition-colors rounded-lg">
          <Plus className="w-4 h-4" strokeWidth={1.5} />
          Create New Agent
        </button>
      </div>

      {/* Agents Grid */}
      <div className="grid grid-cols-2 gap-6">
        {agents.map((agent) => (
          <div
            key={agent.id}
            className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6 hover:shadow-lg transition-shadow"
          >
            {/* Agent Header */}
            <div className="flex items-start justify-between mb-6">
              <div className="flex items-center gap-4">
                <div className="w-16 h-16 rounded-full bg-[#F5F5F5] flex items-center justify-center">
                  <MessageSquare className="w-8 h-8 text-[#003D82]" strokeWidth={1.5} />
                </div>
                <div>
                  <h3 className="text-xl font-medium text-black tracking-tight mb-1">{agent.name}</h3>
                  <p className="text-sm text-[#003D82] tracking-tight font-medium">{agent.specialty}</p>
                </div>
              </div>
              <div className="flex items-center gap-2">
                <span className={`inline-block px-3 py-1 rounded-full text-xs tracking-tight font-medium ${
                  agent.status === 'active' ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-700'
                }`}>
                  {agent.status}
                </span>
                <button className="p-2 hover:bg-[#F5F5F5] rounded transition-colors">
                  <MoreVertical className="w-4 h-4 text-[#575757]" strokeWidth={1.5} />
                </button>
              </div>
            </div>

            {/* Metrics */}
            <div className="grid grid-cols-2 gap-4 mb-6">
              <div>
                <p className="text-xs text-[#575757] tracking-tight mb-1">Conversations</p>
                <p className="text-2xl font-medium text-black tracking-tight">{agent.conversations.toLocaleString()}</p>
              </div>
              <div>
                <p className="text-xs text-[#575757] tracking-tight mb-1">Satisfaction Rate</p>
                <p className="text-2xl font-medium text-black tracking-tight">{agent.satisfactionRate}%</p>
              </div>
              <div>
                <p className="text-xs text-[#575757] tracking-tight mb-1">Response Time</p>
                <p className="text-lg font-medium text-black tracking-tight">{agent.responseTime}</p>
              </div>
              <div>
                <p className="text-xs text-[#575757] tracking-tight mb-1">Last Active</p>
                <p className="text-lg font-medium text-black tracking-tight">{agent.lastActive}</p>
              </div>
            </div>

            {/* Actions */}
            <div className="flex items-center gap-3 pt-4 border-t border-[rgba(0,0,0,0.08)]">
              <button className="flex-1 flex items-center justify-center gap-2 px-4 py-2 bg-[#F5F5F5] text-sm text-black tracking-tight hover:bg-[#E5E5E5] transition-colors rounded-lg">
                <Edit2 className="w-4 h-4" strokeWidth={1.5} />
                Configure
              </button>
              <button className="flex-1 flex items-center justify-center gap-2 px-4 py-2 bg-[#F5F5F5] text-sm text-black tracking-tight hover:bg-[#E5E5E5] transition-colors rounded-lg">
                <MessageSquare className="w-4 h-4" strokeWidth={1.5} />
                View Chats
              </button>
              <button className={`px-4 py-2 rounded-lg transition-colors ${
                agent.status === 'active'
                  ? 'bg-red-50 text-red-600 hover:bg-red-100'
                  : 'bg-green-50 text-green-600 hover:bg-green-100'
              }`}>
                <Power className="w-4 h-4" strokeWidth={1.5} />
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
