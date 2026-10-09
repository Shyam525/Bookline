import React from 'react';
import { Outlet, Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../app/providers/AuthProvider';
import {
  Shield,
  LayoutDashboard,
  Building2,
  Users,
  Grid,
  Clock,
  ShoppingBag,
  DollarSign,
  Send,
  MessageSquare,
  Activity,
  Compass,
  LogOut,
  Sliders,
  FileText,
  History,
  Server,
  TrendingUp,
  Percent,
  CheckSquare,
  Search,
  Command,
} from 'lucide-react';
import { CommandPalette } from '../../components/navigation/CommandPalette';

export const AdminLayout: React.FC = () => {
  const { user, logout } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [isCommandPaletteOpen, setIsCommandPaletteOpen] = React.useState(false);

  // Section 96 Canonical 15 Navigation Sections
  const navLinks = [
    { name: 'Dashboard', path: '/admin', icon: LayoutDashboard },
    { name: 'Providers', path: '/admin/providers', icon: Building2 },
    { name: 'Businesses', path: '/admin/businesses', icon: Building2 },
    { name: 'Customers', path: '/admin/customers', icon: Users },
    { name: 'Categories', path: '/admin/categories', icon: Grid },
    { name: 'Bookings', path: '/admin/bookings', icon: Clock },
    { name: 'Orders', path: '/admin/orders', icon: ShoppingBag },
    { name: 'Payments', path: '/admin/payments', icon: DollarSign },
    { name: 'Commissions', path: '/admin/commissions', icon: Percent },
    { name: 'Payouts', path: '/admin/payouts', icon: Send },
    { name: 'Reviews', path: '/admin/reviews', icon: MessageSquare },
    { name: 'Moderation', path: '/admin/moderation', icon: CheckSquare },
    { name: 'Reports', path: '/admin/reports', icon: TrendingUp },
    { name: 'Audit', path: '/admin/audit', icon: History },
    { name: 'System', path: '/admin/system', icon: Server },
  ];

  return (
    <div className="min-h-screen bg-[#07090E] text-[#ECEFFE] flex flex-col selection:bg-[#FBBF24] selection:text-black">
      {/* Platform Admin Header */}
      <header className="sticky top-0 z-40 bg-[#111520] border-b border-[#212638] px-6 h-16 flex items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 rounded-lg bg-[#FBBF24] text-black flex items-center justify-center font-bold">
            <Shield className="w-5 h-5" />
          </div>
          <div>
            <h1 className="font-heading font-bold text-lg text-white leading-none">
              BOOKLINE <span className="text-[#FBBF24] text-xs font-sans uppercase ml-1">Admin</span>
            </h1>
            <p className="text-[10px] text-[#7E88A8]">Marketplace Governance & Platform Oversight</p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={() => setIsCommandPaletteOpen(true)}
            className="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-xs font-semibold text-[#7E88A8] hover:text-white transition-colors"
            title="Global Search (Ctrl + K)"
          >
            <Search className="w-3.5 h-3.5 text-[#FBBF24]" />
            <span className="hidden sm:inline">Search...</span>
            <kbd className="hidden sm:inline-flex items-center gap-0.5 px-1.5 py-0.5 text-[9px] font-mono bg-[#111520] border border-[#212638] rounded text-[#7E88A8]">
              <Command className="w-2.5 h-2.5" /> K
            </kbd>
          </button>
          <Link
            to="/"
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-xs font-semibold text-[#ECEFFE] transition-colors"
          >
            <Compass className="w-3.5 h-3.5 text-[#34D399]" />
            <span>Marketplace</span>
          </Link>
          <Link
            to="/provider/dashboard"
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-xs font-semibold text-[#ECEFFE] transition-colors"
          >
            <Building2 className="w-3.5 h-3.5 text-[#E8546A]" />
            <span>Provider Portal</span>
          </Link>
          <button
            onClick={() => {
              logout();
              navigate('/login');
            }}
            className="p-2.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-[#7E88A8] hover:text-red-400 transition-colors"
            title="Sign Out"
          >
            <LogOut className="w-4 h-4" />
          </button>
        </div>
      </header>

      {/* Main Container */}
      <div className="flex-1 flex overflow-hidden">
        {/* Sidebar */}
        <aside className="w-64 bg-[#111520] border-r border-[#212638] flex flex-col overflow-y-auto hidden md:flex">
          <div className="p-4 space-y-1 flex-1">
            <p className="px-3 text-[10px] font-bold uppercase tracking-wider text-[#7E88A8] mb-2">
              Platform Controls
            </p>
            {navLinks.map((item) => {
              const isActive = location.pathname === item.path;
              const Icon = item.icon;
              return (
                <Link
                  key={item.name}
                  to={item.path}
                  className={`flex items-center gap-3 px-3 py-2 rounded-xl text-xs font-medium transition-colors ${
                    isActive
                      ? 'bg-[#FBBF24] text-black font-bold shadow-md shadow-[#FBBF24]/20'
                      : 'text-[#ECEFFE] hover:bg-[#181D2C] hover:text-white'
                  }`}
                >
                  <Icon className="w-4 h-4 flex-shrink-0" />
                  <span>{item.name}</span>
                </Link>
              );
            })}
          </div>

          <div className="p-4 border-t border-[#212638] bg-[#0E121B] text-[11px] text-[#7E88A8]">
            <p className="font-semibold text-white">Platform Take Rate: 10%</p>
            <p className="text-[10px]">Deterministic Fraud Checks Active</p>
          </div>
        </aside>

        {/* Workspace Body */}
        <main className="flex-1 overflow-y-auto p-4 sm:p-6 lg:p-8">
          <Outlet />
        </main>
      </div>

      <CommandPalette
        isOpen={isCommandPaletteOpen}
        onClose={() => setIsCommandPaletteOpen(false)}
        onOpen={() => setIsCommandPaletteOpen(true)}
      />
    </div>
  );
};
