import React, { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useCart } from '../../app/providers/CartContext';
import {
  Search,
  MapPin,
  Sparkles,
  Scissors,
  Activity,
  Dumbbell,
  Camera,
  Briefcase,
  Star,
  Clock,
  ShieldCheck,
  ChevronRight,
  ArrowRight,
  Package,
  Plus,
  Navigation,
  CheckCircle,
} from 'lucide-react';

export const HomePage: React.FC = () => {
  const navigate = useNavigate();
  const { addItem } = useCart();

  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCity, setSelectedCity] = useState('Ahmedabad');
  const [locationState, setLocationState] = useState<'IDLE' | 'REQUESTING' | 'GRANTED' | 'DENIED'>('IDLE');

  const popularCities = ['Ahmedabad', 'Mumbai', 'Bangalore', 'Surat', 'Rajkot'];

  // Categories (Section 18 & 23: Lucide icons, no emoji)
  const categories = [
    { name: 'Wellness & Spas', query: 'Wellness', icon: Sparkles, desc: 'Therapeutic massage, holistic recovery & aesthetics', count: 18 },
    { name: 'Hair & Styling', query: 'Beauty', icon: Scissors, desc: 'Master stylists, balayage, keratin & treatments', count: 24 },
    { name: 'Healthcare & Dental', query: 'Healthcare', icon: Activity, desc: 'Cosmetic dentistry, dermatology & physiotherapy', count: 12 },
    { name: 'Fitness & Reformer', query: 'Fitness', icon: Dumbbell, desc: 'Reformer pilates, functional strength & yoga', count: 15 },
    { name: 'Photography Studios', query: 'Photography', icon: Camera, desc: 'Executive headshots, portraits & editorial', count: 9 },
    { name: 'Professional Services', query: 'Professional', icon: Briefcase, desc: 'Legal counsel, notary & financial advisory', count: 8 },
  ];

  // Near You (Section 23)
  const nearYouProviders = [
    {
      id: '11111111-1111-1111-1111-111111111111',
      name: 'Aura Wellness & Spa',
      slug: 'aura-wellness-ahmedabad',
      category: 'Wellness',
      rating: 4.9,
      reviewCount: 38,
      distanceKm: 1.2,
      area: 'Bodakdev',
      city: 'Ahmedabad',
      startingPrice: 1500,
      nextAvailable: '10:30 AM',
      isVerified: true,
      image: 'https://images.unsplash.com/photo-1540555700478-4be289fbecef?auto=format&fit=crop&w=600&q=80',
      services: ['Aromatherapy Massage', 'Hydrafacial', 'Shirodhara'],
    },
    {
      id: '44444444-4444-4444-4444-444444444444',
      name: 'Apex Athletic Club',
      slug: 'apex-athletic-ahmedabad',
      category: 'Fitness',
      rating: 4.9,
      reviewCount: 44,
      distanceKm: 2.4,
      area: 'Sindhu Bhavan',
      city: 'Ahmedabad',
      startingPrice: 1200,
      nextAvailable: '11:15 AM',
      isVerified: true,
      image: 'https://images.unsplash.com/photo-1534438327276-14e5300c3a48?auto=format&fit=crop&w=600&q=80',
      services: ['Reformer Pilates', 'Strength Assessment', 'Mobility Session'],
    },
  ];

  // Available Today (Section 23)
  const availableTodayProviders = [
    {
      id: '22222222-2222-2222-2222-222222222222',
      name: 'Glow Hair Lounge',
      slug: 'glow-hair-lounge-mumbai',
      category: 'Beauty',
      rating: 4.8,
      reviewCount: 52,
      area: 'Bandra West',
      city: 'Mumbai',
      startingPrice: 1800,
      nextAvailableSlot: 'Today 01:15 PM',
      isVerified: true,
      image: 'https://images.unsplash.com/photo-1560066984-138dadb4c035?auto=format&fit=crop&w=600&q=80',
      services: ['Balayage & Gloss', 'Keratin Ritual', 'Precision Cut'],
    },
    {
      id: '33333333-3333-3333-3333-333333333333',
      name: 'Urban Smile Dental Care',
      slug: 'urban-smile-dental-bangalore',
      category: 'Healthcare',
      rating: 4.7,
      reviewCount: 29,
      area: 'Indiranagar',
      city: 'Bangalore',
      startingPrice: 2000,
      nextAvailableSlot: 'Today 02:45 PM',
      isVerified: true,
      image: 'https://images.unsplash.com/photo-1629909613654-28e377c37b09?auto=format&fit=crop&w=600&q=80',
      services: ['Laser Teeth Whitening', 'Smile Analysis', 'Deep Polish'],
    },
  ];

  // Top Rated (Section 23)
  const topRatedProviders = [
    {
      name: 'Aura Wellness & Spa',
      slug: 'aura-wellness-ahmedabad',
      category: 'Wellness',
      rating: 4.95,
      reviewCount: 142,
      location: 'Bodakdev, Ahmedabad',
      price: '₹1,500',
      badge: '98% Client Satisfaction',
      image: 'https://images.unsplash.com/photo-1540555700478-4be289fbecef?auto=format&fit=crop&w=600&q=80',
    },
    {
      name: 'Apex Athletic Club',
      slug: 'apex-athletic-ahmedabad',
      category: 'Fitness',
      rating: 4.92,
      reviewCount: 118,
      location: 'Sindhu Bhavan, Ahmedabad',
      price: '₹1,200',
      badge: 'Certified Master Trainers',
      image: 'https://images.unsplash.com/photo-1534438327276-14e5300c3a48?auto=format&fit=crop&w=600&q=80',
    },
    {
      name: 'Glow Hair Lounge',
      slug: 'glow-hair-lounge-mumbai',
      category: 'Beauty',
      rating: 4.88,
      reviewCount: 96,
      location: 'Bandra West, Mumbai',
      price: '₹1,800',
      badge: 'Master Colorist Award',
      image: 'https://images.unsplash.com/photo-1560066984-138dadb4c035?auto=format&fit=crop&w=600&q=80',
    },
  ];

  // Popular Services (Section 23)
  const popularServices = [
    {
      name: 'Holistic Aromatherapy Massage',
      provider: 'Aura Wellness & Spa',
      slug: 'aura-wellness-ahmedabad',
      duration: '60 mins',
      price: '₹2,400',
      category: 'Wellness',
      bookingUrl: '/business/aura-wellness-ahmedabad',
    },
    {
      name: 'Balayage & Conditioning Gloss',
      provider: 'Glow Hair Lounge',
      slug: 'glow-hair-lounge-mumbai',
      duration: '120 mins',
      price: '₹4,200',
      category: 'Beauty',
      bookingUrl: '/business/glow-hair-lounge-mumbai',
    },
    {
      name: 'Laser Teeth Whitening Consultation',
      provider: 'Urban Smile Dental Care',
      slug: 'urban-smile-dental-bangalore',
      duration: '45 mins',
      price: '₹3,500',
      category: 'Healthcare',
      bookingUrl: '/business/urban-smile-dental-bangalore',
    },
    {
      name: 'Reformer Pilates Induction',
      provider: 'Apex Athletic Club',
      slug: 'apex-athletic-ahmedabad',
      duration: '50 mins',
      price: '₹1,500',
      category: 'Fitness',
      bookingUrl: '/business/apex-athletic-ahmedabad',
    },
  ];

  // Boutique Products (Section 23)
  const retailProducts = [
    {
      id: 'p-1',
      tenantId: '11111111-1111-1111-1111-111111111111',
      providerName: 'Aura Wellness & Spa',
      name: 'Botanical Keratin Restorative Hair Mask (250ml)',
      price: 1299,
      currency: '₹',
      availableQuantity: 23,
      imageUrl: 'https://images.unsplash.com/photo-1556228720-195a672e8a03?auto=format&fit=crop&w=600&q=80',
    },
    {
      id: 'p-2',
      tenantId: '11111111-1111-1111-1111-111111111111',
      providerName: 'Aura Wellness & Spa',
      name: 'Organic Lavender & Eucalyptus Massage Oil (100ml)',
      price: 600,
      currency: '₹',
      availableQuantity: 39,
      imageUrl: 'https://images.unsplash.com/photo-1608248597359-24755f190696?auto=format&fit=crop&w=600&q=80',
    },
    {
      id: 'p-3',
      tenantId: '11111111-1111-1111-1111-111111111111',
      providerName: 'Aura Wellness & Spa',
      name: 'Hydrating Peptide Finishing Mist (150ml)',
      price: 850,
      currency: '₹',
      availableQuantity: 15,
      imageUrl: 'https://images.unsplash.com/photo-1571781926291-c477ebfd024b?auto=format&fit=crop&w=600&q=80',
    },
  ];

  // Recently Viewed (Section 23: Deterministic client history)
  const recentlyViewed = [
    {
      name: 'Aura Wellness & Spa',
      slug: 'aura-wellness-ahmedabad',
      category: 'Wellness',
      city: 'Ahmedabad',
      rating: 4.9,
    },
    {
      name: 'Glow Hair Lounge',
      slug: 'glow-hair-lounge-mumbai',
      category: 'Beauty',
      city: 'Mumbai',
      rating: 4.8,
    },
  ];

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    const params = new URLSearchParams();
    if (searchQuery) params.set('q', searchQuery);
    if (selectedCity && selectedCity !== 'All') params.set('city', selectedCity);
    navigate(`/discover?${params.toString()}`);
  };

  const handleUseLocation = () => {
    if (!navigator.geolocation) {
      alert('Geolocation is not supported by your browser.');
      return;
    }

    setLocationState('REQUESTING');
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setLocationState('GRANTED');
        navigate(`/discover?lat=${pos.coords.latitude}&lng=${pos.coords.longitude}&radius=25`);
      },
      () => {
        setLocationState('DENIED');
        alert('Location access was denied. You can select your city manually from the search bar.');
      }
    );
  };

  return (
    <div className="space-y-16 pb-24">
      {/* ================================================================= */}
      {/* 1. HERO CONSOLE: "What are you looking for?" (Section 23)          */}
      {/* ================================================================= */}
      <section className="relative overflow-hidden pt-12 pb-20 px-4 sm:px-6 lg:px-8 border-b border-[#273142] bg-gradient-to-b from-[#111620] via-[#090B10] to-[#090B10]">
        <div className="max-w-5xl mx-auto text-center space-y-6">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-[8px] bg-[#1A2130] border border-[#273142] text-[12px] font-medium text-[#E8546A] tracking-wide">
            <Sparkles className="w-3.5 h-3.5" />
            <span>Multi-Vendor Discovery Platform</span>
          </div>

          <h1 className="font-heading text-4xl sm:text-6xl font-bold text-[#F4F6FA] tracking-tight leading-tight">
            What are you looking for?
          </h1>

          <p className="max-w-2xl mx-auto text-sm sm:text-base text-[#C3CAD6] font-normal leading-relaxed">
            Discover verified local specialists, reserve calendar slots with guaranteed holds, and purchase curated retail formulations near you.
          </p>

          {/* Unified Search & Location Bar */}
          <form
            onSubmit={handleSearch}
            className="max-w-3xl mx-auto p-2 sm:p-2.5 rounded-[16px] bg-[#111620] border border-[#273142] shadow-xl flex flex-col sm:flex-row items-center gap-2"
          >
            {/* Treatment / Service Search */}
            <div className="flex-1 flex items-center gap-3 px-3 py-2 w-full">
              <Search className="w-4 h-4 text-[#8F9AAF] flex-shrink-0" />
              <input
                type="text"
                placeholder="Salons, massage, dentist, reformer pilates..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full bg-transparent text-[#F4F6FA] placeholder-[#8F9AAF] text-sm focus:outline-none"
              />
            </div>

            <div className="h-6 w-px bg-[#273142] hidden sm:block" />

            {/* City Selector */}
            <div className="flex items-center gap-2 px-3 py-2 w-full sm:w-auto">
              <MapPin className="w-4 h-4 text-[#E8546A] flex-shrink-0" />
              <select
                value={selectedCity}
                onChange={(e) => setSelectedCity(e.target.value)}
                className="bg-transparent text-[#F4F6FA] text-xs font-semibold focus:outline-none cursor-pointer"
              >
                {popularCities.map((city) => (
                  <option key={city} value={city} className="bg-[#111620] text-[#F4F6FA]">
                    {city}
                  </option>
                ))}
              </select>
            </div>

            {/* Geolocation Button */}
            <button
              type="button"
              onClick={handleUseLocation}
              disabled={locationState === 'REQUESTING'}
              className="w-full sm:w-auto px-3.5 py-2.5 rounded-[8px] bg-[#1A2130] hover:bg-[#273142] border border-[#273142] text-xs font-medium text-[#C3CAD6] hover:text-[#F4F6FA] transition-colors flex items-center justify-center gap-1.5"
              title="Use current GPS position"
            >
              <Navigation className="w-3.5 h-3.5 text-[#34D399]" />
              <span>{locationState === 'REQUESTING' ? 'Locating...' : 'Near me'}</span>
            </button>

            {/* Search Submit */}
            <button
              type="submit"
              className="w-full sm:w-auto px-6 py-2.5 rounded-[8px] bg-[#E8546A] hover:bg-[#F06A7D] active:bg-[#C94358] text-white text-xs font-bold transition-all shadow-md flex items-center justify-center gap-2"
            >
              <span>Explore</span>
              <ArrowRight className="w-3.5 h-3.5" />
            </button>
          </form>

          {/* Popular Search Shortcuts */}
          <div className="flex items-center justify-center gap-2 text-xs text-[#8F9AAF] flex-wrap pt-2">
            <span className="font-medium text-[#C3CAD6]">Popular:</span>
            {['Aromatherapy', 'Balayage', 'Teeth Whitening', 'Pilates', 'Facial'].map((tag) => (
              <button
                key={tag}
                type="button"
                onClick={() => {
                  setSearchQuery(tag);
                  navigate(`/discover?q=${encodeURIComponent(tag)}&city=${encodeURIComponent(selectedCity)}`);
                }}
                className="px-2.5 py-1 rounded-[6px] bg-[#151B27] border border-[#273142] text-[#C3CAD6] hover:text-[#F4F6FA] hover:border-[#344054] text-[11px] transition-colors"
              >
                {tag}
              </button>
            ))}
          </div>
        </div>
      </section>

      {/* ================================================================= */}
      {/* 2. CATEGORIES GRID (Section 18 & 23: No Emojis, Lucide Icons)     */}
      {/* ================================================================= */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="font-heading text-2xl font-bold text-[#F4F6FA]">Browse Marketplace Categories</h2>
            <p className="text-xs text-[#C3CAD6]">Explore certified businesses categorized by discipline and practice</p>
          </div>
          <Link
            to="/discover"
            className="text-xs font-semibold text-[#E8546A] hover:text-[#F06A7D] flex items-center gap-1 transition-colors"
          >
            <span>View all</span>
            <ChevronRight className="w-3.5 h-3.5" />
          </Link>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {categories.map((cat) => {
            const Icon = cat.icon;
            return (
              <Link
                key={cat.name}
                to={`/category/${cat.query.toLowerCase()}`}
                className="bg-[#111620] border border-[#273142] hover:border-[#344054] rounded-[12px] p-5 transition-all group flex items-start gap-4"
              >
                <div className="w-10 h-10 rounded-[8px] bg-[#1A2130] border border-[#273142] flex items-center justify-center text-[#E8546A] flex-shrink-0 group-hover:scale-105 transition-transform">
                  <Icon className="w-5 h-5" />
                </div>
                <div className="space-y-1">
                  <div className="flex items-center justify-between">
                    <h3 className="font-heading font-bold text-sm text-[#F4F6FA] group-hover:text-[#E8546A] transition-colors">
                      {cat.name}
                    </h3>
                    <span className="text-[11px] font-mono text-[#8F9AAF]">{cat.count}</span>
                  </div>
                  <p className="text-xs text-[#C3CAD6] line-clamp-1 leading-relaxed">
                    {cat.desc}
                  </p>
                </div>
              </Link>
            );
          })}
        </div>
      </section>

      {/* ================================================================= */}
      {/* 3. NEAR YOU (Section 23: Location-Aware Providers)                */}
      {/* ================================================================= */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="font-heading text-2xl font-bold text-[#F4F6FA]">Near You in {selectedCity}</h2>
            <p className="text-xs text-[#C3CAD6]">Local operating storefronts sorted by geographical proximity</p>
          </div>
          <Link
            to={`/discover?city=${encodeURIComponent(selectedCity)}&sort=Nearest`}
            className="text-xs font-semibold text-[#E8546A] hover:text-[#F06A7D] flex items-center gap-1"
          >
            <span>See more nearby</span>
            <ChevronRight className="w-3.5 h-3.5" />
          </Link>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {nearYouProviders.map((provider) => (
            <div
              key={provider.id}
              className="bg-[#111620] border border-[#273142] hover:border-[#344054] rounded-[16px] overflow-hidden flex flex-col sm:flex-row transition-all group"
            >
              <div className="sm:w-48 h-48 sm:h-auto bg-[#1A2130] relative overflow-hidden flex-shrink-0">
                <img
                  src={provider.image}
                  alt={provider.name}
                  className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                />
                <span className="absolute top-2.5 left-2.5 px-2 py-0.5 rounded-[6px] bg-[#090B10]/85 backdrop-blur-sm text-[10px] font-bold text-[#34D399]">
                  {provider.distanceKm} km away
                </span>
              </div>

              <div className="p-5 flex-1 flex flex-col justify-between space-y-4">
                <div className="space-y-1.5">
                  <div className="flex items-center justify-between">
                    <span className="text-[10px] font-bold uppercase tracking-wider text-[#8F9AAF]">
                      {provider.category} &bull; {provider.area}
                    </span>
                    <span className="flex items-center gap-1 text-xs text-[#FBBF24] font-semibold">
                      <Star className="w-3 h-3 fill-current" />
                      <span>{provider.rating}</span>
                      <span className="text-[#8F9AAF] font-normal">({provider.reviewCount})</span>
                    </span>
                  </div>

                  <h3 className="font-heading font-bold text-base text-[#F4F6FA] group-hover:text-[#E8546A] transition-colors">
                    {provider.name}
                  </h3>

                  <p className="text-xs text-[#C3CAD6] line-clamp-1">
                    {provider.services.join(' &bull; ')}
                  </p>

                  <div className="flex items-center gap-2 text-[11px] text-[#34D399]">
                    <Clock className="w-3 h-3" />
                    <span>Next available: {provider.nextAvailable}</span>
                  </div>
                </div>

                <div className="pt-3 border-t border-[#273142] flex items-center justify-between">
                  <span className="text-xs text-[#C3CAD6]">
                    From <strong className="text-[#F4F6FA]">₹{provider.startingPrice}</strong>
                  </span>
                  <Link
                    to={`/business/${provider.slug}`}
                    className="px-4 py-2 rounded-[8px] bg-[#1A2130] hover:bg-[#E8546A] hover:text-white text-xs font-semibold text-[#F4F6FA] transition-colors"
                  >
                    View Storefront
                  </Link>
                </div>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* ================================================================= */}
      {/* 4. AVAILABLE TODAY (Section 23: Real-Time Openings)               */}
      {/* ================================================================= */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="font-heading text-2xl font-bold text-[#F4F6FA]">Available Today</h2>
            <p className="text-xs text-[#C3CAD6]">Verified specialists with confirmed open appointment windows</p>
          </div>
          <Link
            to="/discover?availability=Today"
            className="text-xs font-semibold text-[#E8546A] hover:text-[#F06A7D] flex items-center gap-1"
          >
            <span>View all slots</span>
            <ChevronRight className="w-3.5 h-3.5" />
          </Link>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {availableTodayProviders.map((provider) => (
            <div
              key={provider.id}
              className="bg-[#111620] border border-[#273142] rounded-[16px] p-5 flex items-center justify-between gap-4"
            >
              <div className="flex items-center gap-4">
                <img
                  src={provider.image}
                  alt={provider.name}
                  className="w-16 h-16 rounded-[12px] object-cover flex-shrink-0"
                />
                <div className="space-y-1">
                  <div className="flex items-center gap-1.5">
                    <span className="font-heading font-bold text-sm text-[#F4F6FA]">{provider.name}</span>
                    <ShieldCheck className="w-3.5 h-3.5 text-[#34D399]" />
                  </div>
                  <p className="text-xs text-[#C3CAD6]">{provider.area}, {provider.city}</p>
                  <p className="text-[11px] font-mono text-[#34D399] flex items-center gap-1">
                    <Clock className="w-3 h-3" /> {provider.nextAvailableSlot}
                  </p>
                </div>
              </div>

              <Link
                to={`/business/${provider.slug}`}
                className="px-4 py-2 rounded-[8px] bg-[#E8546A] hover:bg-[#F06A7D] text-white text-xs font-bold transition-all shadow-md flex-shrink-0"
              >
                Book Now
              </Link>
            </div>
          ))}
        </div>
      </section>

      {/* ================================================================= */}
      {/* 5. TOP RATED & RECOMMENDED (Section 23)                            */}
      {/* ================================================================= */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-6">
        <div>
          <h2 className="font-heading text-2xl font-bold text-[#F4F6FA]">Top Rated &amp; Recommended</h2>
          <p className="text-xs text-[#C3CAD6]">High-satisfaction businesses ranked by verified reviews and booking reliability</p>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {topRatedProviders.map((item) => (
            <div
              key={item.name}
              className="bg-[#111620] border border-[#273142] rounded-[16px] overflow-hidden flex flex-col justify-between group"
            >
              <div className="h-44 bg-[#1A2130] overflow-hidden relative">
                <img
                  src={item.image}
                  alt={item.name}
                  className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                />
                <span className="absolute top-2.5 left-2.5 px-2.5 py-0.5 rounded-[6px] bg-[#090B10]/85 backdrop-blur-sm text-[10px] font-bold text-[#FBBF24]">
                  {item.badge}
                </span>
              </div>

              <div className="p-5 space-y-3">
                <div className="flex items-center justify-between">
                  <h3 className="font-heading font-bold text-base text-[#F4F6FA] group-hover:text-[#E8546A] transition-colors">
                    {item.name}
                  </h3>
                  <div className="flex items-center gap-1 text-xs text-[#FBBF24] font-semibold">
                    <Star className="w-3.5 h-3.5 fill-current" />
                    <span>{item.rating}</span>
                  </div>
                </div>

                <p className="text-xs text-[#C3CAD6]">{item.location}</p>

                <div className="pt-3 border-t border-[#273142] flex items-center justify-between">
                  <span className="text-xs text-[#C3CAD6]">
                    Starting at <strong className="text-[#F4F6FA]">{item.price}</strong>
                  </span>
                  <Link
                    to={`/business/${item.slug}`}
                    className="px-3.5 py-1.5 rounded-[8px] bg-[#1A2130] hover:bg-[#273142] text-xs font-semibold text-[#F4F6FA] transition-colors"
                  >
                    Explore
                  </Link>
                </div>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* ================================================================= */}
      {/* 6. POPULAR SERVICES (Section 23: Direct Service Scheduling)       */}
      {/* ================================================================= */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="font-heading text-2xl font-bold text-[#F4F6FA]">Trending Treatments &amp; Services</h2>
            <p className="text-xs text-[#C3CAD6]">Frequently reserved appointments with master specialists</p>
          </div>
          <Link
            to="/discover"
            className="text-xs font-semibold text-[#E8546A] hover:text-[#F06A7D] flex items-center gap-1"
          >
            <span>All services</span>
            <ChevronRight className="w-3.5 h-3.5" />
          </Link>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {popularServices.map((srv) => (
            <div
              key={srv.name}
              className="bg-[#111620] border border-[#273142] rounded-[12px] p-5 flex flex-col justify-between space-y-3"
            >
              <div className="space-y-1">
                <span className="text-[10px] font-mono text-[#8F9AAF] uppercase">{srv.category}</span>
                <h3 className="font-heading font-bold text-sm text-[#F4F6FA] line-clamp-1">{srv.name}</h3>
                <p className="text-xs text-[#C3CAD6] truncate">{srv.provider}</p>
                <p className="text-[11px] font-mono text-[#34D399]">{srv.duration}</p>
              </div>

              <div className="pt-2 border-t border-[#273142] flex items-center justify-between">
                <span className="font-heading font-bold text-sm text-[#F4F6FA]">{srv.price}</span>
                <Link
                  to={srv.bookingUrl}
                  className="px-3 py-1.5 rounded-[8px] bg-[#1A2130] hover:bg-[#E8546A] hover:text-white text-xs font-semibold text-[#F4F6FA] transition-colors"
                >
                  Book
                </Link>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* ================================================================= */}
      {/* 7. PRODUCTS SHOWCASE (Section 23: Boutique Retail)                */}
      {/* ================================================================= */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="font-heading text-2xl font-bold text-[#F4F6FA]">Boutique Care Formulations</h2>
            <p className="text-xs text-[#C3CAD6]">Salon-grade retail products available for direct fulfillment or pickup</p>
          </div>
          <Link
            to="/discover?type=products"
            className="text-xs font-semibold text-[#E8546A] hover:text-[#F06A7D] flex items-center gap-1"
          >
            <span>Explore catalog</span>
            <ChevronRight className="w-3.5 h-3.5" />
          </Link>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-6">
          {retailProducts.map((prod) => (
            <div
              key={prod.id}
              className="bg-[#111620] border border-[#273142] rounded-[16px] overflow-hidden flex flex-col justify-between"
            >
              <div className="h-44 bg-[#1A2130] overflow-hidden">
                <img src={prod.imageUrl} alt={prod.name} className="w-full h-full object-cover" />
              </div>
              <div className="p-5 space-y-3 flex-1 flex flex-col justify-between">
                <div className="space-y-1">
                  <p className="text-[10px] text-[#8F9AAF] uppercase">{prod.providerName}</p>
                  <h3 className="font-heading font-bold text-sm text-[#F4F6FA] line-clamp-2">{prod.name}</h3>
                </div>
                <div className="pt-3 border-t border-[#273142] flex items-center justify-between">
                  <span className="font-heading font-bold text-base text-[#F4F6FA]">
                    {prod.currency}{prod.price}
                  </span>
                  <button
                    onClick={() => {
                      addItem({
                        productId: prod.id,
                        tenantId: prod.tenantId,
                        providerName: prod.providerName,
                        name: prod.name,
                        price: prod.price,
                        currency: prod.currency,
                        imageUrl: prod.imageUrl,
                        maxStock: prod.availableQuantity,
                      });
                    }}
                    className="px-3 py-1.5 rounded-[8px] bg-[#FBBF24] hover:bg-[#F59E0B] text-black text-xs font-bold transition-all flex items-center gap-1 shadow-sm"
                  >
                    <Plus className="w-3.5 h-3.5" />
                    <span>Add to Cart</span>
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* ================================================================= */}
      {/* 8. RECENTLY VIEWED (Section 23)                                   */}
      {/* ================================================================= */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-4">
        <h3 className="text-xs font-bold uppercase tracking-wider text-[#8F9AAF]">Recently Viewed</h3>
        <div className="flex items-center gap-3 overflow-x-auto pb-2">
          {recentlyViewed.map((item) => (
            <Link
              key={item.name}
              to={`/business/${item.slug}`}
              className="px-4 py-2.5 rounded-[8px] bg-[#111620] border border-[#273142] hover:border-[#344054] text-xs font-semibold text-[#F4F6FA] transition-colors flex items-center gap-2 whitespace-nowrap"
            >
              <span>{item.name}</span>
              <span className="text-[#8F9AAF] font-normal">&bull; {item.city}</span>
              <span className="text-[#FBBF24] font-mono">★ {item.rating}</span>
            </Link>
          ))}
        </div>
      </section>
    </div>
  );
};
