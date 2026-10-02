import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { PublicLayout } from '../../layouts/PublicLayout';
import { AppLayout } from '../../layouts/AppLayout';
import { AuthLayout } from '../../layouts/AuthLayout';
import { HomePage } from '../../pages/marketing/HomePage';
import { LoginPage } from '../../pages/auth/LoginPage';
import { DashboardPage } from '../../pages/app/DashboardPage';
import { PublicBookingPage } from '../../pages/public-booking/PublicBookingPage';

export const AppRouter: React.FC = () => {
  return (
    <BrowserRouter>
      <Routes>
        {/* Public Routes */}
        <Route element={<PublicLayout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/book/:organizationSlug" element={<PublicBookingPage />} />
        </Route>

        {/* Auth Routes */}
        <Route element={<AuthLayout />}>
          <Route path="/login" element={<LoginPage />} />
        </Route>

        {/* Operations App Routes */}
        <Route path="/app" element={<AppLayout />}>
          <Route index element={<DashboardPage />} />
          <Route path="calendar" element={<DashboardPage />} />
          <Route path="customers" element={<DashboardPage />} />
          <Route path="services" element={<DashboardPage />} />
          <Route path="staff" element={<DashboardPage />} />
          <Route path="payments" element={<DashboardPage />} />
          <Route path="analytics" element={<DashboardPage />} />
          <Route path="settings" element={<DashboardPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
};
