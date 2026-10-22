import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { ShieldAlert, LogIn, ArrowLeft } from 'lucide-react';
import { useAuth } from '../../app/providers/AuthProvider';

export const UnauthorizedPage: React.FC = () => {
  const { user, logout } = useAuth();
  const location = useLocation();
  const requiredRoles = (location.state as any)?.requiredRoles as string[] | undefined;

  return (
    <div className="min-h-screen bg-[#0A0C13] text-[#ECEFFE] flex flex-col items-center justify-center p-6 text-center">
      <div className="w-16 h-16 rounded-2xl bg-amber-500/10 border border-amber-500/20 flex items-center justify-center text-amber-400 mb-6">
        <ShieldAlert className="w-8 h-8" />
      </div>

      <span className="font-mono text-xs uppercase tracking-widest text-amber-400 font-bold mb-2">
        403 — Access Restricted
      </span>

      <h1 className="font-heading text-3xl font-bold text-white mb-3">
        Permissions Required
      </h1>

      <p className="max-w-md text-sm text-[#7E88A8] mb-4">
        Your current role (<strong className="text-white">{user?.role || 'Guest'}</strong>) does not have authorization to view this operational workspace.
        {requiredRoles && requiredRoles.length > 0 && (
          <span className="block mt-1 text-xs text-[#7E88A8]">
            Required role: {requiredRoles.join(' or ')}
          </span>
        )}
      </p>

      <div className="flex flex-wrap items-center justify-center gap-3 mt-4">
        <Link
          to="/"
          className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-xs font-semibold text-white transition-colors"
        >
          <ArrowLeft className="w-4 h-4 text-[#7E88A8]" />
          <span>Return Home</span>
        </Link>
        <Link
          to="/login"
          onClick={() => logout()}
          className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-[#E8546A] hover:bg-[#D44359] text-xs font-bold text-white shadow-lg shadow-[#E8546A]/20 transition-all"
        >
          <LogIn className="w-4 h-4" />
          <span>Switch Account</span>
        </Link>
      </div>
    </div>
  );
};
