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
