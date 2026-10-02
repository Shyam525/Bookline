import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import {
  LayoutDashboard,
  Calendar,
  Clock,
  Users,
  Briefcase,
  Shield,
  CreditCard,
  BarChart2,
  FileText,
  UserPlus,
  Settings,
  History,
  Sparkles,
  ChevronLeft,
  ChevronRight,
  X,
} from 'lucide-react';
import { clsx } from 'clsx';

export interface SidebarProps {
  isCollapsed: boolean;
  onToggleCollapse: () => void;
  isMobileOpen: boolean;
  onCloseMobile: () => void;
}

export const Sidebar: React.FC<SidebarProps> = ({
  isCollapsed,
  onToggleCollapse,
  isMobileOpen,
  onCloseMobile,
}) => {
  const location = useLocation();

  const groups = [
    {
      title: 'OPERATIONS',
      items: [
        { label: 'Dashboard', path: '/app', icon: LayoutDashboard },
        { label: 'Calendar', path: '/app/calendar', icon: Calendar },
        { label: 'Bookings', path: '/app/bookings', icon: Clock },
        { label: 'Customers', path: '/app/customers', icon: Users },
      ],
    },
    {
      title: 'CATALOGUE',
      items: [
        { label: 'Services', path: '/app/services', icon: Briefcase },
        { label: 'Staff', path: '/app/staff', icon: Shield },
      ],
    },
    {
      title: 'BUSINESS',
      items: [
        { label: 'Payments', path: '/app/payments', icon: CreditCard },
        { label: 'Analytics', path: '/app/analytics', icon: BarChart2 },
        { label: 'Reports', path: '/app/reports', icon: FileText },
      ],
    },
    {
      title: 'ADMINISTRATION',
      items: [
        { label: 'Team', path: '/app/team', icon: UserPlus },
        { label: 'Settings', path: '/app/settings', icon: Settings },
        { label: 'Audit Log', path: '/app/audit', icon: History },
        { label: 'Design System', path: '/app/design-system', icon: Sparkles },
      ],
    },
  ];

  const sidebarContent = (
    <div className="flex flex-col h-full justify-between select-none">
      <div>
        {/* Brand Header */}
        <div className="h-16 flex items-center justify-between px-6 border-b border-[#212638]">
          <Link to="/" className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-xl bg-[#E8546A] flex items-center justify-center font-heading font-bold text-white text-lg shadow-lg shadow-[#E8546A]/20 shrink-0">
              B
            </div>
            {!isCollapsed && (
              <span className="font-heading text-xl font-bold tracking-tight text-white animate-fade-in">
                BOOKLINE
              </span>
            )}
          </Link>
          <button
            onClick={onToggleCollapse}
            className="hidden md:flex p-1.5 rounded-lg text-[#7E88A8] hover:text-white hover:bg-[#181D2C] transition-colors"
            title={isCollapsed ? 'Expand Sidebar' : 'Collapse Sidebar'}
          >
            {isCollapsed ? <ChevronRight className="w-4 h-4" /> : <ChevronLeft className="w-4 h-4" />}
          </button>
          <button
            onClick={onCloseMobile}
            className="md:hidden p-1.5 rounded-lg text-[#7E88A8] hover:text-white"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Grouped Nav Items */}
        <div className="p-3 space-y-6 overflow-y-auto max-h-[calc(100vh-8rem)]">
          {groups.map((group) => (
            <div key={group.title} className="space-y-1">
              {!isCollapsed && (
                <div className="px-3 text-[10px] font-semibold text-[#7E88A8] uppercase tracking-wider mb-2">
                  {group.title}
                </div>
              )}
              {group.items.map((item) => {
                const Icon = item.icon;
                const isActive = location.pathname === item.path;
                return (
                  <Link
                    key={item.path}
                    to={item.path}
                    onClick={onCloseMobile}
                    title={isCollapsed ? item.label : undefined}
                    className={clsx(
                      'flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-sm font-medium transition-all group relative',
                      isActive
                        ? 'bg-[#181D2C] text-[#E8546A] font-semibold shadow-sm border border-[#212638]'
                        : 'text-[#7E88A8] hover:text-[#ECEFFE] hover:bg-[#181D2C]/40',
                      isCollapsed && 'justify-center px-2'
                    )}
                  >
                    <Icon className={clsx('w-4 h-4 shrink-0', isActive ? 'text-[#E8546A]' : 'group-hover:text-[#ECEFFE]')} />
                    {!isCollapsed && <span>{item.label}</span>}
                  </Link>
                );
              })}
            </div>
          ))}
        </div>
      </div>

      {/* Footer System Status */}
      {!isCollapsed && (
        <div className="p-4 border-t border-[#212638] bg-[#0A0C13]/30">
          <div className="flex items-center gap-2 text-xs font-mono text-[#34D399]">
            <span className="w-2 h-2 rounded-full bg-[#34D399] animate-ping" />
            <span>Multi-Tenant Engine Live</span>
          </div>
        </div>
      )}
    </div>
  );

  return (
    <>
      {/* Desktop Sidebar */}
      <aside
        className={clsx(
          'hidden md:flex flex-col bg-[#111520] border-r border-[#212638] transition-all duration-200 shrink-0 z-20',
          isCollapsed ? 'w-16' : 'w-64'
        )}
      >
        {sidebarContent}
      </aside>

      {/* Mobile Drawer Overlay */}
      {isMobileOpen && (
        <div className="md:hidden fixed inset-0 z-50 flex">
          <div className="fixed inset-0 bg-black/70 backdrop-blur-sm" onClick={onCloseMobile} />
          <div className="relative flex flex-col w-72 bg-[#111520] border-r border-[#212638] h-full shadow-2xl z-10">
            {sidebarContent}
          </div>
        </div>
      )}
    </>
  );
};
