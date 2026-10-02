import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../app/providers/AuthProvider';
import { Input } from '../../components/forms/Inputs';
import { Button } from '../../components/ui/Button';
import { Alert } from '../../components/feedback/Feedback';

export const RegisterPage: React.FC = () => {
  const navigate = useNavigate();
  const { register, isLoading } = useAuth();

  const [formData, setFormData] = useState({
    tenantName: '',
    tenantSlug: '',
    ownerEmail: '',
    password: '',
    firstName: '',
    lastName: '',
  });

  const [error, setError] = useState<string | null>(null);

  const handleChange = (field: string, val: string) => {
    setFormData((prev) => {
      const next = { ...prev, [field]: val };
      if (field === 'tenantName' && !prev.tenantSlug) {
        next.tenantSlug = val.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '');
      }
      return next;
    });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      await register(formData);
      navigate('/app');
    } catch (err: any) {
      setError(err.message || 'Registration failed');
    }
  };

  return (
    <div className="space-y-6">
      <div className="text-center space-y-1">
        <h2 className="font-heading text-2xl font-bold text-white">Create Organization</h2>
        <p className="text-xs text-[#7E88A8]">Onboard your salon, clinic, spa, or barbershop</p>
      </div>

      {error && (
        <Alert variant="error" title="Registration Error">
          {error}
        </Alert>
      )}

      <form onSubmit={handleSubmit} className="space-y-4">
        <Input
          label="Business Name"
          value={formData.tenantName}
          onChange={(e) => handleChange('tenantName', e.target.value)}
          placeholder="e.g. Acme Hair Salon"
          required
        />

        <Input
          label="Booking URL Slug"
          value={formData.tenantSlug}
          onChange={(e) => handleChange('tenantSlug', e.target.value)}
          placeholder="e.g. acme-hair-salon"
          helperText="Your public booking link: bookline.local/?tenant=your-slug"
          required
        />

        <div className="grid grid-cols-2 gap-3">
          <Input
            label="First Name"
            value={formData.firstName}
            onChange={(e) => handleChange('firstName', e.target.value)}
            placeholder="Jane"
            required
          />
          <Input
            label="Last Name"
            value={formData.lastName}
            onChange={(e) => handleChange('lastName', e.target.value)}
            placeholder="Doe"
            required
          />
        </div>

        <Input
          label="Owner Email Address"
          type="email"
          value={formData.ownerEmail}
          onChange={(e) => handleChange('ownerEmail', e.target.value)}
          placeholder="owner@business.com"
          required
        />

        <Input
          label="Account Password"
          type="password"
          value={formData.password}
          onChange={(e) => handleChange('password', e.target.value)}
          placeholder="At least 8 characters"
          required
        />

        <Button type="submit" variant="primary" className="w-full" isLoading={isLoading}>
          Create Organization & Launch
        </Button>
      </form>

      <div className="text-center pt-2 border-t border-[#212638]">
        <p className="text-xs text-[#7E88A8]">
          Already registered?{' '}
          <Link to="/login" className="text-[#E8546A] font-semibold hover:underline">
            Sign In
          </Link>
        </p>
      </div>
    </div>
  );
};
