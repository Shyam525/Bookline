import React, { useState, useRef, useEffect } from 'react';
import { Link, useLocation } from 'react-router-dom';
import {
  Building2,
  MapPin,
  ChevronDown,
  User,
  Bell,
  LogOut,
  ChevronRight,
  Home,
  Check,
  Shield,
  Menu,
  X,
} from 'lucide-react';
import { Avatar } from '../data-display/DataDisplay';

/* =========================================================================
 * 1. BREADCRUMBS
 * ========================================================================= */
export interface BreadcrumbItem {
  label: string;
  path?: string;
}

export interface BreadcrumbsProps {
  items: BreadcrumbItem[];
  homePath?: string;
  className?: string;
}

export const Breadcrumbs: React.FC<BreadcrumbsProps> = ({
  items,
  homePath = '/',
  className = '',
}) => {
  return (
    <nav aria-label="Breadcrumb" className={`flex items-center text-xs text-[#7E88A8] ${className}`}>
      <ol className="flex items-center space-x-1.5 flex-wrap">
        <li>
          <Link
            to={homePath}
            className="flex items-center gap-1 hover:text-[#ECEFFE] focus:outline-none focus:text-[#E8546A] transition-colors"
          >
            <Home className="w-3.5 h-3.5" />
            <span className="sr-only">Home</span>
          </Link>
        </li>
        {items.map((item, index) => {
          const isLast = index === items.length - 1;
          return (
            <li key={item.label} className="flex items-center space-x-1.5">
              <ChevronRight className="w-3 h-3 text-[#7E88A8]/40 shrink-0" />
              {isLast || !item.path ? (
                <span
                  aria-current={isLast ? 'page' : undefined}
                  className={`font-medium truncate max-w-[200px] ${
                    isLast ? 'text-[#ECEFFE] font-semibold' : 'text-[#7E88A8]'
                  }`}
                >
                  {item.label}
                </span>
              ) : (
                <Link
                  to={item.path}
                  className="hover:text-[#ECEFFE] focus:outline-none focus:text-[#E8546A] transition-colors truncate max-w-[200px]"
                >
                  {item.label}
                </Link>
              )}
            </li>
          );
        })}
      </ol>
    </nav>
  );
};

/* =========================================================================
 * 2. NOTIFICATION BELL
 * ========================================================================= */
export interface NotificationBellProps {
  unreadCount?: number;
  onClick?: () => void;
  isOpen?: boolean;
  className?: string;
}

export const NotificationBell: React.FC<NotificationBellProps> = ({
  unreadCount = 0,
  onClick,
  isOpen = false,
  className = '',
}) => {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={`Notifications ${unreadCount > 0 ? `(${unreadCount} unread)` : ''}`}
      aria-expanded={isOpen}
      className={`relative p-2 rounded-xl transition-all duration-150 focus:outline-none focus:ring-2 focus:ring-[#E8546A] ${
        isOpen
          ? 'bg-[#181D2C] text-white border border-[#E8546A]/40'
          : 'bg-[#111520] hover:bg-[#181D2C] text-[#7E88A8] hover:text-[#ECEFFE] border border-[#212638]'
      } ${className}`}
    >
      <Bell className="w-4 h-4" />
      {unreadCount > 0 && (
        <span className="absolute -top-1 -right-1 flex h-4 min-w-[16px] px-1 items-center justify-center rounded-full bg-[#E8546A] text-[9px] font-bold text-white shadow-md animate-pulse">
          {unreadCount > 99 ? '99+' : unreadCount}
        </span>
      )}
    </button>
  );
};

/* =========================================================================
 * 3. ORGANIZATION SWITCHER
 * ========================================================================= */
export interface OrganizationOption {
  slug: string;
  name: string;
  role?: string;
}

export interface OrganizationSwitcherProps {
  organizations: OrganizationOption[];
  activeOrg: string;
  onSelectOrg: (slug: string) => void;
  className?: string;
}

export const OrganizationSwitcher: React.FC<OrganizationSwitcherProps> = ({
  organizations,
  activeOrg,
  onSelectOrg,
  className = '',
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleOutsideClick = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleOutsideClick);
    return () => document.removeEventListener('mousedown', handleOutsideClick);
  }, []);

  const current = organizations.find((o) => o.slug === activeOrg) || organizations[0] || {
    slug: 'default',
    name: 'My Workspace',
    role: 'Owner',
  };

  return (
    <div ref={containerRef} className={`relative ${className}`}>
      <button
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        aria-expanded={isOpen}
        aria-haspopup="listbox"
        className="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-xs font-semibold text-white transition-colors focus:outline-none focus:ring-2 focus:ring-[#E8546A]"
      >
        <Building2 className="w-3.5 h-3.5 text-[#E8546A] shrink-0" />
        <span className="truncate max-w-[140px] sm:max-w-[180px]">{current.name}</span>
        <ChevronDown className="w-3 h-3 text-[#7E88A8] shrink-0" />
      </button>

      {isOpen && (
        <div
          role="listbox"
          className="absolute left-0 mt-2 w-64 bg-[#111520] border border-[#212638] rounded-2xl shadow-2xl z-50 p-1.5 space-y-1 animate-fadeIn"
        >
          <div className="px-3 py-1.5 text-[10px] font-semibold text-[#7E88A8] uppercase tracking-wider border-b border-[#212638]">
            Switch Organization
          </div>
          {organizations.map((org) => (
            <button
              key={org.slug}
              role="option"
              aria-selected={org.slug === activeOrg}
              onClick={() => {
                onSelectOrg(org.slug);
                setIsOpen(false);
              }}
              className={`w-full flex items-center justify-between px-3 py-2 rounded-xl text-xs font-medium text-left transition-colors ${
                org.slug === activeOrg
                  ? 'bg-[#181D2C] text-[#E8546A] font-semibold'
                  : 'text-[#ECEFFE] hover:bg-[#181D2C]/60'
              }`}
            >
              <div className="truncate">
                <div className="truncate">{org.name}</div>
                {org.role && (
                  <span className="text-[10px] text-[#7E88A8] block">{org.role}</span>
                )}
              </div>
              {org.slug === activeOrg && <Check className="w-3.5 h-3.5 text-[#E8546A] shrink-0" />}
            </button>
          ))}
        </div>
      )}
    </div>
  );
};

/* =========================================================================
 * 4. LOCATION SWITCHER
 * ========================================================================= */
export interface LocationSwitcherProps {
  locations: string[];
  activeLocation: string;
  onSelectLocation: (loc: string) => void;
  className?: string;
}

export const LocationSwitcher: React.FC<LocationSwitcherProps> = ({
  locations,
  activeLocation,
  onSelectLocation,
  className = '',
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleOutsideClick = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleOutsideClick);
    return () => document.removeEventListener('mousedown', handleOutsideClick);
  }, []);

  return (
    <div ref={containerRef} className={`relative ${className}`}>
      <button
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        aria-expanded={isOpen}
        aria-haspopup="listbox"
        className="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-[#181D2C]/80 hover:bg-[#181D2C] border border-[#212638] text-xs font-semibold text-[#ECEFFE] transition-colors focus:outline-none focus:ring-2 focus:ring-[#34D399]"
      >
        <MapPin className="w-3.5 h-3.5 text-[#34D399] shrink-0" />
        <span className="truncate max-w-[130px] sm:max-w-[160px]">{activeLocation}</span>
        <ChevronDown className="w-3 h-3 text-[#7E88A8] shrink-0" />
      </button>

      {isOpen && (
        <div
          role="listbox"
          className="absolute left-0 mt-2 w-56 bg-[#111520] border border-[#212638] rounded-2xl shadow-2xl z-50 p-1.5 space-y-1 animate-fadeIn"
        >
          <div className="px-3 py-1.5 text-[10px] font-semibold text-[#7E88A8] uppercase tracking-wider border-b border-[#212638]">
            Operating Branch
          </div>
          {locations.map((loc) => (
            <button
              key={loc}
              role="option"
              aria-selected={loc === activeLocation}
              onClick={() => {
                onSelectLocation(loc);
                setIsOpen(false);
              }}
              className={`w-full flex items-center justify-between px-3 py-2 rounded-xl text-xs font-medium text-left transition-colors ${
                loc === activeLocation
                  ? 'bg-[#181D2C] text-[#34D399] font-semibold'
                  : 'text-[#ECEFFE] hover:bg-[#181D2C]/60'
              }`}
            >
              <span className="truncate">{loc}</span>
              {loc === activeLocation && <Check className="w-3.5 h-3.5 text-[#34D399] shrink-0" />}
            </button>
          ))}
        </div>
      )}
    </div>
  );
};

/* =========================================================================
 * 5. USER MENU
 * ========================================================================= */
export interface UserMenuProps {
  name: string;
  email?: string;
  role?: string;
  onLogout?: () => void;
  customLinks?: Array<{ label: string; href: string; icon?: any }>;
  className?: string;
}

export const UserMenu: React.FC<UserMenuProps> = ({
  name,
  email,
  role = 'Staff',
  onLogout,
  customLinks = [],
  className = '',
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleOutsideClick = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleOutsideClick);
    return () => document.removeEventListener('mousedown', handleOutsideClick);
  }, []);

  return (
    <div ref={containerRef} className={`relative ${className}`}>
      <button
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        aria-expanded={isOpen}
        className="flex items-center gap-2 p-1 rounded-xl hover:bg-[#181D2C] transition-colors focus:outline-none focus:ring-2 focus:ring-[#E8546A]"
      >
        <Avatar name={name} size="sm" />
        <span className="hidden sm:inline text-xs font-semibold text-[#ECEFFE] max-w-[100px] truncate">
          {name}
        </span>
        <ChevronDown className="w-3 h-3 text-[#7E88A8]" />
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-56 bg-[#111520] border border-[#212638] rounded-2xl shadow-2xl z-50 p-2 space-y-1 animate-fadeIn">
          <div className="px-3 py-2 border-b border-[#212638] mb-1">
            <p className="text-xs font-bold text-white truncate">{name}</p>
            {email && <p className="text-[11px] text-[#7E88A8] truncate">{email}</p>}
            <span className="inline-block mt-1 px-2 py-0.5 rounded-full text-[9px] font-mono bg-[#E8546A]/10 text-[#E8546A] border border-[#E8546A]/20">
              {role}
            </span>
          </div>

          {customLinks.map((link) => {
            const Icon = link.icon || User;
            return (
              <Link
                key={link.label}
                to={link.href}
                onClick={() => setIsOpen(false)}
                className="flex items-center gap-2 px-3 py-2 rounded-xl text-xs text-[#ECEFFE] hover:bg-[#181D2C] transition-colors"
              >
                <Icon className="w-3.5 h-3.5 text-[#7E88A8]" />
                <span>{link.label}</span>
              </Link>
            );
          })}

          {onLogout && (
            <button
              type="button"
              onClick={() => {
                setIsOpen(false);
                onLogout();
              }}
              className="w-full flex items-center gap-2 px-3 py-2 rounded-xl text-xs text-red-400 hover:bg-red-500/10 transition-colors"
            >
              <LogOut className="w-3.5 h-3.5" />
              <span>Log out</span>
            </button>
          )}
        </div>
      )}
    </div>
  );
};

/* =========================================================================
 * 6. APP SHELL
 * ========================================================================= */
export interface AppShellProps {
  topbar?: React.ReactNode;
  sidebar?: React.ReactNode;
  children: React.ReactNode;
  density?: 'customer' | 'provider' | 'admin';
  className?: string;
}

export const AppShell: React.FC<AppShellProps> = ({
  topbar,
  sidebar,
  children,
  density = 'provider',
  className = '',
}) => {
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  // Section 108 Visual Density configuration:
  // Customer: breathing room, max-w-7xl
  // Provider: higher info density, full width
  // Admin: highest info density, compact paddings
  const densityStyles = {
    customer: 'p-4 sm:p-6 lg:p-8 max-w-7xl mx-auto',
    provider: 'p-4 sm:p-6 lg:p-6 w-full',
    admin: 'p-3 sm:p-4 lg:p-5 w-full font-mono-friendly',
  };

  return (
    <div className={`min-h-screen bg-[#0A0C13] text-[#ECEFFE] flex flex-col overflow-x-hidden ${className}`}>
      {topbar && (
        <header className="sticky top-0 z-40 bg-[#111520]/95 backdrop-blur-md border-b border-[#212638] h-16 shrink-0 flex items-center px-4 sm:px-6">
          {sidebar && (
            <button
              type="button"
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
              className="lg:hidden p-2 rounded-xl bg-[#181D2C] text-[#7E88A8] hover:text-white mr-3"
              aria-label="Toggle navigation menu"
            >
              {mobileMenuOpen ? <X className="w-4 h-4" /> : <Menu className="w-4 h-4" />}
            </button>
          )}
          <div className="flex-1 flex items-center justify-between">{topbar}</div>
        </header>
      )}

      <div className="flex-1 flex min-h-0">
        {sidebar && (
          <aside
            className={`fixed inset-y-0 left-0 z-30 w-64 bg-[#111520] border-r border-[#212638] transform transition-transform duration-200 lg:translate-x-0 lg:static shrink-0 ${
              mobileMenuOpen ? 'translate-x-0 pt-16 lg:pt-0' : '-translate-x-full'
            }`}
          >
            {sidebar}
          </aside>
        )}

        <main className={`flex-1 overflow-y-auto ${densityStyles[density]}`}>{children}</main>
      </div>

      {mobileMenuOpen && (
        <div
          className="fixed inset-0 bg-black/60 backdrop-blur-sm z-20 lg:hidden"
          onClick={() => setMobileMenuOpen(false)}
        />
      )}
    </div>
  );
};
