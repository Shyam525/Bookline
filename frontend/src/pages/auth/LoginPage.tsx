import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../app/providers/AuthProvider';
import { Input } from '../../components/forms/Inputs';
import { Button } from '../../components/ui/Button';
import { Alert } from '../../components/feedback/Feedback';

export const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const { login, isLoading } = useAuth();

  const [email, setEmail] = useState('demo@bookline.local');
  const [password, setPassword] = useState('BooklineDemo123!');
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      await login(email, password);
      navigate('/app');
    } catch (err: any) {
      setError(err.message || 'Invalid credentials');
    }
  };

  const handleDemoFill = () => {
    setEmail('demo@bookline.local');
    setPassword('BooklineDemo123!');
  };

  return (
    <div className="space-y-6">
      <div className="text-center space-y-1">
        <h2 className="font-heading text-2xl font-bold text-white">Sign In</h2>
        <p className="text-xs text-[#7E88A8]">Enter your business owner or staff credentials</p>
      </div>

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
          <button
            type="button"
            onClick={handleDemoFill}
            className="text-[#E8546A] hover:underline font-mono"
          >
            Auto-Fill Demo Credentials
          </button>
          <Link to="/forgot-password" className="text-[#7E88A8] hover:text-white transition-colors">
            Forgot password?
          </Link>
        </div>

        <Button type="submit" variant="primary" className="w-full" isLoading={isLoading}>
          Sign In to Dashboard
        </Button>
      </form>

      <div className="text-center pt-2 border-t border-[#212638]">
        <p className="text-xs text-[#7E88A8]">
          Don't have an organization account?{' '}
          <Link to="/register" className="text-[#E8546A] font-semibold hover:underline">
            Register Business
          </Link>
        </p>
      </div>
    </div>
  );
};
