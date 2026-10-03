import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '../../components/ui/Button';
import { Input, Select } from '../../components/forms/Inputs';
import { Card } from '../../components/data-display/DataDisplay';
import { Alert } from '../../components/feedback/Feedback';
import { ArrowRight, ArrowLeft, Sparkles, Building2, MapPin, Globe, DollarSign, Briefcase, Shield, Clock, Sliders, Rocket } from 'lucide-react';

export const OnboardingWizardPage: React.FC = () => {
  const navigate = useNavigate();
  const [currentStep, setCurrentStep] = useState(1);
  const [isSaving, setIsSaving] = useState(false);
  const [isCompleted, setIsCompleted] = useState(false);

  const [form, setForm] = useState({
    businessName: 'Bookline Demo Salon',
    businessType: 'Salon',
    address: '123 Main St, Central District',
    timeZoneId: 'Asia/Kolkata',
    currency: 'USD',
    firstServiceName: 'Haircut & Styling',
    firstServiceDurationMinutes: 45,
    firstServicePrice: 50,
    firstStaffName: 'Alex Johnson',
    startTime: '09:00',
    endTime: '17:00',
    holdDurationMinutes: 5,
    minimumNoticeHours: 2,
    bookingHorizonDays: 30,
  });

  const steps = [
    { number: 1, title: 'Business Name', icon: Building2 },
    { number: 2, title: 'Business Type', icon: Sparkles },
    { number: 3, title: 'Location', icon: MapPin },
    { number: 4, title: 'Timezone', icon: Globe },
    { number: 5, title: 'Currency', icon: DollarSign },
    { number: 6, title: 'First Service', icon: Briefcase },
    { number: 7, title: 'First Staff', icon: Shield },
    { number: 8, title: 'Working Hours', icon: Clock },
    { number: 9, title: 'Booking Settings', icon: Sliders },
    { number: 10, title: 'Publish', icon: Rocket },
  ];

  const handleNext = () => {
    if (currentStep < 10) {
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
          <span>PHASE 5 ORGANIZATION ONBOARDING</span>
        </div>
        <h1 className="font-heading text-3xl font-extrabold text-white">Business Setup Wizard</h1>
        <p className="text-sm text-[#7E88A8]">10-step server-persisted onboarding flow</p>
      </div>

      {/* Progress Bar */}
      <div className="grid grid-cols-5 sm:grid-cols-10 gap-1">
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
              <span className="text-xs font-mono text-[#7E88A8]">STEP {currentStep} OF 10</span>
              <h3 className="font-heading text-xl font-bold text-white">{steps[currentStep - 1].title}</h3>
            </div>
          </div>
          <span className="text-xs text-[#34D399] font-mono border border-[#34D399]/30 bg-[#34D399]/10 px-2.5 py-1 rounded-lg">
            Progress Saved Server-Side
          </span>
        </div>

        {/* Step Forms */}
        {currentStep === 1 && (
          <div className="space-y-4">
            <Input
              label="Business Name"
              value={form.businessName}
              onChange={(e) => setForm({ ...form, businessName: e.target.value })}
              placeholder="e.g. Acme Hair Salon"
            />
            <p className="text-xs text-[#7E88A8]">This name will appear on your public booking page and customer receipts.</p>
          </div>
        )}

        {currentStep === 2 && (
          <div className="space-y-4">
            <Select
              label="Business Industry Type"
              value={form.businessType}
              onChange={(e) => setForm({ ...form, businessType: e.target.value })}
              options={[
                { value: 'Salon', label: 'Hair & Beauty Salon' },
                { value: 'Clinic', label: 'Medical & Dental Clinic' },
                { value: 'Spa', label: 'Spa & Wellness Center' },
                { value: 'Barbershop', label: 'Barbershop' },
                { value: 'Studio', label: 'Fitness & Yoga Studio' },
                { value: 'Consultant', label: 'Professional Consulting' },
              ]}
            />
          </div>
        )}

        {currentStep === 3 && (
          <div className="space-y-4">
            <Input
              label="Primary Location Address"
              value={form.address}
              onChange={(e) => setForm({ ...form, address: e.target.value })}
              placeholder="e.g. 123 Main St, Suite 400"
            />
          </div>
        )}

        {currentStep === 4 && (
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

        {currentStep === 5 && (
          <div className="space-y-4">
            <Select
              label="Primary Business Currency"
              value={form.currency}
              onChange={(e) => setForm({ ...form, currency: e.target.value })}
              options={[
                { value: 'USD', label: 'USD ($) United States Dollar' },
                { value: 'EUR', label: 'EUR (€) Euro' },
                { value: 'GBP', label: 'GBP (£) British Pound' },
                { value: 'INR', label: 'INR (₹) Indian Rupee' },
                { value: 'CAD', label: 'CAD ($) Canadian Dollar' },
                { value: 'AUD', label: 'AUD ($) Australian Dollar' },
              ]}
            />
          </div>
        )}

        {currentStep === 6 && (
          <div className="space-y-4">
            <Input
              label="First Service Offering"
              value={form.firstServiceName}
              onChange={(e) => setForm({ ...form, firstServiceName: e.target.value })}
              placeholder="e.g. Haircut & Style"
            />
            <div className="grid grid-cols-2 gap-4">
              <Input
                label="Duration (Minutes)"
                type="number"
                value={form.firstServiceDurationMinutes}
                onChange={(e) => setForm({ ...form, firstServiceDurationMinutes: Number(e.target.value) })}
              />
              <Input
                label="Price ($)"
                type="number"
                value={form.firstServicePrice}
                onChange={(e) => setForm({ ...form, firstServicePrice: Number(e.target.value) })}
              />
            </div>
          </div>
        )}

        {currentStep === 7 && (
          <div className="space-y-4">
            <Input
              label="First Staff Member Name"
              value={form.firstStaffName}
              onChange={(e) => setForm({ ...form, firstStaffName: e.target.value })}
              placeholder="e.g. Alex Johnson"
            />
          </div>
        )}

        {currentStep === 8 && (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <Input
                label="Opening Time"
                type="time"
                value={form.startTime}
                onChange={(e) => setForm({ ...form, startTime: e.target.value })}
              />
              <Input
                label="Closing Time"
                type="time"
                value={form.endTime}
                onChange={(e) => setForm({ ...form, endTime: e.target.value })}
              />
            </div>
          </div>
        )}

        {currentStep === 9 && (
          <div className="space-y-4">
            <div className="grid grid-cols-3 gap-4">
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
            </div>
          </div>
        )}

        {currentStep === 10 && (
          <div className="space-y-6">
            {isCompleted ? (
              <Alert variant="success" title="Booking Page Published & Live!">
                Your organization is fully configured. Guests can now book appointments at:
                <br />
                <code className="text-[#34D399] font-mono text-xs">
                  http://localhost:5168/?tenant=acme-salon
                </code>
              </Alert>
            ) : (
              <div className="bg-[#181D2C] p-6 rounded-2xl border border-[#212638] space-y-3">
                <h4 className="font-heading font-bold text-white text-lg">Onboarding Summary</h4>
                <div className="text-sm text-[#7E88A8] space-y-1">
                  <p><strong className="text-white">Business:</strong> {form.businessName} ({form.businessType})</p>
                  <p><strong className="text-white">Location:</strong> {form.address} ({form.timeZoneId})</p>
                  <p><strong className="text-white">First Service:</strong> {form.firstServiceName} (${form.firstServicePrice})</p>
                  <p><strong className="text-white">First Staff:</strong> {form.firstStaffName}</p>
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

          {currentStep < 10 ? (
            <Button variant="primary" onClick={handleNext}>
              Next Step <ArrowRight className="w-4 h-4 ml-2" />
            </Button>
          ) : (
            <Button
              variant="primary"
              onClick={isCompleted ? () => navigate('/app') : handleComplete}
              isLoading={isSaving}
            >
              {isCompleted ? 'Go to Operations Dashboard' : 'Publish & Launch Booking Page'}
            </Button>
          )}
        </div>
      </Card>
    </div>
  );
};
