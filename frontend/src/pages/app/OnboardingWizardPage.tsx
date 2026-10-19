import React, { useState, useEffect } from 'react';
import { useNavigate, useSearchParams, Link } from 'react-router-dom';
import { Button } from '../../components/ui/Button';
import { Input, Select } from '../../components/forms/Inputs';
import { Card } from '../../components/data-display/DataDisplay';
import { Alert } from '../../components/feedback/Feedback';
import { useAuth } from '../../app/providers/AuthProvider';
import {
  onboardingApi,
  OnboardingStatus,
  OnboardingReadiness,
  SaveStepData,
} from '../../services/api/onboarding';
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
  CheckCircle2,
  AlertCircle,
  AlertTriangle,
  ExternalLink,
  RefreshCw,
} from 'lucide-react';

/**
 * Specification Section 28 & 74: PROVIDER ONBOARDING PIPELINE
 * Flow:
 * 1. Account -> 2. Business -> 3. Business Type -> 4. Profile -> 5. Location ->
 * 6. Timezone & Currency -> 7. Services -> 8. Products -> 9. Staff -> 10. Hours ->
 * 11. Booking Rules -> 12. Review Readiness -> 13. Publish Storefront
 */
export const OnboardingWizardPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const { user, token } = useAuth();

  const [currentStep, setCurrentStep] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isPublishing, setIsPublishing] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [publishError, setPublishError] = useState<string | null>(null);

  const [completedSteps, setCompletedSteps] = useState<number[]>([1]);
  const [readiness, setReadiness] = useState<OnboardingReadiness | null>(null);
  const [isPublished, setIsPublished] = useState(false);
  const [publishedStorefrontUrl, setPublishedStorefrontUrl] = useState<string>('');

  const [form, setForm] = useState({
    // Step 1: Account
    ownerName: user?.fullName || 'Business Owner',
    ownerEmail: user?.email || 'owner@bookline.local',
    ownerPhone: '+91 98250 11223',
    // Step 2: Business
    businessName: 'Glow Luxury Studio',
    businessSlug: 'glow-luxury-studio',
    // Step 3: Business Type & Category
    businessType: 'Hair Salon',
    category: 'Hair Salon',
    // Step 4: Business Profile
    profileBio: 'Premium boutique hair styling, creative coloring, and restorative scalp therapy.',
    websiteUrl: 'https://glowstudio.example.com',
    publicPhone: '+91 98250 11223',
    // Step 5: Location
    locationName: 'Glow Studio Bodakdev',
    city: 'Ahmedabad',
    address: '402 Bodakdev Commercial Hub, SG Highway',
    state: 'Gujarat',
    postalCode: '380054',
    country: 'India',
    // Step 6: Timezone & Currency
    timeZoneId: 'Asia/Kolkata',
    currency: 'INR',
    // Step 7: Services
    firstServiceName: 'Signature Balayage & Cut',
    firstServiceDurationMinutes: 60,
    firstServicePrice: 3200,
    // Step 8: Products (Optional)
    firstProductName: 'Botanical Argan Hair Oil (100ml)',
    firstProductPrice: 850,
    firstProductStock: 25,
    firstProductSku: 'GLW-OIL-100',
    // Step 9: Staff
    firstStaffName: 'Marcus Brody',
    firstStaffTitle: 'Master Stylist',
    // Step 10: Working Hours
    startTime: '09:00',
    endTime: '18:00',
    workingDays: [1, 2, 3, 4, 5],
    // Step 11: Booking Rules
    holdDurationMinutes: 5,
    minimumNoticeHours: 2,
    bookingHorizonDays: 30,
    cancellationWindowHours: 4,
  });

  const steps = [
    { number: 1, title: 'Account', icon: User, subtitle: 'Authenticated Identity' },
    { number: 2, title: 'Business', icon: Building2, subtitle: 'Name & Handle' },
    { number: 3, title: 'Category', icon: Layers, subtitle: 'Business Category' },
    { number: 4, title: 'Profile', icon: FileText, subtitle: 'Bio & Contact' },
    { number: 5, title: 'Location', icon: MapPin, subtitle: 'Physical Address' },
    { number: 6, title: 'Time & Currency', icon: Globe, subtitle: 'IANA Zone & Billing' },
    { number: 7, title: 'Services', icon: Briefcase, subtitle: 'Treatments & Pricing' },
    { number: 8, title: 'Products', icon: Package, subtitle: 'Retail Merchandise' },
    { number: 9, title: 'Staff', icon: UserCheck, subtitle: 'Specialists & Team' },
    { number: 10, title: 'Hours', icon: Clock, subtitle: 'Weekly Operating Timetable' },
    { number: 11, title: 'Booking Rules', icon: Sliders, subtitle: 'Holds & Horizons' },
    { number: 12, title: 'Readiness', icon: Shield, subtitle: 'Operational Audit' },
    { number: 13, title: 'Publish', icon: Rocket, subtitle: 'Launch on Marketplace' },
  ];

  // Section 28: Save-and-resume. Recover persisted onboarding progress from database on mount.
  useEffect(() => {
    if (!token) {
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    setSaveError(null);

    onboardingApi
      .getStatus(token)
      .then((status) => {
        setForm((prev) => ({
          ...prev,
          businessName: status.businessName || prev.businessName,
          businessSlug: status.businessSlug || prev.businessSlug,
          businessType: status.businessType || prev.businessType,
          category: status.category || prev.category,
          profileBio: status.description || prev.profileBio,
          websiteUrl: status.website || prev.websiteUrl,
          publicPhone: status.phone || prev.publicPhone,
          address: status.address || prev.address,
          city: status.city || prev.city,
          state: status.state || prev.state,
          postalCode: status.postalCode || prev.postalCode,
          country: status.country || prev.country,
          timeZoneId: status.timeZoneId || prev.timeZoneId,
          currency: status.currency || prev.currency,
          firstServiceName: status.firstServiceName || prev.firstServiceName,
          firstServiceDurationMinutes: status.firstServiceDurationMinutes || prev.firstServiceDurationMinutes,
          firstServicePrice: status.firstServicePrice ? Number(status.firstServicePrice) : prev.firstServicePrice,
          firstProductName: status.firstProductName || prev.firstProductName,
          firstProductPrice: status.firstProductPrice ? Number(status.firstProductPrice) : prev.firstProductPrice,
          firstProductStock: status.firstProductStock ?? prev.firstProductStock,
          firstProductSku: status.firstProductSku || prev.firstProductSku,
          firstStaffName: status.firstStaffName || prev.firstStaffName,
          firstStaffTitle: status.firstStaffTitle || prev.firstStaffTitle,
          startTime: status.startTime || prev.startTime,
          endTime: status.endTime || prev.endTime,
          holdDurationMinutes: status.holdDurationMinutes || prev.holdDurationMinutes,
          minimumNoticeHours: status.minimumNoticeHours || prev.minimumNoticeHours,
          bookingHorizonDays: status.bookingHorizonDays || prev.bookingHorizonDays,
        }));

        setCompletedSteps(status.completedSteps && status.completedSteps.length > 0 ? status.completedSteps : [1]);
        setIsPublished(status.isPublished);
        if (status.isPublished && status.businessSlug) {
          setPublishedStorefrontUrl(`/business/${status.businessSlug}`);
        }

        // Check if query param specifies step (e.g. ?step=5)
        const stepParam = searchParams.get('step');
        if (stepParam && Number(stepParam) >= 1 && Number(stepParam) <= 13) {
          setCurrentStep(Number(stepParam));
        } else if (status.currentStep && status.currentStep >= 1 && status.currentStep <= 13) {
          setCurrentStep(status.currentStep);
        }
      })
      .catch((err) => {
        // Fallback gracefully to default step 1
        console.warn('Could not retrieve remote onboarding status, running in local recovery mode:', err);
      })
      .finally(() => {
        setIsLoading(false);
      });
  }, [token, searchParams]);

  // When entering step 12 or 13, fetch server readiness
  useEffect(() => {
    if (token && (currentStep === 12 || currentStep === 13)) {
      loadReadiness();
    }
  }, [currentStep, token]);

  const loadReadiness = async () => {
    if (!token) return;
    try {
      const res = await onboardingApi.getReadiness(token);
      setReadiness(res);
    } catch (err: any) {
      console.warn('Could not evaluate readiness:', err);
    }
  };

  const handleStepClick = (stepNumber: number) => {
    setCurrentStep(stepNumber);
    setSearchParams({ step: stepNumber.toString() });
  };

  // Section 28: Each step must persist its data to the backend
  const handleNext = async () => {
    setSaveError(null);

    // Build payload for current step
    const payload: SaveStepData = { step: currentStep };

    if (currentStep === 2) {
      payload.businessName = form.businessName;
      payload.businessSlug = form.businessSlug;
    } else if (currentStep === 3) {
      payload.businessType = form.businessType;
      payload.category = form.category;
    } else if (currentStep === 4) {
      payload.description = form.profileBio;
      payload.website = form.websiteUrl;
      payload.phone = form.publicPhone;
    } else if (currentStep === 5) {
      payload.locationName = form.locationName;
      payload.address = form.address;
      payload.city = form.city;
      payload.state = form.state;
      payload.postalCode = form.postalCode;
      payload.country = form.country;
    } else if (currentStep === 6) {
      payload.timeZoneId = form.timeZoneId;
      payload.currency = form.currency;
    } else if (currentStep === 7) {
      payload.firstServiceName = form.firstServiceName;
      payload.firstServiceDurationMinutes = form.firstServiceDurationMinutes;
      payload.firstServicePrice = form.firstServicePrice;
    } else if (currentStep === 8) {
      payload.firstProductName = form.firstProductName;
      payload.firstProductPrice = form.firstProductPrice;
      payload.firstProductStock = form.firstProductStock;
      payload.firstProductSku = form.firstProductSku;
    } else if (currentStep === 9) {
      payload.firstStaffName = form.firstStaffName;
      payload.firstStaffTitle = form.firstStaffTitle;
    } else if (currentStep === 10) {
      payload.startTime = form.startTime;
      payload.endTime = form.endTime;
      payload.workingDays = form.workingDays;
    } else if (currentStep === 11) {
      payload.holdDurationMinutes = form.holdDurationMinutes;
      payload.minimumNoticeHours = form.minimumNoticeHours;
      payload.bookingHorizonDays = form.bookingHorizonDays;
      payload.cancellationWindowHours = form.cancellationWindowHours;
    }

    if (token) {
      setIsSaving(true);
      try {
        const updatedStatus = await onboardingApi.saveStep(token, payload);
        if (updatedStatus.completedSteps) {
          setCompletedSteps(updatedStatus.completedSteps);
        }
      } catch (err: any) {
        setSaveError(err.message || 'Failed to persist step changes to backend.');
        setIsSaving(false);
        return;
      } finally {
        setIsSaving(false);
      }
    }

    if (currentStep < 13) {
      const next = currentStep + 1;
      setCurrentStep(next);
      setSearchParams({ step: next.toString() });
    }
  };

  const handleBack = () => {
    if (currentStep > 1) {
      const prev = currentStep - 1;
      setCurrentStep(prev);
      setSearchParams({ step: prev.toString() });
    }
  };

  // Section 28 Step 13: Publishing evaluates actual readiness on the server
  const handlePublishStorefront = async () => {
    if (!token) return;
    setIsPublishing(true);
    setPublishError(null);

    try {
      const result = await onboardingApi.publishStorefront(token);
      setIsPublished(true);
      setPublishedStorefrontUrl(result.storefrontUrl || `/store/${form.businessSlug}`);
      setCompletedSteps((prev) => (prev.includes(13) ? prev : [...prev, 13]));
    } catch (err: any) {
      setPublishError(err.message || 'Publication rejected. Please review readiness checklist.');
    } finally {
      setIsPublishing(false);
    }
  };

  const calculatedPercentage = Math.round((completedSteps.length / 13) * 100);

  if (isLoading) {
    return (
      <div className="max-w-4xl mx-auto py-20 text-center space-y-4 animate-pulse">
        <div className="w-12 h-12 rounded-2xl bg-[#181D2C] border border-[#212638] mx-auto flex items-center justify-center text-[#E8546A]">
          <RefreshCw className="w-6 h-6 animate-spin" />
        </div>
        <p className="text-sm font-mono text-[#7E88A8]">Recovering persisted onboarding state from server...</p>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto space-y-8 pb-24">
      {/* Header */}
      <div className="text-center space-y-2 border-b border-[#212638] pb-6">
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-[#181D2C] border border-[#212638] text-xs font-mono text-[#E8546A]">
          <Sparkles className="w-3.5 h-3.5" />
          <span>SECTION 28: VERIFIED ONBOARDING PIPELINE</span>
        </div>
        <h1 className="font-heading text-3xl font-extrabold text-white">Provider Setup &amp; Storefront Launch</h1>
        <p className="text-sm text-[#7E88A8]">
          Server-persisted setup pipeline with automated readiness validation
        </p>

        {/* Dynamic Progress Indicator based on saved DB records */}
        <div className="pt-2 max-w-xs mx-auto">
          <div className="flex justify-between text-[11px] font-mono text-[#7E88A8] mb-1">
            <span>Server-Persisted Progress</span>
            <span className="text-[#34D399] font-bold">{calculatedPercentage}%</span>
          </div>
          <div className="w-full h-2 rounded-full bg-[#181D2C] overflow-hidden border border-[#212638]">
            <div
              className="h-full bg-gradient-to-r from-[#E8546A] to-[#34D399] transition-all duration-500 rounded-full"
              style={{ width: `${calculatedPercentage}%` }}
            />
          </div>
        </div>
      </div>

      {/* Stepper Navigation */}
      <div className="grid grid-cols-7 sm:grid-cols-13 gap-1.5 bg-[#111520] p-2 rounded-2xl border border-[#212638]">
        {steps.map((s) => {
          const isDone = completedSteps.includes(s.number);
          const isCurrent = s.number === currentStep;

          return (
            <button
              key={s.number}
              type="button"
              onClick={() => handleStepClick(s.number)}
              title={`Step ${s.number}: ${s.title}`}
              className={`h-2.5 rounded-full transition-all ${
                isCurrent
                  ? 'bg-[#E8546A] ring-2 ring-[#E8546A]/40'
                  : isDone
                  ? 'bg-[#34D399] hover:bg-[#34D399]/80'
                  : 'bg-[#212638] hover:bg-[#2e354d]'
              }`}
            />
          );
        })}
      </div>

      {saveError && (
        <Alert variant="error" title="Step Persistence Notice">
          {saveError}
        </Alert>
      )}

      {/* Main Step Container */}
      <Card className="p-8 space-y-6 bg-[#111520] border-[#212638] shadow-2xl">
        <div className="flex items-center justify-between border-b border-[#212638] pb-4">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 rounded-2xl bg-[#E8546A]/10 border border-[#E8546A]/20 flex items-center justify-center text-[#E8546A]">
              {React.createElement(steps[currentStep - 1].icon, { className: 'w-6 h-6' })}
            </div>
            <div>
              <span className="text-xs font-mono text-[#7E88A8]">
                STEP {currentStep} OF 13 &bull; {steps[currentStep - 1].subtitle}
              </span>
              <h3 className="font-heading text-2xl font-bold text-white">
                {steps[currentStep - 1].title}
              </h3>
            </div>
          </div>
          <span className="text-xs text-[#34D399] font-mono border border-[#34D399]/30 bg-[#34D399]/10 px-3 py-1 rounded-xl flex items-center gap-1.5">
            <CheckCircle2 className="w-3.5 h-3.5" />
            <span>Persisted</span>
          </span>
        </div>

        {/* Step 1: Account */}
        {currentStep === 1 && (
          <div className="space-y-4 animate-fadeIn">
            <div className="p-4 rounded-xl bg-[#181D2C] border border-[#212638] text-xs text-[#7E88A8] space-y-1">
              <p className="font-bold text-white">Authenticated Provider Identity</p>
              <p>Account ownership and authorization boundaries are securely enforced via JWT tokens.</p>
            </div>
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
                label="Direct Contact Phone"
                value={form.ownerPhone}
                onChange={(e) => setForm({ ...form, ownerPhone: e.target.value })}
              />
            </div>
          </div>
        )}

        {/* Step 2: Business */}
        {currentStep === 2 && (
          <div className="space-y-4 animate-fadeIn">
            <Input
              label="Business Legal / Brand Name"
              value={form.businessName}
              onChange={(e) => setForm({ ...form, businessName: e.target.value })}
              placeholder="e.g. Glow Luxury Studio"
              required
            />
            <Input
              label="Storefront Custom URL Slug"
              value={form.businessSlug}
              onChange={(e) => setForm({ ...form, businessSlug: e.target.value })}
              placeholder="e.g. glow-luxury-studio"
              required
            />
            <p className="text-[11px] text-[#7E88A8]">
              Your public storefront will be discoverable at: <code className="text-[#34D399] font-mono">/business/{form.businessSlug}</code>
            </p>
          </div>
        )}

        {/* Step 3: Business Type */}
        {currentStep === 3 && (
          <div className="space-y-4 animate-fadeIn">
            <Select
              label="Primary Business Category"
              value={form.category}
              onChange={(e) => setForm({ ...form, category: e.target.value, businessType: e.target.value })}
              options={[
                { value: 'Hair Salon', label: 'Hair Salon & Master Stylists' },
                { value: 'Wellness & Spa', label: 'Wellness, Spa & Massage Therapy' },
                { value: 'Barbershop', label: 'Barbershop & Traditional Grooming' },
                { value: 'Aesthetic Clinic', label: 'Aesthetic & Skin Clinic' },
                { value: 'Nail Lounge', label: 'Nail, Lash & Brow Lounge' },
                { value: 'Fitness Studio', label: 'Fitness & Personal Training' },
              ]}
            />
          </div>
        )}

        {/* Step 4: Business Profile */}
        {currentStep === 4 && (
          <div className="space-y-4 animate-fadeIn">
            <div>
              <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                Storefront Overview &amp; Client Experience Philosophy <span className="text-[#E8546A]">*</span>
              </label>
              <textarea
                rows={4}
                value={form.profileBio}
                onChange={(e) => setForm({ ...form, profileBio: e.target.value })}
                placeholder="Describe your studio's ambiance, hygiene protocols, and treatment philosophy..."
                className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white text-xs focus:outline-none focus:border-[#E8546A]"
                required
              />
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Input
                label="Official Website URL"
                value={form.websiteUrl}
                onChange={(e) => setForm({ ...form, websiteUrl: e.target.value })}
                placeholder="https://glowstudio.example.com"
              />
              <Input
                label="Public Customer Support Phone"
                value={form.publicPhone}
                onChange={(e) => setForm({ ...form, publicPhone: e.target.value })}
                placeholder="+91 98250 11223"
              />
            </div>
          </div>
        )}

        {/* Step 5: First Location */}
        {currentStep === 5 && (
          <div className="space-y-4 animate-fadeIn">
            <Input
              label="Location Name"
              value={form.locationName}
              onChange={(e) => setForm({ ...form, locationName: e.target.value })}
              placeholder="e.g. Flagship Studio Bodakdev"
            />
            <Input
              label="Street Address / Building / Suite"
              value={form.address}
              onChange={(e) => setForm({ ...form, address: e.target.value })}
              placeholder="e.g. 402 Bodakdev Hub, SG Highway"
              required
            />
            <div className="grid grid-cols-1 sm:grid-cols-4 gap-4">
              <Input
                label="City"
                value={form.city}
                onChange={(e) => setForm({ ...form, city: e.target.value })}
                required
              />
              <Input
                label="State"
                value={form.state}
                onChange={(e) => setForm({ ...form, state: e.target.value })}
              />
              <Input
                label="Postal Code"
                value={form.postalCode}
                onChange={(e) => setForm({ ...form, postalCode: e.target.value })}
                required
              />
              <Input
                label="Country"
                value={form.country}
                onChange={(e) => setForm({ ...form, country: e.target.value })}
              />
            </div>
          </div>
        )}

        {/* Step 6: Timezone & Currency */}
        {currentStep === 6 && (
          <div className="space-y-4 animate-fadeIn">
            <div className="p-4 rounded-xl bg-[#181D2C] border border-[#212638] text-xs text-[#7E88A8]">
              <strong className="text-white block mb-1">Timezone &amp; Currency Policy (Section 28 Step 6)</strong>
              Requires a valid IANA timezone identifier. Changing timezone or currency establishes policy for new appointments without silently altering existing scheduled bookings.
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Select
                label="Location IANA Timezone"
                value={form.timeZoneId}
                onChange={(e) => setForm({ ...form, timeZoneId: e.target.value })}
                options={[
                  { value: 'Asia/Kolkata', label: 'Asia/Kolkata (IST +05:30)' },
                  { value: 'America/New_York', label: 'America/New_York (EST -05:00)' },
                  { value: 'America/Los_Angeles', label: 'America/Los_Angeles (PST -08:00)' },
                  { value: 'Europe/London', label: 'Europe/London (GMT/BST +00:00)' },
                  { value: 'Europe/Paris', label: 'Europe/Paris (CET +01:00)' },
                  { value: 'Asia/Dubai', label: 'Asia/Dubai (GST +04:00)' },
                  { value: 'UTC', label: 'UTC (Coordinated Universal Time)' },
                ]}
              />
              <Select
                label="Platform Operating Currency"
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
          </div>
        )}

        {/* Step 7: Services */}
        {currentStep === 7 && (
          <div className="space-y-4 animate-fadeIn">
            <Input
              label="Primary Service / Treatment Name"
              value={form.firstServiceName}
              onChange={(e) => setForm({ ...form, firstServiceName: e.target.value })}
              placeholder="e.g. Signature Balayage & Cut"
              required
            />
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Input
                label="Duration (Minutes)"
                type="number"
                value={form.firstServiceDurationMinutes}
                onChange={(e) => setForm({ ...form, firstServiceDurationMinutes: Number(e.target.value) })}
                required
              />
              <Input
                label={`Price (${form.currency})`}
                type="number"
                value={form.firstServicePrice}
                onChange={(e) => setForm({ ...form, firstServicePrice: Number(e.target.value) })}
                required
              />
            </div>
          </div>
        )}

        {/* Step 8: Products (Optional) */}
        {currentStep === 8 && (
          <div className="space-y-4 animate-fadeIn">
            <div className="flex items-center justify-between p-3 rounded-xl bg-[#181D2C] border border-[#212638] text-xs">
              <span className="text-[#7E88A8]">Physical Retail Catalog</span>
              <span className="text-[#34D399] font-bold">Optional Step</span>
            </div>
            <Input
              label="First Retail Product Name"
              value={form.firstProductName}
              onChange={(e) => setForm({ ...form, firstProductName: e.target.value })}
              placeholder="e.g. Botanical Argan Hair Oil (100ml)"
            />
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              <Input
                label={`Price (${form.currency})`}
                type="number"
                value={form.firstProductPrice}
                onChange={(e) => setForm({ ...form, firstProductPrice: Number(e.target.value) })}
              />
              <Input
                label="Inventory Stock Units"
                type="number"
                value={form.firstProductStock}
                onChange={(e) => setForm({ ...form, firstProductStock: Number(e.target.value) })}
              />
              <Input
                label="SKU Identifier"
                value={form.firstProductSku}
                onChange={(e) => setForm({ ...form, firstProductSku: e.target.value })}
                placeholder="e.g. GLW-OIL-100"
              />
            </div>
          </div>
        )}

        {/* Step 9: Staff */}
        {currentStep === 9 && (
          <div className="space-y-4 animate-fadeIn">
            <Input
              label="Specialist / Staff Member Full Name"
              value={form.firstStaffName}
              onChange={(e) => setForm({ ...form, firstStaffName: e.target.value })}
              placeholder="e.g. Marcus Brody"
              required
            />
            <Input
              label="Title / Specialist Role"
              value={form.firstStaffTitle}
              onChange={(e) => setForm({ ...form, firstStaffTitle: e.target.value })}
              placeholder="e.g. Master Stylist & Colorist"
              required
            />
          </div>
        )}

        {/* Step 10: Working Hours */}
        {currentStep === 10 && (
          <div className="space-y-4 animate-fadeIn">
            <div className="p-4 rounded-xl bg-[#181D2C] border border-[#212638] text-xs text-[#7E88A8]">
              <strong className="text-white block mb-1">Weekly Operating Schedule</strong>
              Configures standard working windows for specialist availability calculations.
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Input
                label="Shift Start Time"
                type="time"
                value={form.startTime}
                onChange={(e) => setForm({ ...form, startTime: e.target.value })}
                required
              />
              <Input
                label="Shift End Time"
                type="time"
                value={form.endTime}
                onChange={(e) => setForm({ ...form, endTime: e.target.value })}
                required
              />
            </div>
          </div>
        )}

        {/* Step 11: Booking Rules */}
        {currentStep === 11 && (
          <div className="space-y-4 animate-fadeIn">
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              <Input
                label="Atomic Slot Hold (Minutes)"
                type="number"
                value={form.holdDurationMinutes}
                onChange={(e) => setForm({ ...form, holdDurationMinutes: Number(e.target.value) })}
                required
              />
              <Input
                label="Minimum Notice (Hours)"
                type="number"
                value={form.minimumNoticeHours}
                onChange={(e) => setForm({ ...form, minimumNoticeHours: Number(e.target.value) })}
                required
              />
              <Input
                label="Advance Booking Horizon (Days)"
                type="number"
                value={form.bookingHorizonDays}
                onChange={(e) => setForm({ ...form, bookingHorizonDays: Number(e.target.value) })}
                required
              />
            </div>
          </div>
        )}

        {/* Step 12: Review Readiness (Section 28 Step 12) */}
        {currentStep === 12 && (
          <div className="space-y-6 animate-fadeIn">
            <div className="flex items-center justify-between pb-3 border-b border-[#212638]">
              <div>
                <h4 className="font-heading text-lg font-bold text-white">Server Publication Readiness Audit</h4>
                <p className="text-xs text-[#7E88A8]">
                  Automated verification of persisted business, service, staff, and operational parameters
                </p>
              </div>
              <button
                type="button"
                onClick={loadReadiness}
                className="px-3 py-1.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] text-xs font-semibold text-white border border-[#212638] flex items-center gap-1.5 transition-colors"
              >
                <RefreshCw className="w-3.5 h-3.5 text-[#34D399]" />
                <span>Re-evaluate</span>
              </button>
            </div>

            {readiness && (
              <div className="space-y-4">
                <div
                  className={`p-4 rounded-2xl border flex items-center justify-between ${
                    readiness.isReady
                      ? 'bg-[#34D399]/10 border-[#34D399]/30 text-[#34D399]'
                      : 'bg-amber-500/10 border-amber-500/30 text-amber-300'
                  }`}
                >
                  <div className="flex items-center gap-3">
                    {readiness.isReady ? (
                      <CheckCircle2 className="w-6 h-6 text-[#34D399]" />
                    ) : (
                      <AlertTriangle className="w-6 h-6 text-amber-400" />
                    )}
                    <div>
                      <p className="font-bold text-sm text-white">
                        {readiness.isReady
                          ? 'All Mandatory Requirements Satisfied'
                          : 'Mandatory Requirements Incomplete'}
                      </p>
                      <p className="text-xs opacity-90">
                        {readiness.satisfiedMandatory} of {readiness.totalMandatory} mandatory requirements verified on backend.
                      </p>
                    </div>
                  </div>
                  <span className="font-mono text-xs font-bold px-3 py-1 rounded-full bg-[#181D2C]">
                    {readiness.isReady ? 'READY TO LAUNCH' : 'ACTION REQUIRED'}
                  </span>
                </div>

                <div className="divide-y divide-[#212638] bg-[#181D2C] rounded-2xl border border-[#212638] overflow-hidden">
                  {readiness.requirements.map((req) => (
                    <div key={req.key} className="p-4 flex items-center justify-between gap-4">
                      <div className="flex items-start gap-3 min-w-0">
                        {req.isSatisfied ? (
                          <CheckCircle2 className="w-5 h-5 text-[#34D399] mt-0.5 flex-shrink-0" />
                        ) : req.isMandatory ? (
                          <AlertCircle className="w-5 h-5 text-red-400 mt-0.5 flex-shrink-0" />
                        ) : (
                          <Clock className="w-5 h-5 text-[#7E88A8] mt-0.5 flex-shrink-0" />
                        )}
                        <div>
                          <div className="flex items-center gap-2">
                            <span className="text-xs font-bold text-white">{req.title}</span>
                            {req.isMandatory ? (
                              <span className="text-[10px] uppercase font-bold tracking-wider text-red-400 bg-red-400/10 px-1.5 py-0.5 rounded">
                                Mandatory
                              </span>
                            ) : (
                              <span className="text-[10px] uppercase font-bold tracking-wider text-[#7E88A8] bg-[#7E88A8]/10 px-1.5 py-0.5 rounded">
                                Optional
                              </span>
                            )}
                          </div>
                          <p className="text-[11px] text-[#7E88A8] mt-0.5">{req.description}</p>
                        </div>
                      </div>

                      <div className="flex items-center gap-2 flex-shrink-0">
                        <span
                          className={`text-xs font-mono px-2.5 py-0.5 rounded-full ${
                            req.isSatisfied
                              ? 'bg-[#34D399]/10 text-[#34D399] border border-[#34D399]/20 font-bold'
                              : 'bg-red-500/10 text-red-400 border border-red-500/20'
                          }`}
                        >
                          {req.isSatisfied ? 'Verified' : 'Missing'}
                        </span>
                        {!req.isSatisfied && (
                          <button
                            type="button"
                            onClick={() => handleStepClick(req.stepNumber)}
                            className="px-2.5 py-1 rounded-lg bg-[#212638] hover:bg-[#E8546A] text-white text-xs font-semibold transition-colors"
                          >
                            Edit
                          </button>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}

        {/* Step 13: Publish Storefront (Section 28 Step 13) */}
        {currentStep === 13 && (
          <div className="space-y-6 animate-fadeIn">
            {publishError && (
              <Alert variant="error" title="Publication Check Failed">
                {publishError}
              </Alert>
            )}

            {isPublished ? (
              <div className="p-8 rounded-3xl bg-gradient-to-br from-[#181D2C] to-[#111520] border border-[#34D399]/30 text-center space-y-4 shadow-2xl">
                <div className="w-16 h-16 rounded-3xl bg-[#34D399]/20 text-[#34D399] flex items-center justify-center mx-auto shadow-lg shadow-[#34D399]/10">
                  <Rocket className="w-8 h-8" />
                </div>
                <h3 className="font-heading text-2xl font-bold text-white">Storefront Live &amp; Published!</h3>
                <p className="text-xs text-[#7E88A8] max-w-md mx-auto leading-relaxed">
                  Your business profile, treatment catalogue, and real-time scheduling calendar are now discoverable across Bookline.
                </p>
                <div className="pt-2 flex flex-col sm:flex-row items-center justify-center gap-3">
                  <Link
                    to={publishedStorefrontUrl || `/business/${form.businessSlug}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="w-full sm:w-auto px-6 py-3 rounded-2xl bg-[#E8546A] hover:bg-[#D44359] text-white text-xs font-bold shadow-lg shadow-[#E8546A]/20 flex items-center justify-center gap-2 transition-all"
                  >
                    <span>View Public Storefront</span>
                    <ExternalLink className="w-4 h-4" />
                  </Link>
                  <button
                    type="button"
                    onClick={() => navigate('/provider/dashboard')}
                    className="w-full sm:w-auto px-6 py-3 rounded-2xl bg-[#181D2C] hover:bg-[#212638] text-white text-xs font-bold border border-[#212638] transition-all"
                  >
                    Go to Provider OS
                  </button>
                </div>
              </div>
            ) : (
              <div className="space-y-6">
                <div className="bg-[#181D2C] p-6 rounded-2xl border border-[#212638] space-y-4">
                  <div className="flex items-center justify-between border-b border-[#212638] pb-3">
                    <h4 className="font-heading font-bold text-white text-base">Storefront Pre-Launch Summary</h4>
                    <span className="text-xs font-mono text-[#34D399]">Readiness Verified</span>
                  </div>
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs text-[#7E88A8]">
                    <p><strong className="text-white">Business:</strong> {form.businessName} ({form.category})</p>
                    <p><strong className="text-white">Location:</strong> {form.address}, {form.city}</p>
                    <p><strong className="text-white">Timezone &amp; Currency:</strong> {form.timeZoneId} &bull; {form.currency}</p>
                    <p><strong className="text-white">Service:</strong> {form.firstServiceName} ({form.currency} {form.firstServicePrice})</p>
                    <p><strong className="text-white">Specialist:</strong> {form.firstStaffName} ({form.firstStaffTitle})</p>
                    <p><strong className="text-white">Working Hours:</strong> {form.startTime} &ndash; {form.endTime}</p>
                    <p><strong className="text-white">Hold &amp; Notice:</strong> {form.holdDurationMinutes}m Hold &bull; {form.minimumNoticeHours}h Notice</p>
                    <p><strong className="text-white">Public URL:</strong> /business/{form.businessSlug}</p>
                  </div>
                </div>

                <div className="p-4 rounded-xl bg-[#181D2C] border border-[#212638] text-xs text-[#7E88A8] space-y-1">
                  <p className="font-bold text-white">Platform Governance Distinction</p>
                  <p className="leading-relaxed">
                    Storefront publication makes your profile discoverable for appointments. Platform verification and payout authorization badges operate under independent governance checks.
                  </p>
                </div>

                <Button
                  variant="primary"
                  onClick={handlePublishStorefront}
                  isLoading={isPublishing}
                  className="w-full py-3.5 rounded-2xl bg-[#E8546A] hover:bg-[#D44359] text-white text-sm font-bold shadow-xl shadow-[#E8546A]/20 flex items-center justify-center gap-2"
                >
                  <Rocket className="w-4 h-4" />
                  <span>Publish Storefront &amp; Go Live</span>
                </Button>
              </div>
            )}
          </div>
        )}

        {/* Action Controls */}
        <div className="flex items-center justify-between pt-6 border-t border-[#212638]">
          <Button variant="secondary" onClick={handleBack} disabled={currentStep === 1 || isSaving || isPublishing}>
            <ArrowLeft className="w-4 h-4 mr-2" /> Back
          </Button>

          {currentStep < 13 && (
            <Button variant="primary" onClick={handleNext} isLoading={isSaving}>
              Next Step <ArrowRight className="w-4 h-4 ml-2" />
            </Button>
          )}
        </div>
      </Card>
    </div>
  );
};
