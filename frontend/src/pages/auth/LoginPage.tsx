import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';

export const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const [email, setEmail] = useState('demo@bookline.local');
  const [password, setPassword] = useState('BooklineDemo123!');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    navigate('/app');
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div>
        <label className="block text-xs font-semibold text-[#7E88A8] uppercase tracking-wider mb-2">
          Email Address
        </label>
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className="w-full bg-[#181D2C] border border-[#212638] rounded-xl px-4 py-3 text-sm text-white focus:outline-none focus:border-[#E8546A]"
          required
        />
      </div>

      <div>
        <label className="block text-xs font-semibold text-[#7E88A8] uppercase tracking-wider mb-2">
          Password
        </label>
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          className="w-full bg-[#181D2C] border border-[#212638] rounded-xl px-4 py-3 text-sm text-white focus:outline-none focus:border-[#E8546A]"
          required
        />
      </div>

      <button
        type="submit"
        className="w-full bg-[#E8546A] hover:bg-[#D44359] text-white font-medium py-3 rounded-xl transition-colors font-sans"
      >
        Sign In to Dashboard
      </button>

      <div className="text-center pt-2">
        <p className="text-xs text-[#7E88A8]">
          Demo Credentials Pre-filled for Local Verification
        </p>
      </div>
    </form>
  );
};
