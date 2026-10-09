import React, { useState } from 'react';
import { Outlet, Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../app/providers/AuthProvider';
import { useCart } from '../../app/providers/CartContext';
import { CartDrawer } from '../../components/commerce/CartDrawer';
import { NotificationDropdown } from '../../components/navigation/NotificationDropdown';
import { CommandPalette } from '../../components/navigation/CommandPalette';
import {
  Compass,
  Heart,
  Calendar,
  ShoppingBag,
  User,
  MapPin,
  Sparkles,
  ChevronDown,
  LogOut,
  Building2,
  Shield,
  Search,
  Bell,
  Command,
} from 'lucide-react';

export const CustomerLayout: React.FC = () => {
  const { user, isAuthenticated, logout, quickLoginAs } = useAuth();
  const { toggleCart, totalCount } = useCart();
  const navigate = useNavigate();
  const location = useLocation();

  const [selectedCity, setSelectedCity] = useState('Ahmedabad');
  const [isCityDropdownOpen, setIsCityDropdownOpen] = useState(false);
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);
  const [isDemoSwitcherOpen, setIsDemoSwitcherOpen] = useState(false);
  const [isCommandPaletteOpen, setIsCommandPaletteOpen] = useState(false);

  const cities = ['Ahmedabad', 'Mumbai', 'Bangalore', 'Surat', 'Rajkot'];

  const handleCityChange = (city: string) => {
    setSelectedCity(city);
    setIsCityDropdownOpen(false);
    navigate(`/discover?city=${encodeURIComponent(city)}`);
  };

  return (
    <div className="min-h-screen bg-[#0A0C13] text-[#ECEFFE] flex flex-col selection:bg-[#E8546A] selection:text-white">
      {/* Top Marketplace Header */}
      <header className="sticky top-0 z-40 bg-[#111520]/95 backdrop-blur-md border-b border-[#212638] px-4 lg:px-8 h-16 flex items-center justify-between gap-4">
        {/* Left: Brand + Location Selector */}
        <div className="flex items-center gap-6">
          <Link to="/" className="flex items-center gap-3 group">
            <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-[#E8546A] to-[#B32D42] flex items-center justify-center font-heading font-black text-white text-lg shadow-lg shadow-[#E8546A]/20 group-hover:scale-105 transition-transform">
              B
            </div>
            <div className="flex flex-col">
              <span className="font-heading text-xl font-bold tracking-tight text-white leading-none">
                BOOKLINE
              </span>
              <span className="text-[9px] uppercase tracking-widest text-[#7E88A8] font-bold">
                Marketplace
              </span>
            </div>
          </Link>

          {/* Location Selector (Point 9 & 22) */}
          <div className="relative hidden md:block">
            <button
              onClick={() => setIsCityDropdownOpen((prev) => !prev)}
              className="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-xs font-semibold text-white transition-colors"
            >
              <MapPin className="w-3.5 h-3.5 text-[#E8546A]" />
              <span>{selectedCity}</span>
              <ChevronDown className="w-3 h-3 text-[#7E88A8]" />
            </button>

            {isCityDropdownOpen && (
              <>
                <div className="fixed inset-0 z-20" onClick={() => setIsCityDropdownOpen(false)} />
                <div className="absolute left-0 mt-2 w-48 bg-[#111520] border border-[#212638] rounded-xl shadow-2xl z-30 py-1 overflow-hidden animate-fadeIn">
                  <div className="px-3 py-1.5 text-[10px] uppercase font-bold text-[#7E88A8] tracking-wider border-b border-[#212638]">
                    Select Marketplace City
                  </div>
                  {cities.map((city) => (
                    <button
                      key={city}
                      onClick={() => handleCityChange(city)}
                      className={`w-full px-3 py-2 text-xs font-medium text-left hover:bg-[#181D2C] transition-colors flex items-center justify-between ${
                        selectedCity === city ? 'text-[#E8546A] font-bold bg-[#181D2C]/60' : 'text-[#ECEFFE]'
                      }`}
                    >
                      <span>{city}</span>
                      {selectedCity === city && <span className="w-1.5 h-1.5 rounded-full bg-[#E8546A]" />}
                    </button>
                  ))}
                </div>
              </>
            )}
          </div>
        </div>

        {/* Center: Search & Navigation */}
        <nav className="hidden lg:flex items-center gap-1 text-sm font-medium">
          <Link
            to="/discover"
            className={`px-3.5 py-2 rounded-xl transition-colors flex items-center gap-2 ${
              location.pathname.startsWith('/discover')
                ? 'bg-[#181D2C] text-white font-semibold'
                : 'text-[#7E88A8] hover:text-white hover:bg-[#181D2C]/50'
            }`}
          >
            <Compass className="w-4 h-4 text-[#E8546A]" />
            <span>Discover</span>
          </Link>
          <Link
            to="/favorites"
            className={`px-3.5 py-2 rounded-xl transition-colors flex items-center gap-2 ${
              location.pathname === '/favorites'
                ? 'bg-[#181D2C] text-white font-semibold'
                : 'text-[#7E88A8] hover:text-white hover:bg-[#181D2C]/50'
            }`}
          >
            <Heart className="w-4 h-4 text-[#E8546A]" />
            <span>Favorites</span>
          </Link>
          <Link
            to="/appointments"
            className={`px-3.5 py-2 rounded-xl transition-colors flex items-center gap-2 ${
              location.pathname === '/appointments'
                ? 'bg-[#181D2C] text-white font-semibold'
                : 'text-[#7E88A8] hover:text-white hover:bg-[#181D2C]/50'
            }`}
          >
            <Calendar className="w-4 h-4 text-[#34D399]" />
            <span>My Bookings</span>
          </Link>
          <Link
            to="/orders"
            className={`px-3.5 py-2 rounded-xl transition-colors flex items-center gap-2 ${
              location.pathname === '/orders'
                ? 'bg-[#181D2C] text-white font-semibold'
                : 'text-[#7E88A8] hover:text-white hover:bg-[#181D2C]/50'
            }`}
          >
            <ShoppingBag className="w-4 h-4 text-[#FBBF24]" />
            <span>Orders</span>
          </Link>
          <Link
            to="/dashboard"
            className={`px-3.5 py-2 rounded-xl transition-colors flex items-center gap-2 ${
              location.pathname === '/dashboard'
                ? 'bg-[#181D2C] text-white font-semibold'
                : 'text-[#7E88A8] hover:text-white hover:bg-[#181D2C]/50'
            }`}
          >
            <Compass className="w-4 h-4 text-[#34D399]" />
            <span>Dashboard</span>
          </Link>
        </nav>

        {/* Right: Global Search, Cart, Notifications, Demo Quick Switcher, Auth */}
        <div className="flex items-center gap-3">
          {/* Global Search Button (Section 94 Ctrl + K) */}
          <button
            onClick={() => setIsCommandPaletteOpen(true)}
            className="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-xs font-semibold text-[#7E88A8] hover:text-white transition-colors"
            title="Global Search (Ctrl + K)"
          >
            <Search className="w-3.5 h-3.5 text-[#E8546A]" />
            <span className="hidden sm:inline">Search...</span>
            <kbd className="hidden sm:inline-flex items-center gap-0.5 px-1.5 py-0.5 text-[9px] font-mono bg-[#111520] border border-[#212638] rounded text-[#7E88A8]">
              <Command className="w-2.5 h-2.5" /> K
            </kbd>
          </button>

          {/* Cart Trigger (Point 50) */}
          <button
            onClick={toggleCart}
            className="relative p-2.5 rounded-xl bg-[#111520] hover:bg-[#181D2C] border border-[#212638] text-[#ECEFFE] transition-colors"
            aria-label="Shopping Cart"
          >
            <ShoppingBag className="w-4 h-4" />
            {totalCount > 0 && (
              <span className="absolute -top-1 -right-1 w-4 h-4 rounded-full bg-[#E8546A] text-white text-[10px] font-bold flex items-center justify-center">
                {totalCount}
              </span>
            )}
          </button>

          {/* Notifications Center (Point 61) */}
          <NotificationDropdown />

          {/* Quick Demo Switcher Pill (For seamless actor testing: Customer / Provider / Admin) */}
          <div className="relative">
            <button
              onClick={() => setIsDemoSwitcherOpen((prev) => !prev)}
              className="hidden sm:flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-[#181D2C] border border-[#212638] hover:border-[#E8546A]/50 text-xs font-semibold text-[#ECEFFE] transition-colors"
            >
              <Sparkles className="w-3.5 h-3.5 text-[#E8546A]" />
              <span>Role: {user?.role || 'Guest'}</span>
              <ChevronDown className="w-3 h-3 text-[#7E88A8]" />
            </button>

            {isDemoSwitcherOpen && (
              <>
                <div className="fixed inset-0 z-20" onClick={() => setIsDemoSwitcherOpen(false)} />
                <div className="absolute right-0 mt-2 w-64 bg-[#111520] border border-[#212638] rounded-xl shadow-2xl z-30 py-2 overflow-hidden animate-fadeIn">
                  <div className="px-3 py-1 text-[10px] uppercase font-bold text-[#7E88A8] tracking-wider border-b border-[#212638] mb-1">
                    Demo Account Quick-Switch
                  </div>
                  <button
                    onClick={() => {
                      quickLoginAs('customer');
                      setIsDemoSwitcherOpen(false);
                      navigate('/');
                    }}
                    className="w-full px-3 py-2 text-left hover:bg-[#181D2C] transition-colors flex items-center gap-2.5 text-xs text-white"
                  >
                    <User className="w-4 h-4 text-[#34D399]" />
                    <div>
                      <p className="font-bold">Customer Mode</p>
                      <p className="text-[10px] text-[#7E88A8]">customer@bookline.local</p>
                    </div>
                  </button>
                  <button
                    onClick={() => {
                      quickLoginAs('provider');
                      setIsDemoSwitcherOpen(false);
                      navigate('/provider/dashboard');
                    }}
                    className="w-full px-3 py-2 text-left hover:bg-[#181D2C] transition-colors flex items-center gap-2.5 text-xs text-white"
                  >
                    <Building2 className="w-4 h-4 text-[#E8546A]" />
                    <div>
                      <p className="font-bold">Provider Platform</p>
                      <p className="text-[10px] text-[#7E88A8]">provider@bookline.local</p>
                    </div>
                  </button>
                  <button
                    onClick={() => {
                      quickLoginAs('admin');
                      setIsDemoSwitcherOpen(false);
                      navigate('/admin');
                    }}
                    className="w-full px-3 py-2 text-left hover:bg-[#181D2C] transition-colors flex items-center gap-2.5 text-xs text-white"
                  >
                    <Shield className="w-4 h-4 text-[#FBBF24]" />
                    <div>
                      <p className="font-bold">Platform Admin</p>
                      <p className="text-[10px] text-[#7E88A8]">admin@bookline.local</p>
                    </div>
                  </button>
                </div>
              </>
            )}
          </div>

          {/* User Account / Auth */}
          {isAuthenticated ? (
            <div className="relative">
              <button
                onClick={() => setIsUserMenuOpen((prev) => !prev)}
                className="w-9 h-9 rounded-xl bg-[#181D2C] border border-[#212638] hover:border-[#E8546A] flex items-center justify-center font-bold text-xs text-[#ECEFFE] transition-colors"
              >
                {user?.firstName?.substring(0, 1) || 'U'}
              </button>

              {isUserMenuOpen && (
                <>
                  <div className="fixed inset-0 z-20" onClick={() => setIsUserMenuOpen(false)} />
                  <div className="absolute right-0 mt-2 w-52 bg-[#111520] border border-[#212638] rounded-xl shadow-2xl z-30 py-1 overflow-hidden animate-fadeIn text-xs">
                    <div className="px-3 py-2 border-b border-[#212638]">
                      <p className="font-bold text-white truncate">{user?.email}</p>
                      <p className="text-[10px] text-[#7E88A8] capitalize">{user?.role}</p>
                    </div>
                    <Link
                      to="/dashboard"
                      onClick={() => setIsUserMenuOpen(false)}
                      className="px-3 py-2 hover:bg-[#181D2C] text-[#ECEFFE] flex items-center gap-2"
                    >
                      <Compass className="w-3.5 h-3.5 text-[#34D399]" /> Client Dashboard
                    </Link>
                    <Link
                      to="/profile"
                      onClick={() => setIsUserMenuOpen(false)}
                      className="px-3 py-2 hover:bg-[#181D2C] text-[#ECEFFE] flex items-center gap-2"
                    >
                      <User className="w-3.5 h-3.5 text-[#7E88A8]" /> Profile & Settings
                    </Link>
                    <Link
                      to="/notifications"
                      onClick={() => setIsUserMenuOpen(false)}
                      className="px-3 py-2 hover:bg-[#181D2C] text-[#ECEFFE] flex items-center gap-2"
                    >
                      <Bell className="w-3.5 h-3.5 text-[#34D399]" /> Notifications & Alerts
                    </Link>
                    <Link
                      to="/provider/dashboard"
                      onClick={() => setIsUserMenuOpen(false)}
                      className="px-3 py-2 hover:bg-[#181D2C] text-[#ECEFFE] flex items-center gap-2"
                    >
                      <Building2 className="w-3.5 h-3.5 text-[#E8546A]" /> Provider Workspace
                    </Link>
                    {user?.role === 'PlatformAdmin' && (
                      <Link
                        to="/admin"
                        onClick={() => setIsUserMenuOpen(false)}
                        className="px-3 py-2 hover:bg-[#181D2C] text-[#ECEFFE] flex items-center gap-2"
                      >
                        <Shield className="w-3.5 h-3.5 text-[#FBBF24]" /> Admin Console
                      </Link>
                    )}
                    <button
                      onClick={() => {
                        logout();
                        setIsUserMenuOpen(false);
                      }}
                      className="w-full text-left px-3 py-2 hover:bg-[#181D2C] text-red-400 flex items-center gap-2 border-t border-[#212638]"
                    >
                      <LogOut className="w-3.5 h-3.5" /> Sign Out
                    </button>
                  </div>
                </>
              )}
            </div>
          ) : (
            <div className="flex items-center gap-2">
              <Link
                to="/login"
                className="text-xs font-semibold text-[#7E88A8] hover:text-white px-3 py-2 rounded-xl transition-colors"
              >
                Sign In
              </Link>
              <Link
                to="/register"
                className="text-xs font-bold bg-[#E8546A] hover:bg-[#D44359] text-white px-4 py-2 rounded-xl shadow-md transition-all hover:scale-105"
              >
                Sign Up
              </Link>
            </div>
          )}
        </div>
      </header>

      {/* Main Content */}
      <main className="flex-1 flex flex-col">
        <Outlet />
      </main>

      {/* Global Slide-Over Cart Drawer */}
      <CartDrawer />

      {/* Section 94 Global Search Modal */}
      <CommandPalette
        isOpen={isCommandPaletteOpen}
        onClose={() => setIsCommandPaletteOpen(false)}
        onOpen={() => setIsCommandPaletteOpen(true)}
        initialScope="customer"
      />

      {/* Luxury Marketplace Footer */}
      <footer className="bg-[#0E121B] border-t border-[#212638] pt-12 pb-8 px-6 lg:px-12 text-[#7E88A8] text-xs">
        <div className="max-w-7xl mx-auto grid grid-cols-1 md:grid-cols-4 gap-8 mb-10">
          <div className="space-y-3">
            <div className="flex items-center gap-2">
              <div className="w-7 h-7 rounded-lg bg-[#E8546A] flex items-center justify-center font-heading font-bold text-white text-sm">
                B
              </div>
              <span className="font-heading font-bold text-white text-base">BOOKLINE</span>
            </div>
            <p className="text-xs text-[#7E88A8] leading-relaxed">
              The premier location-aware multi-vendor marketplace connecting discerning clients with verified local service providers and boutique retail.
            </p>
          </div>

          <div>
            <h4 className="font-bold text-white mb-3 uppercase tracking-wider text-[11px]">Discover by City</h4>
            <ul className="space-y-1.5 text-xs">
              {cities.map((c) => (
                <li key={c}>
                  <Link to={`/discover?city=${encodeURIComponent(c)}`} className="hover:text-white transition-colors">
                    Providers in {c}
                  </Link>
                </li>
              ))}
            </ul>
          </div>

          <div>
            <h4 className="font-bold text-white mb-3 uppercase tracking-wider text-[11px]">Marketplace Categories</h4>
            <ul className="space-y-1.5 text-xs">
              <li><Link to="/discover?category=Beauty" className="hover:text-white transition-colors">Beauty & Aesthetics</Link></li>
              <li><Link to="/discover?category=Healthcare" className="hover:text-white transition-colors">Dental & Healthcare</Link></li>
              <li><Link to="/discover?category=Fitness" className="hover:text-white transition-colors">Fitness & Reformer Pilates</Link></li>
              <li><Link to="/discover?category=Photography" className="hover:text-white transition-colors">Photography & Creative Studios</Link></li>
              <li><Link to="/discover?category=Professional" className="hover:text-white transition-colors">Legal & Advisory Services</Link></li>
            </ul>
          </div>

          <div>
            <h4 className="font-bold text-white mb-3 uppercase tracking-wider text-[11px]">For Businesses</h4>
            <ul className="space-y-1.5 text-xs">
              <li><Link to="/register?type=provider" className="hover:text-white transition-colors">Join as Provider</Link></li>
              <li><Link to="/provider/dashboard" className="hover:text-white transition-colors">Provider Operations OS</Link></li>
              <li><Link to="/admin" className="hover:text-white transition-colors">Platform Administration</Link></li>
            </ul>
          </div>
        </div>

        <div className="max-w-7xl mx-auto pt-6 border-t border-[#212638] flex flex-col sm:flex-row items-center justify-between gap-4 text-[11px]">
          <p>&copy; {new Date().getFullYear()} Bookline Marketplace Platform. All rights reserved.</p>
          <div className="flex gap-4">
            <span className="text-[#34D399] font-medium">&bull; System Operational</span>
            <span>Local Mode Enabled</span>
            <span>PostGIS Spatial Search</span>
          </div>
        </div>
      </footer>
    </div>
  );
};
