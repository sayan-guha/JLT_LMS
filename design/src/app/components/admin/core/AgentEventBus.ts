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
