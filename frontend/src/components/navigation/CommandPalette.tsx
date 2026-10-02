import React, { useState, useEffect } from 'react';
import { Search, Calendar, Users, Briefcase, Shield, Settings, ArrowRight, Command } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

export interface CommandPaletteProps {
  isOpen: boolean;
  onClose: () => void;
}

export const CommandPalette: React.FC<CommandPaletteProps> = ({ isOpen, onClose }) => {
  const navigate = useNavigate();
  const [query, setQuery] = useState('');

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault();
        if (isOpen) onClose();
        else setQuery('');
      }
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  const items = [
    { title: 'Open Calendar', category: 'Navigation', icon: Calendar, action: () => navigate('/app/calendar') },
    { title: 'View Customers Directory', category: 'Navigation', icon: Users, action: () => navigate('/app/customers') },
    { title: 'Manage Services Catalog', category: 'Navigation', icon: Briefcase, action: () => navigate('/app/services') },
    { title: 'Staff Roster & Working Hours', category: 'Navigation', icon: Shield, action: () => navigate('/app/staff') },
    { title: 'Organization Settings', category: 'Navigation', icon: Settings, action: () => navigate('/app/settings') },
    { title: 'Priya Shah — Haircut & Style', category: 'Appointments', icon: Calendar, action: () => navigate('/app/calendar') },
    { title: 'Alex Johnson (Senior Stylist)', category: 'Staff', icon: Shield, action: () => navigate('/app/staff') },
  ];

  const filtered = items.filter(
    (item) =>
      item.title.toLowerCase().includes(query.toLowerCase()) ||
      item.category.toLowerCase().includes(query.toLowerCase())
  );

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center pt-20 p-4 bg-black/70 backdrop-blur-sm animate-fade-in">
      <div className="bg-[#111520] border border-[#212638] rounded-2xl w-full max-w-xl shadow-2xl overflow-hidden flex flex-col">
        {/* Search Header */}
        <div className="flex items-center px-4 border-b border-[#212638] bg-[#181D2C]/40">
          <Search className="w-5 h-5 text-[#7E88A8] shrink-0" />
          <input
            type="text"
            autoFocus
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Search appointments, customers, staff, services, or commands... (ESC to close)"
            className="w-full bg-transparent px-3 py-4 text-sm text-white focus:outline-none placeholder:text-[#7E88A8]"
          />
          <kbd className="hidden sm:inline-flex items-center gap-1 px-2 py-1 text-[10px] font-mono font-medium text-[#7E88A8] bg-[#181D2C] border border-[#212638] rounded">
            <Command className="w-3 h-3" /> K
          </kbd>
        </div>

        {/* Results List */}
        <div className="p-2 max-h-80 overflow-y-auto space-y-1">
          {filtered.length === 0 ? (
            <div className="p-6 text-center text-sm text-[#7E88A8]">No results found for "{query}"</div>
          ) : (
            filtered.map((item, idx) => {
              const Icon = item.icon;
              return (
                <button
                  key={idx}
                  onClick={() => {
                    item.action();
                    onClose();
                  }}
                  className="w-full flex items-center justify-between p-3 rounded-xl hover:bg-[#181D2C] text-left group transition-colors"
                >
                  <div className="flex items-center gap-3">
                    <div className="w-8 h-8 rounded-lg bg-[#181D2C] border border-[#212638] flex items-center justify-center text-[#7E88A8] group-hover:text-[#E8546A] group-hover:border-[#E8546A]/30 transition-colors">
                      <Icon className="w-4 h-4" />
                    </div>
                    <div>
                      <p className="text-sm font-medium text-white group-hover:text-[#E8546A] transition-colors">{item.title}</p>
                      <span className="text-xs text-[#7E88A8]">{item.category}</span>
                    </div>
                  </div>
                  <ArrowRight className="w-4 h-4 text-[#7E88A8] opacity-0 group-hover:opacity-100 transition-opacity" />
                </button>
              );
            })
          )}
        </div>
      </div>
    </div>
  );
};
