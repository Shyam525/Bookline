import React, { useState } from 'react';
import { useAuth } from '../../app/providers/AuthProvider';
import { Link } from 'react-router-dom';
import {
  Store,
  ExternalLink,
  Save,
  CheckCircle,
  Image,
  Clock,
  ShieldCheck,
  MapPin,
} from 'lucide-react';

export const StorefrontSettingsPage: React.FC = () => {
  const { activeBusiness } = useAuth();

  const [businessName, setBusinessName] = useState(activeBusiness?.name || 'Aura Wellness & Spa');
  const [description, setDescription] = useState(
    'A sanctuary of restorative holistic wellness, bespoke clinical aesthetics, and therapeutic recovery in Ahmedabad.'
  );
  const [coverImageUrl, setCoverImageUrl] = useState(
    'https://images.unsplash.com/photo-1540555700478-4be289fbecef?auto=format&fit=crop&w=1600&q=80'
  );
  const [logoUrl, setLogoUrl] = useState('');
  const [minimumNoticeHours, setMinimumNoticeHours] = useState(4);
  const [depositType, setDepositType] = useState('Fixed');
  const [depositAmount, setDepositAmount] = useState(500);
  const [phone, setPhone] = useState('+91 79 4001 2233');
  const [website, setWebsite] = useState('https://aurawellness.example.com');
  const [savedSuccess, setSavedSuccess] = useState(false);

  const handleSave = (e: React.FormEvent) => {
    e.preventDefault();
    setSavedSuccess(true);
    setTimeout(() => setSavedSuccess(false), 3000);
  };

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="font-heading text-2xl font-bold text-white">Public Storefront Profile</h1>
          <p className="text-xs text-[#7E88A8]">
            Configure what clients see on the global Bookline marketplace for{' '}
            <strong className="text-white">{businessName}</strong>
          </p>
        </div>

        <Link
          to={`/business/${activeBusiness?.slug || 'aura-wellness-ahmedabad'}`}
          target="_blank"
          className="px-4 py-2 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-white text-xs font-semibold transition-all flex items-center gap-2 self-start sm:self-auto"
        >
          <span>View Public Storefront</span>
          <ExternalLink className="w-3.5 h-3.5" />
        </Link>
      </div>

      {savedSuccess && (
        <div className="p-3 rounded-xl bg-[#34D399]/10 border border-[#34D399]/30 text-[#34D399] text-xs flex items-center gap-2">
          <CheckCircle className="w-4 h-4" />
          <span>Storefront branding and booking rules published successfully!</span>
        </div>
      )}

      {/* Form */}
      <form onSubmit={handleSave} className="bg-[#111520] border border-[#212638] rounded-3xl p-6 sm:p-8 space-y-6 shadow-xl text-xs">
        {/* Cover Preview */}
        <div className="space-y-2">
          <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8]">
            Storefront Cover Banner
          </label>
          <div className="relative h-44 rounded-2xl overflow-hidden bg-[#181D2C] border border-[#212638]">
            {coverImageUrl ? (
              <img src={coverImageUrl} alt="Cover Preview" className="w-full h-full object-cover" />
            ) : (
              <div className="flex items-center justify-center h-full text-[#7E88A8]">
                <Image className="w-8 h-8" />
              </div>
            )}
          </div>
          <input
            type="url"
            value={coverImageUrl}
            onChange={(e) => setCoverImageUrl(e.target.value)}
            placeholder="Banner image URL..."
            className="w-full px-4 py-2 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
          />
        </div>

        {/* Business Identity */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
              Public Business Name
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
              Storefront Logo URL
            </label>
            <input
              type="url"
              value={logoUrl}
              onChange={(e) => setLogoUrl(e.target.value)}
              placeholder="https://... logo.png"
              className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
            />
          </div>
        </div>

        <div>
          <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
            Storefront Bio & Treatment Philosophy
          </label>
          <textarea
            rows={4}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
          />
        </div>

        {/* Policies Section */}
        <div className="pt-4 border-t border-[#212638] space-y-4">
          <h3 className="font-heading font-bold text-sm text-white">Booking Policies & Protection</h3>
          
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div>
              <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                Minimum Cancellation Notice
              </label>
              <div className="flex items-center gap-2">
                <input
                  type="number"
                  min="0"
                  max="72"
                  value={minimumNoticeHours}
                  onChange={(e) => setMinimumNoticeHours(Number(e.target.value))}
                  className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                />
                <span className="text-[#7E88A8]">Hours</span>
              </div>
            </div>

            <div>
              <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                Deposit Policy
              </label>
              <select
                value={depositType}
                onChange={(e) => setDepositType(e.target.value)}
                className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
              >
                <option value="None">No Deposit</option>
                <option value="Fixed">Fixed Amount</option>
                <option value="Percentage">Percentage</option>
              </select>
            </div>

            <div>
              <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                Advance Deposit Amount
              </label>
              <input
                type="number"
                min="0"
                value={depositAmount}
                onChange={(e) => setDepositAmount(Number(e.target.value))}
                className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
              />
            </div>
          </div>
        </div>

        {/* Contact Details */}
        <div className="pt-4 border-t border-[#212638] grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
              Storefront Contact Phone
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
              Website URL
            </label>
            <input
              type="url"
              value={website}
              onChange={(e) => setWebsite(e.target.value)}
              className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
            />
          </div>
        </div>

        <div className="pt-4 border-t border-[#212638] flex justify-end">
          <button
            type="submit"
            className="px-6 py-3 rounded-xl bg-[#E8546A] hover:bg-[#D44359] text-white font-bold text-xs transition-all shadow-lg flex items-center gap-2"
          >
            <Save className="w-4 h-4" />
            <span>Publish Storefront Changes</span>
          </button>
        </div>
      </form>
    </div>
  );
};
