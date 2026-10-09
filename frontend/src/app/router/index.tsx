import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { CustomerLayout } from '../../layouts/CustomerLayout';
import { ProviderLayout } from '../../layouts/ProviderLayout';
import { AdminLayout } from '../../layouts/AdminLayout';
import { AuthLayout } from '../../layouts/AuthLayout';

// Customer Pages
import { HomePage } from '../../pages/marketing/HomePage';
import { DiscoveryPage } from '../../pages/customer/DiscoveryPage';
import { ProviderStorefrontPage } from '../../pages/customer/ProviderStorefrontPage';
import { CustomerAppointmentsPage } from '../../pages/customer/CustomerAppointmentsPage';
import { CustomerOrdersPage } from '../../pages/customer/CustomerOrdersPage';
import { FavoritesPage } from '../../pages/customer/FavoritesPage';
import { CustomerProfilePage } from '../../pages/customer/CustomerProfilePage';
import { CustomerNotificationsPage } from '../../pages/customer/CustomerNotificationsPage';
import { CustomerDashboardPage } from '../../pages/customer/CustomerDashboardPage';
import { CheckoutPage } from '../../pages/customer/CheckoutPage';

// Auth Pages
import { LoginPage } from '../../pages/auth/LoginPage';
import { RegisterPage } from '../../pages/auth/RegisterPage';
import { ForgotPasswordPage } from '../../pages/auth/ForgotPasswordPage';
import { ResetPasswordPage } from '../../pages/auth/ResetPasswordPage';

// Provider Pages
import { DashboardPage } from '../../pages/app/DashboardPage';
import { CalendarPage } from '../../pages/app/CalendarPage';
import { ProviderBookingsPage } from '../../pages/app/ProviderBookingsPage';
import { CustomersPage } from '../../pages/app/CustomersPage';
import { ServicesPage } from '../../pages/app/ServicesPage';
import { ProviderProductsPage } from '../../pages/app/ProviderProductsPage';
import { StaffPage } from '../../pages/app/StaffPage';
import { LocationsPage } from '../../pages/app/LocationsPage';
import { AvailabilityPage } from '../../pages/app/AvailabilityPage';
import { ProviderOrdersPage } from '../../pages/app/ProviderOrdersPage';
import { PaymentsPage } from '../../pages/app/PaymentsPage';
import { AnalyticsPage } from '../../pages/app/AnalyticsPage';
import { StorefrontSettingsPage } from '../../pages/app/StorefrontSettingsPage';
import { ProviderTeamPage } from '../../pages/app/ProviderTeamPage';
import { ProviderAuditPage } from '../../pages/app/ProviderAuditPage';
import { OnboardingWizardPage } from '../../pages/app/OnboardingWizardPage';
import { NotificationsPage } from '../../pages/app/NotificationsPage';
import { DesignSystemPage } from '../../pages/app/DesignSystemPage';

// Platform Admin Pages
import { AdminDashboardPage } from '../../pages/admin/AdminDashboardPage';
import { AdminProvidersPage } from '../../pages/admin/AdminProvidersPage';
import { AdminBusinessesPage } from '../../pages/admin/AdminBusinessesPage';
import { AdminCustomersPage } from '../../pages/admin/AdminCustomersPage';
import { AdminCategoriesPage } from '../../pages/admin/AdminCategoriesPage';
import { AdminBookingsPage } from '../../pages/admin/AdminBookingsPage';
import { AdminOrdersPage } from '../../pages/admin/AdminOrdersPage';
import { AdminFinancialsPage } from '../../pages/admin/AdminFinancialsPage';
import { AdminReviewsPage } from '../../pages/admin/AdminReviewsPage';
import { AdminModerationPage } from '../../pages/admin/AdminModerationPage';
import { AdminReportsPage } from '../../pages/admin/AdminReportsPage';
import { AdminAuditPage } from '../../pages/admin/AdminAuditPage';
import { AdminHealthPage } from '../../pages/admin/AdminHealthPage';

export const AppRouter: React.FC = () => {
  return (
    <BrowserRouter>
      <Routes>
        {/* ========================================================= */}
        {/* 1. CUSTOMER MARKETPLACE (CustomerLayout - Section 22)     */}
        {/* ========================================================= */}
        <Route element={<CustomerLayout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/discover" element={<DiscoveryPage />} />
          <Route path="/discover/:category" element={<DiscoveryPage />} />
          <Route path="/category/:slug" element={<DiscoveryPage />} />
          <Route path="/business/:slug" element={<ProviderStorefrontPage />} />
          <Route path="/book/:businessSlug" element={<ProviderStorefrontPage />} />
          <Route path="/book/:slug" element={<ProviderStorefrontPage />} />
          <Route path="/favorites" element={<FavoritesPage />} />
          <Route path="/appointments" element={<CustomerAppointmentsPage />} />
          <Route path="/appointments/:id" element={<CustomerAppointmentsPage />} />
          <Route path="/orders" element={<CustomerOrdersPage />} />
          <Route path="/orders/:id" element={<CustomerOrdersPage />} />
          <Route path="/notifications" element={<CustomerNotificationsPage />} />
          <Route path="/dashboard" element={<CustomerDashboardPage />} />
          <Route path="/customer/dashboard" element={<CustomerDashboardPage />} />
          <Route path="/account" element={<CustomerDashboardPage />} />
          <Route path="/profile" element={<CustomerProfilePage />} />
          <Route path="/settings" element={<CustomerProfilePage />} />
          <Route path="/checkout" element={<CheckoutPage />} />
        </Route>

        {/* ========================================================= */}
        {/* 2. AUTHENTICATION (AuthLayout)                            */}
        {/* ========================================================= */}
        <Route element={<AuthLayout />}>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />
        </Route>

        {/* ========================================================= */}
        {/* 3. PROVIDER OPERATING OS (ProviderLayout)                */}
        {/* ========================================================= */}
        <Route path="/provider" element={<ProviderLayout />}>
          <Route index element={<Navigate to="/provider/dashboard" replace />} />
          <Route path="dashboard" element={<DashboardPage />} />
          <Route path="calendar" element={<CalendarPage />} />
          <Route path="bookings" element={<ProviderBookingsPage />} />
          <Route path="customers" element={<CustomersPage />} />
          <Route path="services" element={<ServicesPage />} />
          <Route path="products" element={<ProviderProductsPage />} />
          <Route path="staff" element={<StaffPage />} />
          <Route path="locations" element={<LocationsPage />} />
          <Route path="availability" element={<AvailabilityPage />} />
          <Route path="orders" element={<ProviderOrdersPage />} />
          <Route path="payments" element={<PaymentsPage />} />
          <Route path="analytics" element={<AnalyticsPage />} />
          <Route path="reports" element={<AnalyticsPage />} />
          <Route path="storefront" element={<StorefrontSettingsPage />} />
          <Route path="team" element={<ProviderTeamPage />} />
          <Route path="settings" element={<StorefrontSettingsPage />} />
          <Route path="audit" element={<ProviderAuditPage />} />
          <Route path="onboarding" element={<OnboardingWizardPage />} />
          <Route path="notifications" element={<NotificationsPage />} />
          <Route path="design-system" element={<DesignSystemPage />} />
        </Route>

        {/* Backward Compatibility for /app -> /provider */}
        <Route path="/app/*" element={<Navigate to="/provider/dashboard" replace />} />

        {/* ========================================================= */}
        {/* 4. PLATFORM GOVERNANCE ADMIN (AdminLayout)                */}
        {/* ========================================================= */}
        <Route path="/admin" element={<AdminLayout />}>
          <Route index element={<AdminDashboardPage />} />
          <Route path="providers" element={<AdminProvidersPage />} />
          <Route path="businesses" element={<AdminBusinessesPage />} />
          <Route path="customers" element={<AdminCustomersPage />} />
          <Route path="categories" element={<AdminCategoriesPage />} />
          <Route path="bookings" element={<AdminBookingsPage />} />
          <Route path="orders" element={<AdminOrdersPage />} />
          <Route path="payments" element={<AdminFinancialsPage />} />
          <Route path="commissions" element={<AdminFinancialsPage />} />
          <Route path="payouts" element={<AdminFinancialsPage />} />
          <Route path="reviews" element={<AdminReviewsPage />} />
          <Route path="moderation" element={<AdminModerationPage />} />
          <Route path="reports" element={<AdminReportsPage />} />
          <Route path="audit" element={<AdminAuditPage />} />
          <Route path="system" element={<AdminHealthPage />} />
          <Route path="health" element={<AdminHealthPage />} />
        </Route>

        {/* Fallback Catch-all */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
};
