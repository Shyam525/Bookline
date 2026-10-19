/**
 * Bookline Route Registry & Navigation Architecture
 * Sections 105 - 109 Master Specifications
 */

export type RouteAccessLevel = 'public' | 'auth' | 'customer' | 'provider' | 'admin';

export interface RouteDefinition {
  path: string;
  label: string;
  access: RouteAccessLevel;
  layout: 'customer' | 'auth' | 'provider' | 'admin';
  description: string;
}

export const ROUTES = {
  // Public Marketplace (Section 104 & 108)
  MARKETPLACE: {
    HOME: '/',
    DISCOVER: '/discover',
    DISCOVER_CATEGORY: (category: string) => `/discover/${category}`,
    BUSINESS_STOREFRONT: (slug: string) => `/business/${slug}`,
    BOOKING: (slug: string) => `/book/${slug}`,
  },

  // Authentication (Section 107)
  AUTH: {
    LOGIN: '/login',
    REGISTER: '/register',
    FORGOT_PASSWORD: '/forgot-password',
    RESET_PASSWORD: '/reset-password',
  },

  // Customer Account (Section 108)
  CUSTOMER: {
    DASHBOARD: '/dashboard',
    FAVORITES: '/favorites',
    APPOINTMENTS: '/appointments',
    APPOINTMENT_DETAIL: (id: string) => `/appointments/${id}`,
    ORDERS: '/orders',
    ORDER_DETAIL: (id: string) => `/orders/${id}`,
    PROFILE: '/profile',
    NOTIFICATIONS: '/notifications',
    CHECKOUT: '/checkout',
  },

  // Provider Operating Workspace (Section 105)
  PROVIDER: {
    DASHBOARD: '/provider/dashboard',
    CALENDAR: '/provider/calendar',
    BOOKINGS: '/provider/bookings',
    CUSTOMERS: '/provider/customers',
    SERVICES: '/provider/services',
    PRODUCTS: '/provider/products',
    STAFF: '/provider/staff',
    LOCATIONS: '/provider/locations',
    AVAILABILITY: '/provider/availability',
    ORDERS: '/provider/orders',
    PAYMENTS: '/provider/payments',
    ANALYTICS: '/provider/analytics',
    REPORTS: '/provider/reports',
    STOREFRONT: '/provider/storefront',
    TEAM: '/provider/team',
    SETTINGS: '/provider/settings',
    AUDIT: '/provider/audit',
    ONBOARDING: '/provider/onboarding',
    NOTIFICATIONS: '/provider/notifications',
    DESIGN_SYSTEM: '/provider/design-system',
  },

  // Platform Administration (Section 106)
  ADMIN: {
    DASHBOARD: '/admin',
    PROVIDERS: '/admin/providers',
    BUSINESSES: '/admin/businesses',
    CUSTOMERS: '/admin/customers',
    CATEGORIES: '/admin/categories',
    BOOKINGS: '/admin/bookings',
    ORDERS: '/admin/orders',
    PAYMENTS: '/admin/payments',
    COMMISSIONS: '/admin/commissions',
    PAYOUTS: '/admin/payouts',
    REVIEWS: '/admin/reviews',
    MODERATION: '/admin/moderation',
    REPORTS: '/admin/reports',
    AUDIT: '/admin/audit',
    HEALTH: '/admin/health',
  },
} as const;

export const ROUTE_REGISTRY: RouteDefinition[] = [
  // Marketplace
  { path: '/', label: 'Home', access: 'public', layout: 'customer', description: 'Marketplace discovery home' },
  { path: '/discover', label: 'Discover', access: 'public', layout: 'customer', description: 'Search & geolocated venue list' },
  { path: '/business/:slug', label: 'Storefront', access: 'public', layout: 'customer', description: 'Provider public storefront' },
  { path: '/book/:slug', label: 'Book Appointment', access: 'public', layout: 'customer', description: 'Live slot reservation and hold flow' },
  { path: '/favorites', label: 'Favorites', access: 'customer', layout: 'customer', description: 'Customer saved providers' },
  { path: '/appointments', label: 'My Appointments', access: 'customer', layout: 'customer', description: 'Customer appointment history & upcoming sessions' },
  { path: '/orders', label: 'My Orders', access: 'customer', layout: 'customer', description: 'Customer product orders & retail fulfillment' },
  { path: '/profile', label: 'Profile', access: 'customer', layout: 'customer', description: 'Personal preferences & contact settings' },
  { path: '/notifications', label: 'Notifications', access: 'customer', layout: 'customer', description: 'Real-time alert notifications' },

  // Auth
  { path: '/login', label: 'Login', access: 'auth', layout: 'auth', description: 'Identity authentication & session initiation' },
  { path: '/register', label: 'Register', access: 'auth', layout: 'auth', description: 'Account creation & verification' },
  { path: '/forgot-password', label: 'Forgot Password', access: 'auth', layout: 'auth', description: 'Account recovery dispatch' },
  { path: '/reset-password', label: 'Reset Password', access: 'auth', layout: 'auth', description: 'Credential reset verification' },

  // Provider Workspace
  { path: '/provider/dashboard', label: 'Dashboard', access: 'provider', layout: 'provider', description: 'Daily metrics & operational overview' },
  { path: '/provider/calendar', label: 'Calendar', access: 'provider', layout: 'provider', description: 'Multi-staff schedule & appointment management' },
  { path: '/provider/bookings', label: 'Bookings', access: 'provider', layout: 'provider', description: 'Appointment filtering & lifecycle transitions' },
  { path: '/provider/customers', label: 'Customers', access: 'provider', layout: 'provider', description: 'Authorized customer database & history' },
  { path: '/provider/services', label: 'Services', access: 'provider', layout: 'provider', description: 'Catalogue, pricing, and category configuration' },
  { path: '/provider/products', label: 'Products', access: 'provider', layout: 'provider', description: 'Inventory management & retail items' },
  { path: '/provider/staff', label: 'Staff', access: 'provider', layout: 'provider', description: 'Specialist roster & commission management' },
  { path: '/provider/locations', label: 'Locations', access: 'provider', layout: 'provider', description: 'Physical branches & timezone configuration' },
  { path: '/provider/orders', label: 'Orders', access: 'provider', layout: 'provider', description: 'Retail orders & fulfillment state' },
  { path: '/provider/payments', label: 'Payments', access: 'provider', layout: 'provider', description: 'Financial ledger & payout balance visibility' },
  { path: '/provider/analytics', label: 'Analytics', access: 'provider', layout: 'provider', description: 'Revenue trends, utilization, & performance' },
  { path: '/provider/reports', label: 'Reports', access: 'provider', layout: 'provider', description: 'Financial & operational export generation' },
  { path: '/provider/team', label: 'Team', access: 'provider', layout: 'provider', description: 'Staff memberships & role-based permissions' },
  { path: '/provider/settings', label: 'Settings', access: 'provider', layout: 'provider', description: 'Business policies, booking rules & profile' },

  // Admin Console
  { path: '/admin', label: 'Platform Dashboard', access: 'admin', layout: 'admin', description: 'Platform-level aggregate metrics & telemetry' },
  { path: '/admin/providers', label: 'Providers', access: 'admin', layout: 'admin', description: 'Provider review & verification governance' },
  { path: '/admin/customers', label: 'Customers', access: 'admin', layout: 'admin', description: 'Customer support & status management' },
  { path: '/admin/bookings', label: 'Platform Bookings', access: 'admin', layout: 'admin', description: 'Cross-tenant appointment inspection' },
  { path: '/admin/orders', label: 'Platform Orders', access: 'admin', layout: 'admin', description: 'Cross-tenant order lifecycle oversight' },
  { path: '/admin/payments', label: 'Financials', access: 'admin', layout: 'admin', description: 'Gross volume, commissions & payout batches' },
  { path: '/admin/reviews', label: 'Reviews', access: 'admin', layout: 'admin', description: 'Platform review verification & moderation' },
  { path: '/admin/moderation', label: 'Moderation', access: 'admin', layout: 'admin', description: 'Flagged content & moderation decisions' },
  { path: '/admin/reports', label: 'Reports', access: 'admin', layout: 'admin', description: 'Platform audit & tax report generation' },
  { path: '/admin/audit', label: 'Audit Trail', access: 'admin', layout: 'admin', description: 'Immutable audit logs for security & compliance' },
  { path: '/admin/health', label: 'System Health', access: 'admin', layout: 'admin', description: 'Database, cache, worker & temporal diagnostics' },
];
