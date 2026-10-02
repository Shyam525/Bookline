import React, { useState } from 'react';
import { useNavigate, useSearchParams, Link } from 'react-router-dom';
import { authApi } from '../../services/api/auth';
import { Input } from '../../components/forms/Inputs';
import { Button } from '../../components/ui/Button';
import { Alert } from '../../components/feedback/Feedback';

export const ResetPasswordPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const emailParam = searchParams.get('email') || '';
  const tokenParam = searchParams.get('token') || '';

  const [email, setEmail] = useState(emailParam);
  const [resetToken, setResetToken] = useState(tokenParam);
  const [newPassword, setNewPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);
    try {
      await authApi.resetPassword(email, resetToken, newPassword);
      setSuccess(true);
      setTimeout(() => navigate('/login'), 2000);
    } catch (err: any) {
      setError(err.message || 'Password reset failed');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="text-center space-y-1">
        <h2 className="font-heading text-2xl font-bold text-white">Set New Password</h2>
        <p className="text-xs text-[#7E88A8]">Enter your new secure account password</p>
      </div>

      {error && (
        <Alert variant="error" title="Reset Failed">
          {error}
        </Alert>
      )}

      {success ? (
        <Alert variant="success" title="Password Reset Successful">
          Your password has been updated. Redirecting to sign in...
        </Alert>
      ) : (
        <form onSubmit={handleSubmit} className="space-y-4">
          <Input
            label="Email Address"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="owner@business.com"
            required
          />

          <Input
            label="Reset Token"
            value={resetToken}
            onChange={(e) => setResetToken(e.target.value)}
            placeholder="Enter reset token from email"
            required
          />

          <Input
            label="New Password"
            type="password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            placeholder="At least 8 characters"
            required
          />

          <Button type="submit" variant="primary" className="w-full" isLoading={isLoading}>
            Update Password & Sign In
          </Button>
        </form>
      )}

      <div className="text-center pt-2 border-t border-[#212638]">
        <Link to="/login" className="text-xs text-[#7E88A8] hover:text-white transition-colors">
          &larr; Back to Sign In
        </Link>
      </div>
    </div>
  );
};
