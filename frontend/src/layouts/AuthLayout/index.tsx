import React from 'react';
import { Outlet, Link } from 'react-router-dom';

export const AuthLayout: React.FC = () => {
  return (
    <div className="min-h-screen bg-[#0A0C13] text-[#ECEFFE] flex items-center justify-center p-4">
      <div className="w-full max-w-md bg-[#111520] border border-[#212638] rounded-2xl p-8 shadow-2xl">
        <div className="text-center mb-8">
          <Link to="/" className="inline-flex items-center gap-3 mb-4">
            <div className="w-10 h-10 rounded-xl bg-[#E8546A] flex items-center justify-center font-heading font-bold text-white text-xl">
              B
            </div>
            <span className="font-heading text-2xl font-bold tracking-tight text-white">BOOKLINE</span>
          </Link>
          <p className="text-sm text-[#7E88A8]">Production-Grade Appointment & Scheduling Platform</p>
        </div>
        <Outlet />
      </div>
    </div>
  );
};
