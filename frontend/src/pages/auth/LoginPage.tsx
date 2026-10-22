import React, { useState } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { useAuth } from '../../app/providers/AuthProvider';
import { Input } from '../../components/forms/Inputs';
import { Button } from '../../components/ui/Button';
import { Alert } from '../../components/feedback/Feedback';
import { Shield, Building2, User } from 'lucide-react';

export const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { login, isLoading, user } = useAuth();

  const fromLocation = (location.state as any)?.from;

  const [email, setEmail] = useState('provider@bookline.local');
  const [password, setPassword] = useState('Provider123!');
  const [error, setError] = useState<string | null>(null);

  const resolveTargetRoute = (role?: string) => {
    if (fromLocation?.pathname) {
      return fromLocation.pathname + (fromLocation.search || '');
    }
    const normalized = (role || '').toLowerCase();
    if (normalized === 'admin' || normalized === 'platformadmin') {
      return '/admin';
    }
    if (['owner', 'provider', 'staff', 'receptionist', 'manager'].includes(normalized)) {
      return '/provider/dashboard';
    }
    return '/dashboard';
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      await login(email, password);
      // Wait for user state in auth provider to settle or resolve based on email
      const roleGuess = email.toLowerCase().includes('admin')
        ? 'PlatformAdmin'
        : email.toLowerCase().includes('customer')
        ? 'Customer'
        : 'Owner';
      navigate(resolveTargetRoute(user?.role || roleGuess), { replace: true });
    } catch (err: any) {
      setError(err.message || 'Invalid credentials');
    }
  };

  const handleQuickLogin = async (targetEmail: string, targetPass: string, role: string) => {
    setEmail(targetEmail);
    setPassword(targetPass);
    setError(null);
    try {
      await login(targetEmail, targetPass);
      navigate(resolveTargetRoute(role), { replace: true });
    } catch (err: any) {
      setError(err.message || 'Quick login failed');
    }
  };

  return (
    <div className="space-y-6">
      <div className="text-center space-y-1">
        <h2 className="font-heading text-2xl font-bold text-white">Sign In</h2>
        <p className="text-xs text-[#7E88A8]">Access your customer account, provider workspace, or admin console</p>
      </div>

      {fromLocation && (
        <Alert variant="warning" title="Authentication Required">
          Please sign in to access {fromLocation.pathname}.
        </Alert>
      )}

      {error && (
        <Alert variant="error" title="Authentication Error">
          {error}
        </Alert>
      )}

      <form onSubmit={handleSubmit} className="space-y-4">
        <Input
          label="Email Address"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="name@business.com"
          required
        />

        <Input
          label="Password"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          placeholder="••••••••••••"
          required
        />

        <div className="flex items-center justify-between text-xs pt-1">
          <Link to="/forgot-password" className="text-[#7E88A8] hover:text-white transition-colors">
            Forgot password?
          </Link>
        </div>

        <Button type="submit" variant="primary" className="w-full" isLoading={isLoading}>
          Sign In
        </Button>
      </form>

      {/* Quick Demo Logins for Testing & Seamless Journey Evaluation */}
      <div className="pt-3 border-t border-[#212638] space-y-2">
        <p className="text-[10px] font-bold uppercase tracking-wider text-[#7E88A8] text-center">
          Deterministic Demo Accounts
        </p>
        <div className="grid grid-cols-3 gap-2">
          <button
            type="button"
            onClick={() => handleQuickLogin('customer@bookline.local', 'Customer123!', 'Customer')}
            className="flex flex-col items-center gap-1 p-2 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-[11px] font-medium text-[#ECEFFE] transition-colors"
          >
            <User className="w-3.5 h-3.5 text-[#34D399]" />
            <span>Customer</span>
          </button>
          <button
            type="button"
            onClick={() => handleQuickLogin('provider@bookline.local', 'Provider123!', 'Owner')}
            className="flex flex-col items-center gap-1 p-2 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-[11px] font-medium text-[#ECEFFE] transition-colors"
          >
            <Building2 className="w-3.5 h-3.5 text-[#E8546A]" />
            <span>Provider</span>
          </button>
          <button
            type="button"
            onClick={() => handleQuickLogin('admin@bookline.local', 'Admin123!', 'PlatformAdmin')}
            className="flex flex-col items-center gap-1 p-2 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-[11px] font-medium text-[#ECEFFE] transition-colors"
          >
            <Shield className="w-3.5 h-3.5 text-[#FBBF24]" />
            <span>Admin</span>
          </button>
        </div>
      </div>

      <div className="text-center pt-2 border-t border-[#212638]">
        <p className="text-xs text-[#7E88A8]">
          Don't have an account?{' '}
          <Link to="/register" className="text-[#E8546A] font-semibold hover:underline">
            Register Account
          </Link>
        </p>
      </div>
    </div>
  );
};
