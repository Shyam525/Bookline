import React, { useState, useEffect, useMemo } from 'react';
import {
  Search,
  Calendar,
  Users,
  Briefcase,
  Shield,
  Settings,
  ShoppingBag,
  Building2,
  Tag,
  ArrowRight,
  Command,
  Sparkles,
  MapPin,
  Clock,
  Layers,
} from 'lucide-react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../app/providers/AuthProvider';

export interface CommandPaletteProps {
  isOpen: boolean;
  onClose: () => void;
  onOpen?: () => void;
  initialScope?: 'customer' | 'provider';
}

interface PaletteItem {
  id: string;
  title: string;
  subtitle: string;
  category: 'Providers' | 'Services' | 'Categories' | 'Appointments' | 'Customers' | 'Products' | 'Staff' | 'Settings';
  icon: any;
  path: string;
  badge?: string;
}

export const CommandPalette: React.FC<CommandPaletteProps> = ({
  isOpen,
  onClose,
  onOpen,
  initialScope,
}) => {
  const navigate = useNavigate();
  const location = useLocation();
  const { user } = useAuth();
  const [query, setQuery] = useState('');
  const [selectedIndex, setSelectedIndex] = useState(0);

  // Auto-detect mode: Customer vs Provider based on user role and current path (Section 94)
  const isProviderScope =
    initialScope === 'provider' ||
    location.pathname.startsWith('/app') ||
    location.pathname.startsWith('/provider') ||
    user?.role === 'Owner' ||
    user?.role === 'Staff' ||
    user?.role === 'Admin';

  const [activeScope, setActiveScope] = useState<'customer' | 'provider'>(
    initialScope || (isProviderScope ? 'provider' : 'customer')
  );

  useEffect(() => {
    setActiveScope(initialScope || (isProviderScope ? 'provider' : 'customer'));
  }, [isProviderScope, initialScope, isOpen]);

  // Section 94 Canonical Items
  const customerItems: PaletteItem[] = [
    // Providers
    { id: 'c-p-1', title: 'Aura Wellness & Spa', subtitle: 'Ahmedabad · 4.9 ★ · 142 reviews', category: 'Providers', icon: Building2, path: '/business/aura-wellness', badge: 'Verified' },
    { id: 'c-p-2', title: 'Glow Hair & Beauty Lounge', subtitle: 'Mumbai · 4.8 ★ · 98 reviews', category: 'Providers', icon: Building2, path: '/business/glow-lounge', badge: 'Verified' },
    { id: 'c-p-3', title: 'Apex Men Grooming Co', subtitle: 'Bangalore · 4.7 ★ · 76 reviews', category: 'Providers', icon: Building2, path: '/business/apex-grooming', badge: 'Verified' },
    { id: 'c-p-4', title: 'Zenith Dental Aesthetics', subtitle: 'Ahmedabad · 4.9 ★ · 112 reviews', category: 'Providers', icon: Building2, path: '/business/zenith-dental', badge: 'Verified' },
    { id: 'c-p-5', title: 'Lumina Portraiture Studio', subtitle: 'Surat · 4.85 ★ · 41 reviews', category: 'Providers', icon: Building2, path: '/business/lumina-studio', badge: 'Verified' },

    // Services
    { id: 'c-s-1', title: 'Signature Aromatherapy Massage', subtitle: '60 mins · From ₹1,800 · Aura Wellness', category: 'Services', icon: Sparkles, path: '/discover?service=Aromatherapy' },
    { id: 'c-s-2', title: 'Balayage & Color Gloss', subtitle: '90 mins · From ₹2,400 · Glow Studio', category: 'Services', icon: Sparkles, path: '/discover?service=Balayage' },
    { id: 'c-s-3', title: 'Deep Hydration Facial Ritual', subtitle: '45 mins · From ₹1,200 · Aura Wellness', category: 'Services', icon: Sparkles, path: '/discover?service=Facial' },
    { id: 'c-s-4', title: 'Precision Beard Sculpt & Haircut', subtitle: '45 mins · From ₹650 · Apex Grooming', category: 'Services', icon: Sparkles, path: '/discover?service=Haircut' },
    { id: 'c-s-5', title: 'Laser Teeth Whitening Consultation', subtitle: '45 mins · From ₹3,500 · Zenith Dental', category: 'Services', icon: Sparkles, path: '/discover?service=Dental' },

    // Categories
    { id: 'c-cat-1', title: 'Beauty & Wellness', subtitle: 'Spas, holistic rituals, and skin treatments', category: 'Categories', icon: Tag, path: '/discover?category=Beauty' },
    { id: 'c-cat-2', title: 'Hair & Styling', subtitle: 'Editorial master stylists, balayage, keratin', category: 'Categories', icon: Tag, path: '/discover?category=Hair' },
    { id: 'c-cat-3', title: 'Dental & Healthcare', subtitle: 'Aesthetic veneers, dental hygiene, oral care', category: 'Categories', icon: Tag, path: '/discover?category=Healthcare' },
    { id: 'c-cat-4', title: 'Fitness & Reformer Pilates', subtitle: 'Private athletic coaching, posture clinics', category: 'Categories', icon: Tag, path: '/discover?category=Fitness' },
    { id: 'c-cat-5', title: 'Photography & Creative Studios', subtitle: 'Corporate headshots, editorial campaigns', category: 'Categories', icon: Tag, path: '/discover?category=Photography' },

    // Appointments
    { id: 'c-a-1', title: 'My Bookings Directory', subtitle: 'View upcoming, past, and rescheduled appointments', category: 'Appointments', icon: Calendar, path: '/appointments' },
    { id: 'c-a-2', title: 'Upcoming: Aromatherapy with Master Aarav', subtitle: 'Saturday at 10:00 AM · Aura Wellness Bodakdev', category: 'Appointments', icon: Clock, path: '/appointments?tab=upcoming' },
    { id: 'c-a-3', title: 'Completed: Balayage Treatment', subtitle: '14 Sept 2026 · Glow Hair Lounge Bandra', category: 'Appointments', icon: Clock, path: '/appointments?tab=past' },
  ];

  const providerItems: PaletteItem[] = [
    // Customers
    { id: 'p-c-1', title: 'Customer Directory', subtitle: 'Search client profiles, visit history, notes', category: 'Customers', icon: Users, path: '/app/customers' },
    { id: 'p-c-2', title: 'Priya Shah (VIP Client)', subtitle: '6 visits · ₹14,200 lifetime · No no-shows', category: 'Customers', icon: Users, path: '/app/customers?search=Priya' },
    { id: 'p-c-3', title: 'Rohan Mehta', subtitle: 'Last visited 3 days ago · 3 bookings', category: 'Customers', icon: Users, path: '/app/customers?search=Rohan' },

    // Appointments
    { id: 'p-a-1', title: 'Master Appointment Calendar', subtitle: 'Day, Week, Resource grid, slot management', category: 'Appointments', icon: Calendar, path: '/app/calendar' },
    { id: 'p-a-2', title: 'Today’s Bookings Schedule', subtitle: 'View today’s appointments & arrival status', category: 'Appointments', icon: Clock, path: '/app/calendar?view=day' },
    { id: 'p-a-3', title: 'New Walk-In Appointment', subtitle: 'Create manual booking or slot block', category: 'Appointments', icon: Calendar, path: '/app/calendar?create=true' },

    // Services
    { id: 'p-s-1', title: 'Services Catalog', subtitle: 'Configure durations, pricing, buffers, and resources', category: 'Services', icon: Briefcase, path: '/app/services' },
    { id: 'p-s-2', title: 'Add New Treatment / Service', subtitle: 'Publish new offering with online booking toggles', category: 'Services', icon: Briefcase, path: '/app/services?add=true' },

    // Products
    { id: 'p-pr-1', title: 'Boutique Retail Inventory', subtitle: 'Track physical stock, reserve quantities, sales', category: 'Products', icon: ShoppingBag, path: '/app/products' },
    { id: 'p-pr-2', title: 'Botanical Hair Repair Serum', subtitle: 'Stock: 18 units available · ₹1,200', category: 'Products', icon: ShoppingBag, path: '/app/products?search=Serum' },
    { id: 'p-pr-3', title: 'Organic Lavender Massage Oil', subtitle: 'Stock: 24 units available · ₹850', category: 'Products', icon: ShoppingBag, path: '/app/products?search=Oil' },

    // Staff
    { id: 'p-st-1', title: 'Staff Roster & Working Hours', subtitle: 'Manage specialist schedules, time off, skills', category: 'Staff', icon: Shield, path: '/app/staff' },
    { id: 'p-st-2', title: 'Team Roles & Permissions (Section 95)', subtitle: 'Owner, Admin, Manager, Receptionist, Staff, Viewer', category: 'Staff', icon: Shield, path: '/app/staff?tab=team' },

    // Settings
    { id: 'p-se-1', title: 'Storefront & Business Profile', subtitle: 'Category, description, logo, cover image, slug', category: 'Settings', icon: Settings, path: '/app/settings/storefront' },
    { id: 'p-se-2', title: 'Financials & Payout Ledger', subtitle: 'Available payouts, commission take rates, banking', category: 'Settings', icon: Settings, path: '/app/payments' },
    { id: 'p-se-3', title: 'Booking Rules & Deposit Policy', subtitle: 'Configure None, Fixed, or Percentage deposits', category: 'Settings', icon: Settings, path: '/app/settings/storefront#deposits' },
  ];

  const currentPool = activeScope === 'customer' ? customerItems : providerItems;

  const filteredItems = useMemo(() => {
    if (!query.trim()) return currentPool;
    const q = query.toLowerCase();
    return currentPool.filter(
      (item) =>
        item.title.toLowerCase().includes(q) ||
        item.subtitle.toLowerCase().includes(q) ||
        item.category.toLowerCase().includes(q)
    );
  }, [currentPool, query]);

  useEffect(() => {
    setSelectedIndex(0);
  }, [filteredItems, activeScope]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && (e.key === 'k' || e.key === 'K')) {
        e.preventDefault();
        if (isOpen) {
          onClose();
        } else if (onOpen) {
          onOpen();
        }
        return;
      }
      if (!isOpen) return;

      if (e.key === 'Escape') {
        onClose();
      } else if (e.key === 'ArrowDown') {
        e.preventDefault();
        setSelectedIndex((prev) => (prev + 1) % Math.max(1, filteredItems.length));
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        setSelectedIndex((prev) => (prev - 1 + filteredItems.length) % Math.max(1, filteredItems.length));
      } else if (e.key === 'Enter') {
        e.preventDefault();
        if (filteredItems[selectedIndex]) {
          navigate(filteredItems[selectedIndex].path);
          onClose();
        }
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose, onOpen, filteredItems, selectedIndex, navigate]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center pt-16 sm:pt-24 p-4 bg-black/75 backdrop-blur-md animate-fadeIn">
      <div className="fixed inset-0 -z-10" onClick={onClose} />
      <div className="bg-[#111520] border border-[#212638] rounded-2xl w-full max-w-2xl shadow-2xl overflow-hidden flex flex-col max-h-[80vh]">
        {/* Scope Pill Selector (Section 94: CUSTOMER vs PROVIDER) */}
        <div className="flex items-center justify-between px-4 py-2.5 bg-[#0C0F17] border-b border-[#212638] text-xs">
          <div className="flex items-center gap-2">
            <span className="text-[#7E88A8] font-bold text-[10px] uppercase tracking-wider">Search Scope:</span>
            <div className="flex bg-[#181D2C] p-0.5 rounded-lg border border-[#212638]">
              <button
                type="button"
                onClick={() => setActiveScope('customer')}
                className={`px-3 py-1 rounded-md font-semibold text-xs transition-colors flex items-center gap-1.5 ${
                  activeScope === 'customer'
                    ? 'bg-[#E8546A] text-white shadow-sm'
                    : 'text-[#7E88A8] hover:text-white'
                }`}
              >
                <Sparkles className="w-3 h-3" />
                <span>Customer</span>
              </button>
              <button
                type="button"
                onClick={() => setActiveScope('provider')}
                className={`px-3 py-1 rounded-md font-semibold text-xs transition-colors flex items-center gap-1.5 ${
                  activeScope === 'provider'
                    ? 'bg-[#E8546A] text-white shadow-sm'
                    : 'text-[#7E88A8] hover:text-white'
                }`}
              >
                <Building2 className="w-3 h-3" />
                <span>Provider OS</span>
              </button>
            </div>
          </div>
          <span className="text-[11px] text-[#7E88A8] hidden sm:inline">
            {activeScope === 'customer'
              ? 'providers · services · categories · appointments'
              : 'customers · appointments · services · products · staff · settings'}
          </span>
        </div>

        {/* Input Bar */}
        <div className="flex items-center px-4 border-b border-[#212638] bg-[#141926]/60">
          <Search className="w-5 h-5 text-[#7E88A8] shrink-0" />
          <input
            type="text"
            autoFocus
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder={
              activeScope === 'customer'
                ? 'Search providers, services, categories, or my appointments...'
                : 'Search customers, calendar, services, boutique products, staff, settings...'
            }
            className="w-full bg-transparent px-3 py-4 text-sm text-white focus:outline-none placeholder:text-[#7E88A8]"
          />
          <kbd className="hidden sm:inline-flex items-center gap-1 px-2 py-1 text-[10px] font-mono font-medium text-[#7E88A8] bg-[#181D2C] border border-[#212638] rounded">
            <Command className="w-3 h-3" /> K
          </kbd>
        </div>

        {/* Results List */}
        <div className="p-2 overflow-y-auto space-y-1 max-h-[420px]">
          {filteredItems.length === 0 ? (
            <div className="p-10 text-center space-y-2">
              <Search className="w-8 h-8 text-[#7E88A8] mx-auto opacity-40" />
              <p className="text-sm font-semibold text-white">No results found for "{query}"</p>
              <p className="text-xs text-[#7E88A8]">
                Try switching search scope between Customer and Provider, or checking spelling.
              </p>
            </div>
          ) : (
            filteredItems.map((item, idx) => {
              const Icon = item.icon;
              const isSelected = idx === selectedIndex;
              return (
                <button
                  key={item.id}
                  onClick={() => {
                    navigate(item.path);
                    onClose();
                  }}
                  onMouseEnter={() => setSelectedIndex(idx)}
                  className={`w-full flex items-center justify-between p-3 rounded-xl text-left transition-all ${
                    isSelected
                      ? 'bg-[#181D2C] border border-[#E8546A]/40 shadow-md'
                      : 'hover:bg-[#181D2C]/60 border border-transparent'
                  }`}
                >
                  <div className="flex items-center gap-3 truncate">
                    <div
                      className={`w-9 h-9 rounded-xl border flex items-center justify-center shrink-0 transition-colors ${
                        isSelected
                          ? 'bg-[#E8546A]/20 border-[#E8546A] text-[#E8546A]'
                          : 'bg-[#181D2C] border-[#212638] text-[#7E88A8]'
                      }`}
                    >
                      <Icon className="w-4 h-4" />
                    </div>
                    <div className="truncate">
                      <div className="flex items-center gap-2">
                        <p className={`text-sm font-semibold truncate ${isSelected ? 'text-[#E8546A]' : 'text-white'}`}>
                          {item.title}
                        </p>
                        {item.badge && (
                          <span className="text-[10px] px-1.5 py-0.5 rounded bg-[#34D399]/20 text-[#34D399] font-bold">
                            {item.badge}
                          </span>
                        )}
                      </div>
                      <p className="text-xs text-[#7E88A8] truncate">{item.subtitle}</p>
                    </div>
                  </div>

                  <div className="flex items-center gap-2 shrink-0 ml-3">
                    <span className="text-[10px] px-2 py-0.5 rounded-full bg-[#111520] border border-[#212638] text-[#7E88A8] uppercase font-bold tracking-wider">
                      {item.category}
                    </span>
                    <ArrowRight
                      className={`w-4 h-4 transition-opacity ${
                        isSelected ? 'opacity-100 text-[#E8546A]' : 'opacity-0'
                      }`}
                    />
                  </div>
                </button>
              );
            })
          )}
        </div>

        {/* Footer Shortcut Bar */}
        <div className="px-4 py-2.5 bg-[#0C0F17] border-t border-[#212638] flex items-center justify-between text-[11px] text-[#7E88A8]">
          <div className="flex items-center gap-3">
            <span><kbd className="font-mono bg-[#181D2C] px-1.5 py-0.5 rounded border border-[#212638]">↑↓</kbd> Navigate</span>
            <span><kbd className="font-mono bg-[#181D2C] px-1.5 py-0.5 rounded border border-[#212638]">Enter</kbd> Open</span>
            <span><kbd className="font-mono bg-[#181D2C] px-1.5 py-0.5 rounded border border-[#212638]">Esc</kbd> Close</span>
          </div>
          <span className="font-semibold text-white/80">Section 94 Global Search</span>
        </div>
      </div>
    </div>
  );
};
