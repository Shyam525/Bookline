import React from 'react';
import { Navigate, useLocation, Outlet } from 'react-router-dom';
import { useAuth } from '../providers/AuthProvider';

interface RoleRouteProps {
  allowedRoles: string[];
  children?: React.ReactNode;
}

export const RoleRoute: React.FC<RoleRouteProps> = ({ allowedRoles, children }) => {
  const { user, isAuthenticated, isLoading } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return (
      <div className="min-h-screen bg-[#0A0C13] flex items-center justify-center">
        <div className="w-8 h-8 border-2 border-[#E8546A] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  const role = user?.role || 'Customer';
  const isAllowed = allowedRoles.some((r) => r.toLowerCase() === role.toLowerCase());

  if (!isAllowed) {
    return <Navigate to="/unauthorized" state={{ from: location, requiredRoles: allowedRoles }} replace />;
  }

  return children ? <>{children}</> : <Outlet />;
};
