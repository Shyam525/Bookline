export interface FavoriteItem {
  id: string;
  tenantId: string;
  createdAtUtc: string;
  provider?: {
    id: string;
    name: string;
    slug: string;
    category: string;
    businessType: string;
    city: string;
    address: string;
    averageRating: number;
    reviewCount: number;
    logoUrl?: string;
    coverImageUrl?: string;
  };
}

const API_BASE = '/api/v1/favorites';

export const favoritesApi = {
  async getFavorites(token: string): Promise<FavoriteItem[]> {
    const res = await fetch(API_BASE, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) return [];
    return res.json();
  },

  async addFavorite(tenantId: string, token: string): Promise<void> {
    await fetch(`${API_BASE}/${tenantId}`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
    });
  },

  async removeFavorite(tenantId: string, token: string): Promise<void> {
    await fetch(`${API_BASE}/${tenantId}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` },
    });
  },

  async checkFavorite(tenantId: string, token: string): Promise<boolean> {
    const res = await fetch(`${API_BASE}/check/${tenantId}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) return false;
    const data = await res.json();
    return !!data.isFavorite;
  },
};
