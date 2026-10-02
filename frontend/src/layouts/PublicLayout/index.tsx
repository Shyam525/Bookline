import React from 'react';
import { Outlet, Link } from 'react-router-dom';

export const PublicLayout: React.FC = () => {
  return (
    <div className="min-h-screen bg-[#0A0C13] text-[#ECEFFE] flex flex-col">
      <header className="h-16 bg-[#111520] border-b border-[#212638] px-8 flex items-center justify-between">
        <Link to="/" className="flex items-center gap-3">
          <div className="w-8 h-8 rounded-lg bg-[#E8546A] flex items-center justify-center font-heading font-bold text-white text-lg">
            B
          </div>
          <span className="font-heading text-xl font-bold tracking-tight text-white">BOOKLINE</span>
        </Link>
        <div className="flex items-center gap-4">
          <Link to="/login" className="text-sm font-medium text-[#7E88A8] hover:text-white transition-colors">
            Sign In
          </Link>
          <Link
            to="/app"
            className="text-sm font-medium bg-[#E8546A] text-white px-4 py-2 rounded-lg hover:bg-[#D44359] transition-colors"
          >
            Business Portal
          </Link>
        </div>
      </header>
      <main className="flex-1">
        <Outlet />
      </main>
      <footer className="py-6 border-t border-[#212638] text-center text-xs text-[#7E88A8]">
        Bookline Appointment & Scheduling Platform &copy; {new Date().getFullYear()}
      </footer>
    </div>
  );
};
