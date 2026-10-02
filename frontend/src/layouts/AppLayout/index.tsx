import React, { useState } from 'react';
import { Outlet } from 'react-router-dom';
import { TopBar } from '../../components/navigation/TopBar';
import { Sidebar } from '../../components/navigation/Sidebar';
import { CommandPalette } from '../../components/navigation/CommandPalette';

export const AppLayout: React.FC = () => {
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);
  const [isMobileSidebarOpen, setIsMobileSidebarOpen] = useState(false);
  const [isCommandPaletteOpen, setIsCommandPaletteOpen] = useState(false);
  const [activeTenant, setActiveTenant] = useState('acme-salon');

  return (
    <div className="flex h-screen bg-[#0A0C13] text-[#ECEFFE] overflow-hidden font-sans">
      {/* Grouped Sidebar */}
      <Sidebar
        isCollapsed={isSidebarCollapsed}
        onToggleCollapse={() => setIsSidebarCollapsed(!isSidebarCollapsed)}
        isMobileOpen={isMobileSidebarOpen}
        onCloseMobile={() => setIsMobileSidebarOpen(false)}
      />

      {/* Main Workspace Area */}
      <div className="flex-1 flex flex-col min-w-0 h-full overflow-hidden">
        {/* Top Navigation Bar */}
        <TopBar
          onOpenCommandPalette={() => setIsCommandPaletteOpen(true)}
          activeOrg={activeTenant}
          onOrgChange={setActiveTenant}
        />

        {/* Content Outlet */}
        <main className="flex-1 overflow-y-auto p-6 lg:p-8 bg-[#0A0C13]">
          <Outlet context={{ activeTenant }} />
        </main>
      </div>

      {/* Global Command Palette (Ctrl + K) */}
      <CommandPalette
        isOpen={isCommandPaletteOpen}
        onClose={() => setIsCommandPaletteOpen(false)}
      />
    </div>
  );
};
