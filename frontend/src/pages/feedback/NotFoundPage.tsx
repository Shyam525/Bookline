import React from 'react';
import { Link } from 'react-router-dom';
import { Compass, Home, ArrowLeft } from 'lucide-react';

export const NotFoundPage: React.FC = () => {
  return (
    <div className="min-h-screen bg-[#0A0C13] text-[#ECEFFE] flex flex-col items-center justify-center p-6 text-center">
      <div className="w-16 h-16 rounded-2xl bg-[#E8546A]/10 border border-[#E8546A]/20 flex items-center justify-center text-[#E8546A] mb-6">
        <Compass className="w-8 h-8 animate-pulse" />
      </div>

      <span className="font-mono text-xs uppercase tracking-widest text-[#E8546A] font-bold mb-2">
        404 — Page Not Found
      </span>

      <h1 className="font-heading text-3xl sm:text-4xl font-bold text-white mb-3">
        The destination you seek is unavailable
      </h1>

      <p className="max-w-md text-sm text-[#7E88A8] mb-8">
        The route may have moved, expired, or you might have entered an invalid URL. Explore the marketplace or return home.
      </p>

      <div className="flex flex-wrap items-center justify-center gap-3">
        <Link
          to="/"
          className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-xs font-semibold text-white transition-colors"
        >
          <Home className="w-4 h-4 text-[#7E88A8]" />
          <span>Marketplace Home</span>
        </Link>
        <Link
          to="/discover"
          className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-[#E8546A] hover:bg-[#D44359] text-xs font-bold text-white shadow-lg shadow-[#E8546A]/20 transition-all"
        >
          <Compass className="w-4 h-4" />
          <span>Discover Providers</span>
        </Link>
      </div>
    </div>
  );
};
