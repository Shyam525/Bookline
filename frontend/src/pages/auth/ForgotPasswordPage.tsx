import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { authApi } from '../../services/api/auth';
import { Input } from '../../components/forms/Inputs';
import { Button } from '../../components/ui/Button';
import { Alert } from '../../components/feedback/Feedback';

export const ForgotPasswordPage: React.FC = () => {
  const [email, setEmail] = useState('');
  const [submitted, setSubmitted] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    try {
      await authApi.forgotPassword(email);
      setSubmitted(true);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="text-center space-y-1">
        <h2 className="font-heading text-2xl font-bold text-white">Reset Password</h2>
        <p className="text-xs text-[#7E88A8]">We will send password reset instructions to your email</p>
      </div>

      {submitted ? (
        <Alert variant="success" title="Check Your Email">
          If your email is registered in Bookline, password recovery instructions have been delivered to your inbox.
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

          <Button type="submit" variant="primary" className="w-full" isLoading={isLoading}>
            Send Password Reset Link
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
