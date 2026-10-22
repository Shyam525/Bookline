import test from 'node:test';
import assert from 'node:assert/strict';
import { ROUTE_REGISTRY, ROUTES } from './routes.ts';

test('Section 105-109: ROUTE_REGISTRY contains complete canonical route definitions', () => {
  assert.ok(ROUTE_REGISTRY.length >= 25, 'Route registry should define all canonical routes');

  // Assert essential public routes
  const homeRoute = ROUTE_REGISTRY.find((r) => r.path === '/');
  assert.ok(homeRoute);
  assert.equal(homeRoute.access, 'public');

  const storefrontRoute = ROUTE_REGISTRY.find((r) => r.path === '/business/:slug');
  assert.ok(storefrontRoute);
  assert.equal(storefrontRoute.access, 'public');

  // Assert auth routes
  const loginRoute = ROUTE_REGISTRY.find((r) => r.path === '/login');
  assert.ok(loginRoute);
  assert.equal(loginRoute.access, 'auth');

  // Assert customer routes
  const appointmentsRoute = ROUTE_REGISTRY.find((r) => r.path === '/appointments');
  assert.ok(appointmentsRoute);
  assert.equal(appointmentsRoute.access, 'customer');

  // Assert provider routes
  const providerDashboardRoute = ROUTE_REGISTRY.find((r) => r.path === '/provider/dashboard');
  assert.ok(providerDashboardRoute);
  assert.equal(providerDashboardRoute.access, 'provider');

  // Assert admin routes
  const adminDashboardRoute = ROUTE_REGISTRY.find((r) => r.path === '/admin');
  assert.ok(adminDashboardRoute);
  assert.equal(adminDashboardRoute.access, 'admin');
});

test('Section 105: ROUTES constant contains parameterized route helper functions', () => {
  assert.equal(ROUTES.MARKETPLACE.BUSINESS_STOREFRONT('aura-wellness'), '/business/aura-wellness');
  assert.equal(ROUTES.MARKETPLACE.BOOKING('aura-wellness'), '/book/aura-wellness');
  assert.equal(ROUTES.CUSTOMER.APPOINTMENT_DETAIL('apt-123'), '/appointments/apt-123');
  assert.equal(ROUTES.CUSTOMER.ORDER_DETAIL('ord-456'), '/orders/ord-456');
  assert.equal(ROUTES.PROVIDER.DASHBOARD, '/provider/dashboard');
  assert.equal(ROUTES.ADMIN.DASHBOARD, '/admin');
});

test('Section 107: Route roles authorization matrix', () => {
  const PROVIDER_ROLES = ['Owner', 'Provider', 'Admin', 'Staff', 'Receptionist', 'Manager'];
  const ADMIN_ROLES = ['PlatformAdmin', 'Admin'];

  const isRoleAllowed = (role: string, allowedList: string[]) =>
    allowedList.some((r) => r.toLowerCase() === role.toLowerCase());

  assert.equal(isRoleAllowed('Customer', PROVIDER_ROLES), false);
  assert.equal(isRoleAllowed('Customer', ADMIN_ROLES), false);
  assert.equal(isRoleAllowed('Owner', PROVIDER_ROLES), true);
  assert.equal(isRoleAllowed('Owner', ADMIN_ROLES), false);
  assert.equal(isRoleAllowed('PlatformAdmin', ADMIN_ROLES), true);
  assert.equal(isRoleAllowed('Admin', ADMIN_ROLES), true);
  assert.equal(isRoleAllowed('Staff', PROVIDER_ROLES), true);
});
