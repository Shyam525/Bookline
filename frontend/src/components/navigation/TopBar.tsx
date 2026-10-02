import React, { useState, useRef, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Building2, MapPin, Search, Command, User, Shield, Settings, LogOut, ChevronDown, Check } from 'lucide-react';
import { NotificationCenter } from './NotificationCenter';
import { Avatar } from '../data-display/DataDisplay';

export interface TopBarProps {
  onOpenCommandPalette: () => void;
  activeOrg: string;
  onOrgChange: (org: string) => void;
}

export const TopBar: React.FC<TopBarProps> = ({ onOpenCommandPalette, activeOrg, onOrgChange }) => {
  const navigate = useNavigate();
  const [isOrgDropdownOpen, setIsOrgDropdownOpen] = useState(false);
  const [isLocationDropdownOpen, setIsLocationDropdownOpen] = useState(false);
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);
  const [activeLocation, setActiveLocation] = useState('Main Branch — 123 Main St');

  const orgRef = useRef<HTMLDivElement>(null);
  const locRef = useRef<HTMLDivElement>(null);
  const userRef = useRef<HTMLDivElement>(null);

  const organizations = [
    { slug: 'acme-salon', name: 'Bookline Demo Salon', role: 'Owner' },
    { slug: 'downtown-spa', name: 'Downtown Spa & Wellness', role: 'Owner' },
  ];

  const locations = ['Main Branch — 123 Main St', 'Westside Studio — 456 West Ave'];

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (orgRef.current && !orgRef.current.contains(e.target as Node)) setIsOrgDropdownOpen(false);
      if (locRef.current && !locRef.current.contains(e.target as Node)) setIsLocationDropdownOpen(false);
      if (userRef.current && !userRef.current.contains(e.target as Node)) setIsUserMenuOpen(false);
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const currentOrgObj = organizations.find((o) => o.slug === activeOrg) || organizations[0];

  return (
    <header className="h-16 bg-[#111520] border-b border-[#212638] px-6 flex items-center justify-between shrink-0 select-none z-30">
      {/* Left: Organization & Location Selectors */}
      <div className="flex items-center gap-4">
        {/* Organization Switcher */}
        <div ref={orgRef} className="relative">
          <button
            onClick={() => setIsOrgDropdownOpen(!isOrgDropdownOpen)}
            className="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-[#181D2C] border border-[#212638] text-sm font-semibold text-white hover:border-[#E8546A]/50 transition-colors"
          >
            <Building2 className="w-4 h-4 text-[#E8546A]" />
            <span className="truncate max-w-[160px] sm:max-w-[200px]">{currentOrgObj.name}</span>
            <ChevronDown className="w-3.5 h-3.5 text-[#7E88A8]" />
          </button>

          {isOrgDropdownOpen && (
            <div className="absolute left-0 mt-2 w-64 bg-[#111520] border border-[#212638] rounded-2xl shadow-2xl z-50 p-2 space-y-1">
              <div className="px-3 py-1.5 text-[10px] font-semibold text-[#7E88A8] uppercase tracking-wider">
                Select Organization Context
              </div>
              {organizations.map((org) => (
                <button
                  key={org.slug}
                  onClick={() => {
                    onOrgChange(org.slug);
                    setIsOrgDropdownOpen(false);
                  }}
                  className="w-full flex items-center justify-between p-2.5 rounded-xl text-xs font-medium text-left hover:bg-[#181D2C] transition-colors"
                >
                  <div className="space-y-0.5">
                    <p className="text-white font-semibold">{org.name}</p>
                    <p className="text-[#7E88A8] text-[10px]">{org.slug}</p>
                  </div>
                  {activeOrg === org.slug && <Check className="w-4 h-4 text-[#E8546A]" />}
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Location Switcher */}
        <div ref={locRef} className="relative hidden md:block">
          <button
            onClick={() => setIsLocationDropdownOpen(!isLocationDropdownOpen)}
            className="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-[#181D2C]/60 border border-[#212638] text-xs font-medium text-[#7E88A8] hover:text-white transition-colors"
          >
            <MapPin className="w-3.5 h-3.5 text-[#34D399]" />
            <span className="truncate max-w-[180px]">{activeLocation}</span>
            <ChevronDown className="w-3 h-3 text-[#7E88A8]" />
          </button>

          {isLocationDropdownOpen && (
            <div className="absolute left-0 mt-2 w-60 bg-[#111520] border border-[#212638] rounded-2xl shadow-2xl z-50 p-2 space-y-1">
              <div className="px-3 py-1.5 text-[10px] font-semibold text-[#7E88A8] uppercase tracking-wider">
                Operational Location
              </div>
              {locations.map((loc) => (
                <button
                  key={loc}
                  onClick={() => {
                    setActiveLocation(loc);
                    setIsLocationDropdownOpen(false);
                  }}
                  className="w-full flex items-center justify-between p-2.5 rounded-xl text-xs font-medium text-left hover:bg-[#181D2C] transition-colors"
                >
                  <span className="text-white">{loc}</span>
                  {activeLocation === loc && <Check className="w-4 h-4 text-[#34D399]" />}
                </button>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Center: Global Search Command Palette Trigger */}
      <div className="flex-1 max-w-md mx-6 hidden lg:block">
        <button
          onClick={onOpenCommandPalette}
          className="w-full bg-[#181D2C] border border-[#212638] hover:border-[#E8546A]/40 rounded-xl px-4 py-2 flex items-center justify-between text-xs text-[#7E88A8] transition-colors group"
        >
          <div className="flex items-center gap-2">
            <Search className="w-4 h-4 text-[#7E88A8] group-hover:text-[#E8546A] transition-colors" />
            <span>Search appointments, customers, staff...</span>
          </div>
          <kbd className="inline-flex items-center gap-1 px-2 py-0.5 font-mono text-[10px] text-[#7E88A8] bg-[#111520] border border-[#212638] rounded">
            <Command className="w-3 h-3" /> K
          </kbd>
        </button>
      </div>

      {/* Right: Notifications & User Menu */}
      <div className="flex items-center gap-3">
        <NotificationCenter />

        {/* User Menu Dropdown */}
        <div ref={userRef} className="relative">
          <button
            onClick={() => setIsUserMenuOpen(!isUserMenuOpen)}
            className="flex items-center gap-3 p-1.5 rounded-xl hover:bg-[#181D2C] transition-colors focus:outline-none"
          >
            <Avatar name="Demo Owner" size="sm" />
            <div className="text-left hidden sm:block">
              <p className="text-xs font-semibold text-white">Demo Owner</p>
              <p className="text-[10px] text-[#E8546A] font-mono">Owner</p>
            </div>
            <ChevronDown className="w-3.5 h-3.5 text-[#7E88A8] hidden sm:block" />
          </button>

          {isUserMenuOpen && (
            <div className="absolute right-0 mt-2 w-56 bg-[#111520] border border-[#212638] rounded-2xl shadow-2xl z-50 p-2 space-y-1">
              <div className="px-3 py-2 border-b border-[#212638] space-y-0.5">
                <p className="text-xs font-bold text-white">Demo Owner</p>
                <p className="text-[10px] text-[#7E88A8]">demo@bookline.local</p>
              </div>

              <button
                onClick={() => {
                  navigate('/app/settings');
                  setIsUserMenuOpen(false);
                }}
                className="w-full flex items-center gap-2.5 p-2.5 rounded-xl text-xs text-[#7E88A8] hover:text-white hover:bg-[#181D2C] transition-colors"
              >
                <User className="w-4 h-4" /> Profile & Account
              </button>

              <button
                onClick={() => {
                  navigate('/app/settings');
                  setIsUserMenuOpen(false);
                }}
                className="w-full flex items-center gap-2.5 p-2.5 rounded-xl text-xs text-[#7E88A8] hover:text-white hover:bg-[#181D2C] transition-colors"
              >
                <Shield className="w-4 h-4" /> Security & 2FA
              </button>

              <button
                onClick={() => {
                  navigate('/app/settings');
                  setIsUserMenuOpen(false);
                }}
                className="w-full flex items-center gap-2.5 p-2.5 rounded-xl text-xs text-[#7E88A8] hover:text-white hover:bg-[#181D2C] transition-colors"
              >
                <Settings className="w-4 h-4" /> Preferences
              </button>

              <div className="border-t border-[#212638] pt-1">
                <button
                  onClick={() => navigate('/login')}
                  className="w-full flex items-center gap-2.5 p-2.5 rounded-xl text-xs text-red-400 hover:bg-red-500/10 transition-colors"
                >
                  <LogOut className="w-4 h-4" /> Sign Out
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </header>
  );
};
