export interface ProviderCard {
  id: string;
  name: string;
  slug: string;
  category: string;
  businessType: string;
  rating: number;
  reviewCount: number;
  distanceKm?: number;
  city: string;
  address: string;
  latitude: number;
  longitude: number;
  startingPrice: number;
  currency: string;
  nextAvailableSlot?: string;
  isVerified: boolean;
  logoUrl?: string;
  coverImageUrl?: string;
  servicesSummary: string[];
  rankingScore: number;
}

export interface PublicServiceSearchResult {
  id: string;
  name: string;
  description?: string;
  durationMinutes: number;
  price: number;
  currency: string;
  providerName: string;
  providerSlug: string;
  providerCity: string;
  providerCategory: string;
  isOnlineBookingEnabled: boolean;
}

export interface ProviderSearchResponse {
  items: ProviderCard[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ProviderStorefrontData {
  provider: {
    id: string;
    name: string;
    slug: string;
    category: string;
    businessType: string;
    description: string;
    address: string;
    city: string;
    state: string;
    postalCode: string;
    latitude: number;
    longitude: number;
    phone?: string;
    website?: string;
    averageRating: number;
    reviewCount: number;
    logoUrl?: string;
    coverImageUrl?: string;
    galleryUrls?: string[];
    isVerified: boolean;
    depositType: string;
    depositAmount: number;
    currency: string;
    holdDurationMinutes: number;
    minimumNoticeHours: number;
    distanceKm?: number;
  };
  locations: Array<{
    id: string;
    name: string;
    address: string;
    city: string;
    latitude: number;
    longitude: number;
    phone: string;
  }>;
  categories: Array<{
    id: string;
    name: string;
    description?: string;
  }>;
  services: Array<{
    id: string;
    categoryId: string;
    name: string;
    description?: string;
    durationMinutes: number;
    price: number;
    currency: string;
    colorHex?: string;
    imageUrl?: string;
  }>;
  products: Array<{
    id: string;
    name: string;
    description: string;
    price: number;
    currency: string;
    sku?: string;
    imageUrl?: string;
    stockQuantity: number;
    availableQuantity: number;
  }>;
  staff: Array<{
    id: string;
    name: string;
    title?: string;
    bio?: string;
    avatarUrl?: string;
  }>;
  reviews: Array<{
    id: string;
    customerName: string;
    rating: number;
    title?: string;
    comment: string;
    providerResponse?: string;
    createdAtUtc: string;
  }>;
}

export interface CategoryCard {
  name: string;
  icon: string;
  description: string;
  count: number;
}

export interface CityCard {
  name: string;
  state: string;
  lat: number;
  lng: number;
  providerCount: number;
}

const API_BASE = '/api/v1/discovery';

export const discoveryApi = {
  async searchProviders(params: {
    q?: string;
    category?: string;
    service?: string;
    city?: string;
    lat?: number;
    lng?: number;
    radius?: number;
    radiusMeters?: number;
    bounds?: string;
    swLat?: number;
    swLng?: number;
    neLat?: number;
    neLng?: number;
    minRating?: number;
    maxPrice?: number;
    availability?: string;
    sort?: string;
    page?: number;
    pageSize?: number;
    signal?: AbortSignal;
  }, signal?: AbortSignal): Promise<ProviderSearchResponse> {
    const effectiveSignal = signal || params.signal;
    const searchParams = new URLSearchParams();
    if (params.q) searchParams.set('q', params.q);
    if (params.category && params.category !== 'All') searchParams.set('category', params.category);
    if (params.service && params.service !== 'All') searchParams.set('service', params.service);
    if (params.city && params.city !== 'All') searchParams.set('city', params.city);
    if (params.lat != null) searchParams.set('lat', params.lat.toString());
    if (params.lng != null) searchParams.set('lng', params.lng.toString());
    if (params.radius != null) searchParams.set('radius', params.radius.toString());
    if (params.radiusMeters != null) searchParams.set('radiusMeters', params.radiusMeters.toString());
    if (params.bounds) searchParams.set('bounds', params.bounds);
    if (params.swLat != null) searchParams.set('swLat', params.swLat.toString());
    if (params.swLng != null) searchParams.set('swLng', params.swLng.toString());
    if (params.neLat != null) searchParams.set('neLat', params.neLat.toString());
    if (params.neLng != null) searchParams.set('neLng', params.neLng.toString());
    if (params.minRating != null) searchParams.set('minRating', params.minRating.toString());
    if (params.maxPrice != null) searchParams.set('maxPrice', params.maxPrice.toString());
    if (params.availability && params.availability !== 'all') searchParams.set('availability', params.availability);
    if (params.sort) searchParams.set('sort', params.sort);
    if (params.page) searchParams.set('page', params.page.toString());
    if (params.pageSize) searchParams.set('pageSize', params.pageSize.toString());

    const res = await fetch(`${API_BASE}/providers?${searchParams.toString()}`, { signal: effectiveSignal });
    if (!res.ok) throw new Error('Failed to load discovery results');
    return res.json();
  },

  async searchServices(query: string, limit = 8, signal?: AbortSignal): Promise<PublicServiceSearchResult[]> {
    const params = new URLSearchParams();
    if (query) params.set('q', query);
    params.set('limit', limit.toString());

    const res = await fetch(`${API_BASE}/services?${params.toString()}`, { signal });
    if (!res.ok) return [];
    return res.json();
  },

  async getProviderStorefront(slug: string): Promise<ProviderStorefrontData> {
    // Try public businesses endpoint first (Phase 15), fallback to discovery providers endpoint
    let res = await fetch(`/api/public/businesses/${slug}`);
    if (!res.ok) {
      res = await fetch(`${API_BASE}/providers/${slug}`);
    }
    if (!res.ok) throw new Error('Provider storefront not found');
    return res.json();
  },

  async getCategories(): Promise<CategoryCard[]> {
    const res = await fetch(`${API_BASE}/categories`);
    if (!res.ok) return [];
    return res.json();
  },

  async getCities(): Promise<CityCard[]> {
    const res = await fetch(`${API_BASE}/cities`);
    if (!res.ok) return [];
    return res.json();
  },

  async reverseGeocode(latitude: number, longitude: number): Promise<string> {
    const res = await fetch(`${API_BASE}/reverse-geocode`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ latitude, longitude }),
    });
    if (!res.ok) return 'Ahmedabad';
    const data = await res.json();
    return data.city || 'Ahmedabad';
  },

  async getAvailability(params: {
    staffId: string;
    serviceId: string;
    date: string;
    timezone?: string;
    slug?: string;
  }): Promise<Array<{ start: string; end: string }>> {
    const searchParams = new URLSearchParams({
      staffId: params.staffId,
      serviceId: params.serviceId,
      date: params.date,
      timezone: params.timezone || 'Asia/Kolkata',
    });
    if (params.slug) searchParams.set('slug', params.slug);

    const res = await fetch(`/api/v1/public/availability?${searchParams.toString()}`);
    if (!res.ok) {
      const err = await res.json().catch(() => null);
      throw new Error(err?.message || 'Could not load availability for the selected date.');
    }
    return res.json();
  },

  async holdSlot(payload: {
    staffId: string;
    serviceId: string;
    startUtc: string;
  }): Promise<{ holdId: string; expiresAtUtc: string; slot?: string; status: string }> {
    const res = await fetch('/api/v1/public/hold', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => null);
      throw new Error(err?.message || 'Selected time slot is no longer available.');
    }
    return res.json();
  },

  async createBooking(payload: {
    staffId: string;
    serviceId: string;
    customerId?: string;
    startUtc: string;
    holdId: string;
    customerName?: string;
    customerEmail?: string;
    customerPhone?: string;
    customerNotes?: string;
  }): Promise<{
    id: string;
    tenantId: string;
    staffId: string;
    serviceId: string;
    customerId: string;
    startUtc: string;
    endUtc: string;
    status: string;
    createdAtUtc: string;
    bookingReference?: string;
  }> {
    const res = await fetch('/api/v1/public/book', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Idempotency-Key': `book_${Date.now()}_${Math.random().toString(36).substring(2, 9)}`,
      },
      body: JSON.stringify(payload),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => null);
      throw new Error(err?.message || 'Booking confirmation failed. Your hold may have expired.');
    }
    return res.json();
  },
};
