import React from 'react';
import { Outlet, Link, useLocation } from 'react-router-dom';
import { Calendar, Users, Briefcase, Settings, LayoutDashboard, Shield, CreditCard, BarChart2, Bell } from 'lucide-react';

export const AppLayout: React.FC = () => {
  const location = useLocation();

  const navItems = [
    { label: 'Dashboard', path: '/app', icon: LayoutDashboard },
    { label: 'Calendar', path: '/app/calendar', icon: Calendar },
    { label: 'Customers', path: '/app/customers', icon: Users },
    { label: 'Services', path: '/app/services', icon: Briefcase },
    { label: 'Staff', path: '/app/staff', icon: Shield },
    { label: 'Payments', path: '/app/payments', icon: CreditCard },
    { label: 'Analytics', path: '/app/analytics', icon: BarChart2 },
    { label: 'Settings', path: '/app/settings', icon: Settings },
  ];

  return (
    <div className="flex h-screen bg-[#0A0C13] text-[#ECEFFE] overflow-hidden">
      {/* Sidebar */}
      <aside className="w-64 bg-[#111520] border-r border-[#212638] flex flex-col justify-between">
        <div>
          {/* Logo */}
          <div className="h-16 flex items-center px-6 border-b border-[#212638]">
            <Link to="/" className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-lg bg-[#E8546A] flex items-center justify-center font-heading font-bold text-white text-lg">
                B
              </div>
              <span className="font-heading text-xl font-bold tracking-tight text-white">BOOKLINE</span>
            </Link>
          </div>

          {/* Navigation */}
          <nav className="p-4 space-y-1">
            {navItems.map((item) => {
              const Icon = item.icon;
              const isActive = location.pathname === item.path;
              return (
                <Link
                  key={item.path}
                  to={item.path}
                  className={`flex items-center gap-3 px-4 py-2.5 rounded-lg text-sm font-medium transition-colors ${
                    isActive
                      ? 'bg-[#181D2C] text-[#E8546A] font-semibold'
                      : 'text-[#7E88A8] hover:text-[#ECEFFE] hover:bg-[#181D2C]/50'
                  }`}
                >
                  <Icon className="w-4 h-4" />
                  {item.label}
                </Link>
              );
            })}
          </nav>
        </div>

        {/* User profile block */}
        <div className="p-4 border-t border-[#212638]">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-9 h-9 rounded-full bg-[#181D2C] border border-[#212638] flex items-center justify-center font-semibold text-sm text-[#ECEFFE]">
                DO
              </div>
              <div>
                <p className="text-sm font-medium text-white">Demo Owner</p>
                <p className="text-xs text-[#7E88A8]">acme-salon</p>
              </div>
            </div>
            <button className="text-[#7E88A8] hover:text-white transition-colors" title="Notifications">
              <Bell className="w-4 h-4" />
            </button>
          </div>
        </div>
      </aside>

      {/* Main Content Area */}
      <main className="flex-1 flex flex-col overflow-y-auto">
        <header className="h-16 bg-[#111520] border-b border-[#212638] px-8 flex items-center justify-between">
          <h1 className="font-heading text-lg font-semibold text-white">Operations Workspace</h1>
          <div className="flex items-center gap-4">
            <span className="text-xs font-mono px-2.5 py-1 rounded bg-[#181D2C] border border-[#212638] text-[#34D399]">
              Live Engine Active
            </span>
          </div>
        </header>
        <div className="p-8 flex-1">
          <Outlet />
        </div>
      </main>
    </div>
  );
};
