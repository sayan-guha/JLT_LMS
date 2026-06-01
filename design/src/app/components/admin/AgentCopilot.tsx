import { useState, useEffect, useRef } from 'react';
import { Send, Bot, User, Check, X, ShieldAlert, Loader2, Sparkles } from 'lucide-react';

interface AgentCopilotProps {
  activeSection: string;
  authToken: string;
  tenantSlug: string;
  onRefresh: (section: string) => void;
}

interface Message {
  id: string;
  sender: 'agent' | 'user' | 'system' | 'log';
  text: string;
  time: string;
  type?: 'text' | 'confirmation' | 'error';
  confirmation?: {
    toolCallId: string;
    action: string;
    summary: string;
    parsedArgs: any;
  };
}

const AGENTS_METADATA: Record<string, { name: string; role: string; intro: string }> = {
  users: {
    name: 'Aria',
    role: 'User Management Expert',
    intro: 'Hello! I am Aria, your User Management assistant. I can help you list users, register new accounts, assign roles, or manage user groups. How can I help you today?'
  },
  content: {
    name: 'Marcus',
    role: 'Learning Content Expert',
    intro: 'Hello! I am Marcus, your Learning Content assistant. I can help you browse training materials, add new Document drafts, update content lifecycles, or delete drafts. What can I do for you?'
  },
  skills: {
    name: 'Priya',
    role: 'Skill Structure Expert',
    intro: 'Hello! I am Priya, your Skill Structure assistant. I can help you organize skills, subject topics, competencies, and map them to users. What would you like to explore?'
  },
  agents: {
    name: 'Nova',
    role: 'Meta-Agent Architect',
    intro: 'Hello! I am Nova, the Agent Orchestrator. I manage the settings and configuration profiles of Aria, Marcus, Priya, and Orion. What details would you like to review?'
  },
  analytics: {
    name: 'Orion',
    role: 'Analytics & Insights Expert',
    intro: 'Hello! I am Orion. I track and analyze learning platform engagement, course completions, and assessment metrics. How can I help you extract insights?'
  }
};

export function AgentCopilot({ activeSection, authToken, tenantSlug, onRefresh }: AgentCopilotProps) {
  const meta = AGENTS_METADATA[activeSection] || {
    name: 'JLT Assistant',
    role: 'System AI Co-pilot',
    intro: 'Hello! How can I assist you with JLT platform administration today?'
  };

  const [messages, setMessages] = useState<Message[]>([]);
  const [inputText, setInputText] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [conversationId, setConversationId] = useState(() => Guid());
  const messagesEndRef = useRef<HTMLDivElement>(null);

  function Guid() {
    return 'xxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
      var r = Math.random() * 16 | 0, v = c === 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
  }

  // Reset chat context on switching sections
  useEffect(() => {
    setConversationId(Guid());
    setMessages([
      {
        id: Guid(),
        sender: 'agent',
        text: meta.intro,
        time: new Date().toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: false })
      }
    ]);
  }, [activeSection]);

  // Scroll to bottom
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isLoading]);

  const addLogMessage = (text: string) => {
    setMessages((prev) => [
      ...prev,
      {
        id: Guid(),
        sender: 'log',
        text,
        time: new Date().toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: false })
      }
    ]);
  };

  const handleSend = async () => {
    if (!inputText.trim() || isLoading) return;

    const userText = inputText;
    setInputText('');
    setIsLoading(true);

    const userMsg: Message = {
      id: Guid(),
      sender: 'user',
      text: userText,
      time: new Date().toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: false })
    };

    setMessages((prev) => [...prev, userMsg]);

    try {
      addLogMessage(`[AI Gateway] Posting message for Conversation ID: ${conversationId}`);
      
      const response = await fetch('http://localhost:5200/api/agent/chat', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': authToken ? `Bearer ${authToken}` : '',
          'X-Tenant-Slug': tenantSlug || 'demo'
        },
        body: JSON.stringify({
          conversationId,
          message: userText
        })
      });

      if (!response.ok) {
        throw new Error(`Proxy returned status ${response.status}`);
      }

      const result = await response.json();
      processResponse(result);
    } catch (err: any) {
      console.error(err);
      setMessages((prev) => [
        ...prev,
        {
          id: Guid(),
          sender: 'agent',
          text: `An error occurred connecting to the AI Gateway: ${err.message}`,
          time: new Date().toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: false }),
          type: 'error'
        }
      ]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleConfirmation = async (toolCallId: string, approved: boolean) => {
    setIsLoading(true);
    addLogMessage(`[AI Gateway] Sending user confirmation: ${approved ? 'APPROVED' : 'DECLINED'}`);

    try {
      const response = await fetch('http://localhost:5200/api/agent/chat', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': authToken ? `Bearer ${authToken}` : '',
          'X-Tenant-Slug': tenantSlug || 'demo'
        },
        body: JSON.stringify({
          conversationId,
          confirmAction: {
            toolCallId,
            approved
          }
        })
      });

      if (!response.ok) {
        throw new Error(`Proxy returned status ${response.status}`);
      }

      const result = await response.json();
      processResponse(result);

      if (approved) {
        // Trigger center panel workspace reload
        onRefresh(activeSection);
      }
    } catch (err: any) {
      console.error(err);
      setMessages((prev) => [
        ...prev,
        {
          id: Guid(),
          sender: 'agent',
          text: `Failed to submit confirmation: ${err.message}`,
          time: new Date().toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: false }),
          type: 'error'
        }
      ]);
    } finally {
      setIsLoading(false);
    }
  };

  const processResponse = (result: any) => {
    const time = new Date().toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: false });
    
    if (result.type === 'confirmation') {
      const conf = result.confirmation;
      addLogMessage(`[AI Gateway] Intercepted write action: ${conf.action}`);
      setMessages((prev) => [
        ...prev,
        {
          id: Guid(),
          sender: 'agent',
          text: `I need your approval to proceed with this action.`,
          time,
          type: 'confirmation',
          confirmation: {
            toolCallId: conf.toolCallId,
            action: conf.action,
            summary: conf.summary,
            parsedArgs: conf.parsedArgs
          }
        }
      ]);
    } else if (result.type === 'text') {
      addLogMessage(`[AI Gateway] Text response received.`);
      setMessages((prev) => [
        ...prev,
        {
          id: Guid(),
          sender: 'agent',
          text: result.content || "Operation completed.",
          time,
          type: 'text'
        }
      ]);
    } else {
      setMessages((prev) => [
        ...prev,
        {
          id: Guid(),
          sender: 'agent',
          text: result.content || "Something went wrong.",
          time,
          type: 'error'
        }
      ]);
    }
  };

  return (
    <div className="flex flex-col h-full bg-white border-l border-[rgba(0,0,0,0.08)]">
      {/* Top Bar Header */}
      <div className="p-4 border-b border-[rgba(0,0,0,0.08)] flex items-center gap-3 bg-[#FAFAFA]">
        <div className="w-10 h-10 rounded-full bg-[#003D82] flex items-center justify-center text-white">
          <Bot className="w-5 h-5" strokeWidth={1.5} />
        </div>
        <div>
          <h3 className="text-sm font-semibold text-black tracking-tight flex items-center gap-1.5">
            {meta.name}
            <span className="inline-flex items-center rounded bg-blue-50 px-1.5 py-0.5 text-2xs font-medium text-blue-700 ring-1 ring-inset ring-blue-700/10">
              Agent
            </span>
          </h3>
          <p className="text-xs text-[#575757] tracking-tight">{meta.role}</p>
        </div>
      </div>

      {/* Message & Execution Logs Area */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4 bg-[#FCFCFC]">
        {messages.map((msg) => {
          if (msg.sender === 'log') {
            return (
              <div key={msg.id} className="text-3xs font-mono text-gray-400 bg-gray-50/50 p-1.5 border border-dashed border-gray-100 rounded">
                {msg.text}
              </div>
            );
          }

          const isUser = msg.sender === 'user';
          return (
            <div key={msg.id} className={`flex flex-col ${isUser ? 'items-end' : 'items-start'}`}>
              {/* Bubble wrapper */}
              <div
                className={`max-w-[85%] rounded-lg px-3.5 py-2.5 text-sm tracking-tight leading-relaxed shadow-sm ${
                  isUser
                    ? 'bg-[#003D82] text-white rounded-br-none'
                    : msg.type === 'error'
                    ? 'bg-red-50 text-red-800 border border-red-100 rounded-bl-none'
                    : 'bg-white border border-[rgba(0,0,0,0.08)] text-black rounded-bl-none'
                }`}
              >
                <div className="whitespace-pre-line">{msg.text}</div>

                {/* Confirmation Box inside Bubble */}
                {msg.type === 'confirmation' && msg.confirmation && (
                  <div className="mt-4 p-3 bg-slate-50 border border-slate-200 rounded-md text-slate-800 space-y-3">
                    <div className="flex items-start gap-2">
                      <ShieldAlert className="w-5 h-5 text-amber-600 flex-shrink-0 mt-0.5" />
                      <div>
                        <p className="text-xs font-semibold text-slate-900">Pending Authorization</p>
                        <p className="text-xs mt-1 text-slate-600 leading-normal">{msg.confirmation.summary}</p>
                      </div>
                    </div>
                    
                    <div className="flex items-center gap-2 pt-1 border-t border-slate-100">
                      <button
                        onClick={() => handleConfirmation(msg.confirmation!.toolCallId, true)}
                        className="flex-1 flex items-center justify-center gap-1.5 px-3 py-1.5 bg-[#003D82] hover:bg-[#002D62] text-white text-xs font-medium rounded transition-colors"
                      >
                        <Check className="w-3.5 h-3.5" />
                        Approve
                      </button>
                      <button
                        onClick={() => handleConfirmation(msg.confirmation!.toolCallId, false)}
                        className="flex-1 flex items-center justify-center gap-1.5 px-3 py-1.5 bg-white hover:bg-slate-100 text-slate-700 border border-slate-200 text-xs font-medium rounded transition-colors"
                      >
                        <X className="w-3.5 h-3.5" />
                        Decline
                      </button>
                    </div>
                  </div>
                )}
              </div>
              <span className="text-3xs text-[#A3A3A3] mt-1.5 px-1">{msg.time}</span>
            </div>
          );
        })}

        {/* Typing indicator */}
        {isLoading && (
          <div className="flex items-center gap-2 text-xs text-[#575757] italic pl-2">
            <Loader2 className="w-3.5 h-3.5 animate-spin text-[#003D82]" />
            {meta.name} is thinking...
          </div>
        )}
        <div ref={messagesEndRef} />
      </div>

      {/* Input Box Footer */}
      <div className="p-3 border-t border-[rgba(0,0,0,0.08)] bg-white">
        <div className="flex items-center gap-2">
          <input
            type="text"
            value={inputText}
            onChange={(e) => setInputText(e.target.value)}
            onKeyPress={(e) => e.key === 'Enter' && handleSend()}
            disabled={isLoading}
            placeholder={`Ask ${meta.name} to execute a task...`}
            className="flex-1 min-w-0 px-3 py-2 bg-[#F5F5F5] text-xs text-black placeholder-[#A3A3A3] tracking-tight focus:outline-none focus:bg-[#EFEFEF] transition-colors rounded-md"
          />
          <button
            onClick={handleSend}
            disabled={isLoading || !inputText.trim()}
            className="w-8 h-8 rounded-md bg-[#003D82] hover:bg-[#002D62] disabled:bg-slate-100 disabled:text-slate-400 flex items-center justify-center transition-colors text-white flex-shrink-0"
          >
            <Send className="w-4 h-4" strokeWidth={1.5} />
          </button>
        </div>
        <div className="flex items-center justify-between mt-2 px-1 text-4xs text-[#A3A3A3] tracking-wider uppercase font-semibold">
          <span className="flex items-center gap-0.5">
            <Sparkles className="w-2.5 h-2.5 text-[#003D82]" />
            GPT-5.2 Model
          </span>
          <span>Tenant Context: {tenantSlug || 'demo'}</span>
        </div>
      </div>
    </div>
  );
}
