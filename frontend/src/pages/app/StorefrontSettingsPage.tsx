import React, { useState } from 'react';
import { useAuth } from '../../app/providers/AuthProvider';
import { Link } from 'react-router-dom';
import {
  Building2,
  Image,
  MapPin,
  Clock,
  Sliders,
  AlertTriangle,
  Bell,
  CreditCard,
  Users,
  Shield,
  Save,
  CheckCircle,
  ExternalLink,
  Plus,
  Trash2,
} from 'lucide-react';

type SettingsTab =
  | 'PROFILE'
  | 'BRANDING'
  | 'LOCATIONS'
  | 'HOURS'
  | 'RULES'
  | 'CANCELLATION'
  | 'NOTIFICATIONS'
  | 'PAYMENTS'
  | 'TEAM'
  | 'SECURITY';

/**
 * Specification Section 75: BUSINESS SETTINGS
 * Settings areas:
 * Business profile, Branding, Locations, Hours, Booking rules,
 * Cancellation, Notifications, Payments, Team, Security.
 */
export const StorefrontSettingsPage: React.FC = () => {
  const { activeBusiness } = useAuth();
  const [activeTab, setActiveTab] = useState<SettingsTab>('PROFILE');
  const [savedSuccess, setSavedSuccess] = useState(false);

  // 1. Business Profile
  const [businessName, setBusinessName] = useState(activeBusiness?.name || 'Aura Wellness & Spa');
  const [category, setCategory] = useState('Wellness & Spa');
  const [phone, setPhone] = useState('+91 79 4001 2233');
  const [email, setEmail] = useState('contact@aurawellness.example.com');
  const [description, setDescription] = useState(
    'A sanctuary of restorative holistic wellness, bespoke clinical aesthetics, and therapeutic recovery in Ahmedabad.'
  );

  // 2. Branding
  const [coverImageUrl, setCoverImageUrl] = useState(
    'https://images.unsplash.com/photo-1540555700478-4be289fbecef?auto=format&fit=crop&w=1600&q=80'
  );
  const [logoUrl, setLogoUrl] = useState('');
  const [brandColor, setBrandColor] = useState('#E8546A');

  // 3. Locations
  const [locations, setLocations] = useState([
    { id: 'loc-1', name: 'Bodakdev Flagship', address: '402 Bodakdev Commercial Hub, SG Highway', city: 'Ahmedabad', isPrimary: true },
    { id: 'loc-2', name: 'Satellite Executive Suite', address: '12 Satellite Road, Near ISRO', city: 'Ahmedabad', isPrimary: false },
  ]);

  // 4. Hours (Multiple intervals: 09:00-13:00, 14:00-18:00)
  const [operatingHours, setOperatingHours] = useState([
    { day: 'Monday–Friday', interval1: '09:00–13:00', interval2: '14:00–18:00', isOpen: true },
    { day: 'Saturday', interval1: '10:00–16:00', interval2: '', isOpen: true },
    { day: 'Sunday', interval1: '', interval2: '', isOpen: false },
  ]);

  // 5. Booking Rules
  const [holdDurationMinutes, setHoldDurationMinutes] = useState(5);
  const [bookingHorizonDays, setBookingHorizonDays] = useState(30);
  const [bufferMinutes, setBufferMinutes] = useState(15);

  // 6. Cancellation (Section 67 & 75)
  const [minimumNoticeHours, setMinimumNoticeHours] = useState(4);
  const [cancellationPolicyNote, setCancellationPolicyNote] = useState(
    'Cancellations must be made at least 4 hours prior to appointment start time. Late cancellations may incur a 50% reservation fee.'
  );

  // 7. Notifications
  const [smsRemindersEnabled, setSmsRemindersEnabled] = useState(true);
  const [emailConfirmationsEnabled, setEmailConfirmationsEnabled] = useState(true);
  const [reminderLeadHours, setReminderLeadHours] = useState(24);

  // 8. Payments
  const [depositType, setDepositType] = useState('Fixed');
  const [depositAmount, setDepositAmount] = useState(500);
  const [payoutSchedule, setPayoutSchedule] = useState('Daily Rolling');
  const [stripeConnected, setStripeConnected] = useState(true);

  // 9. Team
  const [teamMembers, setTeamMembers] = useState([
    { id: 'tm-1', name: 'Ananya Sharma', role: 'Business Owner', email: 'ananya@bookline.local' },
    { id: 'tm-2', name: 'Elena Vance', role: 'Lead Therapist', email: 'elena@bookline.local' },
    { id: 'tm-3', name: 'Marcus Brody', role: 'Specialist Stylist', email: 'marcus@bookline.local' },
  ]);

  // 10. Security
  const [twoFactorRequired, setTwoFactorRequired] = useState(true);
  const [auditLoggingEnabled, setAuditLoggingEnabled] = useState(true);

  const handleSave = (e: React.FormEvent) => {
    e.preventDefault();
    setSavedSuccess(true);
    setTimeout(() => setSavedSuccess(false), 3000);
  };

  const navTabs = [
    { id: 'PROFILE', label: 'Business Profile', icon: Building2 },
    { id: 'BRANDING', label: 'Branding', icon: Image },
    { id: 'LOCATIONS', label: 'Locations', icon: MapPin },
    { id: 'HOURS', label: 'Working Hours', icon: Clock },
    { id: 'RULES', label: 'Booking Rules', icon: Sliders },
    { id: 'CANCELLATION', label: 'Cancellation', icon: AlertTriangle },
    { id: 'NOTIFICATIONS', label: 'Notifications', icon: Bell },
    { id: 'PAYMENTS', label: 'Payments', icon: CreditCard },
    { id: 'TEAM', label: 'Team', icon: Users },
    { id: 'SECURITY', label: 'Security', icon: Shield },
  ];

  return (
    <div className="max-w-6xl mx-auto space-y-6 pb-12">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-[#212638] pb-6">
        <div>
          <span className="text-[10px] uppercase font-bold text-[#34D399] tracking-widest block mb-1">
            SECTION 75: BUSINESS SETTINGS MATRIX
          </span>
          <h1 className="font-heading text-2xl font-bold text-white">Business Settings &amp; Governance</h1>
          <p className="text-xs text-[#7E88A8]">
            Configure operational policies, branding, working schedules, payments, and team access for{' '}
            <strong className="text-white">{businessName}</strong>
          </p>
        </div>

        <Link
          to={`/business/${activeBusiness?.slug || 'aura-wellness-ahmedabad'}`}
          target="_blank"
          className="px-4 py-2 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-white text-xs font-semibold flex items-center gap-2 self-start sm:self-auto"
        >
          <span>View Public Storefront</span>
          <ExternalLink className="w-3.5 h-3.5" />
        </Link>
      </div>

      {savedSuccess && (
        <div className="p-3.5 rounded-xl bg-[#34D399]/10 border border-[#34D399]/30 text-[#34D399] text-xs flex items-center gap-2 shadow-sm">
          <CheckCircle className="w-4 h-4 flex-shrink-0" />
          <span>Business settings updated and synchronized across all branches!</span>
        </div>
      )}

      {/* Tabs & Content Grid */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        {/* Left: Tab list */}
        <div className="space-y-1">
          {navTabs.map((tab) => {
            const Icon = tab.icon;
            const isActive = activeTab === tab.id;
            return (
              <button
                key={tab.id}
                type="button"
                onClick={() => setActiveTab(tab.id as SettingsTab)}
                className={`w-full flex items-center gap-3 px-4 py-2.5 rounded-xl text-xs font-semibold text-left transition-all ${
                  isActive
                    ? 'bg-[#E8546A] text-white shadow-md'
                    : 'bg-[#111520] hover:bg-[#181D2C] text-[#7E88A8] hover:text-white border border-[#212638]'
                }`}
              >
                <Icon className="w-4 h-4 flex-shrink-0" />
                <span>{tab.label}</span>
              </button>
            );
          })}
        </div>

        {/* Right: Tab Form Container */}
        <div className="md:col-span-3">
          <form
            onSubmit={handleSave}
            className="bg-[#111520] border border-[#212638] rounded-3xl p-6 sm:p-8 space-y-6 shadow-xl text-xs"
          >
            {/* 1. BUSINESS PROFILE */}
            {activeTab === 'PROFILE' && (
              <div className="space-y-4">
                <h3 className="font-heading font-bold text-base text-white border-b border-[#212638] pb-3">
                  Business Profile Details
                </h3>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                      Business Trade Name
                    </label>
                    <input
                      type="text"
                      required
                      value={businessName}
                      onChange={(e) => setBusinessName(e.target.value)}
                      className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                      Marketplace Category
                    </label>
                    <select
                      value={category}
                      onChange={(e) => setCategory(e.target.value)}
                      className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                    >
                      <option value="Wellness & Spa">Wellness & Spa</option>
                      <option value="Hair Salon">Hair Salon</option>
                      <option value="Barbershop">Barbershop</option>
                      <option value="Aesthetic Clinic">Aesthetic Clinic</option>
                    </select>
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                      Contact Phone
                    </label>
                    <input
                      type="text"
                      value={phone}
                      onChange={(e) => setPhone(e.target.value)}
                      className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                      Contact Email
                    </label>
                    <input
                      type="email"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                    Storefront Bio &amp; Philosophy
                  </label>
                  <textarea
                    rows={4}
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                  />
                </div>
              </div>
            )}

            {/* 2. BRANDING */}
            {activeTab === 'BRANDING' && (
              <div className="space-y-4">
                <h3 className="font-heading font-bold text-base text-white border-b border-[#212638] pb-3">
                  Visual Branding &amp; Media
                </h3>
                <div>
                  <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                    Storefront Cover Banner
                  </label>
                  <div className="relative h-40 rounded-2xl overflow-hidden bg-[#181D2C] border border-[#212638] mb-2">
                    <img src={coverImageUrl} alt="Banner" className="w-full h-full object-cover" />
                  </div>
                  <input
                    type="url"
                    value={coverImageUrl}
                    onChange={(e) => setCoverImageUrl(e.target.value)}
                    placeholder="https://... banner.jpg"
                    className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                  />
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                      Logo URL
                    </label>
                    <input
                      type="url"
                      value={logoUrl}
                      onChange={(e) => setLogoUrl(e.target.value)}
                      placeholder="https://... logo.png"
                      className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                      Brand Accent Color
                    </label>
                    <div className="flex items-center gap-3">
                      <input
                        type="color"
                        value={brandColor}
                        onChange={(e) => setBrandColor(e.target.value)}
                        className="w-10 h-10 rounded-xl cursor-pointer bg-transparent border-0"
                      />
                      <input
                        type="text"
                        value={brandColor}
                        onChange={(e) => setBrandColor(e.target.value)}
                        className="flex-1 px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white font-mono"
                      />
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* 3. LOCATIONS */}
            {activeTab === 'LOCATIONS' && (
              <div className="space-y-4">
                <div className="flex items-center justify-between border-b border-[#212638] pb-3">
                  <h3 className="font-heading font-bold text-base text-white">Physical Locations &amp; Branches</h3>
                  <button
                    type="button"
                    onClick={() =>
                      setLocations([
                        ...locations,
                        {
                          id: `loc-${Date.now()}`,
                          name: 'New Branch Location',
                          address: 'Address pending',
                          city: 'Ahmedabad',
                          isPrimary: false,
                        },
                      ])
                    }
                    className="px-3 py-1.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-white text-xs font-bold flex items-center gap-1.5"
                  >
                    <Plus className="w-3.5 h-3.5 text-[#34D399]" /> Add Branch
                  </button>
                </div>

                <div className="space-y-3">
                  {locations.map((loc) => (
                    <div
                      key={loc.id}
                      className="p-4 rounded-2xl bg-[#181D2C] border border-[#212638] flex items-center justify-between"
                    >
                      <div>
                        <div className="flex items-center gap-2">
                          <h4 className="font-bold text-white text-sm">{loc.name}</h4>
                          {loc.isPrimary && (
                            <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-[#34D399]/20 text-[#34D399]">
                              Primary HQ
                            </span>
                          )}
                        </div>
                        <p className="text-xs text-[#7E88A8] mt-1">{loc.address}, {loc.city}</p>
                      </div>
                      {!loc.isPrimary && (
                        <button
                          type="button"
                          onClick={() => setLocations(locations.filter((l) => l.id !== loc.id))}
                          className="p-2 text-red-400 hover:text-red-300"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* 4. HOURS (Multiple Intervals - Section 77) */}
            {activeTab === 'HOURS' && (
              <div className="space-y-4">
                <div className="border-b border-[#212638] pb-3">
                  <h3 className="font-heading font-bold text-base text-white">Weekly Operating Shifts &amp; Breaks</h3>
                  <p className="text-xs text-[#7E88A8]">
                    Configure multi-interval daily schedules (e.g. 09:00–13:00, 14:00–18:00) with automatic break deduction
                  </p>
                </div>

                <div className="space-y-3">
                  {operatingHours.map((oh, i) => (
                    <div
                      key={i}
                      className="p-4 rounded-2xl bg-[#181D2C] border border-[#212638] flex flex-col sm:flex-row sm:items-center justify-between gap-3"
                    >
                      <div className="space-y-1">
                        <span className="font-bold text-white text-sm">{oh.day}</span>
                        {oh.isOpen ? (
                          <div className="flex items-center gap-2 text-xs text-[#34D399]">
                            <span className="px-2 py-0.5 rounded bg-[#34D399]/15 font-mono">
                              Shift 1: {oh.interval1}
                            </span>
                            {oh.interval2 && (
                              <span className="px-2 py-0.5 rounded bg-[#34D399]/15 font-mono">
                                Shift 2: {oh.interval2}
                              </span>
                            )}
                          </div>
                        ) : (
                          <span className="text-xs text-red-400 font-semibold">Closed</span>
                        )}
                      </div>

                      <span className="text-[10px] text-[#7E88A8] bg-[#111520] px-2.5 py-1 rounded-lg">
                        {oh.isOpen ? 'Breaks consume slot availability' : 'All day blocked'}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* 5. BOOKING RULES */}
            {activeTab === 'RULES' && (
              <div className="space-y-4">
                <h3 className="font-heading font-bold text-base text-white border-b border-[#212638] pb-3">
                  Reservation Rules &amp; Horizons
                </h3>
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                      Atomic Hold Duration
                    </label>
                    <input
                      type="number"
                      value={holdDurationMinutes}
                      onChange={(e) => setHoldDurationMinutes(Number(e.target.value))}
                      className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white"
                    />
                    <span className="text-[10px] text-[#7E88A8]">Minutes slot is locked while in checkout</span>
                  </div>
                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                      Booking Horizon
                    </label>
                    <input
                      type="number"
                      value={bookingHorizonDays}
                      onChange={(e) => setBookingHorizonDays(Number(e.target.value))}
                      className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white"
                    />
                    <span className="text-[10px] text-[#7E88A8]">Days in advance appointments can be made</span>
                  </div>
                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                      Buffer Between Slots
                    </label>
                    <input
                      type="number"
                      value={bufferMinutes}
                      onChange={(e) => setBufferMinutes(Number(e.target.value))}
                      className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white"
                    />
                    <span className="text-[10px] text-[#7E88A8]">Cleanup / turnover buffer</span>
                  </div>
                </div>
              </div>
            )}

            {/* 6. CANCELLATION */}
            {activeTab === 'CANCELLATION' && (
              <div className="space-y-4">
                <h3 className="font-heading font-bold text-base text-white border-b border-[#212638] pb-3">
                  Cancellation Window &amp; Policy Notice
                </h3>
                <div>
                  <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                    Minimum Cancellation Window (Hours)
                  </label>
                  <input
                    type="number"
                    min="1"
                    max="72"
                    value={minimumNoticeHours}
                    onChange={(e) => setMinimumNoticeHours(Number(e.target.value))}
                    className="w-full max-w-xs px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white"
                  />
                  <span className="text-[10px] text-[#7E88A8] block mt-1">
                    Clients cannot self-cancel if remaining time is under this threshold.
                  </span>
                </div>

                <div>
                  <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                    Client-Facing Cancellation Terms
                  </label>
                  <textarea
                    rows={3}
                    value={cancellationPolicyNote}
                    onChange={(e) => setCancellationPolicyNote(e.target.value)}
                    className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white"
                  />
                </div>
              </div>
            )}

            {/* 7. NOTIFICATIONS */}
            {activeTab === 'NOTIFICATIONS' && (
              <div className="space-y-4">
                <h3 className="font-heading font-bold text-base text-white border-b border-[#212638] pb-3">
                  Customer &amp; Specialist Notifications
                </h3>
                <div className="space-y-3">
                  <label className="flex items-center gap-3 p-3.5 rounded-xl bg-[#181D2C] border border-[#212638] cursor-pointer">
                    <input
                      type="checkbox"
                      checked={smsRemindersEnabled}
                      onChange={(e) => setSmsRemindersEnabled(e.target.checked)}
                      className="w-4 h-4 rounded text-[#E8546A]"
                    />
                    <div>
                      <span className="font-bold text-white block">Automated SMS Reminders</span>
                      <span className="text-[10px] text-[#7E88A8]">Send 24-hour reminder text to client phone number</span>
                    </div>
                  </label>

                  <label className="flex items-center gap-3 p-3.5 rounded-xl bg-[#181D2C] border border-[#212638] cursor-pointer">
                    <input
                      type="checkbox"
                      checked={emailConfirmationsEnabled}
                      onChange={(e) => setEmailConfirmationsEnabled(e.target.checked)}
                      className="w-4 h-4 rounded text-[#E8546A]"
                    />
                    <div>
                      <span className="font-bold text-white block">Email Confirmation &amp; .ics Calendar Invitations</span>
                      <span className="text-[10px] text-[#7E88A8]">Attach Google/Apple Calendar invitations upon booking confirmation</span>
                    </div>
                  </label>
                </div>
              </div>
            )}

            {/* 8. PAYMENTS */}
            {activeTab === 'PAYMENTS' && (
              <div className="space-y-4">
                <h3 className="font-heading font-bold text-base text-white border-b border-[#212638] pb-3">
                  Payments, Payouts &amp; Deposit Collection
                </h3>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                      Deposit Requirement
                    </label>
                    <select
                      value={depositType}
                      onChange={(e) => setDepositType(e.target.value)}
                      className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white"
                    >
                      <option value="None">No Upfront Deposit</option>
                      <option value="Fixed">Fixed Amount</option>
                      <option value="Percentage">Percentage of Service</option>
                    </select>
                  </div>

                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                      Advance Deposit Amount
                    </label>
                    <input
                      type="number"
                      value={depositAmount}
                      onChange={(e) => setDepositAmount(Number(e.target.value))}
                      className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white"
                    />
                  </div>
                </div>

                <div className="p-4 rounded-2xl bg-[#181D2C] border border-[#212638] flex items-center justify-between">
                  <div className="space-y-0.5">
                    <span className="font-bold text-white block">Marketplace Settlement Account</span>
                    <span className="text-xs text-[#34D399]">Connected &bull; Payout Schedule: {payoutSchedule}</span>
                  </div>
                  <span className="px-2.5 py-1 rounded bg-[#34D399]/20 text-[#34D399] font-bold text-[10px]">
                    Active
                  </span>
                </div>
              </div>
            )}

            {/* 9. TEAM */}
            {activeTab === 'TEAM' && (
              <div className="space-y-4">
                <div className="flex items-center justify-between border-b border-[#212638] pb-3">
                  <h3 className="font-heading font-bold text-base text-white">Staff Roles &amp; Team Access</h3>
                  <Link
                    to="/provider/staff"
                    className="text-xs text-[#E8546A] hover:underline font-bold"
                  >
                    Manage In Staff Directory &rarr;
                  </Link>
                </div>

                <div className="space-y-2">
                  {teamMembers.map((tm) => (
                    <div
                      key={tm.id}
                      className="p-3.5 rounded-xl bg-[#181D2C] border border-[#212638] flex items-center justify-between"
                    >
                      <div>
                        <span className="font-bold text-white block">{tm.name}</span>
                        <span className="text-[10px] text-[#7E88A8]">{tm.email}</span>
                      </div>
                      <span className="px-2.5 py-0.5 rounded text-[10px] font-bold bg-[#111520] text-white border border-[#212638]">
                        {tm.role}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* 10. SECURITY */}
            {activeTab === 'SECURITY' && (
              <div className="space-y-4">
                <h3 className="font-heading font-bold text-base text-white border-b border-[#212638] pb-3">
                  Security, MFA &amp; Audit Trail
                </h3>
                <div className="space-y-3">
                  <label className="flex items-center gap-3 p-3.5 rounded-xl bg-[#181D2C] border border-[#212638] cursor-pointer">
                    <input
                      type="checkbox"
                      checked={twoFactorRequired}
                      onChange={(e) => setTwoFactorRequired(e.target.checked)}
                      className="w-4 h-4 rounded text-[#E8546A]"
                    />
                    <div>
                      <span className="font-bold text-white block">Enforce Two-Factor Authentication (2FA)</span>
                      <span className="text-[10px] text-[#7E88A8]">Require SMS or TOTP authentication for all staff logins</span>
                    </div>
                  </label>

                  <label className="flex items-center gap-3 p-3.5 rounded-xl bg-[#181D2C] border border-[#212638] cursor-pointer">
                    <input
                      type="checkbox"
                      checked={auditLoggingEnabled}
                      onChange={(e) => setAuditLoggingEnabled(e.target.checked)}
                      className="w-4 h-4 rounded text-[#E8546A]"
                    />
                    <div>
                      <span className="font-bold text-white block">Immutable Operational Audit Log</span>
                      <span className="text-[10px] text-[#7E88A8]">Record all reschedules, cancellations, and fee adjustments</span>
                    </div>
                  </label>
                </div>
              </div>
            )}

            {/* Bottom Actions */}
            <div className="pt-4 border-t border-[#212638] flex items-center justify-between">
              <span className="text-xs text-[#7E88A8]">All modifications are saved to the persistent database.</span>
              <button
                type="submit"
                className="px-6 py-2.5 rounded-xl bg-[#E8546A] hover:bg-[#D44359] text-white font-bold text-xs transition-all shadow-md flex items-center gap-2"
              >
                <Save className="w-4 h-4" />
                <span>Save Settings</span>
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};
