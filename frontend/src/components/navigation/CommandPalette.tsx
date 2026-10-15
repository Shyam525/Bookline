import React, { useState, useEffect, useRef, useCallback } from 'react';
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
  Lock,
  AlertCircle,
  Loader2,
  CheckCircle2,
  ExternalLink,
  Layers,
} from 'lucide-react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../app/providers/AuthProvider';
import { discoveryApi, ProviderCard, PublicServiceSearchResult } from '../../services/api/discovery';
import { customersApi, CustomerItem } from '../../services/api/customers';
import { servicesApi, ServiceItem } from '../../services/api/services';
import { productsApi, ProductItem } from '../../services/api/products';
import { useDebounce } from '../../hooks/useDebounce';

export interface CommandPaletteProps {
  isOpen: boolean;
  onClose: () => void;
  onOpen?: () => void;
  initialScope?: 'customer' | 'provider';
}

export interface PaletteItem {
  id: string;
  title: string;
  subtitle: string;
  category: 'Actions' | 'Navigation' | 'Providers' | 'Services' | 'Categories' | 'Customers' | 'Products' | 'Staff' | 'Settings';
  icon: any;
  path?: string;
  action?: () => void | Promise<void>;
  badge?: string;
  badgeColor?: string;
  disabled?: boolean;
  disabledReason?: string;
}

export const CommandPalette: React.FC<CommandPaletteProps> = ({
  isOpen,
  onClose,
  onOpen,
  initialScope,
}) => {
  const navigate = useNavigate();
  const location = useLocation();
  const { user, token, isAuthenticated, activeTenantId, activeBusiness, businesses } = useAuth();

  const [query, setQuery] = useState('');
  const debouncedQuery = useDebounce(query.trim(), 300);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [isSearching, setIsSearching] = useState(false);
  const [searchError, setSearchError] = useState<string | null>(null);
  const [isExecuting, setIsExecuting] = useState(false);
  const [executionFeedback, setExecutionFeedback] = useState<string | null>(null);

  const inputRef = useRef<HTMLInputElement>(null);
  const resultsContainerRef = useRef<HTMLDivElement>(null);
  const previousActiveElement = useRef<HTMLElement | null>(null);
  const abortControllerRef = useRef<AbortController | null>(null);

  // Auto-detect mode: Customer vs Provider based on user role and current path (Section 79 & 94)
  const isProviderLocation =
    initialScope === 'provider' ||
    location.pathname.startsWith('/app') ||
    location.pathname.startsWith('/provider') ||
    location.pathname.startsWith('/admin') ||
    user?.role === 'Owner' ||
    user?.role === 'Admin' ||
    user?.role === 'Staff' ||
    user?.role === 'Manager';

  const [activeScope, setActiveScope] = useState<'customer' | 'provider'>(
    initialScope || (isProviderLocation ? 'provider' : 'customer')
  );

  // Synchronize scope on open or route changes
  useEffect(() => {
    if (isOpen) {
      setActiveScope(initialScope || (isProviderLocation ? 'provider' : 'customer'));
      setQuery('');
      setSearchError(null);
      setExecutionFeedback(null);
    }
  }, [isOpen, initialScope, isProviderLocation]);

  // Focus management: Store previous focus on open, restore on close
  useEffect(() => {
    if (isOpen) {
      previousActiveElement.current = document.activeElement as HTMLElement;
      // Focus search input after animation
      setTimeout(() => {
        inputRef.current?.focus();
        inputRef.current?.select();
      }, 50);
    } else {
      if (previousActiveElement.current && typeof previousActiveElement.current.focus === 'function') {
        previousActiveElement.current.focus();
      }
    }
  }, [isOpen]);

  // Role permissions evaluation
  const userRole = (user?.role || '').toLowerCase();
  const isOwnerOrAdmin = userRole === 'owner' || userRole === 'admin';
  const isStaffOrAbove = isOwnerOrAdmin || userRole === 'staff' || userRole === 'manager' || userRole === 'receptionist';

  // Dynamic server-backed search results state
  const [customerProviders, setCustomerProviders] = useState<ProviderCard[]>([]);
  const [customerServices, setCustomerServices] = useState<PublicServiceSearchResult[]>([]);
  const [providerCustomers, setProviderCustomers] = useState<CustomerItem[]>([]);
  const [providerServices, setProviderServices] = useState<ServiceItem[]>([]);
  const [providerProducts, setProviderProducts] = useState<ProductItem[]>([]);

  // Reset or resolve search results when business/tenant context changes (Rule 4.3)
  useEffect(() => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }
    setProviderCustomers([]);
    setProviderServices([]);
    setProviderProducts([]);
  }, [activeTenantId]);

  // Server-backed search effect
  useEffect(() => {
    if (!isOpen) return;

    if (!debouncedQuery) {
      setIsSearching(false);
      setSearchError(null);
      setCustomerProviders([]);
      setCustomerServices([]);
      setProviderCustomers([]);
      setProviderServices([]);
      setProviderProducts([]);
      return;
    }

    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }

    const abortController = new AbortController();
    abortControllerRef.current = abortController;
    setIsSearching(true);
    setSearchError(null);

    if (activeScope === 'customer') {
      // Customer Scope: Search published providers and bookable services via real discovery API
      Promise.allSettled([
        discoveryApi.searchProviders({ q: debouncedQuery, pageSize: 5 }, abortController.signal),
        discoveryApi.searchServices(debouncedQuery, 5, abortController.signal),
      ])
        .then(([providersResult, servicesResult]) => {
          if (abortController.signal.aborted) return;

          if (providersResult.status === 'fulfilled') {
            setCustomerProviders(providersResult.value.items || []);
          } else {
            console.warn('Provider search failed:', providersResult.reason);
            setCustomerProviders([]);
          }

          if (servicesResult.status === 'fulfilled') {
            setCustomerServices(servicesResult.value || []);
          } else {
            console.warn('Service search failed:', servicesResult.reason);
            setCustomerServices([]);
          }
        })
        .catch((err) => {
          if (!abortController.signal.aborted) {
            setSearchError('Live search is currently unavailable. Please verify network connection.');
          }
        })
        .finally(() => {
          if (!abortController.signal.aborted) {
            setIsSearching(false);
          }
        });
    } else {
      // Provider Scope: Search customers, services, and products scoped strictly to active tenant
      const promises: Promise<any>[] = [];

      // 1. Customer search (authorized, scoped to active business)
      if (token) {
        promises.push(
          customersApi
            .getCustomers(token, debouncedQuery, false, 1, 5)
            .then((res) => ({ type: 'customers', data: res.items }))
            .catch(() => ({ type: 'customers', data: [] }))
        );
      } else {
        promises.push(Promise.resolve({ type: 'customers', data: [] }));
      }

      // 2. Services search (authorized, scoped to active business)
      if (token) {
        promises.push(
          servicesApi
            .getServices(token, { search: debouncedQuery })
            .then((items) => ({ type: 'services', data: (items || []).slice(0, 5) }))
            .catch(() => ({ type: 'services', data: [] }))
        );
      } else {
        promises.push(Promise.resolve({ type: 'services', data: [] }));
      }

      // 3. Products search (authorized, scoped to active business)
      if (token && activeTenantId) {
        promises.push(
          productsApi
            .getProducts(token, activeTenantId, { search: debouncedQuery })
            .then((items) => ({ type: 'products', data: (items || []).slice(0, 5) }))
            .catch(() => ({ type: 'products', data: [] }))
        );
      } else {
        promises.push(Promise.resolve({ type: 'products', data: [] }));
      }

      Promise.all(promises)
        .then((results) => {
          if (abortController.signal.aborted) return;
          results.forEach((r) => {
            if (r.type === 'customers') setProviderCustomers(r.data);
            if (r.type === 'services') setProviderServices(r.data);
            if (r.type === 'products') setProviderProducts(r.data);
          });
        })
        .catch(() => {
          if (!abortController.signal.aborted) {
            setSearchError('Provider search encountered an error.');
          }
        })
        .finally(() => {
          if (!abortController.signal.aborted) {
            setIsSearching(false);
          }
        });
    }

    return () => {
      abortController.abort();
    };
  }, [debouncedQuery, activeScope, isOpen, token, activeTenantId]);

  // Construct items pool based on scope, state, and permissions
  const items: PaletteItem[] = [];

  if (activeScope === 'customer') {
    // If no query typed, show customer core actions and navigation
    if (!debouncedQuery) {
      items.push(
        {
          id: 'c-nav-my-appointments',
          title: 'My Appointments',
          subtitle: isAuthenticated
            ? 'View upcoming, past, and rescheduled appointments'
            : 'Sign in to access your appointment directory',
          category: 'Actions',
          icon: Calendar,
          action: () => {
            if (isAuthenticated) {
              navigate('/appointments');
            } else {
              navigate('/login?redirect=/appointments');
            }
          },
          badge: isAuthenticated ? 'Directory' : 'Sign In Required',
          badgeColor: isAuthenticated ? 'bg-emerald-500/20 text-emerald-400' : 'bg-amber-500/20 text-amber-400',
        },
        {
          id: 'c-nav-favorites',
          title: 'My Favorites & Saved Providers',
          subtitle: isAuthenticated
            ? 'View bookmarked salons, spas, and wellness studios'
            : 'Sign in to access your saved providers',
          category: 'Actions',
          icon: Sparkles,
          action: () => {
            if (isAuthenticated) {
              navigate('/favorites');
            } else {
              navigate('/login?redirect=/favorites');
            }
          },
          badge: isAuthenticated ? 'Saved' : 'Sign In Required',
          badgeColor: isAuthenticated ? 'bg-pink-500/20 text-pink-400' : 'bg-amber-500/20 text-amber-400',
        },
        {
          id: 'c-nav-discover',
          title: 'Explore Marketplace Directory',
          subtitle: 'Search published businesses, browse categories, and interactive map',
          category: 'Navigation',
          icon: Building2,
          path: '/discover',
        },
        {
          id: 'c-nav-dashboard',
          title: 'Customer Dashboard & Account',
          subtitle: 'Profile settings, saved payment methods, and notification feeds',
          category: 'Navigation',
          icon: Users,
          action: () => {
            if (isAuthenticated) {
              navigate('/dashboard');
            } else {
              navigate('/login?redirect=/dashboard');
            }
          },
        },
        // Categories
        {
          id: 'c-cat-beauty',
          title: 'Beauty & Wellness',
          subtitle: 'Holistic rituals, aesthetic clinics, facials & spas',
          category: 'Categories',
          icon: Tag,
          path: '/discover?category=Beauty',
        },
        {
          id: 'c-cat-hair',
          title: 'Hair & Styling',
          subtitle: 'Master stylists, balayage, keratin, barbering',
          category: 'Categories',
          icon: Tag,
          path: '/discover?category=Hair',
        },
        {
          id: 'c-cat-healthcare',
          title: 'Dental & Healthcare',
          subtitle: 'Oral aesthetics, dermatology, physiotherapy consultations',
          category: 'Categories',
          icon: Tag,
          path: '/discover?category=Healthcare',
        },
        {
          id: 'c-cat-fitness',
          title: 'Fitness & Reformer Pilates',
          subtitle: 'Athletic recovery, reformer clinics, private coaching',
          category: 'Categories',
          icon: Tag,
          path: '/discover?category=Fitness',
        }
      );
    } else {
      // Query typed: Render real live search results
      // 1. Published Providers
      customerProviders.forEach((p) => {
        items.push({
          id: `p-prov-${p.id}`,
          title: p.name,
          subtitle: `${p.city || 'Available'} · ${p.rating ? `${p.rating.toFixed(1)} ★ (${p.reviewCount} reviews)` : 'Newly Listed'} · From ₹${p.startingPrice}`,
          category: 'Providers',
          icon: Building2,
          path: `/business/${p.slug}`,
          badge: p.isVerified ? 'Verified' : undefined,
          badgeColor: 'bg-emerald-500/20 text-emerald-400',
        });
      });

      // 2. Bookable Services
      customerServices.forEach((s) => {
        items.push({
          id: `p-srv-${s.id}`,
          title: s.name,
          subtitle: `${s.durationMinutes} mins · ₹${s.price.toLocaleString()} · ${s.providerName} (${s.providerCity})`,
          category: 'Services',
          icon: Sparkles,
          path: `/business/${s.providerSlug}?serviceId=${s.id}&book=true`,
          badge: s.providerCategory,
          badgeColor: 'bg-rose-500/20 text-rose-400',
        });
      });
    }
  } else {
    // Provider Scope
    if (!debouncedQuery) {
      // Core provider commands (Section 79.2)
      items.push(
        {
          id: 'p-cmd-new-appointment',
          title: 'New Appointment',
          subtitle: isStaffOrAbove
            ? `Schedule appointment in ${activeBusiness?.name || 'current business'}`
            : 'Requires provider staff or admin permissions',
          category: 'Actions',
          icon: Calendar,
          action: () => {
            if (isStaffOrAbove) {
              navigate('/provider/calendar?create=true');
            }
          },
          disabled: !isStaffOrAbove,
          disabledReason: 'Staff role required',
          badge: isStaffOrAbove ? 'Quick Create' : 'Unauthorized',
          badgeColor: isStaffOrAbove ? 'bg-emerald-500/20 text-emerald-400' : 'bg-red-500/20 text-red-400',
        },
        {
          id: 'p-cmd-open-calendar',
          title: 'Open Calendar',
          subtitle: `Master appointment calendar · ${activeBusiness?.name || 'Active Business'}`,
          category: 'Actions',
          icon: Clock,
          path: '/provider/calendar',
        },
        {
          id: 'p-cmd-find-customer',
          title: 'Find Customer',
          subtitle: 'Search authorized customer directory scoped to active business',
          category: 'Actions',
          icon: Users,
          path: '/provider/customers',
        },
        {
          id: 'p-cmd-add-service',
          title: 'Add Service',
          subtitle: isOwnerOrAdmin
            ? 'Publish new service offering, duration, buffer & pricing'
            : 'Adding services requires Owner or Admin role',
          category: 'Actions',
          icon: Briefcase,
          action: () => {
            if (isOwnerOrAdmin) {
              navigate('/provider/services?add=true');
            }
          },
          disabled: !isOwnerOrAdmin,
          disabledReason: 'Owner/Admin only',
          badge: isOwnerOrAdmin ? undefined : 'Restricted',
          badgeColor: 'bg-red-500/20 text-red-400',
        },
        {
          id: 'p-cmd-add-product',
          title: 'Add Product',
          subtitle: isOwnerOrAdmin
            ? 'Add retail merchandise, inventory tracking & stock levels'
            : 'Adding retail products requires Owner or Admin role',
          category: 'Actions',
          icon: ShoppingBag,
          action: () => {
            if (isOwnerOrAdmin) {
              navigate('/provider/products?add=true');
            }
          },
          disabled: !isOwnerOrAdmin,
          disabledReason: 'Owner/Admin only',
          badge: isOwnerOrAdmin ? undefined : 'Restricted',
          badgeColor: 'bg-red-500/20 text-red-400',
        },
        {
          id: 'p-cmd-add-staff',
          title: 'Add Staff Member',
          subtitle: isOwnerOrAdmin
            ? 'Invite specialist, configure working schedules & services'
            : 'Staff administration requires Owner or Admin role',
          category: 'Actions',
          icon: Shield,
          action: () => {
            if (isOwnerOrAdmin) {
              navigate('/provider/staff?add=true');
            }
          },
          disabled: !isOwnerOrAdmin,
          disabledReason: 'Owner/Admin only',
          badge: isOwnerOrAdmin ? undefined : 'Restricted',
          badgeColor: 'bg-red-500/20 text-red-400',
        },
        // Unambiguous Settings destinations (Section 79.2)
        {
          id: 'p-set-storefront',
          title: 'Storefront & Profile Settings',
          subtitle: 'Business name, public bio, category, logo, cover & gallery',
          category: 'Settings',
          icon: Settings,
          path: '/provider/storefront',
        },
        {
          id: 'p-set-team',
          title: 'Team Roles & Permissions Settings',
          subtitle: 'Manage active team members, revoke access, and RBAC policies',
          category: 'Settings',
          icon: Shield,
          path: '/provider/team',
          disabled: !isOwnerOrAdmin,
          disabledReason: 'Owner/Admin only',
        },
        {
          id: 'p-set-financials',
          title: 'Financials, Payouts & Deposit Settings',
          subtitle: 'Bank accounts, payout ledger, commission rates & cancellation fees',
          category: 'Settings',
          icon: Settings,
          path: '/provider/payments',
          disabled: !isOwnerOrAdmin,
          disabledReason: 'Owner/Admin only',
        },
        {
          id: 'p-set-locations',
          title: 'Locations & Operating Facilities',
          subtitle: 'Physical addresses, operating rooms, chairs & coordinate geocodes',
          category: 'Settings',
          icon: MapPin,
          path: '/provider/locations',
        },
        {
          id: 'p-set-availability',
          title: 'Availability & Scheduling Rules',
          subtitle: 'Default operating hours, notice periods, slot buffers & booking policies',
          category: 'Settings',
          icon: Clock,
          path: '/provider/availability',
        }
      );
    } else {
      // Query typed: Render real live search results for current business
      // 1. Customers
      providerCustomers.forEach((c) => {
        items.push({
          id: `pr-cust-${c.id}`,
          title: c.fullName || `${c.firstName} ${c.lastName}`.trim(),
          subtitle: `${c.email || 'No email'} · ${c.phone || 'No phone'} · ${c.totalBookingsCount} bookings`,
          category: 'Customers',
          icon: Users,
          path: `/provider/customers?search=${encodeURIComponent(c.fullName || c.firstName)}`,
        });
      });

      // 2. Services
      providerServices.forEach((s) => {
        items.push({
          id: `pr-srv-${s.id}`,
          title: s.name,
          subtitle: `${s.durationMinutes} mins · ₹${s.price.toLocaleString()} · ${s.categoryName || 'General'}`,
          category: 'Services',
          icon: Briefcase,
          path: `/provider/services?search=${encodeURIComponent(s.name)}`,
        });
      });

      // 3. Products
      providerProducts.forEach((p) => {
        items.push({
          id: `pr-prod-${p.id}`,
          title: p.name,
          subtitle: `Stock: ${p.availableQuantity ?? p.stockQuantity} units · ₹${p.price.toLocaleString()}`,
          category: 'Products',
          icon: ShoppingBag,
          path: `/provider/products?search=${encodeURIComponent(p.name)}`,
        });
      });
    }
  }

  // Ensure selectedIndex is within valid range
  useEffect(() => {
    setSelectedIndex(0);
  }, [items.length, activeScope, debouncedQuery]);

  // Execute selected item with duplicate-execution guard (Rule 4.4)
  const handleExecute = useCallback(
    async (item: PaletteItem) => {
      if (!item || item.disabled || isExecuting) return;

      setIsExecuting(true);
      try {
        if (item.action) {
          await item.action();
          onClose();
        } else if (item.path) {
          navigate(item.path);
          onClose();
        }
      } catch (err: any) {
        console.error('Command execution failed:', err);
        setExecutionFeedback(err.message || 'Operation could not be performed.');
      } finally {
        setIsExecuting(false);
      }
    },
    [isExecuting, navigate, onClose]
  );

  // Keyboard navigation & global shortcuts (Rule 4.4)
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Toggle palette: Ctrl+K or ⌘+K
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
        e.preventDefault();
        onClose();
      } else if (e.key === 'ArrowDown') {
        e.preventDefault();
        if (items.length > 0) {
          setSelectedIndex((prev) => (prev + 1) % items.length);
        }
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        if (items.length > 0) {
          setSelectedIndex((prev) => (prev - 1 + items.length) % items.length);
        }
      } else if (e.key === 'Enter') {
        e.preventDefault();
        const selected = items[selectedIndex];
        if (selected) {
          if (selected.disabled) {
            setExecutionFeedback(selected.disabledReason || 'Action is currently unavailable.');
          } else {
            handleExecute(selected);
          }
        }
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose, onOpen, items, selectedIndex, handleExecute]);

  // Scroll active item into view
  useEffect(() => {
    if (resultsContainerRef.current) {
      const activeEl = resultsContainerRef.current.querySelector(
        `#palette-item-${items[selectedIndex]?.id}`
      ) as HTMLElement;
      if (activeEl) {
        activeEl.scrollIntoView({ block: 'nearest' });
      }
    }
  }, [selectedIndex, items]);

  if (!isOpen) return null;

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-label="Global Command Palette"
      className="fixed inset-0 z-50 flex items-start justify-center pt-16 sm:pt-24 p-4 bg-black/80 backdrop-blur-md animate-fadeIn"
    >
      <div className="fixed inset-0 -z-10" onClick={onClose} aria-hidden="true" />

      <div className="bg-[#111520] border border-[#212638] rounded-2xl w-full max-w-2xl shadow-2xl overflow-hidden flex flex-col max-h-[82vh] transition-all">
        {/* Scope Pill Selector & Active Business Context (Section 79 & 94) */}
        <div className="flex flex-wrap items-center justify-between gap-2 px-4 py-2.5 bg-[#0C0F17] border-b border-[#212638] text-xs">
          <div className="flex items-center gap-2">
            <span className="text-[#7E88A8] font-bold text-[10px] uppercase tracking-wider">
              Search Scope:
            </span>
            <div className="flex bg-[#181D2C] p-0.5 rounded-lg border border-[#212638]" role="tablist">
              <button
                type="button"
                role="tab"
                aria-selected={activeScope === 'customer'}
                onClick={() => {
                  setActiveScope('customer');
                  setQuery('');
                }}
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
                role="tab"
                aria-selected={activeScope === 'provider'}
                onClick={() => {
                  setActiveScope('provider');
                  setQuery('');
                }}
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

          <div className="flex items-center gap-2 text-[11px] text-[#7E88A8]">
            {activeScope === 'provider' && activeBusiness && (
              <span className="inline-flex items-center gap-1 px-2 py-0.5 bg-[#181D2C] border border-[#212638] rounded-full text-[#ECEFFE] font-medium truncate max-w-[180px]">
                <Building2 className="w-3 h-3 text-[#E8546A] shrink-0" />
                <span className="truncate">{activeBusiness.name}</span>
              </span>
            )}
            <span className="hidden sm:inline">
              {activeScope === 'customer'
                ? 'providers · services · appointments'
                : 'customers · calendar · services · staff'}
            </span>
          </div>
        </div>

        {/* Input Bar */}
        <div className="flex items-center px-4 border-b border-[#212638] bg-[#141926]/60 relative">
          {isSearching ? (
            <Loader2 className="w-5 h-5 text-[#E8546A] shrink-0 animate-spin" />
          ) : (
            <Search className="w-5 h-5 text-[#7E88A8] shrink-0" />
          )}

          <input
            ref={inputRef}
            type="text"
            role="combobox"
            aria-expanded="true"
            aria-autocomplete="list"
            aria-controls="command-palette-results"
            aria-activedescendant={items[selectedIndex] ? `palette-item-${items[selectedIndex].id}` : undefined}
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder={
              activeScope === 'customer'
                ? 'Search published providers, bookable services, or appointments...'
                : `Search customers, services, products in ${activeBusiness?.name || 'active business'}...`
            }
            className="w-full bg-transparent px-3 py-4 text-sm text-white focus:outline-none placeholder:text-[#7E88A8]"
          />

          <kbd className="hidden sm:inline-flex items-center gap-1 px-2 py-1 text-[10px] font-mono font-medium text-[#7E88A8] bg-[#181D2C] border border-[#212638] rounded shrink-0">
            <Command className="w-3 h-3" /> K
          </kbd>
        </div>

        {/* Inline Feedback Banner */}
        {executionFeedback && (
          <div className="px-4 py-2 bg-amber-500/10 border-b border-amber-500/30 flex items-center justify-between text-xs text-amber-300">
            <div className="flex items-center gap-2">
              <AlertCircle className="w-4 h-4 shrink-0 text-amber-400" />
              <span>{executionFeedback}</span>
            </div>
            <button
              onClick={() => setExecutionFeedback(null)}
              className="text-amber-400 hover:text-white font-bold text-xs"
            >
              ✕
            </button>
          </div>
        )}

        {/* Error Banner */}
        {searchError && (
          <div className="px-4 py-2 bg-rose-500/10 border-b border-rose-500/30 flex items-center gap-2 text-xs text-rose-300">
            <AlertCircle className="w-4 h-4 shrink-0 text-rose-400" />
            <span>{searchError}</span>
          </div>
        )}

        {/* Results List */}
        <div
          id="command-palette-results"
          role="listbox"
          aria-label="Commands and Search Results"
          ref={resultsContainerRef}
          className="p-2 overflow-y-auto space-y-1 max-h-[460px]"
        >
          {items.length === 0 ? (
            <div className="p-10 text-center space-y-3">
              <Search className="w-8 h-8 text-[#7E88A8] mx-auto opacity-40" />
              <p className="text-sm font-semibold text-white">No results found for "{debouncedQuery}"</p>
              <p className="text-xs text-[#7E88A8] max-w-sm mx-auto">
                {activeScope === 'customer'
                  ? 'No matching providers or bookable services. Try checking spelling or switching to Provider OS.'
                  : `No records found in ${activeBusiness?.name || 'this business'}. Results are strictly isolated to your active business.`}
              </p>
            </div>
          ) : (
            items.map((item, idx) => {
              const Icon = item.icon;
              const isSelected = idx === selectedIndex;
              const isDisabled = item.disabled;

              return (
                <button
                  key={item.id}
                  id={`palette-item-${item.id}`}
                  role="option"
                  aria-selected={isSelected}
                  aria-disabled={isDisabled}
                  type="button"
                  disabled={isDisabled || isExecuting}
                  onClick={() => handleExecute(item)}
                  onMouseEnter={() => setSelectedIndex(idx)}
                  className={`w-full flex items-center justify-between p-3 rounded-xl text-left transition-all ${
                    isDisabled
                      ? 'opacity-50 cursor-not-allowed bg-transparent'
                      : isSelected
                      ? 'bg-[#181D2C] border border-[#E8546A]/40 shadow-md'
                      : 'hover:bg-[#181D2C]/60 border border-transparent'
                  }`}
                >
                  <div className="flex items-center gap-3 truncate">
                    <div
                      className={`w-9 h-9 rounded-xl border flex items-center justify-center shrink-0 transition-colors ${
                        isDisabled
                          ? 'bg-[#141926] border-[#212638] text-[#525B75]'
                          : isSelected
                          ? 'bg-[#E8546A]/20 border-[#E8546A] text-[#E8546A]'
                          : 'bg-[#181D2C] border-[#212638] text-[#7E88A8]'
                      }`}
                    >
                      {isDisabled ? <Lock className="w-4 h-4" /> : <Icon className="w-4 h-4" />}
                    </div>

                    <div className="truncate">
                      <div className="flex items-center gap-2">
                        <p
                          className={`text-sm font-semibold truncate ${
                            isDisabled
                              ? 'text-[#7E88A8]'
                              : isSelected
                              ? 'text-[#E8546A]'
                              : 'text-white'
                          }`}
                        >
                          {item.title}
                        </p>
                        {item.badge && (
                          <span
                            className={`text-[10px] px-1.5 py-0.5 rounded font-bold ${
                              item.badgeColor || 'bg-[#34D399]/20 text-[#34D399]'
                            }`}
                          >
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
                    {!isDisabled && (
                      <ArrowRight
                        className={`w-4 h-4 transition-opacity ${
                          isSelected ? 'opacity-100 text-[#E8546A]' : 'opacity-0'
                        }`}
                      />
                    )}
                  </div>
                </button>
              );
            })
          )}
        </div>

        {/* Footer Navigation Bar */}
        <div className="px-4 py-2.5 bg-[#0C0F17] border-t border-[#212638] flex items-center justify-between text-[11px] text-[#7E88A8]">
          <div className="flex items-center gap-3">
            <span>
              <kbd className="font-mono bg-[#181D2C] px-1.5 py-0.5 rounded border border-[#212638]">
                ↑↓
              </kbd>{' '}
              Navigate
            </span>
            <span>
              <kbd className="font-mono bg-[#181D2C] px-1.5 py-0.5 rounded border border-[#212638]">
                Enter
              </kbd>{' '}
              Open
            </span>
            <span>
              <kbd className="font-mono bg-[#181D2C] px-1.5 py-0.5 rounded border border-[#212638]">
                Esc
              </kbd>{' '}
              Close
            </span>
          </div>
          <span className="font-semibold text-white/80 hidden sm:inline">
            Section 79 Global Command Palette
          </span>
        </div>
      </div>
    </div>
  );
};
