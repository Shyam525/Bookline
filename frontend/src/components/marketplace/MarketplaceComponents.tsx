import React from 'react';
import { Link } from 'react-router-dom';
import {
  Star,
  CheckCircle,
  MapPin,
  Clock,
  Navigation,
  ExternalLink,
  Search,
  SlidersHorizontal,
  Layers,
  ChevronDown,
  X,
  Compass,
} from 'lucide-react';
import { ProviderImage } from '../common/ProviderImage';
import { clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

/* =========================================================================
 * 1. PROVIDER CARD & PREVIEW (Section 37, 105, 109, 110)
 * ========================================================================= */
export interface ProviderCardData {
  id: string;
  name: string;
  slug: string;
  category: string;
  rating: number;
  reviewCount: number;
  city: string;
  address?: string;
  distanceKm?: number | null;
  startingPrice: number;
  currency?: string;
  nextAvailableSlot?: string | null;
  isVerified?: boolean;
  coverImageUrl?: string | null;
  servicesSummary?: string[];
}

export interface ProviderCardProps {
  provider: ProviderCardData;
  isSelected?: boolean;
  isHovered?: boolean;
  onSelect?: () => void;
  onHover?: (hovered: boolean) => void;
  className?: string;
}

export const ProviderCard: React.FC<ProviderCardProps> = ({
  provider,
  isSelected = false,
  isHovered = false,
  onSelect,
  onHover,
  className = '',
}) => {
  const p = provider;
  const locationArea = p.address?.split(',')[0] || p.city;
  const formattedDistance = p.distanceKm != null ? `${p.distanceKm} km` : '1.2 km';
  const servicesString =
    p.servicesSummary && p.servicesSummary.length > 0
      ? p.servicesSummary.slice(0, 3).join(' · ')
      : 'Specialist Services Available';

  return (
    <div
      onClick={onSelect}
      onMouseEnter={() => onHover?.(true)}
      onMouseLeave={() => onHover?.(false)}
      className={twMerge(
        clsx(
          'p-5 rounded-2xl border transition-all cursor-pointer flex flex-col sm:flex-row gap-5 select-none group',
          isSelected || isHovered
            ? 'border-[#E8546A] shadow-xl shadow-[#E8546A]/20 bg-[#151B27] ring-1 ring-[#E8546A]/40 -translate-y-0.5'
            : 'border-[#273142] hover:border-[#344054] bg-[#111620] hover:bg-[#151B27]',
          className
        )
      )}
    >
      {/* Cover Image / Category */}
      <div className="w-full sm:w-44 h-36 rounded-xl bg-[#1A2130] border border-[#273142] overflow-hidden shrink-0 relative">
        <ProviderImage
          src={p.coverImageUrl}
          alt={p.name}
          type="cover"
          category={p.category}
          className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
          loading="lazy"
        />
        <div className="absolute top-2 left-2 bg-[#090B10]/85 backdrop-blur-md px-2.5 py-1 rounded-md text-[10px] font-semibold text-[#ECEFFE] border border-[#212638]">
          {p.category}
        </div>
      </div>

      {/* Details */}
      <div className="flex-1 flex flex-col justify-between">
        <div>
          <div className="flex items-center justify-between gap-2 mb-1">
            <div className="flex items-center gap-1.5 truncate">
              <h3 className="font-heading text-lg font-bold text-[#F4F6FA] truncate group-hover:text-[#E8546A] transition-colors">
                {p.name}
              </h3>
              {p.isVerified && (
                <span title="Verified Business" className="inline-flex items-center">
                  <CheckCircle className="w-4 h-4 text-[#34D399] shrink-0" />
                </span>
              )}
            </div>
          </div>

          <div className="flex items-center gap-1 text-xs text-[#ECEFFE] font-medium mb-1">
            <span className="text-[#FBBF24] font-bold">{p.rating.toFixed(1)} ★</span>
            <span className="text-[#8F9AAF]">&middot;</span>
            <span className="text-[#8F9AAF]">{p.reviewCount} reviews</span>
          </div>

          <div className="flex items-center gap-1.5 text-xs text-[#8F9AAF] mb-2">
            <span className="text-[#34D399] font-medium">{formattedDistance}</span>
            <span>&middot;</span>
            <span className="truncate">{locationArea}</span>
          </div>

          <div className="text-xs text-[#C3CAD6] font-medium mb-2.5 truncate">
            {servicesString}
          </div>
        </div>

        {/* Footer info & CTA */}
        <div className="pt-3 border-t border-[#273142] flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-4">
            <div>
              <span className="text-[11px] text-[#8F9AAF] block">Next available</span>
              <span className="text-xs font-semibold text-[#F4F6FA] flex items-center gap-1">
                <Clock className="w-3.5 h-3.5 text-[#FBBF24]" />
                {p.nextAvailableSlot || '10:30 AM'}
              </span>
            </div>
            <div className="border-l border-[#273142] pl-3">
              <span className="text-[11px] text-[#8F9AAF] block">From</span>
              <span className="text-sm font-bold text-[#34D399]">
                From {p.currency || '₹'}{p.startingPrice}
              </span>
            </div>
          </div>

          <div className="flex items-center gap-2 w-full sm:w-auto pt-2 sm:pt-0">
            <Link
              to={`/business/${p.slug}`}
              className="flex-1 sm:flex-initial min-h-[40px] px-4 py-2 rounded-xl border border-[#273142] hover:bg-[#1A2130] text-xs font-semibold text-[#F4F6FA] transition-colors flex items-center justify-center"
            >
              Profile
            </Link>
            <Link
              to={`/business/${p.slug}?book=true`}
              className="flex-1 sm:flex-initial min-h-[40px] px-5 py-2 rounded-xl bg-[#E8546A] hover:bg-[#F06A7D] text-xs font-bold text-white transition-all shadow-md shadow-[#E8546A]/20 flex items-center justify-center gap-1.5"
            >
              Book Now
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
};

export const ProviderGrid: React.FC<{ children: React.ReactNode; className?: string }> = ({
  children,
  className = '',
}) => {
  return (
    <div className={twMerge('grid grid-cols-1 md:grid-cols-2 gap-4 lg:gap-6', className)}>
      {children}
    </div>
  );
};

export const ProviderList: React.FC<{ children: React.ReactNode; className?: string }> = ({
  children,
  className = '',
}) => {
  return <div className={twMerge('space-y-4', className)}>{children}</div>;
};

/* =========================================================================
 * 2. CATEGORY CARD (Visual Discovery Primitives)
 * ========================================================================= */
export interface CategoryCardProps {
  id: string;
  name: string;
  description?: string;
  icon?: React.ReactNode;
  imageUrl?: string;
  isActive?: boolean;
  onClick?: () => void;
  className?: string;
}

export const CategoryCard: React.FC<CategoryCardProps> = ({
  name,
  description,
  icon,
  imageUrl,
  isActive = false,
  onClick,
  className = '',
}) => {
  return (
    <button
      type="button"
      onClick={onClick}
      className={twMerge(
        clsx(
          'p-4 rounded-2xl border text-left transition-all duration-200 flex flex-col justify-between group overflow-hidden relative select-none focus:outline-none focus:ring-2 focus:ring-[#E8546A]',
          isActive
            ? 'bg-[#E8546A] border-[#E8546A] text-white shadow-xl shadow-[#E8546A]/30 scale-[1.02]'
            : 'bg-[#111520] hover:bg-[#151B27] border-[#212638] text-[#ECEFFE] hover:border-[#E8546A]/50',
          className
        )
      )}
    >
      <div className="flex items-center justify-between mb-3 w-full">
        {icon && (
          <div
            className={`w-10 h-10 rounded-xl flex items-center justify-center transition-colors ${
              isActive ? 'bg-white/20 text-white' : 'bg-[#181D2C] text-[#E8546A]'
            }`}
          >
            {icon}
          </div>
        )}
        <span
          className={`text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded-md ${
            isActive ? 'bg-white/20 text-white' : 'bg-[#181D2C] text-[#7E88A8]'
          }`}
        >
          Explore
        </span>
      </div>

      <div>
        <h4 className="font-heading text-base font-bold text-white mb-0.5">{name}</h4>
        {description && (
          <p className={`text-xs line-clamp-1 ${isActive ? 'text-white/80' : 'text-[#7E88A8]'}`}>
            {description}
          </p>
        )}
      </div>
    </button>
  );
};

/* =========================================================================
 * 3. SEARCH BAR & LOCATION PICKER (Section 106 Mobile Priority)
 * ========================================================================= */
export interface SearchBarProps {
  query: string;
  onQueryChange: (q: string) => void;
  locationLabel: string;
  onOpenLocation: () => void;
  onSubmit: (e: React.FormEvent) => void;
  placeholder?: string;
  className?: string;
}

export const SearchBar: React.FC<SearchBarProps> = ({
  query,
  onQueryChange,
  locationLabel,
  onOpenLocation,
  onSubmit,
  placeholder = 'Search services, specialists, keywords...',
  className = '',
}) => {
  return (
    <form onSubmit={onSubmit} className={twMerge('flex flex-col sm:flex-row items-stretch sm:items-center gap-2 w-full', className)}>
      <div className="relative flex-1">
        <Search className="w-4 h-4 text-[#8F9AAF] absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
        <input
          type="text"
          value={query}
          onChange={(e) => onQueryChange(e.target.value)}
          placeholder={placeholder}
          className="w-full min-h-[48px] bg-[#151B27] border border-[#273142] rounded-xl pl-10 pr-9 py-2.5 text-xs text-[#F4F6FA] placeholder-[#8F9AAF] focus:border-[#E8546A] outline-none transition-colors"
        />
        {query && (
          <button
            type="button"
            onClick={() => onQueryChange('')}
            className="absolute right-3 top-1/2 -translate-y-1/2 p-1 text-[#8F9AAF] hover:text-white"
            aria-label="Clear query"
          >
            <X className="w-3.5 h-3.5" />
          </button>
        )}
      </div>

      <button
        type="button"
        onClick={onOpenLocation}
        className="min-h-[48px] sm:min-h-[40px] px-4 py-2.5 rounded-xl bg-[#151B27] hover:bg-[#1A2130] border border-[#273142] hover:border-[#E8546A] text-xs font-semibold text-[#F4F6FA] transition-colors flex items-center justify-between sm:justify-start gap-2 shrink-0"
      >
        <div className="flex items-center gap-2 truncate">
          <MapPin className="w-4 h-4 text-[#E8546A] shrink-0" />
          <span className="truncate font-medium">{locationLabel}</span>
        </div>
        <span className="text-[11px] text-[#E8546A] font-bold bg-[#E8546A]/10 px-2 py-0.5 rounded-md">
          Change
        </span>
      </button>

      <button
        type="submit"
        className="min-h-[48px] sm:min-h-[40px] px-6 py-2.5 bg-[#E8546A] hover:bg-[#F06A7D] text-white text-xs font-bold rounded-xl transition-all shadow-md shrink-0 flex items-center justify-center gap-1.5"
      >
        <Search className="w-3.5 h-3.5" />
        <span>Search</span>
      </button>
    </form>
  );
};

/* =========================================================================
 * 4. FILTER BAR & SORT SELECTOR
 * ========================================================================= */
export interface FilterBarProps {
  categories: string[];
  selectedCategory: string;
  onSelectCategory: (cat: string) => void;
  openNow?: boolean;
  onToggleOpenNow?: (open: boolean) => void;
  verifiedOnly?: boolean;
  onToggleVerifiedOnly?: (verified: boolean) => void;
  onOpenDrawer?: () => void;
  className?: string;
}

export const FilterBar: React.FC<FilterBarProps> = ({
  categories,
  selectedCategory,
  onSelectCategory,
  openNow,
  onToggleOpenNow,
  verifiedOnly,
  onToggleVerifiedOnly,
  onOpenDrawer,
  className = '',
}) => {
  return (
    <div className={twMerge('flex items-center gap-2 overflow-x-auto pb-2 scrollbar-none', className)}>
      {onOpenDrawer && (
        <button
          type="button"
          onClick={onOpenDrawer}
          className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-[#151B27] border border-[#273142] text-xs font-semibold text-[#F4F6FA] hover:border-[#E8546A] transition-colors shrink-0"
        >
          <SlidersHorizontal className="w-3.5 h-3.5 text-[#E8546A]" />
          <span>Filters</span>
        </button>
      )}

      {categories.map((cat) => {
        const isSelected = cat === selectedCategory;
        return (
          <button
            key={cat}
            type="button"
            onClick={() => onSelectCategory(cat)}
            className={`px-3.5 py-1.5 rounded-xl text-xs font-semibold transition-colors shrink-0 ${
              isSelected
                ? 'bg-[#E8546A] text-white shadow-md shadow-[#E8546A]/20'
                : 'bg-[#151B27] hover:bg-[#1A2130] border border-[#273142] text-[#8F9AAF] hover:text-[#F4F6FA]'
            }`}
          >
            {cat}
          </button>
        );
      })}

      {onToggleOpenNow && (
        <label className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-[#151B27] border border-[#273142] text-xs text-[#ECEFFE] cursor-pointer shrink-0">
          <input
            type="checkbox"
            checked={openNow}
            onChange={(e) => onToggleOpenNow(e.target.checked)}
            className="accent-[#E8546A] w-3.5 h-3.5"
          />
          <span>Open Now</span>
        </label>
      )}

      {onToggleVerifiedOnly && (
        <label className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-[#151B27] border border-[#273142] text-xs text-[#ECEFFE] cursor-pointer shrink-0">
          <input
            type="checkbox"
            checked={verifiedOnly}
            onChange={(e) => onToggleVerifiedOnly(e.target.checked)}
            className="accent-[#E8546A] w-3.5 h-3.5"
          />
          <span>Verified</span>
        </label>
      )}
    </div>
  );
};

export interface SortSelectorProps {
  value: string;
  onChange: (val: string) => void;
  options?: string[];
  className?: string;
}

export const SortSelector: React.FC<SortSelectorProps> = ({
  value,
  onChange,
  options = ['Recommended', 'Nearest', 'Top rated', 'Lowest price', 'Highest price'],
  className = '',
}) => {
  return (
    <div className={twMerge('flex items-center gap-1.5 text-xs text-[#8F9AAF]', className)}>
      <span className="shrink-0">Sort:</span>
      <select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="bg-[#151B27] border border-[#273142] rounded-xl px-2.5 py-1.5 text-xs text-[#F4F6FA] focus:outline-none focus:border-[#E8546A] cursor-pointer"
      >
        {options.map((opt) => (
          <option key={opt} value={opt} className="bg-[#111620] text-white">
            {opt}
          </option>
        ))}
      </select>
    </div>
  );
};

/* =========================================================================
 * 5. MAP VIEW PRIMITIVES (MapMarker, MapCluster, ProviderPreview)
 * ========================================================================= */
export interface MapMarkerProps {
  price: number;
  currency?: string;
  isVerified?: boolean;
  isSelected?: boolean;
  isHovered?: boolean;
  onClick?: () => void;
  onHover?: (hover: boolean) => void;
}

export const MapMarker: React.FC<MapMarkerProps> = ({
  price,
  currency = '₹',
  isVerified = false,
  isSelected = false,
  isHovered = false,
  onClick,
  onHover,
}) => {
  const active = isSelected || isHovered;

  return (
    <div
      onClick={onClick}
      onMouseEnter={() => onHover?.(true)}
      onMouseLeave={() => onHover?.(false)}
      className="cursor-pointer transition-transform duration-150 hover:scale-110 select-none"
    >
      <div
        className={clsx(
          'px-2.5 py-1 rounded-full text-xs font-bold flex items-center gap-1.5 shadow-xl transition-all',
          active
            ? 'bg-[#E8546A] text-white ring-4 ring-[#E8546A]/40 shadow-[#E8546A]/30 scale-105'
            : 'bg-[#151B27] hover:bg-[#1A2130] text-[#F4F6FA] border border-[#273142]'
        )}
      >
        <span>{currency}{price}</span>
        {isVerified && <CheckCircle className="w-3 h-3 text-[#34D399]" />}
      </div>
      <div
        className={clsx(
          'w-2 h-2 rotate-45 mx-auto -mt-1 transition-colors',
          active ? 'bg-[#E8546A]' : 'bg-[#151B27]'
        )}
      />
    </div>
  );
};

export const MapCluster: React.FC<{ count: number; onClick?: () => void }> = ({ count, onClick }) => {
  return (
    <div
      onClick={onClick}
      className="cursor-pointer group select-none transition-transform hover:scale-110"
    >
      <div className="px-3.5 py-1.5 rounded-full bg-[#E8546A] text-white font-bold text-xs flex items-center gap-1.5 shadow-2xl ring-4 ring-[#E8546A]/30">
        <Layers className="w-3.5 h-3.5" />
        <span>{count} venues</span>
      </div>
    </div>
  );
};

export const ProviderPreview: React.FC<{
  provider: ProviderCardData;
  onClose?: () => void;
}> = ({ provider, onClose }) => {
  return (
    <div className="bg-[#111620]/95 backdrop-blur-md border border-[#273142] rounded-2xl p-4 shadow-2xl max-w-xs w-full animate-fadeIn select-none space-y-2.5">
      <div className="flex items-start justify-between gap-2">
        <div>
          <div className="flex items-center gap-1.5">
            <h4 className="text-sm font-bold text-[#F4F6FA] truncate max-w-[180px]">
              {provider.name}
            </h4>
            {provider.isVerified && <CheckCircle className="w-3.5 h-3.5 text-[#34D399] shrink-0" />}
          </div>
          <p className="text-[11px] text-[#8F9AAF]">{provider.category}</p>
        </div>
        <div className="flex items-center gap-1 bg-[#1A2130] px-1.5 py-0.5 rounded text-[11px] font-bold text-[#FBBF24]">
          <Star className="w-3 h-3 fill-current" />
          <span>{provider.rating.toFixed(1)}</span>
        </div>
      </div>

      <p className="text-xs text-[#C3CAD6] line-clamp-1">{provider.address || provider.city}</p>

      <div className="flex items-center justify-between text-xs pt-2 border-t border-[#273142]">
        <span className="font-bold text-[#34D399]">
          From {provider.currency || '₹'}{provider.startingPrice}
        </span>
        <Link
          to={`/business/${provider.slug}`}
          className="text-xs font-semibold text-[#E8546A] hover:underline flex items-center gap-1"
        >
          <span>Storefront</span>
          <ExternalLink className="w-3 h-3" />
        </Link>
      </div>
    </div>
  );
};

/* =========================================================================
 * 8. LOCATION PICKER (Section 103, 106, 109, 110)
 * ========================================================================= */
export interface LocationPickerProps {
  locationName?: string;
  isLocating?: boolean;
  disabled?: boolean;
  hasGPSPermission?: boolean;
  onOpenSelector?: () => void;
  onDetectGPS?: () => void;
  className?: string;
}

export const LocationPicker: React.FC<LocationPickerProps> = ({
  locationName = 'Detecting Location...',
  isLocating = false,
  disabled = false,
  hasGPSPermission = true,
  onOpenSelector,
  onDetectGPS,
  className = '',
}) => {
  return (
    <div
      className={twMerge(
        clsx(
          'flex items-center gap-1.5 p-1 rounded-xl bg-[#111620] border border-[#273142] transition-colors',
          disabled && 'opacity-50 pointer-events-none',
          className
        )
      )}
    >
      <button
        type="button"
        disabled={disabled}
        onClick={onOpenSelector}
        className="flex-1 flex items-center gap-2 px-3 py-2 rounded-lg text-left hover:bg-[#1A2130] focus:outline-none focus:ring-1 focus:ring-[#E8546A] transition-colors min-h-[44px]"
        aria-label="Change location"
      >
        <MapPin className="w-4 h-4 text-[#E8546A] shrink-0" />
        <div className="min-w-0">
          <p className="text-[10px] uppercase tracking-wider text-[#8F9AAF] font-medium leading-none">
            Location
          </p>
          <p className="text-xs font-semibold text-white truncate mt-0.5">
            {locationName}
          </p>
        </div>
      </button>

      {onDetectGPS && (
        <button
          type="button"
          disabled={disabled || isLocating}
          onClick={onDetectGPS}
          className="p-2.5 rounded-lg text-[#8F9AAF] hover:text-white hover:bg-[#1A2130] active:scale-95 transition-all min-h-[44px] min-w-[44px] flex items-center justify-center"
          title="Detect GPS location"
          aria-label="Detect GPS location"
        >
          {isLocating ? (
            <div className="w-4 h-4 border-2 border-[#E8546A]/30 border-t-[#E8546A] rounded-full animate-spin" />
          ) : (
            <Navigation className="w-4 h-4 text-[#E8546A]" />
          )}
        </button>
      )}
    </div>
  );
};

/* =========================================================================
 * 9. MAP VIEW WRAPPER (Section 103, 104, 109, 110)
 * ========================================================================= */
export interface MapViewProps {
  providers?: ProviderCardData[];
  selectedProviderId?: string | null;
  hoveredProviderId?: string | null;
  onSelectProvider?: (provider: ProviderCardData) => void;
  isLoading?: boolean;
  hasError?: boolean;
  onRetry?: () => void;
  className?: string;
  children?: React.ReactNode;
}

export const MapView: React.FC<MapViewProps> = ({
  providers = [],
  selectedProviderId,
  hoveredProviderId,
  onSelectProvider,
  isLoading = false,
  hasError = false,
  onRetry,
  className = '',
  children,
}) => {
  if (isLoading) {
    return (
      <div
        className={twMerge(
          clsx(
            'w-full h-full min-h-[350px] rounded-2xl bg-[#111620] border border-[#273142] flex flex-col items-center justify-center p-6 space-y-3 animate-pulse',
            className
          )
        )}
      >
        <div className="w-12 h-12 rounded-2xl bg-[#1A2130] flex items-center justify-center text-[#8F9AAF]">
          <Compass className="w-6 h-6 animate-spin" />
        </div>
        <p className="text-xs font-semibold text-white">Rendering Geographic Map View...</p>
        <p className="text-[11px] text-[#8F9AAF]">Plotting verified salon & spa coordinates</p>
      </div>
    );
  }

  if (hasError) {
    return (
      <div
        className={twMerge(
          clsx(
            'w-full h-full min-h-[350px] rounded-2xl bg-[#151922] border border-[#EF4444]/30 flex flex-col items-center justify-center p-6 text-center space-y-3',
            className
          )
        )}
      >
        <div className="w-12 h-12 rounded-2xl bg-[#EF4444]/10 text-[#EF4444] flex items-center justify-center">
          <MapPin className="w-6 h-6" />
        </div>
        <div>
          <h4 className="text-sm font-bold text-white">Map Canvas Temporarily Unavailable</h4>
          <p className="text-xs text-[#8F9AAF] max-w-xs mt-1">
            Section 103 Fallback: Providers and online booking features remain fully active in the list view.
          </p>
        </div>
        {onRetry && (
          <button
            type="button"
            onClick={onRetry}
            className="px-4 py-2 rounded-xl bg-[#212638] hover:bg-[#2C344A] text-white text-xs font-semibold transition-colors"
          >
            Retry Map Render
          </button>
        )}
      </div>
    );
  }

  return (
    <div
      className={twMerge(
        clsx(
          'w-full h-full min-h-[350px] rounded-2xl bg-[#090B10] border border-[#273142] relative overflow-hidden flex flex-col',
          className
        )
      )}
    >
      {children || (
        <div className="w-full h-full flex flex-col items-center justify-center p-8 text-center relative">
          {/* Subtle grid pattern background */}
          <div
            className="absolute inset-0 opacity-10 pointer-events-none"
            style={{
              backgroundImage:
                'radial-gradient(circle at 1px 1px, #E8546A 1px, transparent 0)',
              backgroundSize: '24px 24px',
            }}
          />

          {/* Sample interactive markers */}
          <div className="flex flex-wrap gap-4 items-center justify-center z-10">
            {providers.slice(0, 3).map((p, idx) => (
              <MapMarker
                key={p.id}
                price={p.startingPrice}
                currency={p.currency || '₹'}
                isSelected={selectedProviderId === p.id}
                isHovered={hoveredProviderId === p.id}
                onClick={() => onSelectProvider?.(p)}
              />
            ))}
          </div>

          <div className="mt-6 z-10 text-center">
            <p className="text-xs text-[#8F9AAF]">
              Interactive WebGL Map Canvas with {providers.length} plotted provider pins
            </p>
          </div>
        </div>
      )}
    </div>
  );
};

