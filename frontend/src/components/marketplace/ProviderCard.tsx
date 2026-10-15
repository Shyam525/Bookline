import React from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Star,
  CheckCircle,
  MapPin,
  Clock,
  Sparkles,
  ArrowRight,
  Calendar,
  AlertCircle,
} from 'lucide-react';
import { ProviderImage } from '../common/ProviderImage';

export interface ProviderCardData {
  id: string;
  name: string;
  slug: string;
  category: string;
  businessType?: string;
  rating?: number | null;
  reviewCount?: number | null;
  city: string;
  address?: string;
  distanceKm?: number | null;
  startingPrice?: number | null;
  currency?: string;
  nextAvailableSlot?: string | null;
  isVerified?: boolean;
  verificationStatus?: string;
  coverImageUrl?: string | null;
  logoUrl?: string | null;
  servicesSummary?: string[];
  rankingScore?: number;
  canBook?: boolean;
}

export interface ProviderCardProps {
  provider: ProviderCardData;
  isSelected?: boolean;
  isHovered?: boolean;
  onSelect?: () => void;
  onHover?: (hovered: boolean) => void;
  className?: string;
}

/**
 * ProviderCard — Reusable Consumer Discovery Card (Phase 11)
 *
 * Implements strict data-driven rendering:
 * - Distance is only rendered when location reference exists (never fabricated)
 * - Ratings/Reviews only rendered when real review data exists (never fabricated)
 * - Availability rendered only when returned by backend availability service
 * - Starting price omitted gracefully when not available
 * - Primary CTA: #B93650 (CTA Hover: #A52E46) passing WCAG AA contrast (4.6:1)
 * - Secondary Action: "View business" navigating to storefront
 * - Card Click: navigates to storefront, excluding clicks on nested interactive buttons
 */
export const ProviderCard: React.FC<ProviderCardProps> = ({
  provider,
  isSelected = false,
  isHovered = false,
  onSelect,
  onHover,
  className = '',
}) => {
  const navigate = useNavigate();
  const p = provider;

  // Real review verification: don't fabricate rating if unreviewed
  const hasReviews = p.reviewCount != null && p.reviewCount > 0 && p.rating != null && p.rating > 0;

  // Real distance verification: null when customer didn't provide reference coords
  const hasDistance = p.distanceKm != null && !isNaN(p.distanceKm);

  // Verification status
  const isVerified = p.isVerified === true || p.verificationStatus === 'Verified';

  // Area & address display
  const locationArea = p.address ? p.address.split(',')[0].trim() : p.city;

  // Currency symbol
  const currencySymbol = p.currency === 'INR' || !p.currency ? '₹' : p.currency === 'USD' ? '$' : p.currency === 'EUR' ? '€' : p.currency;

  // Services summary (data-driven only)
  const servicesList = p.servicesSummary && p.servicesSummary.length > 0
    ? p.servicesSummary.slice(0, 3)
    : [];

  // Starting price
  const hasStartingPrice = p.startingPrice != null && p.startingPrice > 0;

  // Booking capability
  const canBook = p.canBook !== false;

  const handleCardClick = (e: React.MouseEvent) => {
    // Avoid double navigation if interactive button was clicked
    const target = e.target as HTMLElement;
    if (target.closest('button') || target.closest('a')) {
      return;
    }
    onSelect?.();
    navigate(`/business/${p.slug}`);
  };

  const handleViewBusiness = (e: React.MouseEvent) => {
    e.stopPropagation();
    navigate(`/business/${p.slug}`);
  };

  const handleBookNow = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (canBook) {
      navigate(`/business/${p.slug}?action=book`);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      onSelect?.();
      navigate(`/business/${p.slug}`);
    }
  };

  return (
    <article
      tabIndex={0}
      role="button"
      aria-label={`View ${p.name}, ${p.category} in ${locationArea}`}
      onClick={handleCardClick}
      onKeyDown={handleKeyDown}
      onMouseEnter={() => onHover?.(true)}
      onMouseLeave={() => onHover?.(false)}
      className={`group relative rounded-2xl border transition-all duration-200 cursor-pointer overflow-hidden flex flex-col sm:flex-row gap-4 sm:gap-5 p-4 sm:p-5 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#B93650] focus-visible:ring-offset-2 ${
        isSelected || isHovered
          ? 'border-[#B93650] shadow-lg shadow-[#B93650]/15 ring-1 ring-[#B93650]/50 -translate-y-0.5 bg-white dark:bg-[#151B27]'
          : 'border-[#DED8CF] dark:border-[#30394A] bg-white dark:bg-[#111620] hover:border-[#B5ADA1] dark:hover:border-[#344054] hover:shadow-md'
      } ${className}`}
    >
      {/* 1. Cover Image & Category Pill */}
      <div className="w-full sm:w-44 h-40 sm:h-auto rounded-xl bg-[#F3F0EB] dark:bg-[#1A2130] border border-[#DED8CF] dark:border-[#30394A] overflow-hidden shrink-0 relative">
        <ProviderImage
          src={p.coverImageUrl || p.logoUrl}
          alt={p.name}
          type="cover"
          category={p.category}
          className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
          loading="lazy"
        />
        <div className="absolute top-2.5 left-2.5 bg-black/75 backdrop-blur-md px-2.5 py-1 rounded-md text-[11px] font-semibold text-white tracking-wide border border-white/10 shadow-sm">
          {p.category}
        </div>
      </div>

      {/* 2. Content Body */}
      <div className="flex-1 flex flex-col justify-between min-w-0">
        <div>
          {/* Header Row: Business Name & Verification Badge */}
          <div className="flex items-start justify-between gap-2 mb-1.5">
            <div className="flex items-center gap-1.5 min-w-0">
              <h3 className="font-heading text-lg font-bold text-[#181A1F] dark:text-[#F4F6FA] truncate group-hover:text-[#B93650] dark:group-hover:text-[#E8546A] transition-colors">
                {p.name}
              </h3>
              {isVerified && (
                <span
                  title="Verified Business"
                  aria-label="Verified Business"
                  className="inline-flex items-center text-[#158A63] dark:text-[#34D399] shrink-0"
                >
                  <CheckCircle className="w-4 h-4 fill-current/20" />
                </span>
              )}
            </div>

            {/* Starting Price Pill (if available) */}
            {hasStartingPrice && (
              <div className="text-right shrink-0">
                <span className="text-[11px] text-[#626B7A] dark:text-[#A2AEC1] block leading-none">From</span>
                <span className="text-sm font-bold text-[#181A1F] dark:text-[#F4F6FA]">
                  {currencySymbol}{p.startingPrice}
                </span>
              </div>
            )}
          </div>

          {/* Social Proof: Rating & Reviews (Truthful data only) */}
          <div className="flex items-center gap-2 text-xs mb-2">
            {hasReviews ? (
              <div className="flex items-center gap-1.5">
                <span className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded bg-[#FAF5E6] dark:bg-[#FBBF24]/10 text-[#A66B00] dark:text-[#FBBF24] font-bold">
                  <Star className="w-3 h-3 fill-current" />
                  <span>{p.rating!.toFixed(1)}</span>
                </span>
                <span className="text-[#626B7A] dark:text-[#A2AEC1] font-medium">
                  ({p.reviewCount} {p.reviewCount === 1 ? 'review' : 'reviews'})
                </span>
              </div>
            ) : (
              <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-medium bg-[#F3F0EB] dark:bg-[#1A2130] text-[#626B7A] dark:text-[#A2AEC1]">
                New on Bookline
              </span>
            )}

            <span className="text-[#DED8CF] dark:text-[#30394A]">&bull;</span>

            {/* Location & Distance (Distance ONLY if reference coords exist) */}
            <div className="flex items-center gap-1 text-[#4B5563] dark:text-[#CBD2DE] truncate">
              <MapPin className="w-3.5 h-3.5 shrink-0 text-[#626B7A] dark:text-[#A2AEC1]" />
              {hasDistance && (
                <span className="font-semibold text-[#158A63] dark:text-[#34D399]">
                  {p.distanceKm! < 1 ? `${Math.round(p.distanceKm! * 1000)}m` : `${p.distanceKm!.toFixed(1)} km`} &bull;{' '}
                </span>
              )}
              <span className="truncate">{locationArea}</span>
            </div>
          </div>

          {/* Services Highlights (Data-driven only) */}
          {servicesList.length > 0 && (
            <div className="flex flex-wrap gap-1.5 my-2">
              {servicesList.map((srv, idx) => (
                <span
                  key={idx}
                  className="inline-block px-2 py-0.5 text-[11px] rounded-md bg-[#F3F0EB] dark:bg-[#1A2130] text-[#4B5563] dark:text-[#CBD2DE] border border-[#DED8CF]/60 dark:border-[#30394A]/60"
                >
                  {srv}
                </span>
              ))}
            </div>
          )}

          {/* Real Availability Slot (Only if returned by availability engine) */}
          {p.nextAvailableSlot && (
            <div className="flex items-center gap-1.5 text-xs text-[#158A63] dark:text-[#34D399] font-medium mt-1">
              <Clock className="w-3.5 h-3.5 shrink-0" />
              <span>Next available: {p.nextAvailableSlot}</span>
            </div>
          )}
        </div>

        {/* 3. Action Buttons Footer */}
        <div className="pt-3 mt-2 border-t border-[#DED8CF]/70 dark:border-[#30394A] flex items-center justify-between gap-3">
          <button
            type="button"
            onClick={handleViewBusiness}
            className="min-h-[44px] px-3.5 py-2 text-xs font-semibold text-[#181A1F] dark:text-[#F4F6FA] hover:text-[#B93650] dark:hover:text-[#E8546A] bg-[#F3F0EB] dark:bg-[#1A2130] hover:bg-[#DED8CF] dark:hover:bg-[#273142] rounded-xl transition-colors flex items-center gap-1 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#B93650]"
          >
            <span>View business</span>
          </button>

          {canBook ? (
            <button
              type="button"
              onClick={handleBookNow}
              className="min-h-[44px] px-5 py-2 text-xs font-bold text-white bg-[#B93650] hover:bg-[#A52E46] active:bg-[#92273D] rounded-xl transition-colors flex items-center gap-1.5 shadow-sm shadow-[#B93650]/20 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#B93650] focus-visible:ring-offset-1"
            >
              <span>Book</span>
              <ArrowRight className="w-3.5 h-3.5" />
            </button>
          ) : (
            <button
              type="button"
              disabled
              aria-disabled="true"
              className="min-h-[44px] px-4 py-2 text-xs font-medium text-[#9AA3B2] dark:text-[#687386] bg-[#F3F0EB] dark:bg-[#1A2130] rounded-xl cursor-not-allowed"
            >
              Unavailable
            </button>
          )}
        </div>
      </div>
    </article>
  );
};

/**
 * ProviderCardSkeleton — High-fidelity loading state
 */
export const ProviderCardSkeleton: React.FC<{ className?: string }> = ({ className = '' }) => {
  return (
    <div
      aria-busy="true"
      aria-label="Loading provider details"
      className={`rounded-2xl border border-[#DED8CF] dark:border-[#30394A] bg-white dark:bg-[#111620] p-4 sm:p-5 flex flex-col sm:flex-row gap-4 sm:gap-5 animate-pulse ${className}`}
    >
      <div className="w-full sm:w-44 h-40 sm:h-36 rounded-xl bg-[#F3F0EB] dark:bg-[#1A2130] shrink-0" />
      <div className="flex-1 flex flex-col justify-between py-1 space-y-3">
        <div className="space-y-2.5">
          <div className="flex justify-between items-center gap-4">
            <div className="h-5 bg-[#F3F0EB] dark:bg-[#1A2130] rounded w-2/5" />
            <div className="h-5 bg-[#F3F0EB] dark:bg-[#1A2130] rounded w-16" />
          </div>
          <div className="h-4 bg-[#F3F0EB] dark:bg-[#1A2130] rounded w-3/5" />
          <div className="flex gap-2 pt-1">
            <div className="h-5 bg-[#F3F0EB] dark:bg-[#1A2130] rounded w-16" />
            <div className="h-5 bg-[#F3F0EB] dark:bg-[#1A2130] rounded w-20" />
            <div className="h-5 bg-[#F3F0EB] dark:bg-[#1A2130] rounded w-14" />
          </div>
        </div>
        <div className="pt-3 border-t border-[#DED8CF]/50 dark:border-[#30394A]/50 flex justify-between items-center">
          <div className="h-8 bg-[#F3F0EB] dark:bg-[#1A2130] rounded w-24" />
          <div className="h-8 bg-[#F3F0EB] dark:bg-[#1A2130] rounded w-20" />
        </div>
      </div>
    </div>
  );
};
