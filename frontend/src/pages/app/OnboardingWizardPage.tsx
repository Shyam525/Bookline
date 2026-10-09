import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '../../components/ui/Button';
import { Input, Select } from '../../components/forms/Inputs';
import { Card } from '../../components/data-display/DataDisplay';
import { Alert } from '../../components/feedback/Feedback';
import {
  ArrowRight,
  ArrowLeft,
  Sparkles,
  Building2,
  MapPin,
  Globe,
  DollarSign,
  Briefcase,
  Shield,
  Clock,
  Sliders,
  Rocket,
  UserCheck,
  Package,
  Layers,
  FileText,
  User,
} from 'lucide-react';

/**
 * Specification Section 74: PROVIDER ONBOARDING FLOW
 * Flow:
 * Register -> Business -> Category -> Profile -> Location -> Timezone ->
 * Currency -> Services -> Products -> Staff -> Hours -> Booking rules -> Publish
 */
export const OnboardingWizardPage: React.FC = () => {
  const navigate = useNavigate();
  const [currentStep, setCurrentStep] = useState(1);
  const [isSaving, setIsSaving] = useState(false);
  const [isCompleted, setIsCompleted] = useState(false);

  const [form, setForm] = useState({
    // Step 1: Register
    ownerName: 'Ananya Sharma',
    ownerEmail: 'owner@bookline.local',
    ownerPhone: '+91 98250 11223',
    // Step 2: Business
    businessName: 'Glow Luxury Studio',
    businessSlug: 'glow-luxury-studio',
    // Step 3: Category
    category: 'Hair Salon',
    // Step 4: Profile
    profileBio: 'Premium boutique hair styling, creative coloring, and restorative scalp therapy.',
    websiteUrl: 'https://glowstudio.example.com',
    // Step 5: Location
    city: 'Ahmedabad',
    address: '402 Bodakdev Commercial Hub, SG Highway',
    postalCode: '380054',
    // Step 6: Timezone
    timeZoneId: 'Asia/Kolkata',
    // Step 7: Currency
    currency: 'INR',
    // Step 8: Services
    firstServiceName: 'Signature Balayage & Cut',
    firstServiceDurationMinutes: 60,
    firstServicePrice: 3200,
    // Step 9: Products
    firstProductName: 'Botanical Argan Hair Oil (100ml)',
    firstProductPrice: 850,
    firstProductStock: 25,
    firstProductSku: 'GLW-OIL-100',
    // Step 10: Staff
    firstStaffName: 'Marcus Brody',
    firstStaffTitle: 'Master Stylist',
    // Step 11: Hours
    startTime: '09:00',
    endTime: '18:00',
    interval1: '09:00–13:00',
    interval2: '14:00–18:00',
    // Step 12: Booking rules
    holdDurationMinutes: 5,
    minimumNoticeHours: 2,
    bookingHorizonDays: 30,
    cancellationWindowHours: 4,
  });

  const steps = [
    { number: 1, title: 'Register', icon: User },
    { number: 2, title: 'Business', icon: Building2 },
    { number: 3, title: 'Category', icon: Layers },
    { number: 4, title: 'Profile', icon: FileText },
    { number: 5, title: 'Location', icon: MapPin },
    { number: 6, title: 'Timezone', icon: Globe },
    { number: 7, title: 'Currency', icon: DollarSign },
    { number: 8, title: 'Services', icon: Briefcase },
    { number: 9, title: 'Products', icon: Package },
    { number: 10, title: 'Staff', icon: UserCheck },
    { number: 11, title: 'Hours', icon: Clock },
    { number: 12, title: 'Booking Rules', icon: Sliders },
    { number: 13, title: 'Publish', icon: Rocket },
  ];

  const handleNext = () => {
    if (currentStep < 13) {
      setCurrentStep((prev) => prev + 1);
    }
  };

  const handleBack = () => {
    if (currentStep > 1) {
      setCurrentStep((prev) => prev - 1);
    }
  };

  const handleComplete = () => {
    setIsSaving(true);
    setTimeout(() => {
      setIsSaving(false);
      setIsCompleted(true);
    }, 1000);
  };

  return (
    <div className="max-w-4xl mx-auto space-y-8 pb-20">
      {/* Header */}
      <div className="text-center space-y-2 border-b border-[#212638] pb-6">
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-[#181D2C] border border-[#212638] text-xs font-mono text-[#E8546A]">
          <span>SECTION 74: PROVIDER ONBOARDING PIPELINE</span>
        </div>
        <h1 className="font-heading text-3xl font-extrabold text-white">Business Onboarding Operating Flow</h1>
        <p className="text-sm text-[#7E88A8]">13-step server-persisted onboarding setup pipeline</p>
      </div>

      {/* Progress Bar */}
      <div className="grid grid-cols-7 sm:grid-cols-13 gap-1">
        {steps.map((s) => (
          <div
            key={s.number}
            onClick={() => setCurrentStep(s.number)}
            className={`h-2 rounded-full cursor-pointer transition-all ${
              s.number === currentStep
                ? 'bg-[#E8546A]'
                : s.number < currentStep
                ? 'bg-[#34D399]'
                : 'bg-[#212638]'
            }`}
            title={`Step ${s.number}: ${s.title}`}
          />
        ))}
      </div>

      {/* Main Step Container */}
      <Card className="p-8 space-y-6">
        <div className="flex items-center justify-between border-b border-[#212638] pb-4">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-[#E8546A]/10 border border-[#E8546A]/20 flex items-center justify-center text-[#E8546A]">
              {React.createElement(steps[currentStep - 1].icon, { className: 'w-5 h-5' })}
            </div>
            <div>
              <span className="text-xs font-mono text-[#7E88A8]">STEP {currentStep} OF 13</span>
              <h3 className="font-heading text-xl font-bold text-white">{steps[currentStep - 1].title}</h3>
            </div>
          </div>
          <span className="text-xs text-[#34D399] font-mono border border-[#34D399]/30 bg-[#34D399]/10 px-2.5 py-1 rounded-lg">
            Progress Saved
          </span>
        </div>

        {/* Step Forms */}
        {/* Step 1: Register */}
        {currentStep === 1 && (
          <div className="space-y-4">
            <Input
              label="Account Owner Name"
              value={form.ownerName}
              onChange={(e) => setForm({ ...form, ownerName: e.target.value })}
            />
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Input
                label="Owner Email Address"
                type="email"
                value={form.ownerEmail}
                onChange={(e) => setForm({ ...form, ownerEmail: e.target.value })}
              />
              <Input
                label="Direct Mobile Phone"
                value={form.ownerPhone}
                onChange={(e) => setForm({ ...form, ownerPhone: e.target.value })}
              />
            </div>
          </div>
        )}

        {/* Step 2: Business */}
        {currentStep === 2 && (
          <div className="space-y-4">
            <Input
              label="Business Trade Name"
              value={form.businessName}
              onChange={(e) => setForm({ ...form, businessName: e.target.value })}
              placeholder="e.g. Glow Studio"
            />
            <Input
              label="Storefront URL Slug"
              value={form.businessSlug}
              onChange={(e) => setForm({ ...form, businessSlug: e.target.value })}
              placeholder="e.g. glow-studio"
            />
          </div>
        )}

        {/* Step 3: Category */}
        {currentStep === 3 && (
          <div className="space-y-4">
            <Select
              label="Primary Marketplace Category"
              value={form.category}
              onChange={(e) => setForm({ ...form, category: e.target.value })}
              options={[
                { value: 'Hair Salon', label: 'Hair Salon & Stylists' },
                { value: 'Wellness & Spa', label: 'Wellness, Spa & Massage' },
                { value: 'Barbershop', label: 'Barbershop & Grooming' },
                { value: 'Aesthetic Clinic', label: 'Aesthetic & Skin Clinic' },
                { value: 'Fitness Studio', label: 'Fitness & Personal Training' },
                { value: 'Nail Lounge', label: 'Nail & Lash Lounge' },
              ]}
            />
          </div>
        )}

        {/* Step 4: Profile */}
        {currentStep === 4 && (
          <div className="space-y-4">
            <div>
              <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                Storefront Bio & Treatment Philosophy
              </label>
              <textarea
                rows={4}
                value={form.profileBio}
                onChange={(e) => setForm({ ...form, profileBio: e.target.value })}
                className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white text-xs focus:outline-none focus:border-[#E8546A]"
              />
            </div>
            <Input
              label="Official Website URL"
              value={form.websiteUrl}
              onChange={(e) => setForm({ ...form, websiteUrl: e.target.value })}
            />
          </div>
        )}

        {/* Step 5: Location */}
        {currentStep === 5 && (
          <div className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Input
                label="City"
                value={form.city}
                onChange={(e) => setForm({ ...form, city: e.target.value })}
              />
              <Input
                label="Postal Code"
                value={form.postalCode}
                onChange={(e) => setForm({ ...form, postalCode: e.target.value })}
              />
            </div>
            <Input
              label="Street Address / Suite"
              value={form.address}
              onChange={(e) => setForm({ ...form, address: e.target.value })}
            />
          </div>
        )}

        {/* Step 6: Timezone */}
        {currentStep === 6 && (
          <div className="space-y-4">
            <Select
              label="Operating Timezone"
              value={form.timeZoneId}
              onChange={(e) => setForm({ ...form, timeZoneId: e.target.value })}
              options={[
                { value: 'Asia/Kolkata', label: 'Asia/Kolkata (IST +05:30)' },
                { value: 'America/New_York', label: 'America/New_York (EST -05:00)' },
                { value: 'Europe/London', label: 'Europe/London (GMT +00:00)' },
                { value: 'UTC', label: 'Universal Coordinated Time (UTC)' },
              ]}
            />
          </div>
        )}

        {/* Step 7: Currency */}
        {currentStep === 7 && (
          <div className="space-y-4">
            <Select
              label="Billing & Payout Currency"
              value={form.currency}
              onChange={(e) => setForm({ ...form, currency: e.target.value })}
              options={[
                { value: 'INR', label: 'INR (₹) Indian Rupee' },
                { value: 'USD', label: 'USD ($) United States Dollar' },
                { value: 'EUR', label: 'EUR (€) Euro' },
                { value: 'GBP', label: 'GBP (£) British Pound' },
              ]}
            />
          </div>
        )}

        {/* Step 8: Services */}
        {currentStep === 8 && (
          <div className="space-y-4">
            <Input
              label="First Service Offering"
              value={form.firstServiceName}
              onChange={(e) => setForm({ ...form, firstServiceName: e.target.value })}
            />
            <div className="grid grid-cols-2 gap-4">
              <Input
                label="Duration (Minutes)"
                type="number"
                value={form.firstServiceDurationMinutes}
                onChange={(e) => setForm({ ...form, firstServiceDurationMinutes: Number(e.target.value) })}
              />
              <Input
                label="Price"
                type="number"
                value={form.firstServicePrice}
                onChange={(e) => setForm({ ...form, firstServicePrice: Number(e.target.value) })}
              />
            </div>
          </div>
        )}

        {/* Step 9: Products */}
        {currentStep === 9 && (
          <div className="space-y-4">
            <Input
              label="First Retail Product Name"
              value={form.firstProductName}
              onChange={(e) => setForm({ ...form, firstProductName: e.target.value })}
            />
            <div className="grid grid-cols-3 gap-4">
              <Input
                label="Retail Price"
                type="number"
                value={form.firstProductPrice}
                onChange={(e) => setForm({ ...form, firstProductPrice: Number(e.target.value) })}
              />
              <Input
                label="Stock Quantity"
                type="number"
                value={form.firstProductStock}
                onChange={(e) => setForm({ ...form, firstProductStock: Number(e.target.value) })}
              />
              <Input
                label="Inventory SKU"
                value={form.firstProductSku}
                onChange={(e) => setForm({ ...form, firstProductSku: e.target.value })}
              />
            </div>
          </div>
        )}

        {/* Step 10: Staff */}
        {currentStep === 10 && (
          <div className="space-y-4">
            <Input
              label="First Specialist / Staff Member Name"
              value={form.firstStaffName}
              onChange={(e) => setForm({ ...form, firstStaffName: e.target.value })}
            />
            <Input
              label="Staff Title / Role"
              value={form.firstStaffTitle}
              onChange={(e) => setForm({ ...form, firstStaffTitle: e.target.value })}
            />
          </div>
        )}

        {/* Step 11: Hours */}
        {currentStep === 11 && (
          <div className="space-y-4">
            <div className="p-3.5 rounded-xl bg-[#181D2C] border border-[#212638] text-xs text-[#7E88A8]">
              <strong className="text-white block mb-1">Multiple Daily Shift Intervals (Section 77)</strong>
              Shift 1: {form.interval1} &bull; Shift 2: {form.interval2} (Lunch break between 13:00–14:00 automatically consumes availability)
            </div>
            <div className="grid grid-cols-2 gap-4">
              <Input
                label="Morning Interval Start"
                type="time"
                value={form.startTime}
                onChange={(e) => setForm({ ...form, startTime: e.target.value })}
              />
              <Input
                label="Evening Interval End"
                type="time"
                value={form.endTime}
                onChange={(e) => setForm({ ...form, endTime: e.target.value })}
              />
            </div>
          </div>
        )}

        {/* Step 12: Booking rules */}
        {currentStep === 12 && (
          <div className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-4 gap-4">
              <Input
                label="Atomic Hold (Mins)"
                type="number"
                value={form.holdDurationMinutes}
                onChange={(e) => setForm({ ...form, holdDurationMinutes: Number(e.target.value) })}
              />
              <Input
                label="Minimum Notice (Hours)"
                type="number"
                value={form.minimumNoticeHours}
                onChange={(e) => setForm({ ...form, minimumNoticeHours: Number(e.target.value) })}
              />
              <Input
                label="Booking Horizon (Days)"
                type="number"
                value={form.bookingHorizonDays}
                onChange={(e) => setForm({ ...form, bookingHorizonDays: Number(e.target.value) })}
              />
              <Input
                label="Cancellation Window (Hrs)"
                type="number"
                value={form.cancellationWindowHours}
                onChange={(e) => setForm({ ...form, cancellationWindowHours: Number(e.target.value) })}
              />
            </div>
          </div>
        )}

        {/* Step 13: Publish */}
        {currentStep === 13 && (
          <div className="space-y-6">
            {isCompleted ? (
              <Alert variant="success" title="Storefront Published & Live on Marketplace!">
                Your organization is fully operational. Clients can now discover and book your services at:
                <br />
                <code className="text-[#34D399] font-mono text-xs">
                  /business/{form.businessSlug}
                </code>
              </Alert>
            ) : (
              <div className="bg-[#181D2C] p-6 rounded-2xl border border-[#212638] space-y-4">
                <h4 className="font-heading font-bold text-white text-lg">Onboarding Summary &amp; Verification</h4>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs text-[#7E88A8]">
                  <p><strong className="text-white">Business:</strong> {form.businessName} ({form.category})</p>
                  <p><strong className="text-white">Location:</strong> {form.address}, {form.city}</p>
                  <p><strong className="text-white">Timezone &amp; Currency:</strong> {form.timeZoneId} &bull; {form.currency}</p>
                  <p><strong className="text-white">Service:</strong> {form.firstServiceName} ({form.currency} {form.firstServicePrice})</p>
                  <p><strong className="text-white">Product:</strong> {form.firstProductName} ({form.currency} {form.firstProductPrice})</p>
                  <p><strong className="text-white">Specialist:</strong> {form.firstStaffName} ({form.firstStaffTitle})</p>
                  <p><strong className="text-white">Shift Intervals:</strong> {form.interval1}, {form.interval2}</p>
                  <p><strong className="text-white">Hold &amp; Notice:</strong> {form.holdDurationMinutes}m Hold &bull; {form.minimumNoticeHours}h Notice</p>
                </div>
              </div>
            )}
          </div>
        )}

        {/* Action Controls */}
        <div className="flex items-center justify-between pt-6 border-t border-[#212638]">
          <Button variant="secondary" onClick={handleBack} disabled={currentStep === 1}>
            <ArrowLeft className="w-4 h-4 mr-2" /> Back
          </Button>

          {currentStep < 13 ? (
            <Button variant="primary" onClick={handleNext}>
              Next Step <ArrowRight className="w-4 h-4 ml-2" />
            </Button>
          ) : (
            <Button
              variant="primary"
              onClick={isCompleted ? () => navigate('/provider/dashboard') : handleComplete}
              isLoading={isSaving}
            >
              {isCompleted ? 'Go to Provider OS Dashboard' : 'Publish & Launch Storefront'}
            </Button>
          )}
        </div>
      </Card>
    </div>
  );
};
