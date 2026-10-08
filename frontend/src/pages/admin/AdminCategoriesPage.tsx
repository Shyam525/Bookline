import React from 'react';
import { Grid, Sparkles, Building2 } from 'lucide-react';

export const AdminCategoriesPage: React.FC = () => {
  const categories = [
    {
      name: 'Wellness',
      description: 'Holistic wellness sanctuaries, Ayurvedic retreats, and therapeutic spas.',
      providerCount: 14,
      popularServices: 'Aromatherapy, Deep Tissue, Hot Stone Massage',
    },
    {
      name: 'Beauty',
      description: 'High-end hair salons, brow & lash studios, bespoke nail artists.',
      providerCount: 22,
      popularServices: 'Balayage, Keratin Rituals, Japanese Manicure',
    },
    {
      name: 'Healthcare',
      description: 'Cosmetic dentistry clinics, dermatologists, wellness physiotherapists.',
      providerCount: 11,
      popularServices: 'Teeth Whitening, Dental Consultation, Laser Skin Resurfacing',
    },
    {
      name: 'Fitness',
      description: 'Reformer Pilates studios, functional athletics, boutique strength coaching.',
      providerCount: 9,
      popularServices: 'Reformer Pilates, Personal Athletic Training, Mobility Sessions',
    },
    {
      name: 'Photography',
      description: 'Editorial portrait photographers, family sessions, product studio shoots.',
      providerCount: 7,
      popularServices: 'Studio Headshots, Commercial Product Shoot, Event Coverage',
    },
    {
      name: 'Professional',
      description: 'Legal advisors, notary practitioners, tax & financial consultants.',
      providerCount: 5,
      popularServices: 'Legal Consultation, Financial Strategy Session, Contract Review',
    },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="font-heading text-2xl font-bold text-white">Marketplace Categories</h1>
        <p className="text-xs text-[#7E88A8]">
          Manage consumer taxonomy, category landing pages, and discovery ranking rules
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {categories.map((cat) => (
          <div
            key={cat.name}
            className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-3 flex flex-col justify-between"
          >
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <span className="font-heading font-bold text-lg text-white">{cat.name}</span>
                <span className="px-2.5 py-0.5 rounded-lg bg-[#181D2C] border border-[#212638] text-xs font-mono font-bold text-[#FBBF24]">
                  {cat.providerCount} Providers
                </span>
              </div>
              <p className="text-xs text-[#7E88A8] leading-relaxed">{cat.description}</p>
            </div>

            <div className="pt-3 border-t border-[#212638] text-[11px] text-[#7E88A8]">
              <span className="font-semibold text-white block mb-0.5">Popular Menu Items:</span>
              <span>{cat.popularServices}</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
