import React, { useState, useEffect } from 'react';
import { useParams, Link, useNavigate, useLocation } from 'react-router-dom';
import { discoveryApi, ProviderStorefrontData } from '../../services/api/discovery';
import { reviewsApi, ReviewItem } from '../../services/api/reviews';
import { useCart } from '../../app/providers/CartContext';
import { useAuth } from '../../app/providers/AuthProvider';
import {
  MapPin,
  Star,
  ShieldCheck,
  Clock,
  Phone,
  Globe,
  Scissors,
  Package,
  Users,
  MessageSquare,
  Info,
  Calendar,
  CheckCircle,
  AlertCircle,
  X,
  ChevronRight,
  Download,
  Share2,
  Heart,
  Plus,
  Sparkles,
} from 'lucide-react';
import { favoritesApi } from '../../services/api/favorites';

export const ProviderStorefrontPage: React.FC = () => {
  const { slug, businessSlug } = useParams<{ slug?: string; businessSlug?: string }>();
  const effectiveSlug = businessSlug || slug;
  const navigate = useNavigate();
  const location = useLocation();
  const { addItem } = useCart();
  const { user, token, isAuthenticated } = useAuth();

  const [storefront, setStorefront] = useState<ProviderStorefrontData | null>(null);
  const [reviews, setReviews] = useState<ReviewItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'services' | 'products' | 'team' | 'reviews' | 'about'>('services');
  const [isFavorited, setIsFavorited] = useState(false);

  // Booking Flow Modal State
  const [isBookingOpen, setIsBookingOpen] = useState(false);
  const [bookingStep, setBookingStep] = useState<1 | 2 | 3 | 4 | 5>(1);
  const [selectedService, setSelectedService] = useState<any>(null);
  const [selectedStaff, setSelectedStaff] = useState<any>(null);
  const [selectedDate, setSelectedDate] = useState<string>(() => {
    const d = new Date();
    d.setDate(d.getDate() + 1);
    return d.toISOString().split('T')[0];
  });
  const [selectedSlot, setSelectedSlot] = useState<string | null>(null);
  const [holdId, setHoldId] = useState<string | null>(null);
  const [holdTimer, setHoldTimer] = useState<number>(300); // 5 min hold
  const [bookingCustomerName, setBookingCustomerName] = useState('');
  const [bookingCustomerEmail, setBookingCustomerEmail] = useState('');
  const [bookingCustomerPhone, setBookingCustomerPhone] = useState('');
  const [bookingSubmitting, setBookingSubmitting] = useState(false);
  const [confirmedBooking, setConfirmedBooking] = useState<any>(null);

  // Write Review Modal State
  const [isReviewModalOpen, setIsReviewModalOpen] = useState(false);
  const [reviewRating, setReviewRating] = useState(5);
  const [reviewTitle, setReviewTitle] = useState('');
  const [reviewComment, setReviewComment] = useState('');
  const [reviewSubmitting, setReviewSubmitting] = useState(false);
  const [reviewError, setReviewError] = useState<string | null>(null);
  const [reviewSuccess, setReviewSuccess] = useState(false);

  // Available Time Slots (Simulated & Backend slot calculation)
  const availableTimeSlots = [
    { time: '09:00 AM', utc: '09:00:00Z', status: 'FREE' },
    { time: '09:45 AM', utc: '09:45:00Z', status: 'FREE' },
    { time: '10:30 AM', utc: '10:30:00Z', status: 'HELD' },
    { time: '11:15 AM', utc: '11:15:00Z', status: 'BOOKED' },
    { time: '01:00 PM', utc: '13:00:00Z', status: 'FREE' },
    { time: '02:00 PM', utc: '14:00:00Z', status: 'FREE' },
    { time: '03:15 PM', utc: '15:15:00Z', status: 'FREE' },
    { time: '04:00 PM', utc: '16:00:00Z', status: 'BOOKED' },
    { time: '05:00 PM', utc: '17:00:00Z', status: 'FREE' },
  ];

  // Fetch Storefront Data
  useEffect(() => {
    if (!effectiveSlug) return;
    setLoading(true);
    setError(null);

    discoveryApi
      .getProviderStorefront(effectiveSlug)
      .then((data) => {
        setStorefront(data);
        if (data.provider) {
          reviewsApi.getProviderReviews(data.provider.id).then(setReviews).catch(() => {});
        }
      })
      .catch((err) => {
        setError(err.message || 'Storefront could not be loaded.');
      })
      .finally(() => setLoading(false));
  }, [effectiveSlug]);

  // Open booking modal if on /book/:businessSlug
  useEffect(() => {
    if (location.pathname.startsWith('/book/')) {
      setIsBookingOpen(true);
    }
  }, [location.pathname]);

  // Sync user details to booking form
  useEffect(() => {
    if (user) {
      setBookingCustomerName(user.fullName || `${user.firstName || ''} ${user.lastName || ''}`.trim());
      setBookingCustomerEmail(user.email || '');
      setBookingCustomerPhone(user.phone || '');
    }
  }, [user]);

  // Hold Timer Countdown
  useEffect(() => {
    let interval: any = null;
    if (bookingStep === 4 && holdTimer > 0) {
      interval = setInterval(() => {
        setHoldTimer((prev) => (prev > 0 ? prev - 1 : 0));
      }, 1000);
    }
    return () => clearInterval(interval);
  }, [bookingStep, holdTimer]);

  const handleStartBooking = (service?: any, staff?: any) => {
    if (service) setSelectedService(service);
    if (staff) setSelectedStaff(staff);
    setBookingStep(service ? (staff ? 3 : 2) : 1);
    setIsBookingOpen(true);
  };

  const handleHoldSlot = async (slotTime: string) => {
    setSelectedSlot(slotTime);
    setHoldTimer(300); // 5 minutes
    setHoldId(`hold_${Date.now()}`);
    setBookingStep(4);
  };

  const handleConfirmBooking = async () => {
    if (!selectedService || !selectedSlot) return;
    setBookingSubmitting(true);

    try {
      // Mock hold and book persistence via API or fallback payload
      const [hoursStr, minutesPart] = selectedSlot.split(' ')[0].split(':');
      const isPm = selectedSlot.includes('PM') && parseInt(hoursStr) < 12;
      const hours = (parseInt(hoursStr) + (isPm ? 12 : 0)).toString().padStart(2, '0');
      const isoStart = `${selectedDate}T${hours}:${minutesPart}:00.000Z`;

      const reference = `BL-${Math.floor(100000 + Math.random() * 900000)}`;

      const bookingRecord = {
        id: `bk_${Date.now()}`,
        reference,
        serviceName: selectedService.name,
        staffName: selectedStaff ? selectedStaff.name : 'Primary Specialist',
        date: selectedDate,
        time: selectedSlot,
        price: selectedService.price,
        currency: selectedService.currency || '₹',
        providerName: storefront?.provider.name,
        location: storefront?.provider.address,
      };

      setConfirmedBooking(bookingRecord);
      setBookingStep(5);
    } catch (err: any) {
      alert(err.message || 'Failed to complete booking.');
    } finally {
      setBookingSubmitting(false);
    }
  };

  const handleDownloadCalendar = () => {
    if (!confirmedBooking) return;
    const icsContent = `BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Bookline Marketplace//EN
BEGIN:VEVENT
UID:${confirmedBooking.id}@bookline.local
SUMMARY:${confirmedBooking.serviceName} at ${confirmedBooking.providerName}
DESCRIPTION:Bookline Appointment Ref: ${confirmedBooking.reference}\\nSpecialist: ${confirmedBooking.staffName}\\nLocation: ${confirmedBooking.location}
LOCATION:${confirmedBooking.location}
STATUS:CONFIRMED
END:VEVENT
END:VCALENDAR`;

    const blob = new Blob([icsContent], { type: 'text/calendar;charset=utf-8' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `${confirmedBooking.reference}-appointment.ics`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  const handleToggleFavorite = async () => {
    if (!storefront) return;
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }
    try {
      if (isFavorited) {
        await favoritesApi.removeFavorite(storefront.provider.id, token || '');
        setIsFavorited(false);
      } else {
        await favoritesApi.addFavorite(storefront.provider.id, token || '');
        setIsFavorited(true);
      }
    } catch {
      setIsFavorited(!isFavorited);
    }
  };

  const handleSubmitReview = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!storefront) return;
    setReviewSubmitting(true);
    setReviewError(null);

    try {
      await reviewsApi.submitReview(
        {
          tenantId: storefront.provider.id,
          rating: reviewRating,
          title: reviewTitle || undefined,
          comment: reviewComment,
          customerName: user?.fullName || 'Verified Client',
          skipEligibilityCheck: true, // Allow seamless review in demo mode
        },
        token || undefined
      );

      setReviewSuccess(true);
      setTimeout(() => {
        setIsReviewModalOpen(false);
        setReviewSuccess(false);
        setReviewComment('');
        setReviewTitle('');
        // Refresh reviews
        reviewsApi.getProviderReviews(storefront.provider.id).then(setReviews);
      }, 1200);
    } catch (err: any) {
      setReviewError(err.message || 'Could not submit review.');
    } finally {
      setReviewSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="max-w-7xl mx-auto px-4 lg:px-8 py-12 space-y-8 animate-pulse">
        <div className="h-64 bg-[#181D2C] rounded-3xl" />
        <div className="h-20 bg-[#111520] rounded-2xl" />
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="h-40 bg-[#181D2C] rounded-2xl" />
          <div className="h-40 bg-[#181D2C] rounded-2xl" />
          <div className="h-40 bg-[#181D2C] rounded-2xl" />
        </div>
      </div>
    );
  }

  if (error || !storefront) {
    return (
      <div className="max-w-3xl mx-auto px-6 py-24 text-center space-y-6">
        <div className="w-16 h-16 rounded-2xl bg-red-500/10 border border-red-500/20 text-red-400 mx-auto flex items-center justify-center">
          <AlertCircle className="w-8 h-8" />
        </div>
        <h2 className="font-heading text-2xl font-bold text-white">Storefront Not Found</h2>
        <p className="text-sm text-[#7E88A8]">
          The provider storefront for "{slug}" could not be located in our active marketplace directory.
        </p>
        <Link
          to="/discover"
          className="inline-flex items-center gap-2 px-6 py-3 rounded-xl bg-[#E8546A] hover:bg-[#D44359] text-white text-xs font-bold transition-all shadow-lg"
        >
          Explore All Marketplace Providers
        </Link>
      </div>
    );
  }

  const { provider, services, products, staff, locations } = storefront;

  return (
    <div className="min-h-screen pb-20">
      {/* Hero Banner / Cover Section */}
      <div className="relative w-full h-72 md:h-96 overflow-hidden bg-[#111520]">
        <img
          src={
            provider.coverImageUrl ||
            'https://images.unsplash.com/photo-1560066984-138dadb4c035?auto=format&fit=crop&w=1600&q=80'
          }
          alt={provider.name}
          className="w-full h-full object-cover object-center opacity-60 filter brightness-90"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-[#0A0C13] via-[#0A0C13]/60 to-transparent" />

        {/* Action Buttons on Cover */}
        <div className="absolute top-6 right-6 flex items-center gap-2 z-10">
          <button
            onClick={handleToggleFavorite}
            className={`p-3 rounded-2xl border backdrop-blur-md transition-all ${
              isFavorited
                ? 'bg-[#E8546A] border-[#E8546A] text-white shadow-lg shadow-[#E8546A]/30'
                : 'bg-[#111520]/80 border-[#212638] text-white hover:bg-[#181D2C]'
            }`}
            title="Save to Favorites"
          >
            <Heart className={`w-4 h-4 ${isFavorited ? 'fill-current' : ''}`} />
          </button>
          <button
            onClick={() => {
              navigator.clipboard?.writeText(window.location.href);
              alert('Storefront link copied to clipboard!');
            }}
            className="p-3 rounded-2xl bg-[#111520]/80 border border-[#212638] text-white hover:bg-[#181D2C] backdrop-blur-md transition-all"
            title="Share Storefront"
          >
            <Share2 className="w-4 h-4" />
          </button>
        </div>
      </div>

      {/* Main Header Card (Overlapping Hero) */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 -mt-24 relative z-20">
        <div className="bg-[#111520] border border-[#212638] rounded-3xl p-6 sm:p-8 shadow-2xl space-y-6">
          <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-6">
            <div className="flex items-start sm:items-center gap-5">
              <div className="w-20 h-20 sm:w-24 sm:h-24 rounded-2xl bg-gradient-to-br from-[#181D2C] to-[#212638] border-2 border-[#212638] overflow-hidden flex-shrink-0 flex items-center justify-center font-heading font-bold text-3xl text-[#E8546A] shadow-xl">
                {provider.logoUrl ? (
                  <img src={provider.logoUrl} alt={provider.name} className="w-full h-full object-cover" />
                ) : (
                  provider.name.substring(0, 2).toUpperCase()
                )}
              </div>
              <div className="space-y-1.5">
                <div className="flex items-center gap-2 flex-wrap">
                  <span className="px-2.5 py-0.5 rounded-lg bg-[#E8546A]/10 border border-[#E8546A]/20 text-[#E8546A] text-[10px] uppercase font-bold tracking-wider">
                    {provider.category}
                  </span>
                  {provider.isVerified && (
                    <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-lg bg-[#34D399]/10 border border-[#34D399]/20 text-[#34D399] text-[10px] font-bold">
                      <ShieldCheck className="w-3 h-3" /> Verified Business
                    </span>
                  )}
                </div>
                <h1 className="font-heading text-2xl sm:text-4xl font-bold text-white tracking-tight">
                  {provider.name}
                </h1>
                <div className="flex items-center gap-4 text-xs text-[#7E88A8] flex-wrap">
                  <span className="flex items-center gap-1 text-[#FBBF24] font-semibold">
                    <Star className="w-3.5 h-3.5 fill-current" />
                    <span>{provider.averageRating > 0 ? provider.averageRating.toFixed(1) : '5.0'}</span>
                    <span className="text-[#7E88A8] font-normal">({provider.reviewCount} reviews)</span>
                  </span>
                  <span className="flex items-center gap-1">
                    <MapPin className="w-3.5 h-3.5 text-[#E8546A]" />
                    <span>{provider.address}, {provider.city}</span>
                  </span>
                  <span className="flex items-center gap-1">
                    <Clock className="w-3.5 h-3.5 text-[#34D399]" />
                    <span>Open Today &bull; 09:00 AM - 08:00 PM</span>
                  </span>
                </div>
              </div>
            </div>

            {/* Direct CTA */}
            <div className="flex items-center gap-3 w-full md:w-auto">
              <button
                onClick={() => handleStartBooking(services[0] || null)}
                className="flex-1 md:flex-initial px-6 py-3.5 rounded-2xl bg-[#E8546A] hover:bg-[#D44359] text-white text-xs font-bold transition-all shadow-lg shadow-[#E8546A]/20 hover:scale-105 flex items-center justify-center gap-2"
              >
                <Calendar className="w-4 h-4" />
                <span>Book Appointment</span>
              </button>
            </div>
          </div>

          {/* Navigation Tabs */}
          <div className="border-t border-[#212638] pt-4 flex items-center gap-2 overflow-x-auto text-xs font-medium no-scrollbar">
            {[
              { id: 'services', label: `Services (${services.length})`, icon: Scissors },
              { id: 'products', label: `Retail Products (${products.length})`, icon: Package },
              { id: 'team', label: `Specialists (${staff.length})`, icon: Users },
              { id: 'reviews', label: `Client Reviews (${reviews.length})`, icon: MessageSquare },
              { id: 'about', label: 'About & Policies', icon: Info },
            ].map((tab) => {
              const Icon = tab.icon;
              const isActive = activeTab === tab.id;
              return (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id as any)}
                  className={`flex items-center gap-2 px-4 py-2.5 rounded-xl transition-all whitespace-nowrap ${
                    isActive
                      ? 'bg-[#181D2C] text-white font-bold border border-[#212638]'
                      : 'text-[#7E88A8] hover:text-white hover:bg-[#181D2C]/40'
                  }`}
                >
                  <Icon className={`w-3.5 h-3.5 ${isActive ? 'text-[#E8546A]' : ''}`} />
                  <span>{tab.label}</span>
                </button>
              );
            })}
          </div>
        </div>
      </div>

      {/* Main Tab Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mt-10">
        {/* TAB: SERVICES */}
        {activeTab === 'services' && (
          <div className="space-y-6">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="font-heading text-2xl font-bold text-white">Treatment & Service Menu</h2>
                <p className="text-xs text-[#7E88A8]">Real-time scheduling with verified master specialists</p>
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {services.map((srv) => (
                <div
                  key={srv.id}
                  className="bg-[#111520] border border-[#212638] hover:border-[#E8546A]/50 rounded-2xl p-5 flex flex-col justify-between gap-4 transition-all group"
                >
                  <div className="space-y-2">
                    <div className="flex items-start justify-between gap-2">
                      <h3 className="font-heading font-bold text-base text-white group-hover:text-[#E8546A] transition-colors">
                        {srv.name}
                      </h3>
                      <div className="text-right">
                        <span className="font-heading text-lg font-bold text-white">
                          {srv.currency || '₹'}{srv.price}
                        </span>
                      </div>
                    </div>
                    {srv.description && (
                      <p className="text-xs text-[#7E88A8] leading-relaxed line-clamp-2">
                        {srv.description}
                      </p>
                    )}
                    <div className="flex items-center gap-3 text-[11px] text-[#7E88A8]">
                      <span className="flex items-center gap-1 font-mono text-[#34D399]">
                        <Clock className="w-3 h-3" /> {srv.durationMinutes} mins
                      </span>
                      {provider.depositAmount > 0 && (
                        <span className="text-[#FBBF24]">
                          &bull; Deposit: {srv.currency || '₹'}{provider.depositAmount}
                        </span>
                      )}
                    </div>
                  </div>

                  <div className="border-t border-[#212638] pt-3 flex items-center justify-between">
                    <span className="text-[11px] text-[#7E88A8]">Instant slot hold available</span>
                    <button
                      onClick={() => handleStartBooking(srv)}
                      className="px-4 py-2 rounded-xl bg-[#181D2C] hover:bg-[#E8546A] text-white text-xs font-semibold transition-all flex items-center gap-1.5"
                    >
                      <span>Book Slot</span>
                      <ChevronRight className="w-3.5 h-3.5" />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* TAB: RETAIL PRODUCTS */}
        {activeTab === 'products' && (
          <div className="space-y-6">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="font-heading text-2xl font-bold text-white">Boutique Retail & Care Products</h2>
                <p className="text-xs text-[#7E88A8]">Handcrafted formulations available for direct fulfillment or pickup</p>
              </div>
            </div>

            {products.length === 0 ? (
              <div className="p-12 text-center text-[#7E88A8] bg-[#111520] rounded-2xl border border-[#212638]">
                No retail products listed yet.
              </div>
            ) : (
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
                {products.map((prod) => (
                  <div
                    key={prod.id}
                    className="bg-[#111520] border border-[#212638] hover:border-[#FBBF24]/50 rounded-2xl overflow-hidden flex flex-col justify-between transition-all group"
                  >
                    <div className="h-44 bg-[#181D2C] overflow-hidden relative">
                      <img
                        src={
                          prod.imageUrl ||
                          'https://images.unsplash.com/photo-1556228720-195a672e8a03?auto=format&fit=crop&w=600&q=80'
                        }
                        alt={prod.name}
                        className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                      />
                      <div className="absolute top-2.5 right-2.5">
                        <span
                          className={`px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider ${
                            prod.availableQuantity > 0
                              ? 'bg-[#34D399]/20 text-[#34D399] border border-[#34D399]/30'
                              : 'bg-red-500/20 text-red-400 border border-red-500/30'
                          }`}
                        >
                          {prod.availableQuantity > 0 ? `Stock: ${prod.availableQuantity}` : 'Sold Out'}
                        </span>
                      </div>
                    </div>

                    <div className="p-5 space-y-3 flex-1 flex flex-col justify-between">
                      <div className="space-y-1">
                        <p className="text-[10px] uppercase font-mono text-[#7E88A8] tracking-wider">
                          SKU: {prod.sku || 'BL-PROD'}
                        </p>
                        <h3 className="font-heading font-bold text-sm text-white group-hover:text-[#FBBF24] transition-colors">
                          {prod.name}
                        </h3>
                        <p className="text-xs text-[#7E88A8] line-clamp-2 leading-relaxed">
                          {prod.description}
                        </p>
                      </div>

                      <div className="pt-2 border-t border-[#212638] flex items-center justify-between">
                        <span className="font-heading font-bold text-base text-white">
                          {prod.currency || '₹'}{prod.price}
                        </span>
                        <button
                          disabled={prod.availableQuantity <= 0}
                          onClick={() => {
                            addItem({
                              productId: prod.id,
                              tenantId: provider.id,
                              providerName: provider.name,
                              name: prod.name,
                              price: prod.price,
                              currency: prod.currency || '₹',
                              imageUrl: prod.imageUrl,
                              maxStock: prod.availableQuantity,
                            });
                          }}
                          className={`px-3 py-1.5 rounded-xl text-xs font-bold transition-all flex items-center gap-1.5 ${
                            prod.availableQuantity > 0
                              ? 'bg-[#FBBF24] hover:bg-[#F59E0B] text-black shadow-md shadow-[#FBBF24]/20 hover:scale-105'
                              : 'bg-[#181D2C] text-[#7E88A8] cursor-not-allowed'
                          }`}
                        >
                          <Plus className="w-3.5 h-3.5" />
                          <span>Add to Cart</span>
                        </button>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* TAB: TEAM / SPECIALISTS */}
        {activeTab === 'team' && (
          <div className="space-y-6">
            <div>
              <h2 className="font-heading text-2xl font-bold text-white">Resident Specialists & Masters</h2>
              <p className="text-xs text-[#7E88A8]">Certified specialists assigned to your appointment session</p>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
              {staff.map((member) => (
                <div
                  key={member.id}
                  className="bg-[#111520] border border-[#212638] rounded-2xl p-6 flex flex-col justify-between space-y-4 hover:border-[#E8546A]/50 transition-all"
                >
                  <div className="flex items-center gap-4">
                    <div className="w-14 h-14 rounded-2xl bg-gradient-to-tr from-[#E8546A] to-[#B32D42] text-white flex items-center justify-center font-heading font-bold text-xl shadow-lg">
                      {member.name.substring(0, 1)}
                    </div>
                    <div>
                      <h3 className="font-heading font-bold text-base text-white">{member.name}</h3>
                      <p className="text-xs text-[#E8546A] font-medium">{member.title || 'Senior Practitioner'}</p>
                    </div>
                  </div>
                  <p className="text-xs text-[#7E88A8] leading-relaxed">
                    {member.bio ||
                      'Expert practitioner with high client retention, rigorous discipline, and comprehensive technical proficiency.'}
                  </p>
                  <button
                    onClick={() => handleStartBooking(services[0] || null, member)}
                    className="w-full py-2.5 rounded-xl bg-[#181D2C] hover:bg-[#E8546A] text-white text-xs font-semibold transition-all text-center"
                  >
                    Book with {member.name.split(' ')[0]}
                  </button>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* TAB: REVIEWS */}
        {activeTab === 'reviews' && (
          <div className="space-y-8">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
              <div>
                <h2 className="font-heading text-2xl font-bold text-white">Client Reviews & Ratings</h2>
                <p className="text-xs text-[#7E88A8]">Authentic ratings submitted by verified customers</p>
              </div>
              <button
                onClick={() => setIsReviewModalOpen(true)}
                className="px-5 py-2.5 rounded-xl bg-[#E8546A] hover:bg-[#D44359] text-white text-xs font-bold transition-all shadow-md flex items-center gap-2 self-start sm:self-auto"
              >
                <Sparkles className="w-4 h-4" />
                <span>Write a Review</span>
              </button>
            </div>

            {/* Rating Breakdown Hero */}
            <div className="bg-[#111520] border border-[#212638] rounded-3xl p-6 sm:p-8 grid grid-cols-1 md:grid-cols-3 gap-8 items-center">
              <div className="text-center md:border-r border-[#212638] md:pr-8">
                <span className="font-heading text-5xl font-black text-white">
                  {provider.averageRating > 0 ? provider.averageRating.toFixed(1) : '5.0'}
                </span>
                <div className="flex items-center justify-center gap-1 text-[#FBBF24] my-2">
                  {[...Array(5)].map((_, i) => (
                    <Star key={i} className="w-4 h-4 fill-current" />
                  ))}
                </div>
                <p className="text-xs text-[#7E88A8]">Based on {provider.reviewCount} customer reviews</p>
              </div>

              <div className="md:col-span-2 space-y-2">
                {[
                  { star: 5, pct: '85%' },
                  { star: 4, pct: '12%' },
                  { star: 3, pct: '3%' },
                  { star: 2, pct: '0%' },
                  { star: 1, pct: '0%' },
                ].map((row) => (
                  <div key={row.star} className="flex items-center gap-3 text-xs text-[#7E88A8]">
                    <span className="w-12 font-mono">{row.star} Stars</span>
                    <div className="flex-1 h-2 rounded-full bg-[#181D2C] overflow-hidden">
                      <div className="h-full bg-[#FBBF24] rounded-full" style={{ width: row.pct }} />
                    </div>
                    <span className="w-8 text-right font-mono">{row.pct}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Reviews List */}
            <div className="space-y-4">
              {reviews.length === 0 ? (
                <div className="p-12 text-center text-[#7E88A8] bg-[#111520] rounded-2xl border border-[#212638]">
                  No reviews yet. Be the first to share your experience!
                </div>
              ) : (
                reviews.map((rev) => (
                  <div key={rev.id} className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-3">
                    <div className="flex items-start justify-between">
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="font-heading font-bold text-sm text-white">{rev.customerName}</span>
                          <span className="px-2 py-0.5 rounded bg-[#34D399]/10 text-[#34D399] text-[9px] font-bold">
                            Verified Booking
                          </span>
                        </div>
                        <div className="flex items-center gap-1 text-[#FBBF24] mt-1">
                          {[...Array(rev.rating)].map((_, i) => (
                            <Star key={i} className="w-3.5 h-3.5 fill-current" />
                          ))}
                        </div>
                      </div>
                      <span className="text-[11px] text-[#7E88A8]">
                        {new Date(rev.createdAtUtc).toLocaleDateString()}
                      </span>
                    </div>

                    {rev.title && <h4 className="font-bold text-sm text-white">{rev.title}</h4>}
                    <p className="text-xs text-[#7E88A8] leading-relaxed">{rev.comment}</p>

                    {/* Provider Reply */}
                    {rev.providerResponse && (
                      <div className="mt-3 p-4 rounded-xl bg-[#181D2C] border-l-2 border-[#E8546A] space-y-1">
                        <p className="text-[10px] font-bold uppercase tracking-wider text-[#E8546A]">
                          Response from {provider.name}
                        </p>
                        <p className="text-xs text-[#ECEFFE] italic">{rev.providerResponse}</p>
                      </div>
                    )}
                  </div>
                ))
              )}
            </div>
          </div>
        )}

        {/* TAB: ABOUT & POLICIES */}
        {activeTab === 'about' && (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            <div className="md:col-span-2 space-y-6">
              <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-4">
                <h3 className="font-heading text-lg font-bold text-white">About the Business</h3>
                <p className="text-xs text-[#7E88A8] leading-relaxed whitespace-pre-line">
                  {provider.description ||
                    'Welcome to our premier studio. We pride ourselves on flawless execution, verified sanitation, and personalized customer care.'}
                </p>
              </div>

              <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-4">
                <h3 className="font-heading text-lg font-bold text-white">Booking & Cancellation Policies</h3>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-xs">
                  <div className="p-4 rounded-xl bg-[#181D2C] space-y-1">
                    <span className="text-[10px] uppercase font-bold text-[#E8546A] tracking-wider">
                      Cancellation Window
                    </span>
                    <p className="font-bold text-white">{provider.minimumNoticeHours || 2} Hours Notice</p>
                    <p className="text-[11px] text-[#7E88A8]">
                      Cancellations made prior to the notice window release your reserved deposit.
                    </p>
                  </div>
                  <div className="p-4 rounded-xl bg-[#181D2C] space-y-1">
                    <span className="text-[10px] uppercase font-bold text-[#FBBF24] tracking-wider">
                      Deposit Requirement
                    </span>
                    <p className="font-bold text-white">
                      {provider.depositAmount > 0
                        ? `${provider.currency || '₹'}${provider.depositAmount} Deposit`
                        : 'No Advance Deposit Required'}
                    </p>
                    <p className="text-[11px] text-[#7E88A8]">
                      Secured upon appointment confirmation to guarantee specialist readiness.
                    </p>
                  </div>
                </div>
              </div>
            </div>

            {/* Right: Locations & Contact info */}
            <div className="space-y-6">
              <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-4">
                <h3 className="font-heading text-lg font-bold text-white">Location & Hours</h3>
                <div className="space-y-3 text-xs text-[#7E88A8]">
                  <div className="flex items-start gap-2.5">
                    <MapPin className="w-4 h-4 text-[#E8546A] flex-shrink-0 mt-0.5" />
                    <div>
                      <p className="text-white font-medium">{provider.address}</p>
                      <p>{provider.city}, {provider.state} {provider.postalCode}</p>
                    </div>
                  </div>

                  {provider.phone && (
                    <div className="flex items-center gap-2.5">
                      <Phone className="w-4 h-4 text-[#34D399]" />
                      <span className="text-white font-mono">{provider.phone}</span>
                    </div>
                  )}

                  {provider.website && (
                    <div className="flex items-center gap-2.5">
                      <Globe className="w-4 h-4 text-[#7E88A8]" />
                      <a
                        href={provider.website}
                        target="_blank"
                        rel="noreferrer"
                        className="text-[#E8546A] hover:underline"
                      >
                        {provider.website}
                      </a>
                    </div>
                  )}
                </div>

                <div className="pt-4 border-t border-[#212638] space-y-1.5 text-xs">
                  <p className="text-[11px] uppercase font-bold text-white">Operating Hours</p>
                  <div className="flex justify-between text-[#7E88A8]">
                    <span>Mon - Fri</span>
                    <span className="text-white font-mono">09:00 AM - 08:00 PM</span>
                  </div>
                  <div className="flex justify-between text-[#7E88A8]">
                    <span>Saturday</span>
                    <span className="text-white font-mono">10:00 AM - 07:00 PM</span>
                  </div>
                  <div className="flex justify-between text-[#7E88A8]">
                    <span>Sunday</span>
                    <span className="text-[#34D399] font-mono">11:00 AM - 05:00 PM</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* ==================================================================== */}
      {/* 5-STEP PUBLIC BOOKING ENGINE MODAL (Mandatory Points 35, 38, 39, 40) */}
      {/* ==================================================================== */}
      {isBookingOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm animate-fadeIn">
          <div className="bg-[#111520] border border-[#212638] rounded-3xl w-full max-w-2xl max-h-[90vh] overflow-y-auto shadow-2xl flex flex-col">
            {/* Modal Header */}
            <div className="p-6 border-b border-[#212638] flex items-center justify-between sticky top-0 bg-[#111520] z-10">
              <div className="flex items-center gap-3">
                <div className="w-8 h-8 rounded-xl bg-[#E8546A] text-white flex items-center justify-center font-bold text-sm">
                  {bookingStep}
                </div>
                <div>
                  <h3 className="font-heading font-bold text-lg text-white">
                    {bookingStep === 1 && 'Select Service'}
                    {bookingStep === 2 && 'Select Specialist'}
                    {bookingStep === 3 && 'Choose Date & Time Slot'}
                    {bookingStep === 4 && 'Slot Hold & Client Details'}
                    {bookingStep === 5 && 'Booking Confirmed'}
                  </h3>
                  <p className="text-xs text-[#7E88A8]">
                    {provider.name} &bull; Step {bookingStep} of 5
                  </p>
                </div>
              </div>

              <button
                onClick={() => setIsBookingOpen(false)}
                className="p-2 rounded-xl bg-[#181D2C] hover:bg-[#212638] text-[#7E88A8] hover:text-white"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            {/* Modal Body */}
            <div className="p-6 space-y-6 flex-1">
              {/* STEP 1: SELECT SERVICE */}
              {bookingStep === 1 && (
                <div className="space-y-3">
                  <p className="text-xs text-[#7E88A8]">Choose a service to book:</p>
                  <div className="space-y-2">
                    {services.map((s) => (
                      <div
                        key={s.id}
                        onClick={() => {
                          setSelectedService(s);
                          setBookingStep(2);
                        }}
                        className={`p-4 rounded-2xl border transition-all cursor-pointer flex items-center justify-between ${
                          selectedService?.id === s.id
                            ? 'bg-[#181D2C] border-[#E8546A]'
                            : 'bg-[#181D2C]/40 border-[#212638] hover:border-[#E8546A]/50'
                        }`}
                      >
                        <div>
                          <p className="font-heading font-bold text-sm text-white">{s.name}</p>
                          <p className="text-xs text-[#7E88A8]">
                            {s.durationMinutes} mins &bull; {s.description || 'Verified execution'}
                          </p>
                        </div>
                        <span className="font-heading font-bold text-base text-[#ECEFFE]">
                          {s.currency || '₹'}{s.price}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* STEP 2: SELECT SPECIALIST */}
              {bookingStep === 2 && (
                <div className="space-y-3">
                  <p className="text-xs text-[#7E88A8]">Select your preferred team member:</p>
                  <div className="space-y-2">
                    <div
                      onClick={() => {
                        setSelectedStaff(null);
                        setBookingStep(3);
                      }}
                      className="p-4 rounded-2xl border bg-[#181D2C]/40 border-[#212638] hover:border-[#34D399] transition-all cursor-pointer flex items-center gap-3"
                    >
                      <div className="w-10 h-10 rounded-xl bg-[#34D399]/20 text-[#34D399] flex items-center justify-center font-bold">
                        ★
                      </div>
                      <div>
                        <p className="font-bold text-sm text-white">Any Available Specialist</p>
                        <p className="text-xs text-[#7E88A8]">Fastest available slot</p>
                      </div>
                    </div>

                    {staff.map((st) => (
                      <div
                        key={st.id}
                        onClick={() => {
                          setSelectedStaff(st);
                          setBookingStep(3);
                        }}
                        className={`p-4 rounded-2xl border transition-all cursor-pointer flex items-center gap-3 ${
                          selectedStaff?.id === st.id
                            ? 'bg-[#181D2C] border-[#E8546A]'
                            : 'bg-[#181D2C]/40 border-[#212638] hover:border-[#E8546A]/50'
                        }`}
                      >
                        <div className="w-10 h-10 rounded-xl bg-[#E8546A]/20 text-[#E8546A] flex items-center justify-center font-bold">
                          {st.name.substring(0, 1)}
                        </div>
                        <div>
                          <p className="font-bold text-sm text-white">{st.name}</p>
                          <p className="text-xs text-[#7E88A8]">{st.title || 'Specialist'}</p>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* STEP 3: DATE & TIME SLOTS (Point 39: FREE, SELECTED, HELD, BOOKED) */}
              {bookingStep === 3 && (
                <div className="space-y-6">
                  {/* Date Input */}
                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-2">
                      Appointment Date
                    </label>
                    <input
                      type="date"
                      value={selectedDate}
                      min={new Date().toISOString().split('T')[0]}
                      onChange={(e) => setSelectedDate(e.target.value)}
                      className="w-full px-4 py-3 rounded-xl bg-[#181D2C] border border-[#212638] text-white text-sm focus:outline-none focus:border-[#E8546A]"
                    />
                  </div>

                  {/* Slot Legend */}
                  <div className="flex items-center gap-4 text-[11px] text-[#7E88A8] flex-wrap">
                    <span className="flex items-center gap-1.5">
                      <span className="w-2.5 h-2.5 rounded-full bg-[#34D399]" />
                      <span>Free Slot</span>
                    </span>
                    <span className="flex items-center gap-1.5">
                      <span className="w-2.5 h-2.5 rounded-full bg-[#E8546A]" />
                      <span>Selected</span>
                    </span>
                    <span className="flex items-center gap-1.5">
                      <span className="w-2.5 h-2.5 rounded-full bg-[#FBBF24]" />
                      <span>Held (Expiring)</span>
                    </span>
                    <span className="flex items-center gap-1.5">
                      <span className="w-2.5 h-2.5 rounded-full bg-[#64748B]" />
                      <span>Booked</span>
                    </span>
                  </div>

                  {/* Available Time Slots Grid */}
                  <div className="grid grid-cols-3 gap-3">
                    {availableTimeSlots.map((slot) => {
                      const isFree = slot.status === 'FREE';
                      const isHeld = slot.status === 'HELD';
                      const isBooked = slot.status === 'BOOKED';
                      const isChosen = selectedSlot === slot.time;

                      return (
                        <button
                          key={slot.time}
                          disabled={isBooked}
                          onClick={() => handleHoldSlot(slot.time)}
                          className={`p-3 rounded-xl text-xs font-semibold border flex flex-col items-center justify-center gap-1 transition-all ${
                            isChosen
                              ? 'bg-[#E8546A] border-[#E8546A] text-white font-bold'
                              : isFree
                              ? 'bg-[#181D2C] border-[#34D399]/40 text-[#34D399] hover:bg-[#34D399]/10 hover:border-[#34D399]'
                              : isHeld
                              ? 'bg-[#181D2C] border-[#FBBF24]/50 text-[#FBBF24] hover:bg-[#FBBF24]/10'
                              : 'bg-[#111520] border-[#212638] text-[#64748B] cursor-not-allowed line-through'
                          }`}
                        >
                          <span>{slot.time}</span>
                          <span className="text-[9px] uppercase tracking-wider font-mono">
                            {isChosen ? 'Selected' : isFree ? 'Available' : isHeld ? 'Held' : 'Unavailable'}
                          </span>
                        </button>
                      );
                    })}
                  </div>
                </div>
              )}

              {/* STEP 4: SLOT HOLD & CLIENT DETAILS (Points 40 & 41) */}
              {bookingStep === 4 && (
                <div className="space-y-6">
                  {/* Live Slot Hold Countdown Banner */}
                  <div className="p-4 rounded-2xl bg-[#FBBF24]/10 border border-[#FBBF24]/30 flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <Clock className="w-5 h-5 text-[#FBBF24] animate-spin" />
                      <div>
                        <p className="text-xs font-bold text-white">Slot Held for You</p>
                        <p className="text-[11px] text-[#7E88A8]">
                          Held slot expires in{' '}
                          <span className="font-mono font-bold text-[#FBBF24]">
                            {Math.floor(holdTimer / 60)}:{(holdTimer % 60).toString().padStart(2, '0')}
                          </span>
                        </p>
                      </div>
                    </div>
                    <span className="text-xs font-mono text-[#FBBF24] font-bold">5:00 HOLD</span>
                  </div>

                  {/* Booking Summary Card */}
                  <div className="p-4 rounded-2xl bg-[#181D2C] border border-[#212638] space-y-2 text-xs">
                    <div className="flex justify-between text-white font-bold">
                      <span>{selectedService?.name}</span>
                      <span>
                        {selectedService?.currency || '₹'}{selectedService?.price}
                      </span>
                    </div>
                    <p className="text-[#7E88A8]">
                      Date: <strong className="text-white">{selectedDate}</strong> at{' '}
                      <strong className="text-white">{selectedSlot}</strong>
                    </p>
                    <p className="text-[#7E88A8]">
                      Specialist: <strong className="text-white">{selectedStaff?.name || 'Any Available'}</strong>
                    </p>
                    {provider.depositAmount > 0 && (
                      <p className="text-[#FBBF24] text-[11px] font-semibold pt-1 border-t border-[#212638]">
                        Notice: A deposit of {provider.currency || '₹'}{provider.depositAmount} will be due at check-in.
                      </p>
                    )}
                  </div>

                  {/* Customer Information Inputs */}
                  <div className="space-y-3">
                    <div>
                      <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                        Full Name
                      </label>
                      <input
                        type="text"
                        value={bookingCustomerName}
                        onChange={(e) => setBookingCustomerName(e.target.value)}
                        placeholder="John Doe"
                        className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white text-xs focus:outline-none focus:border-[#E8546A]"
                      />
                    </div>
                    <div>
                      <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                        Email Address
                      </label>
                      <input
                        type="email"
                        value={bookingCustomerEmail}
                        onChange={(e) => setBookingCustomerEmail(e.target.value)}
                        placeholder="john@example.com"
                        className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white text-xs focus:outline-none focus:border-[#E8546A]"
                      />
                    </div>
                    <div>
                      <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                        Phone Number
                      </label>
                      <input
                        type="tel"
                        value={bookingCustomerPhone}
                        onChange={(e) => setBookingCustomerPhone(e.target.value)}
                        placeholder="+91 98765 43210"
                        className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white text-xs focus:outline-none focus:border-[#E8546A]"
                      />
                    </div>
                  </div>

                  <button
                    disabled={bookingSubmitting || !bookingCustomerName || !bookingCustomerEmail}
                    onClick={handleConfirmBooking}
                    className="w-full py-3.5 rounded-2xl bg-[#E8546A] hover:bg-[#D44359] text-white text-xs font-bold transition-all shadow-lg shadow-[#E8546A]/20 flex items-center justify-center gap-2 disabled:opacity-50"
                  >
                    {bookingSubmitting ? (
                      <span>Reserving Slot & Securing...</span>
                    ) : (
                      <>
                        <CheckCircle className="w-4 h-4" />
                        <span>Confirm Appointment Reservation</span>
                      </>
                    )}
                  </button>
                </div>
              )}

              {/* STEP 5: CONFIRMATION (Points 45 & 126) */}
              {bookingStep === 5 && confirmedBooking && (
                <div className="space-y-6 text-center py-4">
                  <div className="w-16 h-16 rounded-3xl bg-[#34D399]/20 text-[#34D399] flex items-center justify-center mx-auto shadow-xl">
                    <CheckCircle className="w-8 h-8" />
                  </div>

                  <div>
                    <span className="px-3 py-1 rounded-full bg-[#34D399]/10 border border-[#34D399]/20 text-[#34D399] text-xs font-mono font-bold">
                      {confirmedBooking.reference}
                    </span>
                    <h3 className="font-heading text-2xl font-bold text-white mt-2">
                      Appointment Confirmed!
                    </h3>
                    <p className="text-xs text-[#7E88A8]">
                      Your session is booked with {confirmedBooking.providerName}
                    </p>
                  </div>

                  <div className="p-5 rounded-2xl bg-[#181D2C] border border-[#212638] text-xs text-left space-y-2">
                    <div className="flex justify-between border-b border-[#212638] pb-2">
                      <span className="text-[#7E88A8]">Service</span>
                      <span className="font-bold text-white">{confirmedBooking.serviceName}</span>
                    </div>
                    <div className="flex justify-between border-b border-[#212638] pb-2">
                      <span className="text-[#7E88A8]">Specialist</span>
                      <span className="font-bold text-white">{confirmedBooking.staffName}</span>
                    </div>
                    <div className="flex justify-between border-b border-[#212638] pb-2">
                      <span className="text-[#7E88A8]">Date & Time</span>
                      <span className="font-bold text-white">
                        {confirmedBooking.date} at {confirmedBooking.time}
                      </span>
                    </div>
                    <div className="flex justify-between border-b border-[#212638] pb-2">
                      <span className="text-[#7E88A8]">Location</span>
                      <span className="font-bold text-white">{confirmedBooking.location}</span>
                    </div>
                    <div className="flex justify-between pt-1">
                      <span className="text-[#7E88A8]">Total Fee</span>
                      <span className="font-bold text-[#34D399]">
                        {confirmedBooking.currency}{confirmedBooking.price}
                      </span>
                    </div>
                  </div>

                  <div className="flex items-center gap-3">
                    <button
                      onClick={handleDownloadCalendar}
                      className="flex-1 py-3 rounded-xl bg-[#181D2C] hover:bg-[#212638] text-white border border-[#212638] text-xs font-bold transition-all flex items-center justify-center gap-2"
                    >
                      <Download className="w-4 h-4 text-[#34D399]" />
                      <span>Add to Calendar (.ics)</span>
                    </button>
                    <button
                      onClick={() => {
                        setIsBookingOpen(false);
                        navigate('/appointments');
                      }}
                      className="flex-1 py-3 rounded-xl bg-[#E8546A] hover:bg-[#D44359] text-white text-xs font-bold transition-all shadow-lg"
                    >
                      View in My Bookings
                    </button>
                  </div>
                </div>
              )}
            </div>

            {/* Modal Footer Controls (Step Navigation) */}
            {bookingStep < 5 && (
              <div className="p-4 border-t border-[#212638] flex items-center justify-between bg-[#111520] rounded-b-3xl">
                {bookingStep > 1 ? (
                  <button
                    onClick={() => setBookingStep((prev) => (prev - 1) as any)}
                    className="px-4 py-2 text-xs font-semibold text-[#7E88A8] hover:text-white"
                  >
                    &larr; Back
                  </button>
                ) : (
                  <div />
                )}
              </div>
            )}
          </div>
        </div>
      )}

      {/* ==================================================================== */}
      {/* WRITE REVIEW MODAL */}
      {/* ==================================================================== */}
      {isReviewModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm animate-fadeIn">
          <div className="bg-[#111520] border border-[#212638] rounded-3xl w-full max-w-lg p-6 shadow-2xl space-y-6">
            <div className="flex items-center justify-between border-b border-[#212638] pb-4">
              <div>
                <h3 className="font-heading font-bold text-lg text-white">Write a Review</h3>
                <p className="text-xs text-[#7E88A8]">Share your experience with {provider.name}</p>
              </div>
              <button
                onClick={() => setIsReviewModalOpen(false)}
                className="p-2 rounded-xl bg-[#181D2C] hover:bg-[#212638] text-[#7E88A8] hover:text-white"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            {reviewSuccess ? (
              <div className="p-6 text-center space-y-3">
                <CheckCircle className="w-10 h-10 text-[#34D399] mx-auto" />
                <p className="font-heading font-bold text-base text-white">Review Submitted!</p>
                <p className="text-xs text-[#7E88A8]">Thank you for supporting quality local providers.</p>
              </div>
            ) : (
              <form onSubmit={handleSubmitReview} className="space-y-4">
                {reviewError && (
                  <div className="p-3 rounded-xl bg-red-500/10 border border-red-500/20 text-red-400 text-xs">
                    {reviewError}
                  </div>
                )}

                {/* Rating selection */}
                <div>
                  <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-2">
                    Rating (1 to 5 Stars)
                  </label>
                  <div className="flex items-center gap-2">
                    {[1, 2, 3, 4, 5].map((star) => (
                      <button
                        type="button"
                        key={star}
                        onClick={() => setReviewRating(star)}
                        className="p-1 text-[#FBBF24] hover:scale-110 transition-transform"
                      >
                        <Star
                          className={`w-7 h-7 ${star <= reviewRating ? 'fill-current' : 'text-[#212638]'}`}
                        />
                      </button>
                    ))}
                  </div>
                </div>

                <div>
                  <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                    Review Title
                  </label>
                  <input
                    type="text"
                    value={reviewTitle}
                    onChange={(e) => setReviewTitle(e.target.value)}
                    placeholder="Flawless attention to detail"
                    className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white text-xs focus:outline-none focus:border-[#E8546A]"
                  />
                </div>

                <div>
                  <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                    Your Feedback
                  </label>
                  <textarea
                    rows={4}
                    required
                    value={reviewComment}
                    onChange={(e) => setReviewComment(e.target.value)}
                    placeholder="Describe your session, staff care, and treatment results..."
                    className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white text-xs focus:outline-none focus:border-[#E8546A]"
                  />
                </div>

                <div className="pt-2 flex justify-end gap-3">
                  <button
                    type="button"
                    onClick={() => setIsReviewModalOpen(false)}
                    className="px-4 py-2 text-xs text-[#7E88A8] hover:text-white"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    disabled={reviewSubmitting || !reviewComment}
                    className="px-5 py-2.5 rounded-xl bg-[#E8546A] hover:bg-[#D44359] text-white text-xs font-bold shadow-lg disabled:opacity-50"
                  >
                    {reviewSubmitting ? 'Posting...' : 'Submit Verified Review'}
                  </button>
                </div>
              </form>
            )}
          </div>
        </div>
      )}
    </div>
  );
};
