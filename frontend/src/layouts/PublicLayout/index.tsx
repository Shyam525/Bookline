import React, { useState } from 'react';
import { Outlet, Link } from 'react-router-dom';
import { Search, Command } from 'lucide-react';
import { CommandPalette } from '../../components/navigation/CommandPalette';

export const PublicLayout: React.FC = () => {
  const [isCommandPaletteOpen, setIsCommandPaletteOpen] = useState(false);

  return (
    <div className="min-h-screen bg-[#0A0C13] text-[#ECEFFE] flex flex-col">
      <header className="h-16 bg-[#111520] border-b border-[#212638] px-8 flex items-center justify-between">
        <Link to="/" className="flex items-center gap-3">
          <div className="w-8 h-8 rounded-lg bg-[#E8546A] flex items-center justify-center font-heading font-bold text-white text-lg">
            B
          </div>
          <span className="font-heading text-xl font-bold tracking-tight text-white">BOOKLINE</span>
        </Link>

        {/* Global Search Button */}
        <button
          onClick={() => setIsCommandPaletteOpen(true)}
          className="hidden sm:flex items-center gap-2 px-3.5 py-1.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-xs font-semibold text-[#7E88A8] hover:text-white transition-colors"
          title="Global Search (Ctrl + K)"
        >
          <Search className="w-3.5 h-3.5 text-[#E8546A]" />
          <span>Search providers, services...</span>
          <kbd className="inline-flex items-center gap-0.5 px-1.5 py-0.5 text-[9px] font-mono bg-[#111520] border border-[#212638] rounded text-[#7E88A8]">
            <Command className="w-2.5 h-2.5" /> K
          </kbd>
        </button>

        <div className="flex items-center gap-4">
          <Link to="/login" className="text-sm font-medium text-[#7E88A8] hover:text-white transition-colors">
            Sign In
          </Link>
          <Link
            to="/app"
            className="text-sm font-medium bg-[#E8546A] text-white px-4 py-2 rounded-lg hover:bg-[#D44359] transition-colors"
          >
            Business Portal
          </Link>
        </div>
      </header>
      <main className="flex-1">
        <Outlet />
      </main>
      <footer className="py-6 border-t border-[#212638] text-center text-xs text-[#7E88A8]">
        Bookline Appointment & Scheduling Platform &copy; {new Date().getFullYear()}
      </footer>

      {/* Global Command Palette (Ctrl + K) */}
      <CommandPalette
        isOpen={isCommandPaletteOpen}
        onClose={() => setIsCommandPaletteOpen(false)}
        onOpen={() => setIsCommandPaletteOpen(true)}
        initialScope="customer"
      />
    </div>
  );
};
