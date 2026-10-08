import React, { useState, useEffect, useRef } from 'react';
import { useSearchParams, Link, useParams } from 'react-router-dom';
import { discoveryApi, ProviderCard, ProviderSearchResponse } from '../../services/api/discovery';
import { InteractiveMap } from '../../components/maps/InteractiveMap';
import {
  LocationControlModal,
  LocationSelection,
} from '../../components/discovery/LocationControlModal';
import {
  Search,
  MapPin,
  SlidersHorizontal,
  Star,
  CheckCircle,
  Clock,
  ArrowUpDown,
  Compass,
  Map as MapIcon,
  List as ListIcon,
  ChevronLeft,
  ChevronRight,
  Sparkles,
  Navigation,
  ExternalLink,
  Sliders,
  X,
} from 'lucide-react';

export const DiscoveryPage: React.FC = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const { slug, category: routeCategory } = useParams<{ slug?: string; category?: string }>();

  const categories = ['All', 'Beauty', 'Hair', 'Healthcare', 'Fitness', 'Photography', 'Professional'];

  const resolveCategoryFromSlug = (raw?: string): string => {
    if (!raw) return 'All';
    const lower = raw.toLowerCase();
    if (lower === 'wellness' || lower === 'spa') return 'Beauty';
    const matched = categories.find((c) => c.toLowerCase() === lower);
    return matched || 'All';
  };

  const initialCategory =
    resolveCategoryFromSlug(slug || routeCategory) !== 'All'
      ? resolveCategoryFromSlug(slug || routeCategory)
      : searchParams.get('category') || 'All';

  const [providers, setProviders] = useState<ProviderCard[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Category-specific services dictionary (Section 36: Only show filters relevant to current category)
  const categoryServicesMap: Record<string, string[]> = {
    'All': ['All Services', 'Haircut', 'Facial', 'Balayage', 'Massage', 'Dental Cleaning', 'Reformer Pilates', 'Headshots', 'Legal Advisory'],
    'Beauty': ['All Services', 'Facial & Skin Ritual', 'Chemical Peel', 'Aromatherapy Massage', 'Body Wrap', 'Manicure & Pedicure'],
    'Hair': ['All Services', 'Haircut & Styling', 'Balayage & Color', 'Keratin Smoothing', 'Scalp Therapy', 'Blowout'],
    'Healthcare': ['All Services', 'Dental Cleaning', 'Laser Teeth Whitening', 'Porcelain Veneers', 'Orthodontic Exam', 'Skin Consultation'],
    'Fitness': ['All Services', 'Reformer Pilates', 'HIIT Circuit', 'Private Athletic Coaching', 'Yoga Vinyasa', 'Sports Recovery'],
    'Photography': ['All Services', 'Corporate Headshots', 'Editorial Campaign', 'Family Heirloom Portrait', 'Fashion Lookbook', 'Product Shoot'],
    'Professional': ['All Services', 'Commercial Contracts', 'Intellectual Property Filing', 'Tax Advisory', 'Corporate Restructuring'],
  };

  // Filters from query params and state (Section 35 & 36)
  const [query, setQuery] = useState(searchParams.get('q') || '');
  const [category, setCategory] = useState(initialCategory);
  const [selectedService, setSelectedService] = useState<string>('All Services');
  const [radius, setRadius] = useState<number>(Number(searchParams.get('radius')) || 25);
  const [minRating, setMinRating] = useState<number>(Number(searchParams.get('minRating')) || 0);
  const [priceRange, setPriceRange] = useState<string>('all'); // 'all', 'under-500', '500-1500', '1500-3000', '3000+'
  const [availability, setAvailability] = useState<string>('all'); // 'all', 'today', 'tomorrow', 'weekend'
  const [openNow, setOpenNow] = useState(false);
  const [verifiedOnly, setVerifiedOnly] = useState(false);
  const [isFiltersOpen, setIsFiltersOpen] = useState(false);
  const [sort, setSort] = useState(searchParams.get('sort') || 'Recommended');
  const [page, setPage] = useState<number>(Number(searchParams.get('page')) || 1);

  // When category changes, reset selectedService to 'All Services' (Section 36 rule)
  const handleCategoryChange = (newCat: string) => {
    setCategory(newCat);
    setSelectedService('All Services');
    setPage(1);
  };

  // Active filter count calculation
  const activeFiltersCount =
    (category !== 'All' ? 1 : 0) +
    (selectedService !== 'All Services' ? 1 : 0) +
    (radius !== 25 ? 1 : 0) +
    (minRating > 0 ? 1 : 0) +
    (priceRange !== 'all' ? 1 : 0) +
    (availability !== 'all' ? 1 : 0) +
    (openNow ? 1 : 0) +
    (verifiedOnly ? 1 : 0);

  // Location State (Section 25 & 26)
  const [isLocationModalOpen, setIsLocationModalOpen] = useState(false);
  const [currentLocation, setCurrentLocation] = useState<LocationSelection>(() => {
    const cityParam = searchParams.get('city') || 'Ahmedabad';
    const latParam = Number(searchParams.get('lat')) || 23.0225;
    const lngParam = Number(searchParams.get('lng')) || 72.5714;
    return {
      label: cityParam,
      city: cityParam,
      lat: latParam,
      lng: lngParam,
    };
  });
  const [userGpsCoords, setUserGpsCoords] = useState<{ lat: number; lng: number } | null>(() => {
    const lat = searchParams.get('lat');
    const lng = searchParams.get('lng');
    return lat && lng ? { lat: Number(lat), lng: Number(lng) } : null;
  });

  // Map / List Mobile Toggle
  const [mobileView, setMobileView] = useState<'list' | 'map'>('list');
  const [selectedProviderId, setSelectedProviderId] = useState<string | null>(null);

  // List card refs for synchronized scrolling (Section 28)
  const cardRefs = useRef<Record<string, HTMLDivElement | null>>({});

  // Sync category if URL param changes
  useEffect(() => {
    const fromParam = resolveCategoryFromSlug(slug || routeCategory);
    if (fromParam !== 'All') {
      setCategory(fromParam);
      setSelectedService('All Services');
    }
  }, [slug, routeCategory]);

  const fetchProviders = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const lat = userGpsCoords?.lat ?? (currentLocation.lat || undefined);
      const lng = userGpsCoords?.lng ?? (currentLocation.lng || undefined);

      let maxPriceVal: number | undefined;
      if (priceRange === 'under-500') maxPriceVal = 500;
      else if (priceRange === '500-1500') maxPriceVal = 1500;
      else if (priceRange === '1500-3000') maxPriceVal = 3000;

      const res = await discoveryApi.searchProviders({
        q: query || undefined,
        category: category !== 'All' ? category : undefined,
        service: selectedService !== 'All Services' ? selectedService : undefined,
        city: currentLocation.city !== 'All' ? currentLocation.city : undefined,
        lat,
        lng,
        radius: radius || undefined,
        minRating: minRating > 0 ? minRating : undefined,
        maxPrice: maxPriceVal,
        availability: availability !== 'all' ? availability : undefined,
        sort,
        page,
        pageSize: 10,
      });

      let items = res.items;
      if (verifiedOnly) {
        items = items.filter((p) => p.isVerified);
      }

      setProviders(items);
      setTotalCount(res.totalCount);
      setTotalPages(res.totalPages);
      if (items.length > 0 && !selectedProviderId) {
        setSelectedProviderId(items[0].id);
      }
    } catch (err: any) {
      setError(err.message || 'We could not load discovery results.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchProviders();
  }, [
    category,
    selectedService,
    currentLocation.city,
    currentLocation.lat,
    currentLocation.lng,
    radius,
    minRating,
    priceRange,
    availability,
    openNow,
    verifiedOnly,
    sort,
    page,
  ]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
    fetchProviders();
  };

  const handleLocationSelect = (loc: LocationSelection, saveToProfile?: boolean) => {
    setCurrentLocation(loc);
    if (loc.isCurrentLocation) {
      setUserGpsCoords({ lat: loc.lat, lng: loc.lng });
    } else {
      setUserGpsCoords(null);
    }
    setPage(1);
  };

  // Synchronized Selection: clicking map marker scrolls card into view (Section 28)
  const handleSelectProvider = (provider: ProviderCard) => {
    setSelectedProviderId(provider.id);
    const element = cardRefs.current[provider.id];
    if (element) {
      element.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }
  };

  // Center coordinates for map
  const mapCenter = { lat: currentLocation.lat, lng: currentLocation.lng };

  // Detect query intent keywords for visual feedback (Section 33)
  const isIntentQuery =
    query.toLowerCase().includes('near me') ||
    query.toLowerCase().includes('best') ||
    query.toLowerCase().includes('satellite') ||
    query.toLowerCase().includes('bodakdev') ||
    query.toLowerCase().includes('bandra');

  return (
    <div className="flex-1 flex flex-col bg-[#090B10] text-[#F4F6FA]">
      {/* ================================================================= */}
      {/* 1. SEARCH HEADER & LOCATION CONTROL (Section 24 & 25)              */}
      {/* ================================================================= */}
      <div className="border-b border-[#273142] bg-[#111620] px-4 lg:px-8 py-3.5 sticky top-16 z-30 shadow-md">
        <div className="max-w-7xl mx-auto flex flex-col lg:flex-row items-center justify-between gap-3">
          {/* Search Input Bar */}
          <form onSubmit={handleSearchSubmit} className="flex-1 w-full flex items-center gap-2">
            <div className="relative flex-1">
              <Search className="w-4 h-4 text-[#8F9AAF] absolute left-3.5 top-1/2 -translate-y-1/2" />
              <input
                type="text"
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                placeholder="Search salons, dentists, massage, reformer pilates, or keywords..."
                className="w-full bg-[#151B27] border border-[#273142] rounded-[8px] pl-10 pr-4 py-2 text-xs text-[#F4F6FA] placeholder-[#8F9AAF] focus:border-[#E8546A] outline-none transition-colors"
              />
            </div>

            {/* Location Selector Button (Section 25 & 26) */}
            <button
              type="button"
              onClick={() => setIsLocationModalOpen(true)}
              className="px-3.5 py-2 rounded-[8px] bg-[#151B27] hover:bg-[#1A2130] border border-[#273142] hover:border-[#E8546A] text-xs font-semibold text-[#F4F6FA] transition-colors flex items-center gap-1.5 flex-shrink-0 shadow-sm"
              title="Change search city, neighborhood, or postal code"
            >
              <MapPin className="w-3.5 h-3.5 text-[#E8546A]" />
              <span className="max-w-[120px] truncate">{currentLocation.label}</span>
              <span className="text-[10px] text-[#8F9AAF]">Change</span>
            </button>

            <button
              type="submit"
              className="px-5 py-2 bg-[#E8546A] hover:bg-[#F06A7D] active:bg-[#C94358] text-white text-xs font-bold rounded-[8px] transition-all shadow-md flex-shrink-0"
            >
              Search
            </button>
          </form>

          {/* Quick Filters: Category, Sort, Mobile Toggle (Section 24) */}
          <div className="flex items-center gap-2 w-full lg:w-auto overflow-x-auto pb-1 lg:pb-0">
            {/* Category Filter */}
            <select
              value={category}
              onChange={(e) => {
                setCategory(e.target.value);
                setPage(1);
              }}
              className="bg-[#151B27] border border-[#273142] rounded-[8px] px-3 py-2 text-xs text-[#F4F6FA] outline-none cursor-pointer"
            >
              {categories.map((cat) => (
                <option key={cat} value={cat} className="bg-[#111620]">
                  {cat === 'All' ? 'All Categories' : cat}
                </option>
              ))}
            </select>

            {/* Sort Dropdown (Section 34) */}
            <div className="flex items-center gap-1.5 bg-[#151B27] border border-[#273142] rounded-[8px] px-2.5 py-1.5 flex-shrink-0">
              <ArrowUpDown className="w-3.5 h-3.5 text-[#8F9AAF]" />
              <select
                value={sort}
                onChange={(e) => setSort(e.target.value)}
                className="bg-transparent text-xs font-medium text-[#F4F6FA] outline-none cursor-pointer"
              >
                <option value="Recommended" className="bg-[#111620]">Recommended (Rank)</option>
                <option value="Nearest" className="bg-[#111620]">Nearest Distance</option>
                <option value="Top rated" className="bg-[#111620]">Top Rated</option>
                <option value="Most reviewed" className="bg-[#111620]">Most Reviewed</option>
                <option value="Lowest price" className="bg-[#111620]">Lowest Starting Price</option>
                <option value="Highest price" className="bg-[#111620]">Highest Starting Price</option>
              </select>
            </div>

            {/* Mobile View Toggle: List vs Map (Section 24) */}
            <div className="lg:hidden flex bg-[#151B27] border border-[#273142] rounded-[8px] p-0.5 flex-shrink-0">
              <button
                type="button"
                onClick={() => setMobileView('list')}
                className={`p-1.5 rounded-[6px] text-xs flex items-center gap-1 ${
                  mobileView === 'list' ? 'bg-[#E8546A] text-white font-bold' : 'text-[#8F9AAF]'
                }`}
              >
                <ListIcon className="w-3.5 h-3.5" /> List
              </button>
              <button
                type="button"
                onClick={() => setMobileView('map')}
                className={`p-1.5 rounded-[6px] text-xs flex items-center gap-1 ${
                  mobileView === 'map' ? 'bg-[#E8546A] text-white font-bold' : 'text-[#8F9AAF]'
                }`}
              >
                <MapIcon className="w-3.5 h-3.5" /> Map
              </button>
            </div>
          </div>
        </div>

        {/* Search Intent Notification Pill (Section 33) */}
        {isIntentQuery && (
          <div className="max-w-7xl mx-auto mt-2 pt-2 border-t border-[#273142]/60 flex items-center gap-2 text-[11px] text-[#C3CAD6]">
            <Sparkles className="w-3.5 h-3.5 text-[#E8546A]" />
            <span>Search intent recognized: spatial proximity, service matching &amp; ranking active for query</span>
          </div>
        )}
      </div>

      {/* ================================================================= */}
      {/* 2. MAIN SPLIT LAYOUT: LIST | MAP (Section 24 Desktop & Mobile)     */}
      {/* ================================================================= */}
      <div className="flex-1 flex max-w-7xl w-full mx-auto p-4 sm:p-6 lg:p-8 gap-6">
        {/* Left Column: Results List */}
        <div
          className={`flex-1 flex flex-col space-y-4 ${
            mobileView === 'map' ? 'hidden lg:flex' : 'flex'
          }`}
        >
          {/* Result Count Headline */}
          <div className="flex items-center justify-between text-xs text-[#8F9AAF]">
            <span>
              Showing <strong className="text-[#F4F6FA]">{providers.length}</strong> of{' '}
              <strong className="text-[#F4F6FA]">{totalCount}</strong> verified venues in {currentLocation.city}
            </span>
            {sort === 'Recommended' && (
              <span className="flex items-center gap-1 text-[#E8546A] font-semibold text-[11px]">
                <Sparkles className="w-3 h-3" /> Section 34 Multi-Signal Ranking
              </span>
            )}
          </div>

          {/* Error State */}
          {error && (
            <div className="p-6 rounded-[16px] bg-[#1A2130] border border-[#F87171]/40 text-center space-y-2">
              <p className="text-sm font-bold text-[#F87171]">We couldn't load discovery results.</p>
              <button
                onClick={fetchProviders}
                className="px-4 py-2 bg-[#E8546A] hover:bg-[#F06A7D] text-white text-xs font-bold rounded-[8px]"
              >
                Retry
              </button>
            </div>
          )}

          {/* Loading Skeletons */}
          {isLoading && !error && (
            <div className="space-y-4">
              {[1, 2, 3].map((i) => (
                <div
                  key={i}
                  className="p-5 rounded-[16px] bg-[#111620] border border-[#273142] animate-pulse space-y-3"
                >
                  <div className="h-4 bg-[#1A2130] rounded w-1/3" />
                  <div className="h-3 bg-[#1A2130] rounded w-1/2" />
                  <div className="h-10 bg-[#1A2130] rounded w-full" />
                </div>
              ))}
            </div>
          )}

          {/* Empty State */}
          {!isLoading && !error && providers.length === 0 && (
            <div className="py-16 p-8 rounded-[16px] bg-[#111620] border border-[#273142] text-center space-y-4">
              <div className="w-16 h-16 rounded-[12px] bg-[#1A2130] border border-[#273142] flex items-center justify-center mx-auto text-[#8F9AAF]">
                <Compass className="w-8 h-8" />
              </div>
              <h3 className="font-heading text-lg font-bold text-[#F4F6FA]">No providers found in this area</h3>
              <p className="text-xs text-[#C3CAD6] max-w-sm mx-auto">
                Try expanding your search radius, selecting a neighboring city, or resetting category filters.
              </p>
              <button
                onClick={() => {
                  setCurrentLocation({ label: 'Ahmedabad', city: 'Ahmedabad', lat: 23.0225, lng: 72.5714 });
                  setCategory('All');
                  setQuery('');
                  setPage(1);
                }}
                className="px-5 py-2.5 bg-[#E8546A] hover:bg-[#F06A7D] text-white text-xs font-bold rounded-[8px] transition-colors"
              >
                Reset Filters
              </button>
            </div>
          )}

          {/* Provider Cards List with List-Map Synchronization (Section 28) */}
          {!isLoading &&
            !error &&
            providers.map((p) => {
              const isSelected = p.id === selectedProviderId;

              return (
                <div
                  key={p.id}
                  ref={(el) => { cardRefs.current[p.id] = el; }}
                  onClick={() => setSelectedProviderId(p.id)}
                  className={`p-5 rounded-[16px] bg-[#111620] border transition-all cursor-pointer flex flex-col sm:flex-row gap-5 ${
                    isSelected
                      ? 'border-[#E8546A] shadow-xl shadow-[#E8546A]/10 bg-[#151B27]'
                      : 'border-[#273142] hover:border-[#344054] hover:bg-[#151B27]'
                  }`}
                >
                  {/* Provider Logo / Cover */}
                  <div className="w-full sm:w-36 h-28 rounded-[12px] bg-[#1A2130] border border-[#273142] overflow-hidden flex-shrink-0 relative">
                    <img
                      src={
                        p.coverImageUrl ||
                        'https://images.unsplash.com/photo-1540555700478-4be289fbecef?auto=format&fit=crop&w=400&q=80'
                      }
                      alt={p.name}
                      className="w-full h-full object-cover"
                    />
                    <div className="absolute top-2 right-2 bg-[#090B10]/85 backdrop-blur-md px-1.5 py-0.5 rounded-[6px] text-[10px] font-bold text-[#FBBF24] flex items-center gap-0.5">
                      <Star className="w-3 h-3 fill-current" /> {p.rating}
                    </div>
                  </div>

                  {/* Card Details */}
                  <div className="flex-1 flex flex-col justify-between">
                    <div>
                      <div className="flex items-center justify-between gap-2 mb-1">
                        <div className="flex items-center gap-1.5 truncate">
                          <h3 className="font-heading text-base font-bold text-[#F4F6FA] truncate">
                            {p.name}
                          </h3>
                          {p.isVerified && (
                            <CheckCircle className="w-3.5 h-3.5 text-[#34D399] flex-shrink-0" />
                          )}
                        </div>
                        {p.distanceKm != null && (
                          <span className="text-[11px] font-semibold text-[#34D399] whitespace-nowrap">
                            {p.distanceKm} km away
                          </span>
                        )}
                      </div>

                      <div className="flex items-center gap-2 text-xs text-[#8F9AAF] mb-2">
                        <span>{p.category}</span>
                        <span>&bull;</span>
                        <span className="truncate">{p.address}</span>
                      </div>

                      {/* Services Summary Tags */}
                      <div className="flex flex-wrap gap-1 mb-2">
                        {p.servicesSummary.map((s) => (
                          <span
                            key={s}
                            className="px-2 py-0.5 rounded-[6px] bg-[#1A2130] text-[10px] text-[#C3CAD6] border border-[#273142]"
                          >
                            {s}
                          </span>
                        ))}
                      </div>
                    </div>

                    <div className="pt-3 border-t border-[#273142] flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        <div>
                          <span className="text-[10px] text-[#8F9AAF] block">From</span>
                          <span className="text-sm font-bold text-[#34D399]">₹{p.startingPrice}</span>
                        </div>
                        <div className="text-[10px] text-[#8F9AAF]">
                          <span className="block">Next slot</span>
                          <span className="text-[#F4F6FA] font-semibold flex items-center gap-1">
                            <Clock className="w-3 h-3 text-[#FBBF24]" /> {p.nextAvailableSlot || '10:30 AM'}
                          </span>
                        </div>
                      </div>

                      <div className="flex items-center gap-2">
                        <Link
                          to={`/business/${p.slug}`}
                          className="px-3 py-1.5 rounded-[8px] border border-[#273142] hover:bg-[#1A2130] text-xs font-semibold text-[#F4F6FA] transition-colors"
                        >
                          View
                        </Link>
                        <Link
                          to={`/book/${p.slug}`}
                          className="px-3.5 py-1.5 rounded-[8px] bg-[#E8546A] hover:bg-[#F06A7D] text-xs font-bold text-white transition-all shadow-md shadow-[#E8546A]/20"
                        >
                          Book Now
                        </Link>
                      </div>
                    </div>
                  </div>
                </div>
              );
            })}

          {/* Pagination Controls */}
          {totalPages > 1 && (
            <div className="pt-4 flex items-center justify-between border-t border-[#273142]">
              <button
                type="button"
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="px-3 py-1.5 rounded-[8px] bg-[#151B27] border border-[#273142] text-xs font-semibold text-[#F4F6FA] disabled:opacity-40 flex items-center gap-1"
              >
                <ChevronLeft className="w-3.5 h-3.5" /> Previous
              </button>
              <span className="text-xs text-[#8F9AAF]">
                Page {page} of {totalPages}
              </span>
              <button
                type="button"
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page >= totalPages}
                className="px-3 py-1.5 rounded-[8px] bg-[#151B27] border border-[#273142] text-xs font-semibold text-[#F4F6FA] disabled:opacity-40 flex items-center gap-1"
              >
                Next <ChevronRight className="w-3.5 h-3.5" />
              </button>
            </div>
          )}
        </div>

        {/* Right Column: Real Interactive Map (Section 28) */}
        <div
          className={`w-full lg:w-[480px] xl:w-[540px] flex-shrink-0 h-[650px] sticky top-36 ${
            mobileView === 'list' ? 'hidden lg:block' : 'block h-[calc(100vh-140px)]'
          }`}
        >
          <InteractiveMap
            providers={providers}
            selectedProviderId={selectedProviderId}
            onSelectProvider={handleSelectProvider}
            onSearchThisArea={fetchProviders}
            center={mapCenter}
            userLocation={userGpsCoords}
          />
        </div>
      </div>

      {/* Location Experience Modal (Section 25, 26, 27) */}
      <LocationControlModal
        isOpen={isLocationModalOpen}
        onClose={() => setIsLocationModalOpen(false)}
        currentSelection={currentLocation}
        onSelectLocation={handleLocationSelect}
      />
    </div>
  );
};
