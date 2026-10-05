import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { PublicLayout } from '../../layouts/PublicLayout';
import { AppLayout } from '../../layouts/AppLayout';
import { AuthLayout } from '../../layouts/AuthLayout';
import { HomePage } from '../../pages/marketing/HomePage';
import { LoginPage } from '../../pages/auth/LoginPage';
import { RegisterPage } from '../../pages/auth/RegisterPage';
import { ForgotPasswordPage } from '../../pages/auth/ForgotPasswordPage';
import { ResetPasswordPage } from '../../pages/auth/ResetPasswordPage';
import { DashboardPage } from '../../pages/app/DashboardPage';
import { PublicBookingPage } from '../../pages/public-booking/PublicBookingPage';
import { DesignSystemPage } from '../../pages/app/DesignSystemPage';
import { OnboardingWizardPage } from '../../pages/app/OnboardingWizardPage';
import { LocationsPage } from '../../pages/app/LocationsPage';
import { ServicesPage } from '../../pages/app/ServicesPage';
import { StaffPage } from '../../pages/app/StaffPage';
import { AvailabilityPage } from '../../pages/app/AvailabilityPage';
import { CustomersPage } from '../../pages/app/CustomersPage';

export const AppRouter: React.FC = () => {
  return (
    <BrowserRouter>
      <Routes>
        {/* Public Routes */}
        <Route element={<PublicLayout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/book/:organizationSlug" element={<PublicBookingPage />} />
        </Route>

        {/* Authentication Routes */}
        <Route element={<AuthLayout />}>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />
        </Route>

        {/* Operations App Routes */}
        <Route path="/app" element={<AppLayout />}>
          <Route index element={<DashboardPage />} />
          <Route path="onboarding" element={<OnboardingWizardPage />} />
          <Route path="locations" element={<LocationsPage />} />
          <Route path="calendar" element={<DashboardPage />} />
          <Route path="customers" element={<CustomersPage />} />
          <Route path="services" element={<ServicesPage />} />
          <Route path="staff" element={<StaffPage />} />
          <Route path="availability" element={<AvailabilityPage />} />
          <Route path="payments" element={<DashboardPage />} />
          <Route path="analytics" element={<DashboardPage />} />
          <Route path="settings" element={<DashboardPage />} />
          <Route path="design-system" element={<DesignSystemPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
};
