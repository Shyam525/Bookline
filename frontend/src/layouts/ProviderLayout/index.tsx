import React, { useState } from 'react';
import { Outlet, Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../app/providers/AuthProvider';
import { NotificationDropdown } from '../../components/navigation/NotificationDropdown';
import {
  LayoutDashboard,
  Calendar,
  Clock,
  Users,
  Scissors,
  Package,
  UserCheck,
  MapPin,
  ShoppingBag,
  CreditCard,
  BarChart3,
  FileText,
  Store,
  ShieldCheck,
  Settings,
  ChevronDown,
  Plus,
  Compass,
  Building2,
  LogOut,
  Sparkles,
} from 'lucide-react';

export const ProviderLayout: React.FC = () => {
  const { user, businesses, activeBusiness, switchBusiness, logout, quickLoginAs } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();

  const [isBusinessMenuOpen, setIsBusinessMenuOpen] = useState(false);
  const [selectedLocation, setSelectedLocation] = useState('Bodakdev Flagship');
  const [isLocationMenuOpen, setIsLocationMenuOpen] = useState(false);

  const locationsList = ['Bodakdev Flagship', 'Satellite Executive Branch', 'Mumbai Studio'];

  const navigationGroups = [
    {
      group: 'Operations',
      items: [
        { name: 'Dashboard', path: '/provider/dashboard', icon: LayoutDashboard },
        { name: 'Calendar', path: '/provider/calendar', icon: Calendar },
        { name: 'Bookings', path: '/provider/bookings', icon: Clock },
        { name: 'Customers CRM', path: '/provider/customers', icon: Users },
      ],
    },
    {
      group: 'Catalog & Resources',
      items: [
        { name: 'Services', path: '/provider/services', icon: Scissors },
        { name: 'Products & Inventory', path: '/provider/products', icon: Package },
        { name: 'Staff & Specialists', path: '/provider/staff', icon: UserCheck },
        { name: 'Locations & Branches', path: '/provider/locations', icon: MapPin },
      ],
    },
    {
      group: 'Commerce & Finance',
      items: [
        { name: 'Retail Orders', path: '/provider/orders', icon: ShoppingBag },
        { name: 'Payments & Payouts', path: '/provider/payments', icon: CreditCard },
      ],
    },
    {
      group: 'Intelligence',
      items: [
        { name: 'Analytics', path: '/provider/analytics', icon: BarChart3 },
        { name: 'Reports', path: '/provider/reports', icon: FileText },
      ],
    },
    {
      group: 'Settings & Branding',
      items: [
        { name: 'Storefront Profile', path: '/provider/storefront', icon: Store },
        { name: 'Team & RBAC', path: '/provider/team', icon: ShieldCheck },
        { name: 'Settings', path: '/provider/settings', icon: Settings },
      ],
    },
  ];

  return (
    <div className="min-h-screen bg-[#0A0C13] text-[#ECEFFE] flex flex-col">
      {/* Provider Operations Header */}
      <header className="sticky top-0 z-40 bg-[#111520] border-b border-[#212638] px-6 h-16 flex items-center justify-between gap-4">
        {/* Brand & Business Switcher */}
        <div className="flex items-center gap-4">
          <Link to="/provider/dashboard" className="flex items-center gap-2.5">
            <div className="w-8 h-8 rounded-lg bg-[#E8546A] flex items-center justify-center font-heading font-bold text-white text-base">
              B
            </div>
            <span className="font-heading font-bold text-lg text-white hidden sm:inline">
              BOOKLINE <span className="text-[10px] uppercase font-sans text-[#E8546A] tracking-wider ml-1">OS</span>
            </span>
          </Link>

          <span className="h-5 w-px bg-[#212638] hidden sm:block" />

          {/* Business Switcher (Point 30) */}
          <div className="relative">
            <button
              onClick={() => setIsBusinessMenuOpen((prev) => !prev)}
              className="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-xs font-semibold text-white transition-colors"
            >
              <Building2 className="w-3.5 h-3.5 text-[#E8546A]" />
              <span className="max-w-[150px] truncate">{activeBusiness?.name || 'Aura Wellness & Spa'}</span>
              <ChevronDown className="w-3 h-3 text-[#7E88A8]" />
            </button>

            {isBusinessMenuOpen && (
              <>
                <div className="fixed inset-0 z-20" onClick={() => setIsBusinessMenuOpen(false)} />
                <div className="absolute left-0 mt-2 w-64 bg-[#111520] border border-[#212638] rounded-xl shadow-2xl z-30 py-1.5 overflow-hidden animate-fadeIn">
                  <div className="px-3 py-1.5 text-[10px] uppercase font-bold text-[#7E88A8] tracking-wider border-b border-[#212638]">
                    Switch Owned Business
                  </div>
                  {businesses.length > 0 ? (
                    businesses.map((b) => (
                      <button
                        key={b.id}
                        onClick={() => {
                          switchBusiness(b.id);
                          setIsBusinessMenuOpen(false);
                        }}
                        className={`w-full px-3 py-2 text-xs text-left hover:bg-[#181D2C] transition-colors flex items-center justify-between ${
                          activeBusiness?.id === b.id ? 'text-[#E8546A] font-bold bg-[#181D2C]/60' : 'text-[#ECEFFE]'
                        }`}
                      >
                        <div className="truncate">
                          <p className="truncate">{b.name}</p>
                          <p className="text-[10px] text-[#7E88A8]">{b.city} &bull; {b.category}</p>
                        </div>
                        {activeBusiness?.id === b.id && <span className="w-1.5 h-1.5 rounded-full bg-[#E8546A]" />}
                      </button>
                    ))
                  ) : (
                    <div className="px-3 py-2 text-xs text-[#7E88A8]">
                      Default Tenant: Aura Wellness
                    </div>
                  )}
                </div>
              </>
            )}
          </div>

          {/* Location Switcher (Point 31) */}
          <div className="relative hidden md:block">
            <button
              onClick={() => setIsLocationMenuOpen((prev) => !prev)}
              className="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-[#181D2C]/60 hover:bg-[#181D2C] border border-[#212638] text-xs font-semibold text-[#ECEFFE] transition-colors"
            >
              <MapPin className="w-3.5 h-3.5 text-[#34D399]" />
              <span className="max-w-[140px] truncate">{selectedLocation}</span>
              <ChevronDown className="w-3 h-3 text-[#7E88A8]" />
            </button>

            {isLocationMenuOpen && (
              <>
                <div className="fixed inset-0 z-20" onClick={() => setIsLocationMenuOpen(false)} />
                <div className="absolute left-0 mt-2 w-52 bg-[#111520] border border-[#212638] rounded-xl shadow-2xl z-30 py-1 overflow-hidden animate-fadeIn">
                  <div className="px-3 py-1 text-[10px] uppercase font-bold text-[#7E88A8] tracking-wider border-b border-[#212638]">
                    Operational Branch
                  </div>
                  {locationsList.map((loc) => (
                    <button
                      key={loc}
                      onClick={() => {
                        setSelectedLocation(loc);
                        setIsLocationMenuOpen(false);
                      }}
                      className={`w-full px-3 py-2 text-xs text-left hover:bg-[#181D2C] transition-colors ${
                        selectedLocation === loc ? 'text-[#34D399] font-bold bg-[#181D2C]/60' : 'text-[#ECEFFE]'
                      }`}
                    >
                      {loc}
                    </button>
                  ))}
                </div>
              </>
            )}
          </div>
        </div>

        {/* Right: Quick Action, Switch to Customer View, Notifications, User */}
        <div className="flex items-center gap-3">
          <Link
            to="/provider/calendar"
            className="hidden sm:flex items-center gap-1.5 px-3.5 py-2 bg-[#E8546A] hover:bg-[#D44359] text-white rounded-xl text-xs font-bold transition-all shadow-md shadow-[#E8546A]/20"
          >
            <Plus className="w-3.5 h-3.5" /> New Booking
          </Link>

          <Link
            to="/"
            className="flex items-center gap-1.5 px-3 py-2 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-xs font-semibold text-[#ECEFFE] transition-colors"
            title="Browse Consumer Marketplace"
          >
            <Compass className="w-3.5 h-3.5 text-[#34D399]" />
            <span className="hidden sm:inline">Marketplace</span>
          </Link>

          <NotificationDropdown />

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

      {/* Main Workspace with Sidebar */}
      <div className="flex-1 flex overflow-hidden">
        {/* Sidebar */}
        <aside className="w-64 bg-[#111520] border-r border-[#212638] flex flex-col overflow-y-auto hidden md:flex">
          <div className="p-4 space-y-6 flex-1">
            {navigationGroups.map((grp) => (
              <div key={grp.group} className="space-y-1">
                <p className="px-3 text-[10px] font-bold uppercase tracking-wider text-[#7E88A8] mb-2">
                  {grp.group}
                </p>
                {grp.items.map((item) => {
                  const isActive = location.pathname === item.path;
                  const Icon = item.icon;
                  return (
                    <Link
                      key={item.name}
                      to={item.path}
                      className={`flex items-center gap-3 px-3 py-2 rounded-xl text-xs font-medium transition-colors ${
                        isActive
                          ? 'bg-[#E8546A] text-white font-semibold shadow-md shadow-[#E8546A]/20'
                          : 'text-[#ECEFFE] hover:bg-[#181D2C] hover:text-white'
                      }`}
                    >
                      <Icon className="w-4 h-4 flex-shrink-0" />
                      <span>{item.name}</span>
                    </Link>
                  );
                })}
              </div>
            ))}
          </div>

          {/* Sidebar Footer */}
          <div className="p-4 border-t border-[#212638] bg-[#0E121B]">
            <div className="flex items-center gap-2 text-xs text-[#7E88A8]">
              <div className="w-2 h-2 rounded-full bg-[#34D399] animate-pulse" />
              <span>Multi-Tenant Sync Active</span>
            </div>
          </div>
        </aside>

        {/* Workspace Body */}
        <main className="flex-1 overflow-y-auto p-4 sm:p-6 lg:p-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
};
