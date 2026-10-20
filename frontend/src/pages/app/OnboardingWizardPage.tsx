import React, { useState, useEffect } from 'react';
import { useNavigate, useSearchParams, Link } from 'react-router-dom';
import { Button } from '../../components/ui/Button';
import { Input, Select } from '../../components/forms/Inputs';
import { Card } from '../../components/data-display/DataDisplay';
import { Alert } from '../../components/feedback/Feedback';
import { useAuth } from '../../app/providers/AuthProvider';
import {
  onboardingApi,
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
  CornerDownRight,
  Check,
} from 'lucide-react';

/**
 * Specification Section 110: PERSISTENT PROVIDER ONBOARDING PIPELINE
 * Flow:
 * 1. Account & Membership -> 2. Business -> 3. Category -> 4. Profile -> 5. Location ->
 * 6. Time & Currency -> 7. Services -> 8. Products (Optional) -> 9. Staff -> 10. Hours ->
 * 11. Booking Rules -> 12. Readiness Checks -> 13. Publish Storefront
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
    // Step 1: Account & Membership
    ownerName: user?.fullName || '',
    ownerEmail: user?.email || '',
    ownerPhone: '',
    membershipRole: 'Owner',
    // Step 2: Business Creation
    businessName: '',
    businessSlug: '',
    // Step 3: Business Type & Category
    businessType: 'Hair Salon',
    category: 'Hair Salon',
    // Step 4: Business Profile
    profileBio: '',
    websiteUrl: '',
    publicPhone: '',
    // Step 5: Location
    locationName: '',
    city: '',
    address: '',
    state: '',
    postalCode: '',
    country: '',
    // Step 6: Timezone & Currency
    timeZoneId: 'Asia/Kolkata',
    currency: 'INR',
    // Step 7: Services
    firstServiceName: '',
    firstServiceDurationMinutes: 60,
    firstServicePrice: 0,
    // Step 8: Products (Optional)
    firstProductName: '',
    firstProductPrice: 0,
    firstProductStock: 10,
    firstProductSku: '',
    // Step 9: Staff
    firstStaffName: '',
    firstStaffTitle: 'Master Specialist',
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
    { number: 1, title: 'Account', icon: User, subtitle: 'Authenticated Identity & Membership' },
    { number: 2, title: 'Business', icon: Building2, subtitle: 'Brand Name & Custom URL' },
    { number: 3, title: 'Category', icon: Layers, subtitle: 'Operational Industry Sector' },
    { number: 4, title: 'Profile', icon: FileText, subtitle: 'Storefront Bio & Contact Details' },
    { number: 5, title: 'Location', icon: MapPin, subtitle: 'Operational Physical Location' },
    { number: 6, title: 'Time & Currency', icon: Globe, subtitle: 'IANA Timezone & Billing Currency' },
    { number: 7, title: 'Services', icon: Briefcase, subtitle: 'Bookable Treatment & Pricing' },
    { number: 8, title: 'Products', icon: Package, subtitle: 'Retail Merchandise (Optional)' },
    { number: 9, title: 'Staff', icon: UserCheck, subtitle: 'Primary Specialist & Role' },
    { number: 10, title: 'Hours', icon: Clock, subtitle: 'Weekly Operating Timetable' },
    { number: 11, title: 'Booking Rules', icon: Sliders, subtitle: 'Holds & Horizon Policies' },
    { number: 12, title: 'Readiness', icon: Shield, subtitle: 'Backend Operational Audit' },
    { number: 13, title: 'Publish', icon: Rocket, subtitle: 'Launch on Bookline Marketplace' },
  ];

  // Section 110: Save-and-resume. Recover persisted onboarding progress from database on mount.
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
          ownerName: status.ownerName || user?.fullName || prev.ownerName,
          ownerEmail: status.ownerEmail || user?.email || prev.ownerEmail,
          ownerPhone: status.ownerPhone || prev.ownerPhone,
          membershipRole: status.membershipRole || prev.membershipRole,
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
          cancellationWindowHours: status.cancellationWindowHours || prev.cancellationWindowHours,
        }));

        setCompletedSteps(status.completedSteps && status.completedSteps.length > 0 ? status.completedSteps : [1]);
        setIsPublished(status.isPublished);
        if (status.isPublished && status.businessSlug) {
          setPublishedStorefrontUrl(`/business/${status.businessSlug}`);
        }

        // Resume step from URL query param if valid, or resume saved step from database
        const stepParam = searchParams.get('step');
        if (stepParam && Number(stepParam) >= 1 && Number(stepParam) <= 13) {
          setCurrentStep(Number(stepParam));
        } else if (status.currentStep && status.currentStep >= 1 && status.currentStep <= 13) {
          setCurrentStep(status.currentStep);
        }
      })
      .catch((err) => {
        console.warn('Could not retrieve remote onboarding status, running in session recovery mode:', err);
      })
      .finally(() => {
        setIsLoading(false);
      });
  }, [token, searchParams, user]);

  // When navigating to readiness (step 12) or launch (step 13), fetch server readiness
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
      console.warn('Could not evaluate server readiness:', err);
    }
  };

  const handleStepClick = (stepNumber: number) => {
    setSaveError(null);
    setCurrentStep(stepNumber);
    setSearchParams({ step: stepNumber.toString() });
  };

  const handleBusinessNameChange = (val: string) => {
    setForm((prev) => {
      // Auto-generate slug if not manually altered
      const autoSlug = val.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '');
      const shouldAutoSlug = !prev.businessSlug || prev.businessSlug === prev.businessName.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '');
      return {
        ...prev,
        businessName: val,
        businessSlug: shouldAutoSlug ? autoSlug : prev.businessSlug,
      };
    });
  };

  const toggleWorkingDay = (day: number) => {
    setForm((prev) => {
      const exists = prev.workingDays.includes(day);
      const updated = exists ? prev.workingDays.filter((d) => d !== day) : [...prev.workingDays, day].sort();
      return { ...prev, workingDays: updated.length ? updated : [day] };
    });
  };

  // Section 110: Save step data to the backend; preserve valid earlier data if later step fails
  const handleSaveStep = async (targetNextStep?: number) => {
    setSaveError(null);

    const payload: SaveStepData = { step: currentStep };

    if (currentStep === 1) {
      payload.ownerName = form.ownerName;
      payload.ownerPhone = form.ownerPhone;
    } else if (currentStep === 2) {
      if (!form.businessName.trim()) {
        setSaveError('Business Legal / Brand Name is required to proceed.');
        return;
      }
      payload.businessName = form.businessName.trim();
      payload.businessSlug = form.businessSlug.trim();
    } else if (currentStep === 3) {
      payload.businessType = form.businessType;
      payload.category = form.category;
    } else if (currentStep === 4) {
      if (!form.profileBio.trim()) {
        setSaveError('Storefront overview is required for public discoverability.');
        return;
      }
      payload.description = form.profileBio.trim();
      payload.website = form.websiteUrl.trim();
      payload.phone = form.publicPhone.trim();
    } else if (currentStep === 5) {
      if (!form.address.trim()) {
        setSaveError('Street address is required to register your physical location.');
        return;
      }
      payload.locationName = form.locationName.trim() || `${form.businessName || 'Main'} Studio`;
      payload.address = form.address.trim();
      payload.city = form.city.trim();
      payload.state = form.state.trim();
      payload.postalCode = form.postalCode.trim();
      payload.country = form.country.trim();
    } else if (currentStep === 6) {
      payload.timeZoneId = form.timeZoneId;
      payload.currency = form.currency;
    } else if (currentStep === 7) {
      if (!form.firstServiceName.trim()) {
        setSaveError('Primary service name is required to accept client bookings.');
        return;
      }
      payload.firstServiceName = form.firstServiceName.trim();
      payload.firstServiceDurationMinutes = Number(form.firstServiceDurationMinutes) || 60;
      payload.firstServicePrice = Number(form.firstServicePrice) || 0;
    } else if (currentStep === 8) {
      // Step 8 is optional: save only if provided
      if (form.firstProductName.trim()) {
        payload.firstProductName = form.firstProductName.trim();
        payload.firstProductPrice = Number(form.firstProductPrice) || 0;
        payload.firstProductStock = Number(form.firstProductStock) || 0;
        payload.firstProductSku = form.firstProductSku.trim();
      }
    } else if (currentStep === 9) {
      if (!form.firstStaffName.trim()) {
        setSaveError('Staff member name is required to assign treatment availability.');
        return;
      }
      payload.firstStaffName = form.firstStaffName.trim();
      payload.firstStaffTitle = form.firstStaffTitle.trim();
    } else if (currentStep === 10) {
      if (form.startTime >= form.endTime) {
        setSaveError('Shift closing time must be after shift start time.');
        return;
      }
      payload.startTime = form.startTime;
      payload.endTime = form.endTime;
      payload.workingDays = form.workingDays;
    } else if (currentStep === 11) {
      payload.holdDurationMinutes = Number(form.holdDurationMinutes) || 5;
      payload.minimumNoticeHours = Number(form.minimumNoticeHours) || 2;
      payload.bookingHorizonDays = Number(form.bookingHorizonDays) || 30;
      payload.cancellationWindowHours = Number(form.cancellationWindowHours) || 4;
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

    const next = targetNextStep !== undefined ? targetNextStep : currentStep < 13 ? currentStep + 1 : currentStep;
    setCurrentStep(next);
    setSearchParams({ step: next.toString() });
  };

  const handleBack = () => {
    setSaveError(null);
    if (currentStep > 1) {
      const prev = currentStep - 1;
      setCurrentStep(prev);
      setSearchParams({ step: prev.toString() });
    }
  };

  // Section 110: Publishing evaluates actual readiness on server and prevents premature launch
  const handlePublishStorefront = async () => {
    if (!token) return;
    setIsPublishing(true);
    setPublishError(null);

    try {
      const result = await onboardingApi.publishStorefront(token);
      setIsPublished(true);
      setPublishedStorefrontUrl(result.storefrontUrl || `/business/${form.businessSlug}`);
      setCompletedSteps((prev) => (prev.includes(13) ? prev : [...prev, 13]));
    } catch (err: any) {
      setPublishError(err.message || 'Publication rejected. Please review readiness checklist.');
    } finally {
      setIsPublishing(false);
    }
  };

  const calculatedPercentage = Math.round((completedSteps.length / 13) * 100);
  const currentStepObj = steps[currentStep - 1];

  const daysList = [
    { id: 1, label: 'Mon' },
    { id: 2, label: 'Tue' },
    { id: 3, label: 'Wed' },
    { id: 4, label: 'Thu' },
    { id: 5, label: 'Fri' },
    { id: 6, label: 'Sat' },
    { id: 0, label: 'Sun' },
  ];

  if (isLoading) {
    return (
      <div className="max-w-4xl mx-auto py-24 text-center space-y-4">
        <div className="w-12 h-12 rounded-2xl bg-surface border border-border mx-auto flex items-center justify-center text-accent">
          <RefreshCw className="w-6 h-6 animate-spin" />
        </div>
        <p className="text-sm font-mono text-txt-secondary">
          Restoring authoritative onboarding state from server...
        </p>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto space-y-8 pb-24 text-txt-primary">
      {/* Editorial Header */}
      <div className="text-center space-y-2 border-b border-border pb-6">
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-surface-secondary border border-border text-xs font-mono text-accent">
          <Sparkles className="w-3.5 h-3.5" />
          <span>SECTION 110: VERIFIED ONBOARDING PIPELINE</span>
        </div>
        <h1 className="font-heading text-3xl md:text-4xl font-extrabold tracking-tight text-txt-primary">
          Provider Setup &amp; Storefront Launch
        </h1>
        <p className="text-sm text-txt-secondary max-w-xl mx-auto">
          Server-persisted onboarding workflow. Resume anytime across devices without losing saved data.
        </p>

        {/* Dynamic Progress Indicator based on actual persisted DB entities */}
        <div className="pt-3 max-w-xs mx-auto">
          <div className="flex justify-between text-xs font-mono text-txt-secondary mb-1">
            <span>Server-Persisted Progress</span>
            <span className="text-status-success font-bold">{calculatedPercentage}%</span>
          </div>
          <div className="w-full h-2 rounded-full bg-surface-secondary overflow-hidden border border-border">
            <div
              className="h-full bg-gradient-to-r from-accent to-status-success transition-all duration-500 rounded-full"
              style={{ width: `${calculatedPercentage}%` }}
            />
          </div>
        </div>
      </div>

      {/* Responsive Horizontal Stepper Navigation */}
      <div
        role="tablist"
        aria-label="Onboarding Steps"
        className="flex gap-2 overflow-x-auto pb-2 scrollbar-thin scrollbar-thumb-border scrollbar-track-transparent bg-surface p-2.5 rounded-2xl border border-border"
      >
        {steps.map((s) => {
          const isDone = completedSteps.includes(s.number);
          const isCurrent = s.number === currentStep;

          return (
            <button
              key={s.number}
              type="button"
              role="tab"
              aria-selected={isCurrent}
              aria-label={`Step ${s.number}: ${s.title}`}
              onClick={() => handleStepClick(s.number)}
              className={`flex-shrink-0 flex items-center gap-2 px-3 py-1.5 rounded-xl text-xs font-medium transition-all ${
                isCurrent
                  ? 'bg-accent text-white shadow-md shadow-accent/20 font-bold'
                  : isDone
                  ? 'bg-surface-secondary text-status-success hover:bg-surface-elevated border border-status-success/30'
                  : 'bg-surface-secondary text-txt-secondary hover:text-txt-primary hover:bg-surface-elevated border border-border'
              }`}
            >
              <span
                className={`w-4 h-4 rounded-full flex items-center justify-center text-[10px] font-bold ${
                  isCurrent
                    ? 'bg-white text-accent'
                    : isDone
                    ? 'bg-status-success text-bg'
                    : 'bg-border text-txt-secondary'
                }`}
              >
                {isDone ? <Check className="w-2.5 h-2.5 stroke-[3]" /> : s.number}
              </span>
              <span>{s.title}</span>
              {s.number === 8 && (
                <span className="text-[9px] uppercase px-1 py-0.2 rounded bg-border text-txt-tertiary">
                  Opt
                </span>
              )}
            </button>
          );
        })}
      </div>

      {saveError && (
        <Alert variant="error" title="Step Persistence Notice">
          {saveError}
        </Alert>
      )}

      {/* Main Step Card Container */}
      <Card className="p-6 md:p-8 space-y-6 bg-surface border-border shadow-xl">
        <div className="flex items-center justify-between border-b border-border pb-4">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 rounded-2xl bg-accent/10 border border-accent/20 flex items-center justify-center text-accent">
              {React.createElement(currentStepObj.icon, { className: 'w-6 h-6' })}
            </div>
            <div>
              <span className="text-xs font-mono text-txt-tertiary">
                STEP {currentStep} OF 13 &bull; {currentStepObj.subtitle}
              </span>
              <h3 className="font-heading text-2xl font-bold text-txt-primary">
                {currentStepObj.title}
              </h3>
            </div>
          </div>
          <span className="text-xs text-status-success font-mono border border-status-success/30 bg-status-success/10 px-3 py-1 rounded-xl flex items-center gap-1.5">
            <CheckCircle2 className="w-3.5 h-3.5" />
            <span>Persisted</span>
          </span>
        </div>

        {/* Step 1: Account & Membership */}
        {currentStep === 1 && (
          <div className="space-y-4 animate-fadeIn">
            <div className="p-4 rounded-xl bg-surface-secondary border border-border text-xs text-txt-secondary space-y-2">
              <div className="flex items-center justify-between">
                <span className="font-bold text-txt-primary">Authenticated Provider Identity</span>
                <span className="px-2 py-0.5 rounded-full bg-status-success/10 text-status-success border border-status-success/20 font-mono text-[10px] font-bold">
                  {form.membershipRole} &bull; Active
                </span>
              </div>
              <p>
                Account ownership and organization membership are secured via backend authorization boundaries.
              </p>
            </div>
            <Input
              label="Account Owner Full Name"
              value={form.ownerName}
              onChange={(e) => setForm({ ...form, ownerName: e.target.value })}
              placeholder="e.g. Elena Ray"
            />
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Input
                label="Owner Email Address (Read-only)"
                type="email"
                value={form.ownerEmail}
                disabled
              />
              <Input
                label="Direct Contact Phone"
                value={form.ownerPhone}
                onChange={(e) => setForm({ ...form, ownerPhone: e.target.value })}
                placeholder="+1 555 123 4567"
              />
            </div>
          </div>
        )}

        {/* Step 2: Business Creation */}
        {currentStep === 2 && (
          <div className="space-y-4 animate-fadeIn">
            <Input
              label="Business Legal / Brand Name"
              value={form.businessName}
              onChange={(e) => handleBusinessNameChange(e.target.value)}
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
            <p className="text-xs text-txt-secondary">
              Your public storefront will be discoverable at:{' '}
              <code className="text-status-success font-mono">/business/{form.businessSlug || 'your-business'}</code>
            </p>
          </div>
        )}

        {/* Step 3: Business Type & Category */}
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
            <p className="text-xs text-txt-tertiary">
              Classification helps clients discover your offerings in marketplace search and category filtering.
            </p>
          </div>
        )}

        {/* Step 4: Business Profile */}
        {currentStep === 4 && (
          <div className="space-y-4 animate-fadeIn">
            <div>
              <div className="flex justify-between items-center mb-1">
                <label className="text-xs font-bold uppercase tracking-wider text-txt-secondary">
                  Storefront Overview &amp; Treatment Philosophy <span className="text-accent">*</span>
                </label>
                <span className="text-[11px] font-mono text-txt-tertiary">
                  {form.profileBio.length} chars
                </span>
              </div>
              <textarea
                rows={4}
                value={form.profileBio}
                onChange={(e) => setForm({ ...form, profileBio: e.target.value })}
                placeholder="Describe your studio's ambiance, hygiene protocols, and treatment philosophy..."
                className="w-full px-4 py-2.5 rounded-xl bg-surface-secondary border border-border text-txt-primary text-xs focus:outline-none focus:border-accent"
                required
              />
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Input
                label="Official Website URL"
                value={form.websiteUrl}
                onChange={(e) => setForm({ ...form, websiteUrl: e.target.value })}
                placeholder="https://example.com"
              />
              <Input
                label="Public Customer Support Phone"
                value={form.publicPhone}
                onChange={(e) => setForm({ ...form, publicPhone: e.target.value })}
                placeholder="+1 555 987 6543"
              />
            </div>
          </div>
        )}

        {/* Step 5: First Location */}
        {currentStep === 5 && (
          <div className="space-y-4 animate-fadeIn">
            <Input
              label="Location Branch Name"
              value={form.locationName}
              onChange={(e) => setForm({ ...form, locationName: e.target.value })}
              placeholder="e.g. Flagship Studio Bodakdev"
            />
            <Input
              label="Street Address / Building / Suite"
              value={form.address}
              onChange={(e) => setForm({ ...form, address: e.target.value })}
              placeholder="e.g. 402 Bodakdev Commercial Hub, SG Highway"
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
                label="State / Province"
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
            <div className="p-4 rounded-xl bg-surface-secondary border border-border text-xs text-txt-secondary">
              <strong className="text-txt-primary block mb-1">Timezone &amp; Currency Policy</strong>
              Requires a valid IANA timezone identifier. Changing timezone or currency establishes policy for new appointments without silently altering existing scheduled bookings.
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Select
                label="Location IANA Timezone"
                value={form.timeZoneId}
                onChange={(e) => setForm({ ...form, timeZoneId: e.target.value })}
                options={[
                  { value: 'Asia/Kolkata', label: 'Asia/Kolkata (IST +05:30)' },
                  { value: 'America/New_York', label: 'America/New_York (EST/EDT)' },
                  { value: 'America/Los_Angeles', label: 'America/Los_Angeles (PST/PDT)' },
                  { value: 'America/Chicago', label: 'America/Chicago (CST/CDT)' },
                  { value: 'Europe/London', label: 'Europe/London (GMT/BST)' },
                  { value: 'Europe/Paris', label: 'Europe/Paris (CET/CEST)' },
                  { value: 'Asia/Dubai', label: 'Asia/Dubai (GST +04:00)' },
                  { value: 'Asia/Singapore', label: 'Asia/Singapore (SGT +08:00)' },
                  { value: 'Australia/Sydney', label: 'Australia/Sydney (AEST/AEDT)' },
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
                  { value: 'AUD', label: 'AUD ($) Australian Dollar' },
                  { value: 'CAD', label: 'CAD ($) Canadian Dollar' },
                  { value: 'AED', label: 'AED (د.إ) UAE Dirham' },
                  { value: 'SGD', label: 'SGD ($) Singapore Dollar' },
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
            <div className="flex items-center justify-between p-3.5 rounded-xl bg-surface-secondary border border-border text-xs">
              <div>
                <span className="font-bold text-txt-primary block">Physical Retail Merchandise</span>
                <span className="text-txt-secondary">
                  Optional — You can skip this step and add retail products anytime after storefront launch.
                </span>
              </div>
              <span className="text-status-success font-mono font-bold bg-status-success/10 px-2.5 py-1 rounded-lg">
                Optional
              </span>
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
              label="Professional Title / Specialist Role"
              value={form.firstStaffTitle}
              onChange={(e) => setForm({ ...form, firstStaffTitle: e.target.value })}
              placeholder="e.g. Master Stylist & Colorist"
              required
            />
            <p className="text-xs text-txt-secondary">
              This specialist will automatically be linked to your primary bookable service.
            </p>
          </div>
        )}

        {/* Step 10: Working Hours */}
        {currentStep === 10 && (
          <div className="space-y-4 animate-fadeIn">
            <div className="p-4 rounded-xl bg-surface-secondary border border-border text-xs text-txt-secondary">
              <strong className="text-txt-primary block mb-1">Weekly Operating Schedule</strong>
              Configures standard working windows for specialist availability calculations.
            </div>

            <div>
              <label className="block text-xs font-bold uppercase tracking-wider text-txt-secondary mb-2">
                Active Operating Days
              </label>
              <div className="flex flex-wrap gap-2">
                {daysList.map((d) => {
                  const isActive = form.workingDays.includes(d.id);
                  return (
                    <button
                      key={d.id}
                      type="button"
                      onClick={() => toggleWorkingDay(d.id)}
                      className={`px-3.5 py-1.5 rounded-xl text-xs font-bold transition-all ${
                        isActive
                          ? 'bg-accent text-white shadow-md shadow-accent/20'
                          : 'bg-surface-secondary text-txt-tertiary hover:bg-surface-elevated border border-border'
                      }`}
                    >
                      {d.label}
                    </button>
                  );
                })}
              </div>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-2">
              <Input
                label="Daily Shift Start Time"
                type="time"
                value={form.startTime}
                onChange={(e) => setForm({ ...form, startTime: e.target.value })}
                required
              />
              <Input
                label="Daily Shift End Time"
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
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4">
              <Input
                label="Slot Hold (Minutes)"
                type="number"
                value={form.holdDurationMinutes}
                onChange={(e) => setForm({ ...form, holdDurationMinutes: Number(e.target.value) })}
                required
              />
              <Input
                label="Notice (Hours)"
                type="number"
                value={form.minimumNoticeHours}
                onChange={(e) => setForm({ ...form, minimumNoticeHours: Number(e.target.value) })}
                required
              />
              <Input
                label="Horizon (Days)"
                type="number"
                value={form.bookingHorizonDays}
                onChange={(e) => setForm({ ...form, bookingHorizonDays: Number(e.target.value) })}
                required
              />
              <Input
                label="Cancel Window (Hours)"
                type="number"
                value={form.cancellationWindowHours}
                onChange={(e) => setForm({ ...form, cancellationWindowHours: Number(e.target.value) })}
                required
              />
            </div>
            <p className="text-xs text-txt-secondary">
              Slot holds prevent race conditions during customer booking checkout. Horizon controls how far in advance appointments can be scheduled.
            </p>
          </div>
        )}

        {/* Step 12: Review Readiness */}
        {currentStep === 12 && (
          <div className="space-y-6 animate-fadeIn">
            <div className="flex items-center justify-between pb-3 border-b border-border">
              <div>
                <h4 className="font-heading text-lg font-bold text-txt-primary">
                  Server Publication Readiness Audit
                </h4>
                <p className="text-xs text-txt-secondary">
                  Automated verification of persisted business, service, staff, and operational parameters
                </p>
              </div>
              <button
                type="button"
                onClick={loadReadiness}
                className="px-3 py-1.5 rounded-xl bg-surface-secondary hover:bg-surface-elevated text-xs font-semibold text-txt-primary border border-border flex items-center gap-1.5 transition-colors"
              >
                <RefreshCw className="w-3.5 h-3.5 text-status-success" />
                <span>Re-evaluate</span>
              </button>
            </div>

            {readiness && (
              <div className="space-y-4">
                <div
                  className={`p-4 rounded-2xl border flex items-center justify-between ${
                    readiness.isReady
                      ? 'bg-status-success/10 border-status-success/30 text-status-success'
                      : 'bg-status-warning/10 border-status-warning/30 text-status-warning'
                  }`}
                >
                  <div className="flex items-center gap-3">
                    {readiness.isReady ? (
                      <CheckCircle2 className="w-6 h-6 text-status-success" />
                    ) : (
                      <AlertTriangle className="w-6 h-6 text-status-warning" />
                    )}
                    <div>
                      <p className="font-bold text-sm text-txt-primary">
                        {readiness.isReady
                          ? 'All Mandatory Requirements Satisfied'
                          : 'Mandatory Requirements Incomplete'}
                      </p>
                      <p className="text-xs opacity-90 text-txt-secondary">
                        {readiness.satisfiedMandatory} of {readiness.totalMandatory} mandatory requirements verified on backend database.
                      </p>
                    </div>
                  </div>
                  <span className="font-mono text-xs font-bold px-3 py-1 rounded-full bg-surface-secondary">
                    {readiness.isReady ? 'READY TO LAUNCH' : 'ACTION REQUIRED'}
                  </span>
                </div>

                <div className="divide-y divide-border bg-surface-secondary rounded-2xl border border-border overflow-hidden">
                  {readiness.requirements.map((req) => (
                    <div key={req.key} className="p-4 flex items-center justify-between gap-4">
                      <div className="flex items-start gap-3 min-w-0">
                        {req.isSatisfied ? (
                          <CheckCircle2 className="w-5 h-5 text-status-success mt-0.5 flex-shrink-0" />
                        ) : req.isMandatory ? (
                          <AlertCircle className="w-5 h-5 text-status-error mt-0.5 flex-shrink-0" />
                        ) : (
                          <Clock className="w-5 h-5 text-txt-tertiary mt-0.5 flex-shrink-0" />
                        )}
                        <div>
                          <div className="flex items-center gap-2">
                            <span className="text-xs font-bold text-txt-primary">{req.title}</span>
                            {req.isMandatory ? (
                              <span className="text-[10px] uppercase font-bold tracking-wider text-status-error bg-status-error/10 px-1.5 py-0.5 rounded">
                                Mandatory
                              </span>
                            ) : (
                              <span className="text-[10px] uppercase font-bold tracking-wider text-txt-tertiary bg-surface-elevated px-1.5 py-0.5 rounded">
                                Optional
                              </span>
                            )}
                          </div>
                          <p className="text-[11px] text-txt-secondary mt-0.5">{req.description}</p>
                        </div>
                      </div>

                      <div className="flex items-center gap-2 flex-shrink-0">
                        <span
                          className={`text-xs font-mono px-2.5 py-0.5 rounded-full ${
                            req.isSatisfied
                              ? 'bg-status-success/10 text-status-success border border-status-success/20 font-bold'
                              : 'bg-status-error/10 text-status-error border border-status-error/20'
                          }`}
                        >
                          {req.isSatisfied ? 'Verified' : 'Missing'}
                        </span>
                        {!req.isSatisfied && (
                          <button
                            type="button"
                            onClick={() => handleStepClick(req.stepNumber)}
                            className="px-2.5 py-1 rounded-lg bg-border hover:bg-accent text-white text-xs font-semibold transition-colors flex items-center gap-1"
                          >
                            <span>Fix</span>
                            <CornerDownRight className="w-3 h-3" />
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

        {/* Step 13: Publish Storefront */}
        {currentStep === 13 && (
          <div className="space-y-6 animate-fadeIn">
            {publishError && (
              <Alert variant="error" title="Publication Check Failed">
                {publishError}
              </Alert>
            )}

            {isPublished ? (
              <div className="p-8 rounded-3xl bg-surface-secondary border border-status-success/30 text-center space-y-4 shadow-xl">
                <div className="w-16 h-16 rounded-3xl bg-status-success/20 text-status-success flex items-center justify-center mx-auto shadow-lg shadow-status-success/10">
                  <Rocket className="w-8 h-8" />
                </div>
                <h3 className="font-heading text-2xl font-bold text-txt-primary">Storefront Live &amp; Published!</h3>
                <p className="text-xs text-txt-secondary max-w-md mx-auto leading-relaxed">
                  Your business profile, treatments catalogue, and real-time scheduling calendar are now discoverable across Bookline.
                </p>
                <div className="pt-2 flex flex-col sm:flex-row items-center justify-center gap-3">
                  <Link
                    to={publishedStorefrontUrl || `/business/${form.businessSlug}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="w-full sm:w-auto px-6 py-3 rounded-2xl bg-accent hover:bg-accent-hover text-white text-xs font-bold shadow-lg shadow-accent/20 flex items-center justify-center gap-2 transition-all"
                  >
                    <span>View Public Storefront</span>
                    <ExternalLink className="w-4 h-4" />
                  </Link>
                  <button
                    type="button"
                    onClick={() => navigate('/provider/dashboard')}
                    className="w-full sm:w-auto px-6 py-3 rounded-2xl bg-surface-elevated hover:bg-surface text-txt-primary text-xs font-bold border border-border transition-all"
                  >
                    Go to Provider OS
                  </button>
                </div>
              </div>
            ) : (
              <div className="space-y-6">
                <div className="bg-surface-secondary p-6 rounded-2xl border border-border space-y-4">
                  <div className="flex items-center justify-between border-b border-border pb-3">
                    <h4 className="font-heading font-bold text-txt-primary text-base">Storefront Pre-Launch Summary</h4>
                    <span className="text-xs font-mono text-status-success">Readiness Verified</span>
                  </div>
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs text-txt-secondary">
                    <p><strong className="text-txt-primary">Business:</strong> {form.businessName || 'Configured'} ({form.category})</p>
                    <p><strong className="text-txt-primary">Location:</strong> {form.address || 'Configured'}, {form.city}</p>
                    <p><strong className="text-txt-primary">Timezone &amp; Currency:</strong> {form.timeZoneId} &bull; {form.currency}</p>
                    <p><strong className="text-txt-primary">Primary Service:</strong> {form.firstServiceName || 'Configured'} ({form.currency} {form.firstServicePrice})</p>
                    <p><strong className="text-txt-primary">Lead Specialist:</strong> {form.firstStaffName || 'Configured'} ({form.firstStaffTitle})</p>
                    <p><strong className="text-txt-primary">Working Hours:</strong> {form.startTime} &ndash; {form.endTime}</p>
                    <p><strong className="text-txt-primary">Hold &amp; Notice:</strong> {form.holdDurationMinutes}m Hold &bull; {form.minimumNoticeHours}h Notice</p>
                    <p><strong className="text-txt-primary">Public Storefront:</strong> /business/{form.businessSlug}</p>
                  </div>
                </div>

                <div className="p-4 rounded-xl bg-surface-secondary border border-border text-xs text-txt-secondary space-y-1">
                  <p className="font-bold text-txt-primary">Platform Governance Distinction</p>
                  <p className="leading-relaxed">
                    Storefront publication makes your profile discoverable for appointments. Platform verification and payout authorization badges operate under independent governance checks.
                  </p>
                </div>

                <Button
                  variant="primary"
                  onClick={handlePublishStorefront}
                  isLoading={isPublishing}
                  className="w-full py-3.5 rounded-2xl bg-accent hover:bg-accent-hover text-white text-sm font-bold shadow-xl shadow-accent/20 flex items-center justify-center gap-2"
                >
                  <Rocket className="w-4 h-4" />
                  <span>Publish Storefront &amp; Go Live</span>
                </Button>
              </div>
            )}
          </div>
        )}

        {/* Action Controls */}
        <div className="flex items-center justify-between pt-6 border-t border-border">
          <Button variant="secondary" onClick={handleBack} disabled={currentStep === 1 || isSaving || isPublishing}>
            <ArrowLeft className="w-4 h-4 mr-2" /> Back
          </Button>

          <div className="flex items-center gap-3">
            {/* Optional Step 8 skip shortcut */}
            {currentStep === 8 && (
              <Button
                variant="ghost"
                onClick={() => handleSaveStep(9)}
                disabled={isSaving}
                className="text-xs text-txt-secondary hover:text-txt-primary"
              >
                Skip Products for Now
              </Button>
            )}

            {currentStep < 13 && (
              <Button variant="primary" onClick={() => handleSaveStep()} isLoading={isSaving}>
                <span>{currentStep === 12 ? 'Proceed to Launch' : 'Save & Continue'}</span>
                <ArrowRight className="w-4 h-4 ml-2" />
              </Button>
            )}
          </div>
        </div>
      </Card>
    </div>
  );
};
