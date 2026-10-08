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
    isVerified: boolean;
    depositType: string;
    depositAmount: number;
    currency: string;
    holdDurationMinutes: number;
    minimumNoticeHours: number;
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
    minRating?: number;
    maxPrice?: number;
    availability?: string;
    sort?: string;
    page?: number;
    pageSize?: number;
  }): Promise<ProviderSearchResponse> {
    const searchParams = new URLSearchParams();
    if (params.q) searchParams.set('q', params.q);
    if (params.category && params.category !== 'All') searchParams.set('category', params.category);
    if (params.service && params.service !== 'All') searchParams.set('service', params.service);
    if (params.city && params.city !== 'All') searchParams.set('city', params.city);
    if (params.lat != null) searchParams.set('lat', params.lat.toString());
    if (params.lng != null) searchParams.set('lng', params.lng.toString());
    if (params.radius != null) searchParams.set('radius', params.radius.toString());
    if (params.minRating != null) searchParams.set('minRating', params.minRating.toString());
    if (params.maxPrice != null) searchParams.set('maxPrice', params.maxPrice.toString());
    if (params.availability && params.availability !== 'all') searchParams.set('availability', params.availability);
    if (params.sort) searchParams.set('sort', params.sort);
    if (params.page) searchParams.set('page', params.page.toString());
    if (params.pageSize) searchParams.set('pageSize', params.pageSize.toString());

    const res = await fetch(`${API_BASE}/providers?${searchParams.toString()}`);
    if (!res.ok) throw new Error('Failed to load discovery results');
    return res.json();
  },

  async getProviderStorefront(slug: string): Promise<ProviderStorefrontData> {
    const res = await fetch(`${API_BASE}/providers/${slug}`);
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
};
